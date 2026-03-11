using Godot;
using System;
using System.Collections.Generic;
using static Godot.WebSocketPeer;

public class ClientProjectileManager
{
    public static ClientProjectileManager Instance { get; private set; }


    public Dictionary<ushort, Projectile> _knownProjectiles = new();
    public Dictionary<ushort, Projectile> _knownPredictedProjectiles = new();

    public List<Projectile> _predictedProjectiles = new();

    bool _firedPredictedProjectile = false;
    public ushort _lastFiredPredictedProjectileID;

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
        if (unackedProjectileSpawnDataArray == null || unackedProjectileSpawnDataArray.Length == 0)
            return;

        GD.Print($"num unacked projectiles received by CL = {unackedProjectileSpawnDataArray.Length}");

        foreach (var unacked in unackedProjectileSpawnDataArray)
        {
            // If it's ours
            if (unacked.ownerPlayerID == ClientGame.Instance.LocalPlayerID)
            {
                var predicted = FindMatchingPredictedProjectile(unacked);
                if (predicted != null)
                {
                    predicted.Reconcile();
                    _predictedProjectiles.Remove(predicted);
                }
            }

            // If we don't already know about this projectile, spawn it
            if (!_knownProjectiles.ContainsKey(unacked.ProjectileID))
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
            if (predicted.State.OwningPlayerID != serverProjectile.ownerPlayerID)
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

        GD.Print($"num unacked projectiles states received by CL = {unackedProjectileStatesArray.Length}");


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
        GD.Print($"Spawning predicted projectile on client. Network mode = {NetworkSession.Instance.NetworkMode}. Adding projectile ID {spawnData.ProjectileID} to known projectiles");

        var spawnedProjectile = ProjectileManager.Instance.LocalSpawnProjectile(_nextAvailableClientProjectileID, spawnData.Type, spawnData.SpawnLocation, spawnData.SpawnRotation);
        _knownPredictedProjectiles.Add(_nextAvailableClientProjectileID, spawnedProjectile);
        _lastFiredPredictedProjectileID = _nextAvailableClientProjectileID;
        _nextAvailableClientProjectileID++;

        _firedPredictedProjectile = true;
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
        if(_firedPredictedProjectile)
        {
            cmd.Mask |= ClientCommandMask.FIRED_PREDICTED_PROJECTILE;
        }

        _firedPredictedProjectile = false;

        return cmd;
    }


}
