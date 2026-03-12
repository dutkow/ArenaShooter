using Godot;
using System;

public class ProjectileSpawnData
{
    public ushort ProjectileID;
    public byte ownerPlayerID;
    public ProjectileType Type;
    public ushort ServerTickOnSpawn;

    public Vector3 SpawnLocation;
    public Vector3 SpawnDirection;
}


public class ProjectileState
{
    public ushort ProjectileID; // destroyed if received

    public Vector3 Position;
}

/// <summary>
/// Base class for all projectiles. Handles lifetime, damage, and collision API.
/// </summary>
public abstract partial class Projectile : Entity
{
    public ProjectileState State = new();

    [Export] public int Damage = 75;
    [Export] public float LifeTime = 5f;

    protected float _timeAlive = 0f;

    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;

    protected bool _isAlive = true;

    protected ushort _lastProcessedServerTickOnSpawn;

    public virtual void Initialize(Vector3 origin, Vector3 direction, ushort projectileID, bool isPredicted)
    {
        GlobalPosition = origin;
        LookAt(origin + direction, Vector3.Up);

        if (IsAuthority && Area != null)
        {
            Area.AreaEntered += OnCollision;
        }

        State.ProjectileID = projectileID;

        if(isPredicted)
        {
            _lastProcessedServerTickOnSpawn = ClientGame.Instance.LastServerTickProcessedByClient;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if(!_isAlive)
        {
            return;
        }

        _timeAlive += (float)delta;
        if (_timeAlive > LifeTime)
        {
            QueueFree();
        }
    }

    public virtual void OnCollision(Area3D hit)
    {
        GD.Print($"hit: {hit}");
        if(hit is IDamageable damageable)
        {
            damageable.ApplyDamage(Damage);
        }

        QueueFree();

        if(IsAuthority)
        {
            ServerDestroy();
        }
        else
        {
            LocalDestroy();
        }
    }


    public virtual void ServerDestroy()
    {
        ServerProjectileManager.Instance.UpdateProjectileState(State);
        LocalDestroy();
        QueueFree();
    }

    public virtual void LocalDestroy()
    {
        Visible = false;
        _isAlive = false;
    }


    public virtual void ApplyState(ProjectileState state)
    {
        // no logic yet, just remove and queue free

        if(_isAlive)
        {
            LocalDestroy(); // play destroy stuff if still alive on the server
        }

        ClientProjectileManager.Instance.OnLocalProjectileDestroyed(State.ProjectileID);

        QueueFree();
    }

    protected virtual void Destroy()
    {
        if (IsAuthority)
        {
            ServerDestroy();
        }
        else
        {
            LocalDestroy();
        }
    }

    public virtual void Reconcile(ProjectileSpawnData spawnData)
    {

    }
}