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

        var pawn = MatchState.Instance.ConnectedPlayers[NetworkSession.Instance.LocalPlayerID].Pawn;
        if(pawn != null && pawn is Character character)
        {
            character.HealthComp.SetHealth(msg.Health);
            character.HealthComp.SetShield(msg.Shield);
        }
    }

    public static void HandlePlayerDied(byte[] data)
    {
        var msg = new PlayerDied();
        msg.ReadMessage(data);

        var character = MatchState.Instance.ConnectedPlayers[msg.PlayerID].Pawn;
        if(character != null)
        {
            character.OnDeath();
        }
    }
};
