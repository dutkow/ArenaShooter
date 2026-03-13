using Godot;
using System;

/// <summary>
/// Handles server-side logic when receiving connection-related messages from a client.
/// </summary>
public static class ServerConnectionService
{
    public static void HandleConnectionRequest(ENetPacketPeer peer, byte[] data)
    {
        var msg = new ConnectionRequest();
        msg.ReadMessage(data);

        NetworkSession.Instance.HandleConnectionRequest(peer, msg.PlayerName);

        GD.Print($"Server received: {msg.MessageType}");
    }

    public static void HandleClientLoaded(ENetPacketPeer peer, byte[] data)
    {
        var msg = new ClientLoaded();
        msg.ReadMessage(data);

        int peerID = (int)peer.GetMeta("id");

        byte playerID = NetworkSession.Instance.PeerIDsToPlayerIDs[peerID];
        string playerName = NetworkSession.Instance.PlayerIDsToPlayerStates[playerID].PlayerName;

        NetworkHandler.Instance.ReadyPeers.Add(peer);



        ServerGame.Instance.LastProcessedServerTicksByPlayerID[playerID] = 0;
        ServerGame.Instance.LastProcessedClientTicksByPlayerID[playerID] = 0;

        //PlayerJoined.Execute(playerID, playerName);
        MatchState.Instance.AddPlayer(playerID, playerName);

        GD.Print($"calling initial match state send in client loaded");

        var spawnedPlayer = SpawnManager.Instance.ServerSpawnPlayer(playerID); // spawn the joining player


        GD.Print($"Server received: {msg.MessageType}");

        InitialMatchState.Send(peer);
    }
}
