using Godot;
using System.Collections.Generic;
using System;

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
            if(!_knownProjectileStates.ContainsKey(unackedProjectileSpawnData.ProjectileID))
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

            // Apply the state change — currently, your ProjectileState only tracks destruction
            // So we can remove the projectile locally
            _knownProjectileStates.Remove(state.ProjectileID);
            ProjectileManager.Instance.DestroyProjectile(state.ProjectileID);

            ApplyProjectileState(state);
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

    public void ApplyProjectileState(ProjectileState state)
    {
        if(_knownProjectiles.TryGetValue(state.ProjectileID, out var projectile))
        {
            projectile.ApplyState(state);
        }
        else
        {
            GD.Print($"didn't find projectile when trying to apply state");
        }
    }
}
