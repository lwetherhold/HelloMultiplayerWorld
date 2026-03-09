using Unity.Netcode;
using UnityEngine;

public class RaceGameManager : NetworkBehaviour
{

    // declare enum for race state
    public enum RaceState
    {
        WaitingForPicks = 0,
        Racing = 1,
        Finished = 2
    }

    // prefab reference for spawning a server-owned round token
   [SerializeField] private NetworkObject roundTokenPrefab;
   // store the currently spawned round token
   private NetworkObject spawnedRoundToken;

   // declare network variables to track the race state, winner, and player picks
   public NetworkVariable<int> raceState = new NetworkVariable<int>((int)RaceState.WaitingForPicks);
   public NetworkVariable<int> winnerHorse = new NetworkVariable<int>(-1);
   public NetworkVariable<int> player1Pick = new NetworkVariable<int>(-1);
   public NetworkVariable<int> player2Pick = new NetworkVariable<int>(-1);
   // score tracking for each player
   public NetworkVariable<int> player1Score = new NetworkVariable<int>(0);
   public NetworkVariable<int> player2Score = new NetworkVariable<int>(0);

   // add a Server RPC so players can submit horse picks
   [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] // InvokePermission is to allow clients to call the RPC (in case of issues with ownership)
   // NOTE: I actually didn't need the InvokePermission stuff as a safeguard
   //       since the issue I was debugging was related to "Allow Remote Connections?" being unchecked in the NetworkManager settings
   //RequireOwnership = false is DEPRECATED
   public void SubmitPickServerRpc(int horseIndex, RpcParams rpcParams = default)
   {
        // check if the race is in the waiting for picks state (if it's not, don't allow picks)
        if (raceState.Value != (int)RaceState.WaitingForPicks) return;
        // check if the horse index is valid (0..7)
        if (horseIndex < 0 || horseIndex > 7) return;

        // get the sender client id from the RPC parameters
        ulong senderId = rpcParams.Receive.SenderClientId;
        // get the host id from the server client id (this is the id of the server client)
        ulong hostId = NetworkManager.ServerClientId; // below causes an error
        //ulong hostId = NetworkManager.Singleton.ServerClientId; // safer host id getter than below
        //ulong hostId = NetworkManager.Singleton.ConnectedClientsIds[0];

        // check if the sender is the host to make picks (host is the server client)
        if (senderId == hostId)
        {
            // host is picking for player 1

            // make picks only once per player per round (locked after first pick)
            if (player1Pick.Value != -1) return; // if player 1 has already picked, don't allow another pick
            player1Pick.Value = horseIndex; // set the player 1 pick to the horse index
        }
        else
        {
            // client is picking for player 2

            // make picks only once per player per round (locked after first pick)
            if (player2Pick.Value != -1) return; // if player 2 has already picked, don't allow another pick
            player2Pick.Value = horseIndex; // set the player 2 pick to the horse index
        }

        // make server auto-finish a simple race once both picks are in
        TryRunRaceIfReady();
   }
   
   // add a Server RPC so players can reset the race
   [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)] // InvokePermission is to allow clients to call the RPC (in case of issues with ownership)
   // NOTE: I actually didn't need the InvokePermission stuff as a safeguard
   //       since the issue I was debugging was related to "Allow Remote Connections?" being unchecked in the NetworkManager settings
   //RequireOwnership = false is DEPRECATED
   public void ResetRaceServerRpc()
   {
        // check if the server is the one calling the RPC
        // only the server should run this reset logic and change shared round state
        // (host is like server + client in one)
        // NOTE: due to InvokePermission = Everyone, a client can request next race and the server executes it
        if (!IsServer) return;

        // despawn the old round token before starting next race
        if (spawnedRoundToken != null && spawnedRoundToken.IsSpawned) // if the old round token exists and is spawned
            spawnedRoundToken.Despawn(true); // despawn the old round token

        // reset the player picks and winner horse
        player1Pick.Value = -1; // reset player 1 pick
        player2Pick.Value = -1; // reset player 2 pick
        winnerHorse.Value = -1; // reset winner horse

        // reset race state to waiting for picks
        raceState.Value = (int)RaceState.WaitingForPicks;
   }

    // helper method to check if the race is ready to run
   private void TryRunRaceIfReady()
   {
        // check if the server is the one calling the RPC
        if (!IsServer) return;

        // check if both players have picked a horse (if not, don't run the race)
        if (player1Pick.Value == -1 || player2Pick.Value == -1) return;

        // set the race state to racing
        raceState.Value = (int)RaceState.Racing;

        // get a random winner horse index (0..7)
        int winner = Random.Range(0, 8);
        // set the winner before finishing the race
        winnerHorse.Value = winner; // set the winner horse index

        // spawn a server-owned round token to show winner result in world
        if (roundTokenPrefab != null)
        {
            // despawn previous token if one already exists
            if (spawnedRoundToken != null && spawnedRoundToken.IsSpawned) // if the old round token exists and is spawned
                spawnedRoundToken.Despawn(true); // despawn the old round token

            // spawn the new round token at the winner horse position
            Vector3 tokenPos = new Vector3(winner - 3.5f, 2f, 0f); // 3.5f is the offset from the winner horse position
            spawnedRoundToken = Instantiate(roundTokenPrefab, tokenPos, Quaternion.identity); // instantiate the new round token
            spawnedRoundToken.Spawn(); // spawn the new round token
        }

        // update scores
        if (player1Pick.Value == winner) player1Score.Value++;
        if (player2Pick.Value == winner) player2Score.Value++;

        // set the race state to finished
        raceState.Value = (int)RaceState.Finished;
   }

    // helper method for getting round summary for UI text display
    public string GetRoundSummary()
    {
        // get the player picks and winner horse index (0..7) -> convert to 1..8 for UI display
        // if player has not picked a horse, display a "-"
        // if player has picked a horse, display the horse number (1..8)
        // add 1 to the horse index to match 1..8 horse numbering convention in UI
        string p1 = player1Pick.Value == -1 ? "-" : (player1Pick.Value + 1).ToString(); 
        string p2 = player2Pick.Value == -1 ? "-" : (player2Pick.Value + 1).ToString();
        string w = winnerHorse.Value == -1 ? "-" : (winnerHorse.Value + 1).ToString();
        return $"P1 Pick: {p1} | P2 Pick: {p2} | Winner: {w}"; // return the round summary string

        // NOTE: replaced since to match 1..8 horse numbering convention in UI
        //return $"P1 Pick: {player1Pick.Value} | P2 Pick: {player2Pick.Value} | Winner: {winnerHorse.Value}";
    }

    // helper method for getting score summary for UI text display
    public string GetScoreSummary()
    {
        // get the player scores and display the score for each player
        return $"P1 Score: {player1Score.Value} | P2 Score: {player2Score.Value}"; // return the score summary string
    }
}