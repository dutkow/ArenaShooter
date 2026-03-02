using Godot;
using System;

/// <summary>
/// Handles client-side logic when receiving connection-related messages from the server.
/// </summary>
public static class ClientConnectionService
{
    public static void HandleConnectionAccepted(byte[] data)
    {
        var msg = new ConnectionAccepted();
        msg.ReadMessage(data);

        NetworkSession.Instance.LocalPlayerID = msg.AssignedPlayerID;
        SceneNavigator.Instance.OpenMultiplayerMap(NetworkSession.Instance.ServerInfo.MapID);

        GD.Print($"Client received: {msg.MessageType}");
    }

    public static void HandleConnectionDenied(byte[] data)
    {
        var msg = new ConnectionDenied();
        msg.ReadMessage(data);

        GD.Print($"Client received: {msg.MessageType}");
    }

    public static void HandleInitialMatchState(byte[] data)
    {
        var msg = new InitialMatchState();
        msg.ReadMessage(data);

        GD.Print($"Client received: {msg.MessageType}");
    }
}
