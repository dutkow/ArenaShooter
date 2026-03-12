using Godot;
using System;
using System.Collections.Generic;
using static Godot.WebSocketPeer;

public class ClientProjectileManager
{
    public static ClientProjectileManager Instance { get; private set; }


    public Dictionary<ushort, Projectile> _knownProjectiles = new();

    public List<Projectile> _predictedProjectiles = new();

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
        foreach(var spawnData in unackedProjectileSpawnDataArray)
        {
            if (spawnData.ownerPlayerID == ClientGame.Instance.LocalPlayerID)
            {
                var projectile = FindPredictedProjectile();
                if(projectile != null)
                {
                    projectile.Reconcile(spawnData);
                    projectile.State.ProjectileID = spawnData.ProjectileID;

                    _knownProjectiles[spawnData.ProjectileID] = projectile;
                    _predictedProjectiles.Remove(projectile);
                }
            }
            else if(!_knownProjectiles.ContainsKey(spawnData.ProjectileID))
            {
                SpawnAuthoritativeProjectile(spawnData);
            }
        }
    }

    public Projectile FindPredictedProjectile()
    {
        if(_predictedProjectiles.Count == 0)
        {
            return null;
        }

        return _predictedProjectiles[0];
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


    public void SpawnPredictedProjectile(ProjectileSpawnData spawnData)
    {
        GD.Print($"Spawning projectile on client. Network mode = {NetworkSession.Instance.NetworkMode}. Adding projectile ID {spawnData.ProjectileID} to known projectiles");

        var spawnedProjectile = ProjectileManager.Instance.LocalSpawnProjectile(_nextAvailableClientProjectileID, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnDirection, true);
        _predictedProjectiles.Add(spawnedProjectile);
        _nextAvailableClientProjectileID++;

    }

    public void SpawnAuthoritativeProjectile(ProjectileSpawnData spawnData)
    {
        GD.Print($"Spawning projectile on client. Network mode = {NetworkSession.Instance.NetworkMode}. Adding projectile ID {spawnData.ProjectileID} to known projectiles");
        var spawnedProjectile = ProjectileManager.Instance.LocalSpawnProjectile(spawnData.ProjectileID, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnDirection, false);
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
