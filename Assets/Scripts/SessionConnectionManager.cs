using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SessionConnectionManager : MonoBehaviour
{
    public async Task<bool> CreateSessionAndStartHost(string sessionName, int maxPlayers = 2, bool isPrivate = false)
    {
        var options = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = isPrivate };
        //await MultiplayerService.Instance.CreateSessionAsync(sessionName, options);
        //var session = await MultiplayerService.Instance.CreateSessionAsync(sessionName, options); // CAUSED ERROR
        var session = await MultiplayerService.Instance.CreateSessionAsync(options);
        Debug.Log("Session Join Code: " + session.Code);
        return NetworkManager.Singleton.StartHost();
    }

    public async Task<bool> JoinSessionAndStartClient(string sessionName)
    {
        await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionName);
        // WRONG: NEED TO JOIN BY NAME, NOT ID
        //await MultiplayerService.Instance.JoinSessionByIdAsync(sessionName);
        return NetworkManager.Singleton.StartClient();
    }
}