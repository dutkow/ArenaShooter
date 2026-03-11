using Godot;
using System.Collections.Generic;
using System;

public class ClientProjectileManager
{
    public static ClientProjectileManager Instance { get; private set; }

    public Dictionary<ushort, ProjectileState> _existingProjectileData = new();



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
            if(!_existingProjectileData.ContainsKey(unackedProjectileSpawnData.ProjectileID))
            {
                SpawnProjectile(unackedProjectileSpawnData);
            }
        }
    }

    public void HandleUnackedProjectileStates(ProjectileState[] unackedProjectileStatesArray)
    {
        foreach (var state in unackedProjectileStatesArray)
        {
            if (!_existingProjectileData.ContainsKey(state.ProjectileID))
            {
                // If the projectile doesn't exist on the client yet, maybe just skip?
                GD.Print($"Warning: received state for unknown projectile ID {state.ProjectileID}");
                continue;
            }

            // Apply the state change — currently, your ProjectileState only tracks destruction
            // So we can remove the projectile locally
            _existingProjectileData.Remove(state.ProjectileID);
            ProjectileManager.Instance.DestroyProjectile(state.ProjectileID);
            GD.Print($"Applied projectile state change: removed projectile {state.ProjectileID}");
        }
    }

    public void SpawnProjectile(ProjectileSpawnData spawnData)
    {
        GD.Print($"Spawning projectile on client. Network mode = {NetworkSession.Instance.NetworkMode}");
        _existingProjectileData[spawnData.ProjectileID] = new();

        ProjectileManager.Instance.SpawnProjectile(spawnData.ProjectileID, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnRotation);
    }

    public void ApplyWorldSnapshot(WorldSnapshot snapshot)
    {
        HandleUnackedProjectiles(snapshot.UnacknowledgedProjectiles);
        HandleUnackedProjectileStates(snapshot.UnacknowledgedProjectileStates);
    }
}
