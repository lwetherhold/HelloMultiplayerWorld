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

   // declare network variables
   public NetworkVariable<int> raceState = new NetworkVariable<int>((int)RaceState.WaitingForPicks);
   public NetworkVariable<int> winnerHorse = new NetworkVariable<int>(-1);
   public NetworkVariable<int> player1Pick = new NetworkVariable<int>(-1);
   public NetworkVariable<int> player2Pick = new NetworkVariable<int>(-1);
   // score tracking
   public NetworkVariable<int> player1Score = new NetworkVariable<int>(0);
   public NetworkVariable<int> player2Score = new NetworkVariable<int>(0);
   // prefab reference for spawning a server-owned round token
   [SerializeField] private NetworkObject roundTokenPrefab;
   // store the currently spawned round token
   private NetworkObject spawnedRoundToken;

   // add a Server RPC so players can submit horse picks
   [Rpc(SendTo.Server, RequireOwnership = false)]
   public void SubmitPickServerRpc(int horseIndex, RpcParams rpcParams = default)
   {
        if (raceState.Value != (int)RaceState.WaitingForPicks) return;
        if (horseIndex < 0 || horseIndex > 7) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        // get the host id from the server client id (this is the id of the server client)
        ulong hostId = NetworkManager.ServerClientId; // below causes an error
        //ulong hostId = NetworkManager.Singleton.ServerClientId; // safer host id getter than below
        //ulong hostId = NetworkManager.Singleton.ConnectedClientsIds[0];

        if (senderId == hostId)
        {
            // make picks only once per player per round (locked after first pick)
            if (player1Pick.Value != -1) return;
            player1Pick.Value = horseIndex;
        }
        else
        {
            // make picks only once per player per round (locked after first pick)
            if (player2Pick.Value != -1) return;
            player2Pick.Value = horseIndex;
        }

        // make server auto-finish a simple race once both picks are in
        TryRunRaceIfReady();
   }

   [Rpc(SendTo.Server, RequireOwnership = false)]
   public void ResetRaceServerRpc()
   {
        if (!IsServer) return;

        // despawn the old round token before starting next race
        if (spawnedRoundToken != null && spawnedRoundToken.IsSpawned)
            spawnedRoundToken.Despawn(true);

        player1Pick.Value = -1;
        player2Pick.Value = -1;
        winnerHorse.Value = -1;
        raceState.Value = (int)RaceState.WaitingForPicks;
   }

   private void TryRunRaceIfReady()
   {
        if (!IsServer) return;
        if (player1Pick.Value == -1 || player2Pick.Value == -1) return;

        raceState.Value = (int)RaceState.Racing;

        int winner = Random.Range(0, 8); // 0..7
        // set the winner before finishing the race
        winnerHorse.Value = winner;

        // spawn a server-owned round token to show winner result in world
        if (roundTokenPrefab != null)
        {
            // despawn previous token if one already exists
            if (spawnedRoundToken != null && spawnedRoundToken.IsSpawned)
                spawnedRoundToken.Despawn(true);

            Vector3 tokenPos = new Vector3(winner - 3.5f, 2f, 0f);
            spawnedRoundToken = Instantiate(roundTokenPrefab, tokenPos, Quaternion.identity);
            spawnedRoundToken.Spawn();
        }

        // update scores
        if (player1Pick.Value == winner) player1Score.Value++;
        if (player2Pick.Value == winner) player2Score.Value++;

        raceState.Value = (int)RaceState.Finished;
   }

    public string GetRoundSummary()
    {
        string p1 = player1Pick.Value == -1 ? "-" : (player1Pick.Value + 1).ToString();
        string p2 = player2Pick.Value == -1 ? "-" : (player2Pick.Value + 1).ToString();
        string w = winnerHorse.Value == -1 ? "-" : (winnerHorse.Value + 1).ToString();
        return $"P1 Pick: {p1} | P2 Pick: {p2} | Winner: {w}";

        // NOTE: replaced since to match 1..8 horse numbering convention in UI
        //return $"P1 Pick: {player1Pick.Value} | P2 Pick: {player2Pick.Value} | Winner: {winnerHorse.Value}";
    }

    // helper method for getting score summary for UI text display
    public string GetScoreSummary()
    {
        return $"P1 Score: {player1Score.Value} | P2 Score: {player2Score.Value}";
    }
}