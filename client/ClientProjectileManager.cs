using Godot;
using System;
using System.Collections.Generic;
using static Godot.WebSocketPeer;

public class ClientProjectileManager
{
    public static ClientProjectileManager Instance { get; private set; }


    public Dictionary<ushort, Projectile> _knownProjectiles = new();

    public List<Projectile> _predictedProjectiles = new();


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
        if (unackedProjectileSpawnDataArray == null || unackedProjectileSpawnDataArray.Length == 0)
            return;

        

        foreach (var unacked in unackedProjectileSpawnDataArray)
        {
            GD.Print($"unacked proj on client has id: {unacked.ProjectileID}");
            if (unacked.OwnerPlayerID == ClientGame.Instance.LocalPlayerID)
            {
                var predicted = FindMatchingPredictedProjectile(unacked);
                if (predicted != null)
                {
                    //predicted.Reconcile();
                    _predictedProjectiles.Remove(predicted);
                }
            }
            else if (!_knownProjectiles.ContainsKey(unacked.ProjectileID))
            {
                SpawnAuthoritativeProjectile(unacked);
            }
        }
    }

    private Projectile FindMatchingPredictedProjectile(ProjectileSpawnData serverProjectile)
    {
        // Only check our predicted projectiles
        for (int i = 0; i < _predictedProjectiles.Count; i++)
        {
            var predicted = _predictedProjectiles[i];

            // Make sure owner matches
            if (predicted.State.OwningPlayerID != serverProjectile.OwnerPlayerID)
                continue;

            // Option 1: Match by order (first un-reconciled predicted projectile)
            // This is usually sufficient for arena FPS if server returns mostly ordered projectiles
            return predicted;

            // Option 2 (optional): Match by approximate position/velocity
            // float maxDistance = 1.0f; // adjust to your scale
            // if ((predicted.Position - serverProjectile.SpawnLocation).Length() <= maxDistance)
            //     return predicted;
        }

        // No match found
        return null;
    }

    public void HandleUnackedProjectileStates(ProjectileState[] unackedProjectileStatesArray)
    {
        if (unackedProjectileStatesArray == null)
        {
            return;
        }

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
        var spawnedProjectile = ProjectileManager.Instance.LocalSpawnProjectile(0, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnRotation);
    }

    public void SpawnAuthoritativeProjectile(ProjectileSpawnData spawnData)
    {
        var spawnedProjectile = ProjectileManager.Instance.LocalSpawnProjectile(spawnData.ProjectileID, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnRotation);
        
        if (NetworkSession.Instance.IsClient)
        {
            GD.Print($"Spawning authoritative projectile on client. Network mode = {NetworkSession.Instance.NetworkMode}. Adding projectile ID {spawnData.ProjectileID} to known projectiles");
            _knownProjectiles.Add(spawnData.ProjectileID, spawnedProjectile);
        }
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

    public ClientInputCommand AddInfoToClientInputCommand(ClientInputCommand cmd)
    {

        return cmd;
    }


}
