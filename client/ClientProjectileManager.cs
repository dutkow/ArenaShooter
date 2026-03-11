using Godot;
using System;

public class ClientProjectileManager
{
    public static ClientProjectileManager Instance { get; private set; }

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
            SpawnProjectile(unackedProjectileSpawnData);
        }
    }

    public void SpawnProjectile(ProjectileSpawnData spawnData)
    {
        GD.Print($"Spawning projectile on client!");
    }
}
