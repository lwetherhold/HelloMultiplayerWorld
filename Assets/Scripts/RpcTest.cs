using Unity.Netcode;
using UnityEngine;

public class RpcTest : NetworkBehaviour
{
    // on network spawn, if not server and owner, send an RPC to the server
   public override void OnNetworkSpawn()
   {
       if (!IsServer && IsOwner) // only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
       {
            // send an RPC to the server
           TestServerRpc(0, NetworkObjectId);
       }
   }

    // send an RPC to the clients and host
   [Rpc(SendTo.ClientsAndHost)]
   //  receive an RPC from the clients and host
   // check if the client is the owner
   void TestClientRpc(int value, ulong sourceNetworkObjectId)
   {
       Debug.Log($"Client Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
       if (IsOwner) // only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
       {
            // send an RPC to the server
           TestServerRpc(value + 1, sourceNetworkObjectId);
       }
   }

    // send an RPC to the server
   [Rpc(SendTo.Server)]
   // receive an RPC from the server
   void TestServerRpc(int value, ulong sourceNetworkObjectId)
   {
       Debug.Log($"Server Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
       // send an RPC to the clients and host
       TestClientRpc(value, sourceNetworkObjectId);
   }
}