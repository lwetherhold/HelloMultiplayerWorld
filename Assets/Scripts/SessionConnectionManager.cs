using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using System.Linq;

public class SessionConnectionManager : MonoBehaviour
{
    public async Task<bool> StartHostSession(string sessionName, int maxPlayers = 2, bool isPrivate = false)
    {
        var options = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = isPrivate, Name = sessionName };
        //await MultiplayerService.Instance.CreateSessionAsync(sessionName, options);
        //var session = await MultiplayerService.Instance.CreateSessionAsync(sessionName, options); // CAUSED ERROR
        var session = await MultiplayerService.Instance.CreateSessionAsync(options);
        Debug.Log("Session Join Code: " + session.Code);
        return NetworkManager.Singleton.StartHost();
    }

    // WRONG: THERE IS NO JOIN SESSION BY NAME ASYNC METHOD
    /*
    public async Task<bool> JoinSessionAndStartClient(string sessionName)
    {
        // WRONG: NEED TO JOIN BY CODE, NOT ID // WRONG
        //await MultiplayerService.Instance.JoinSessionByIdAsync(sessionName);
        // WRONG: NEED TO JOIN BY NAME, NOT CODE
        //await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionName);
        await MultiplayerService.Instance.JoinSessionByNameAsync(sessionName);
        return NetworkManager.Singleton.StartClient();
    }
    */

    public async Task<bool> StartClientSession(string sessionName)
    {
        //var queryResult = await MultiplayerService.Instance.QuerySessionsAsync(); // CAUSES ERROR
        var queryResult = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());
        var target = queryResult.Sessions.FirstOrDefault(s => s.Name == sessionName);

        if (target == null)
        {
            Debug.LogError("No session found with name: " + sessionName);
            return false;
        }

        await MultiplayerService.Instance.JoinSessionByIdAsync(target.Id);
        return NetworkManager.Singleton.StartClient();
    }

    public bool StartHostIP(string ipAddress, ushort port)
    {
        var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        utp.SetConnectionData(ipAddress, port);
        return NetworkManager.Singleton.StartHost();
    }

    public bool StartClientIP(string ipAddress, ushort port)
    {
        var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        utp.SetConnectionData(ipAddress, port);
        return NetworkManager.Singleton.StartClient();
    }
}