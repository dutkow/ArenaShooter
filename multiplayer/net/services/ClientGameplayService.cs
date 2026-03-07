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

        ushort serverTick = msg.LastProcessedClientTick;
        if (!NetUtils.IsNewerTick(serverTick, MatchState.Instance.LastAppliedServerTick))
        {
            return;
        }

        ushort lastAckedTick = serverTick;
        MatchState.Instance.LastAppliedServerTick = lastAckedTick;

        var snapshots = msg.GetCharacterSnapshots();


        for (int i = 0; i < snapshots.Length; i++)
        {
            var snapshot = snapshots[i];

            if (MatchState.Instance.ConnectedPlayers.TryGetValue(snapshot.PlayerID, out var playerState))
            {
                if (playerState.Pawn != null && playerState.Pawn is Character character)
                {
                    character.ApplyServerSnapshot(snapshot, lastAckedTick);
                }
                else
                {
                    SpawnManager.Instance.LocalSpawnPlayer(snapshot.PlayerID, snapshot.Position, snapshot.Yaw);
                    GD.Print($"Player not found so spawning player at position {snapshot.Position}");
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
