using Godot;
using System;
using System.Collections.Generic;
using static Godot.WebSocketPeer;

public class ClientProjectileManager
{
    public static ClientProjectileManager Instance { get; private set; }


    public Dictionary<ushort, Projectile> _knownProjectiles = new();

    public ushort _nextAvailableClientProjectileID;

    public static void Create()
    {
        if (Instance != null)
        {
            GD.PushError("Server projectile manager already exists!");
        }

        Instance = new();
    }

    public ushort GetNextAvailableClientProjectileID()
    {
        ushort nextAvailable = _nextAvailableClientProjectileID;
        _nextAvailableClientProjectileID++;
        return nextAvailable;
    }

    public void HandleUnackedProjectiles(ProjectileSpawnData[] unackedProjectileSpawnDataArray)
    {
        foreach(var unackedProjectileSpawnData in unackedProjectileSpawnDataArray)
        {
            if(!_knownProjectiles.ContainsKey(unackedProjectileSpawnData.ProjectileID))
            {
                // This needs to be purely projectiles which the client didn't predict, if any.
                // starting with fully predicted ones, we instead need to find projectiles and sync them to the client's predicted projectiles
                //SpawnProjectile(unackedProjectileSpawnData);
                SpawnAuthoritativeProjectile(unackedProjectileSpawnData);
            }
        }
    }

    public void HandleUnackedProjectileStates(ProjectileState[] unackedProjectileStatesArray)
    {
        foreach (var state in unackedProjectileStatesArray)
        {
            if (!_knownProjectiles.ContainsKey(state.ProjectileID))
            {
                continue;
            }

            ApplyState(state);
        }
    }

    public void SpawnProjectile(ProjectileSpawnData data, bool wasPredicted)
    {
        if(wasPredicted)
        {
            SpawnPredictedProjectile(data);
        }
        else
        {
            SpawnAuthoritativeProjectile(data);
        }
    }

    public void SpawnPredictedProjectile(ProjectileSpawnData spawnData)
    {
        GD.Print($"Spawning projectile on client. Network mode = {NetworkSession.Instance.NetworkMode}. Adding projectile ID {spawnData.ProjectileID} to known projectiles");

        var spawnedProjectile = ProjectileManager.Instance.LocalSpawnProjectile(_nextAvailableClientProjectileID, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnRotation);
        _nextAvailableClientProjectileID++;
        //_knownProjectiles.Add(spawnData.ProjectileID, spawnedProjectile);
        // we can't add predicted projeciltes that are our own
    }

    public void SpawnAuthoritativeProjectile(ProjectileSpawnData spawnData)
    {
        GD.Print($"Spawning projectile on client. Network mode = {NetworkSession.Instance.NetworkMode}. Adding projectile ID {spawnData.ProjectileID} to known projectiles");
        var spawnedProjectile = ProjectileManager.Instance.LocalSpawnProjectile(spawnData.ProjectileID, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnRotation);
        _knownProjectiles.Add(spawnData.ProjectileID, spawnedProjectile);
    }

    public void ApplyWorldSnapshot(WorldSnapshot snapshot)
    {
        HandleUnackedProjectiles(snapshot.UnacknowledgedRemoteProjectiles);
        HandleUnackedProjectileStates(snapshot.UnacknowledgedProjectileStates);
    }

    public void ApplyState(ProjectileState state)
    {
        if(_knownProjectiles.TryGetValue(state.ProjectileID, out var projectile))
        {
            projectile.ApplyState(state);
        }
    }

    public void OnLocalProjectileDestroyed(ushort projectileID)
    {
        GD.Print($"removing projectile with id: {projectileID}");
        _knownProjectiles.Remove(projectileID);
    }
}
