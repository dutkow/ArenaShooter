using Godot;
using System;
using System.Reflection.Metadata;

public static class ClientGameplayService
{
    public static void HandlePlayerSpawned(byte[] data)
    {
        var msg = new PlayerSpawned();
        msg.ReadMessage(data);

        PlayerSpawned.Execute(msg.PlayerID, msg.SpawnPosition, msg.SpawnRotationY);
    }
}
