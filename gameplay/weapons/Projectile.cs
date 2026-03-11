using Godot;
using System;

public class ProjectileState
{
    public ushort ProjectileID; // destroyed if received

    public byte OwningPlayerID;

    public Vector3 Position;
}

/// <summary>
/// Base class for all projectiles. Handles lifetime, damage, and collision API.
/// </summary>
public abstract partial class Projectile : Entity
{
    public ProjectileState State { get; private set; } = new();

    [Export] public int Damage = 75;
    [Export] public float LifeTime = 5f;

    protected float _timeAlive = 0f;

    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;

    bool _isAlive = true;

    public virtual void Initialize(Vector3 origin, Vector3 direction, ushort projectileID)
    {
        GlobalPosition = origin;
        LookAt(origin + direction, Vector3.Up);

        if (IsAuthority && Area != null)
        {
            Area.AreaEntered += OnCollision;
        }

        State.ProjectileID = projectileID;
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

    public void Reconcile()
    {
        GD.Print($"reconcile ran on {State.ProjectileID}");
    }
}