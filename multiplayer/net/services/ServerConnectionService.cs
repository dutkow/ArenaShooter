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

        InitialMatchState.Send(peer);

        int peerID = (int)peer.GetMeta("id");
        byte playerID = NetworkSession.Instance.peerIDtoPlayerID[peerID];
        string playerName = NetworkSession.Instance.playerIDtoPlayerState[playerID].PlayerName;

        GD.Print("Server sending player joined");

        PlayerJoined.Execute(playerID, playerName);
        PlayerJoined.Send(playerID, playerName);

        GD.Print($"Server received: {msg.MessageType}");
    }
}
