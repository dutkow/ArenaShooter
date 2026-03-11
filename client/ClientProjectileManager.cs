using Godot;
using System;
using System.Collections.Generic;
using static Godot.WebSocketPeer;

public class ClientProjectileManager
{
    public static ClientProjectileManager Instance { get; private set; }


    public Dictionary<ushort, Projectile> _knownProjectiles = new();
    public Dictionary<ushort, Projectile> _knownPredictedProjectiles = new();

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
        if(unackedProjectileSpawnDataArray == null)
        {
            return;
        }

        GD.Print($"num unacked projectiles received by CL = {unackedProjectileSpawnDataArray.Length}");


        foreach (var unackedProjectileSpawnData in unackedProjectileSpawnDataArray)
        {
            if(!_knownProjectiles.ContainsKey(unackedProjectileSpawnData.ProjectileID))
            {
                SpawnAuthoritativeProjectile(unackedProjectileSpawnData);
            }
        }
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

    public void HandleUnackedPredictedProjectiles(ProjectileSpawnData[] unackedPredictedProjectiles)
    {
        if (unackedPredictedProjectiles == null)
        {
            return;
        }

        GD.Print($"num unacked predicted projectiles received by CL = {unackedPredictedProjectiles.Length}");

        foreach (var unackedPredictedProjectile in unackedPredictedProjectiles)
        {
            if(_knownPredictedProjectiles.TryGetValue(unackedPredictedProjectile.ProjectileID, out var predictedProjectile))
            {
                // handle it eventually
                predictedProjectile.Reconcile();
            }
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
        HandleUnackedPredictedProjectiles(snapshot.UnacknowledgedPredictedProjectiles);
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
            cmd.PredictedProjectileClientID = _lastFiredPredictedProjectileID;

            GD.Print($"adding predicted projectile with client ID {cmd.PredictedProjectileClientID} to CL input cmd. net mode = {NetworkSession.Instance.NetworkMode}");

        }

        _firedPredictedProjectile = false;

        return cmd;
    }


}
