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

        if (MatchState.Instance == null)
        {
            GD.Print("match state is null");
            return;
        }

        if (MatchState.Instance.ConnectedPlayers == null)
        {
            GD.Print("connected players is null on match state");
            return;
        }

        var snapshots = msg.GetCharacterSnapshots();

        for (int i = 0; i < snapshots.Length; i++)
        {
            var snapshot = snapshots[i];

            if (MatchState.Instance.ConnectedPlayers.TryGetValue(snapshot.PlayerID, out var playerState))
            {
                if (playerState.Character != null)
                {
                    playerState.Character.ApplySnapshot(snapshot);
                }
                else
                {
                    GD.Print($"player state character is null for PlayerID {snapshot.PlayerID}!");
                }
            }
            else
            {
                GD.Print($"player not found in ConnectedPlayers: {snapshot.PlayerID}");
            }
        }
    }

    public static void HandleProjectileSpawned(byte[] data)
    {
        var msg = new ProjectileSpawned();
        msg.ReadMessage(data);

        ProjectileManager.Instance.LocalSpawnProjectile(msg.ID, msg.Type, msg.SpawnPosition, msg.SpawnRotation);
    }

    public static void HandleHealthChanged(byte[] data)
    {
        var msg = new HealthUpdate();
        msg.ReadMessage(data);

        var character = MatchState.Instance.ConnectedPlayers[NetworkSession.Instance.LocalPlayerID].Character;
        if(character != null)
        {
            character.HealthComponent.SetHealth(msg.Health);
            character.HealthComponent.SetShield(msg.Shield);
        }
    }

    public static void HandlePlayerDied(byte[] data)
    {
        var msg = new PlayerDied();
        msg.ReadMessage(data);

        var character = MatchState.Instance.ConnectedPlayers[msg.PlayerID].Character;
        if(character != null)
        {
            character.OnDeath();
        }
    }
};
