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

        byte playerID = msg.AssignedPlayerID;
        ClientGame.Instance.SetLocalPlayerID(playerID);

        GD.Print($"initializing network mode as client on player");

        Main.Instance.OpenMultiplayerMap(NetworkManager.Instance.ServerInfo.MapID);



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

        MatchState.Instance.OnReceivedInitialMatchState(msg);
        GD.Print($"Client received: {msg.MessageType}");

        foreach(var playerState in msg.PlayerStates)
        {
            GD.Print($"initial match state RECEIVE PLAYER DATA. playerID: {playerState.PlayerInfo.PlayerID}. is alive: {playerState.IsSpawned}. player name: {playerState.PlayerInfo.PlayerName}. player position: {playerState.CharacterPublicState.Position}");
        }
        GD.Print($"----------------");

    }

    public static void HandlePlayerJoined(byte[] data)
    {
        var msg = new PlayerJoined();
        msg.ReadMessage(data);

    }
}
