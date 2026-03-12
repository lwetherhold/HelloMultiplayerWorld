using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using System.Linq;

public class SessionConnectionManager : MonoBehaviour
{
    // creates a host session with the given name, max players, and privacy settings
    public async Task<bool> StartHostSession(string sessionName, int maxPlayers = 2, bool isPrivate = false)
    {
        // initalize session options with the given name, max players, and privacy settings
        var options = new SessionOptions { MaxPlayers = maxPlayers, IsPrivate = isPrivate, Name = sessionName }
            .WithRelayNetwork(); // USE NO-IP SESSIONS W/ RELAY, DO NOT USE MANUAL IP

        //await MultiplayerService.Instance.CreateSessionAsync(sessionName, options); // NEED TO ADD DEBUG LOGGING
        //var session = await MultiplayerService.Instance.CreateSessionAsync(sessionName, options); // CAUSED ERROR

        // get the session with the given options
        var session = await MultiplayerService.Instance.CreateSessionAsync(options);
        Debug.Log("Session Join Code: " + session.Code);

        //return NetworkManager.Singleton.StartHost(); STOP MANUALLY STARTING NGO (let sessions/relay network handler do it)
        
        // return true if the session was created successfully
        return true;
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

    // joins a session with the given name
    public async Task<bool> StartClientSession(string sessionName)
    {
        //var queryResult = await MultiplayerService.Instance.QuerySessionsAsync(); // CAUSES ERROR

        // query the sessions with the given options
        var queryResult = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions());

        // get the target session with the given name
        var target = queryResult.Sessions.FirstOrDefault(s => s.Name == sessionName);

        // check if the session was found
        if (target == null)
        {
            Debug.LogError("No session found with name: " + sessionName);
            // return false if the session was not found
            return false;
        }

        // join the session with the given id
        await MultiplayerService.Instance.JoinSessionByIdAsync(target.Id);

        //return NetworkManager.Singleton.StartClient(); // STOP MANUALLY STARTING NGO (let sessions/relay network handler do it)
        
        // return true if the session was joined successfully
        return true;
    }

    // starts a host with the given ip address and port
    public bool StartHostIP(string ipAddress, ushort port)
    {
        // get the network transport
        var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

        // set the connection data with the given ip address and port
        utp.SetConnectionData(ipAddress, port);

        // start the host
        return NetworkManager.Singleton.StartHost();
    }

    // starts a client with the given ip address and port
    public bool StartClientIP(string ipAddress, ushort port)
    {
        // get the network transport
        var utp = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

        // set the connection data with the given ip address and port
        utp.SetConnectionData(ipAddress, port);

        // start the client
        return NetworkManager.Singleton.StartClient();
    }
}