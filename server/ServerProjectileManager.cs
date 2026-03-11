using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static Godot.WebSocketPeer;

public enum ProjectileType
{
    DEFAULT,
}

public class ProjectileSpawnData
{
    public ushort ProjectileID;
    public byte OwnerPlayerID;
    public ProjectileType Type;
    public ushort ServerTickOnSpawn;

    public Vector3 SpawnLocation;
    public Vector3 SpawnRotation;
}


public class ServerProjectileManager
{
    public static ServerProjectileManager Instance { get; private set; }

    // --- Dictionaries keyed by ProjectileID instead of List ---
    public Dictionary<byte, Dictionary<ushort, ProjectileSpawnData>> _unackedProjectilesByPlayerID = new();
    public Dictionary<ushort, Dictionary<byte, ProjectileSpawnData[]>> _unackedProjectileHistory = new();

    public Dictionary<byte, Dictionary<ushort, ProjectileState>> _unackedProjectileStatesByPlayerID = new();
    public Dictionary<ushort, Dictionary<byte, ProjectileState[]>> _unackedProjectileStateHistory = new();

    private ushort _nextAvailableProjectileID;

    public static void Create()
    {
        if (Instance != null)
        {
            GD.PushError("Server projectile manager already exists!");
        }
        Instance = new();
    }

    public void CreateProjectilePendingSpawn(ProjectileSpawnData spawnData, bool wasPredicted)
    {
        foreach (var playerID in _unackedProjectilesByPlayerID.Keys)
        {
            _unackedProjectilesByPlayerID[playerID][spawnData.ProjectileID] = spawnData;
        }
        spawnData.ProjectileID = _nextAvailableProjectileID;
        _nextAvailableProjectileID++;

        ClientProjectileManager.Instance?.SpawnAuthoritativeProjectile(spawnData);
    }

    public Dictionary<ushort, ProjectileSpawnData> GetUnackedProjectilesByPlayerID(byte playerID)
    {
        if (!_unackedProjectilesByPlayerID.TryGetValue(playerID, out var dict))
        {
            dict = new Dictionary<ushort, ProjectileSpawnData>();
            _unackedProjectilesByPlayerID[playerID] = dict;
        }
        return dict;
    }

    public Dictionary<ushort, ProjectileState> GetUnackedProjectileStatesByPlayerID(byte playerID)
    {
        if (!_unackedProjectileStatesByPlayerID.TryGetValue(playerID, out var dict))
        {
            dict = new Dictionary<ushort, ProjectileState>();
            _unackedProjectileStatesByPlayerID[playerID] = dict;
        }
        return dict;
    }

    public void AddUnackedProjectileHistoryByPlayerID(ushort tick, byte playerID, ProjectileSpawnData[] projectileSpawnData)
    {
        if (!_unackedProjectileHistory.TryGetValue(tick, out var playerHistory))
        {
            playerHistory = new Dictionary<byte, ProjectileSpawnData[]>();
            _unackedProjectileHistory[tick] = playerHistory;
        }
        playerHistory[playerID] = projectileSpawnData;
    }

    public void AddUnackedProjectileStateHistoryByPlayerID(ushort tick, byte playerID, ProjectileState[] states)
    {
        if (!_unackedProjectileStateHistory.TryGetValue(tick, out var playerHistory))
        {
            playerHistory = new Dictionary<byte, ProjectileState[]>();
            _unackedProjectileStateHistory[tick] = playerHistory;
        }
        playerHistory[playerID] = states;
    }

    public void UpdateProjectileState(ProjectileState state)
    {
        foreach (var playerID in _unackedProjectileStatesByPlayerID.Keys)
        {
            _unackedProjectileStatesByPlayerID[playerID][state.ProjectileID] = state;
        }

        // for now, the only state update is being destroyed
        OnProjectileDestroyed(state.ProjectileID);
    }

    public void OnProjectileDestroyed(ushort projectileID)
    {
        foreach (var playerID in _unackedProjectilesByPlayerID.Keys)
        {
            _unackedProjectilesByPlayerID[playerID].Remove(projectileID);
        }
    }

    public void RemoveUnackedProjectilesByPlayerID(byte playerID, ushort lastProcessedTick)
    {
        if (!_unackedProjectileHistory.TryGetValue(lastProcessedTick, out var tickHistory)) return;
        if (!tickHistory.TryGetValue(playerID, out var snapshot)) return;
        if (snapshot.Length == 0) return;

        var unacked = GetUnackedProjectilesByPlayerID(playerID);
        foreach (var proj in snapshot)
        {
            unacked.Remove(proj.ProjectileID);
        }
    }

    public void RemoveUnackedProjectileStatesByPlayerID(byte playerID, ushort lastProcessedTick)
    {
        if (!_unackedProjectileStateHistory.TryGetValue(lastProcessedTick, out var tickHistory)) return;
        if (!tickHistory.TryGetValue(playerID, out var snapshot)) return;
        if (snapshot.Length == 0) return;

        var unacked = GetUnackedProjectileStatesByPlayerID(playerID);
        foreach (var state in snapshot)
        {
            unacked.Remove(state.ProjectileID);
        }
    }

    public void AddInfoToWorldSnapshot(WorldSnapshot snapshot, byte playerID)
    {
        ushort serverTick = snapshot.ServerTick;

        snapshot.UnacknowledgedRemoteProjectiles = GetUnackedProjectilesByPlayerID(playerID).Values.ToArray();
        AddUnackedProjectileHistoryByPlayerID(serverTick, playerID, snapshot.UnacknowledgedRemoteProjectiles);

        snapshot.UnacknowledgedProjectileStates = GetUnackedProjectileStatesByPlayerID(playerID).Values.ToArray();
        AddUnackedProjectileStateHistoryByPlayerID(serverTick, playerID, snapshot.UnacknowledgedProjectileStates);
    }


    public void ReceiveClientCommand(ClientCommand cmd, byte playerID)
    {
        RemoveUnackedProjectilesByPlayerID(playerID, cmd.LastServerTickProcessedByClient);
        RemoveUnackedProjectileStatesByPlayerID(playerID, cmd.LastServerTickProcessedByClient);
    }
}