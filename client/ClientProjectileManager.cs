using Godot;
using System.Collections.Generic;
using System;

public class ClientProjectileManager
{
    public static ClientProjectileManager Instance { get; private set; }

    public Dictionary<ushort, ProjectileStateChangeData> _existingProjectileData = new();



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

    public void SpawnProjectile(ProjectileSpawnData spawnData)
    {
        GD.Print($"Spawning projectile on client. Network mode = {NetworkSession.Instance.NetworkMode}");
        _existingProjectileData[spawnData.ProjectileID] = new();

        ProjectileManager.Instance.SpawnProjectile(spawnData.ProjectileID, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnRotation);
    }

    public void ApplyWorldSnapshot(WorldSnapshot snapshot)
    {
        HandleUnackedProjectiles(snapshot.UnacknowledgedProjectiles);
    }
}
