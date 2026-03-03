using Godot;
using System;
using System.Reflection.Metadata;

public static class ClientGameplayService
{
    public static void HandlePlayerSpawned(byte[] data)
    {

        var msg = new PlayerSpawned();
        msg.ReadMessage(data);

        GD.Print($"Client received: {msg.MessageType}");

        SpawnManager.Instance.LocalSpawnPlayer(msg.PlayerID, msg.SpawnPosition, msg.SpawnRotationY);
    }

    public static void HandleWorldSnapshot(byte[] data)
    {
        var msg = new WorldSnapshot();
        msg.ReadMessage(data);

        for(var i = 0; i < msg.PlayerIDs.Length; i++)
        {
            if(MatchState.Instance == null)
            {
                GD.Print("match state is null");
                return;
            }

            if(MatchState.Instance.ConnectedPlayers == null)
            {
                GD.Print("connected players is null on match state");
                return;
            }

            if(MatchState.Instance.ConnectedPlayers.TryGetValue(msg.PlayerIDs[i], out var playerState))
            {
                if(playerState.Character != null)
                {
                    playerState.Character.Body.GlobalPosition = msg.Positions[i];
                    playerState.Character.Body.GlobalRotation = new Vector3(0.0f, msg.CharacterYaws[i], 0.0f);
                }
                else
                {
                    GD.Print("player state character is null!");
                }
            }
        }
    }
}
