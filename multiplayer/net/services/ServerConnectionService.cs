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

        //InitialMatchState.Send(peer, ) -> TODO: send this data

        GD.Print($"Server received: {msg.MessageType}");
    }
}
