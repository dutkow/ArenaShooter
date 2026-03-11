using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

public enum ProjectileType
{
    DEFAULT,
}
public class ProjectileSpawnData
{
    public ushort ProjectileID;
    public byte ownerPlayerID;
    public ProjectileType Type;
    public ushort ServerTickOnSpawn;

    // potentially can be optimized out by making client reconstruct
    public Vector3 SpawnLocation;
    public Vector3 SpawnRotation;
} 

public struct ProjectileState
{
    public ushort ProjectileID; // nothing for now yet, if you receive this, it means it was destroyed
}

public class ServerProjectileManager
{
    public static ServerProjectileManager Instance { get; private set; }

    public Dictionary<byte, List<ProjectileSpawnData>> _unackedProjectilesByPlayerID = new();
    public Dictionary<ushort, Dictionary<byte, ProjectileSpawnData[]>> _unackedProjectileHistory = new();

    public Dictionary<byte, List<ProjectileState>> _unackedProjectileStatesByPlayerID = new();
    public Dictionary<ushort, Dictionary<byte, ProjectileState[]>> _unackedProjectileStateHistory = new();


    private ushort _nextAvailableProjectileID;

    public static void Create()
    {
        if(Instance != null)
        {
            GD.PushError("Server projectile manager already exists!");
        }

        Instance = new();
    }

    public void CreateProjectilePendingSpawn(ProjectileSpawnData spawnData)
    {
        foreach (var playerID in _unackedProjectilesByPlayerID.Keys)
        {
            spawnData.ProjectileID = _nextAvailableProjectileID;
            _unackedProjectilesByPlayerID[playerID].Add(spawnData);
            _nextAvailableProjectileID++;
        }
    }

    public List<ProjectileSpawnData> GetUnackedProjectilesByPlayerID(byte playerID)
    {
        if (!_unackedProjectilesByPlayerID.TryGetValue(playerID, out var list))
        {
            list = new List<ProjectileSpawnData>();
            _unackedProjectilesByPlayerID[playerID] = list;
        }
        return list;
    }

    public List<ProjectileState> GetUnackedProjectileStateChangesByPlayerID(byte playerID)
    {
        if (!_unackedProjectileStatesByPlayerID.TryGetValue(playerID, out var list))
        {
            list = new List<ProjectileState>();
            _unackedProjectileStatesByPlayerID[playerID] = list;
        }
        return list;
    }

    public void AddUnackedProjectileHistoryByPlayerID(ushort tick, byte playerID, ProjectileSpawnData[] projectileSpawnData)
    {
        if (!_unackedProjectileHistory.TryGetValue(tick, out var playerHistoryDict))
        {
            playerHistoryDict = new Dictionary<byte, ProjectileSpawnData[]>();
            _unackedProjectileHistory[tick] = playerHistoryDict;
        }

        playerHistoryDict[playerID] = projectileSpawnData;
    }

    public void RemoveUnackedProjectilesByPlayerID(byte playerID, ushort lastProcessedTick)
    {
        var unackedProjectiles = GetUnackedProjectilesByPlayerID(playerID);
        if (unackedProjectiles.Count == 0)
        {
            GD.Print($"no unacked projectiles, returning");
            return;
        }

        if (!_unackedProjectileHistory.TryGetValue(lastProcessedTick, out var playerProjectileHistoryAtTick))
        {
            return;
        }

        if (!playerProjectileHistoryAtTick.TryGetValue(playerID, out var playerUnackedProjectilesAtTick))
        {
            return;
        }

        if (playerUnackedProjectilesAtTick.Length == 0)
        {
            return;
        }

        ushort firstUnackedProjectileID = playerUnackedProjectilesAtTick[0].ProjectileID;
        if(unackedProjectiles[0].ProjectileID != firstUnackedProjectileID)
        {
            return;
        }

        unackedProjectiles.RemoveRange(0, playerUnackedProjectilesAtTick.Length);
    }

    public List<ProjectileState> GetUnackedProjectileStatesByPlayerID(byte playerID)
    {
        if (!_unackedProjectileStatesByPlayerID.TryGetValue(playerID, out var list))
        {
            list = new List<ProjectileState>();
            _unackedProjectileStatesByPlayerID[playerID] = list;
        }
        return list;
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
            _unackedProjectileStatesByPlayerID[playerID].Add(state);
        }
    }

    public void RemoveUnackedProjectileStatesByPlayerID(byte playerID, ushort lastProcessedTick)
    {
        var unacked = GetUnackedProjectileStatesByPlayerID(playerID);
        if (!_unackedProjectileStateHistory.TryGetValue(lastProcessedTick, out var history) ||
            !history.TryGetValue(playerID, out var snapshot) ||
            snapshot.Length == 0 || unacked.Count == 0 ||
            unacked[0].ProjectileID != snapshot[0].ProjectileID)
        {
            return;
        }

        unacked.RemoveRange(0, snapshot.Length);
    }

    public void AddStateInfoToWorldSnapshot(WorldSnapshot snapshot, byte playerID)
    {
        var unackedStates = GetUnackedProjectileStatesByPlayerID(playerID);
        snapshot.UnacknowledgedProjectileStates = unackedStates.ToArray();
        AddUnackedProjectileStateHistoryByPlayerID(snapshot.ServerTick, playerID, snapshot.UnacknowledgedProjectileStates);
    }

    public void AddInfoToWorldSnapshot(WorldSnapshot snapshot, byte playerID)
    {
        snapshot.UnacknowledgedProjectiles = GetUnackedProjectilesByPlayerID(playerID).ToArray();
        AddUnackedProjectileHistoryByPlayerID(snapshot.ServerTick, playerID, snapshot.UnacknowledgedProjectiles);

        snapshot.UnacknowledgedProjectileStates = GetUnackedProjectileStatesByPlayerID(playerID).ToArray();
        AddUnackedProjectileStateHistoryByPlayerID(snapshot.ServerTick, playerID, snapshot.UnacknowledgedProjectileStates);
    }
}
