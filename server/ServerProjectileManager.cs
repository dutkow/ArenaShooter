using Godot;
using System.Collections.Generic;
using System.Linq;

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

public struct ProjectileStateChangeData
{
    public ushort ProjectileID; // nothing for now yet, if you receive this, it means it was destroyed
}

public class ServerProjectileManager
{
    public Dictionary<ushort, List<ProjectileSpawnData>> _unackedProjectilesByPlayerID = new();
    public Dictionary<ushort, List<ProjectileStateChangeData>> _unackedProjectileStateChangesByPlayerID = new();

    public Dictionary<ushort, Dictionary<byte, ProjectileSpawnData[]>> _unackedProjectileHistory = new();

    public void CreateProjectilePendingSpawn(ProjectileSpawnData spawnData)
    {
        foreach (var playerID in _unackedProjectilesByPlayerID.Keys)
        {
            _unackedProjectilesByPlayerID[playerID].Add(spawnData);
        }
    }

    public void SetProjectileStateChange(ProjectileStateChangeData data)
    {
        foreach (var playerID in _unackedProjectileStateChangesByPlayerID.Keys)
        {
            _unackedProjectileStateChangesByPlayerID[playerID].Add(data);
        }
    }

    public List<ProjectileSpawnData> GetUnackedProjectilesByPlayerID(byte playerID)
    {
        return _unackedProjectilesByPlayerID[playerID];
    }

    public List<ProjectileStateChangeData> GetUnackedProjectileStateChangesByPlayerID(byte playerID)
    {
        return _unackedProjectileStateChangesByPlayerID[playerID];
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

    public void RemoveProjectileStateChangesByPlayerID(byte playerID, ushort lastProcessedTick)
    {
        // Ensure player has an unacked projectile
        var unackedProjectiles = GetUnackedProjectilesByPlayerID(playerID);
        int numUnackedProjectiles = unackedProjectiles.Count;
        if (numUnackedProjectiles == 0)
        {
            return;
        }

        var lastReceivedProjectileHistory = _unackedProjectileHistory[lastProcessedTick];
        lastReceivedProjectileHistory.TryGetValue(playerID, out var playerHistoryDict);
        if(playerHistoryDict == null)
        {
            return;
        }

        // Ensure the client had an unacknowledged projectile on the last tick. If they didn't, then we don't have anything to remove
        int numUnackedLastTick = playerHistoryDict.Length;
        if(numUnackedLastTick == 0)
        {
            return;
        }

        // Ensure the newly acknowledged projectile has the same first entry, confirming the client's newly received list should be removed
        ushort firstUnackedProjectileID = playerHistoryDict[0].ProjectileID;
        
        if (unackedProjectiles[0].ProjectileID == firstUnackedProjectileID)
        {
            unackedProjectiles.RemoveRange(0, numUnackedLastTick - 1);
        }
    }
}
