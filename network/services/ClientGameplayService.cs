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

        //SpawnManager.Instance.LocalSpawnPlayer(msg.PlayerID, msg.SpawnPosition, msg.SpawnRotationY);
    }

    public static void HandleWorldSnapshot(byte[] data)
    {
        var msg = new WorldSnapshot();
        msg.ReadMessage(data);

        GD.Print($"client received world snap");

        ClientGame.Instance.ApplyWorldSnapshot(msg);
    }

    public static void HandleProjectileSpawned(byte[] data)
    {
        var msg = new ProjectileSpawned();
        msg.ReadMessage(data);

        //ProjectileManager.Instance.LocalSpawnProjectile(msg.ID, msg.Type, msg.SpawnPosition, msg.SpawnRotation);
    }

    public static void HandleHealthChanged(byte[] data)
    {
        var msg = new HealthUpdate();
        msg.ReadMessage(data);


    }

    public static void HandlePlayerDied(byte[] data)
    {
        var msg = new PlayerDied();
        msg.ReadMessage(data);


    }
};
