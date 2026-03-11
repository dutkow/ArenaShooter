using Godot;
using System;
using System.Collections.Generic;
using static Godot.WebSocketPeer;

public class ClientProjectileManager
{
    public static ClientProjectileManager Instance { get; private set; }

    public Dictionary<ushort, ProjectileState> _knownProjectileStates = new();

    public Dictionary<ushort, Projectile> _knownProjectiles = new();

    public static void Create()
    {
        if (Instance != null)
        {
            GD.PushError("Server projectile manager already exists!");
        }

        Instance = new();
    }

    public void HandleUnackedProjectiles(ProjectileSpawnData[] unackedProjectileSpawnDataArray)
    {
        foreach(var unackedProjectileSpawnData in unackedProjectileSpawnDataArray)
        {
            if(!_knownProjectiles.ContainsKey(unackedProjectileSpawnData.ProjectileID))
            {
                SpawnProjectile(unackedProjectileSpawnData);
            }
        }
    }

    public void HandleUnackedProjectileStates(ProjectileState[] unackedProjectileStatesArray)
    {
        foreach (var state in unackedProjectileStatesArray)
        {
            if (!_knownProjectileStates.ContainsKey(state.ProjectileID))
            {
                // If the projectile doesn't exist on the client yet, maybe just skip?
                GD.Print($"Warning: received state for unknown projectile ID {state.ProjectileID}");
                continue;
            }

            ApplyState(state);
            //_knownProjectileStates.Remove(state.ProjectileID);
        }
    }

    public void SpawnProjectile(ProjectileSpawnData spawnData)
    {
        _knownProjectileStates[spawnData.ProjectileID] = new();
        GD.Print($"Spawning projectile on client. Network mode = {NetworkSession.Instance.NetworkMode}. Adding projectile ID {spawnData.ProjectileID} to known projectiles");

        var spawnedProjectile = ProjectileManager.Instance.LocalSpawnProjectile(spawnData.ProjectileID, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnRotation);
        _knownProjectiles.Add(spawnData.ProjectileID, spawnedProjectile);
    }

    public void ApplyWorldSnapshot(WorldSnapshot snapshot)
    {
        HandleUnackedProjectiles(snapshot.UnacknowledgedProjectiles);
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
        _knownProjectiles.Remove(projectileID);
        _knownProjectileStates.Remove(projectileID);
    }
}
