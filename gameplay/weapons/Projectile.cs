using Godot;
using System;

/// <summary>
/// Base class for all projectiles. Handles lifetime, damage, and collision API.
/// </summary>
public abstract partial class Projectile : Entity
{
    ProjectileState _state = new();

    [Export] public int Damage = 75;
    [Export] public float LifeTime = 5f;

    protected float _timeAlive = 0f;

    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;

    public virtual void Initialize(Vector3 origin, Vector3 direction, ushort projectileID)
    {
        GlobalPosition = origin;
        LookAt(origin + direction, Vector3.Up);

        if (IsAuthority && Area != null)
        {
            Area.AreaEntered += OnCollision;
        }

        _state.ProjectileID = projectileID;
    }

    public override void _PhysicsProcess(double delta)
    {
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
        ServerProjectileManager.Instance.UpdateProjectileState(_state);
        LocalDestroy();
    }

    public virtual void LocalDestroy()
    {
        QueueFree();
    }

    public virtual void ApplyState(ProjectileState state)
    {
        Destroy(); // no state info yet, just destruction
    }

    protected virtual void Destroy()
    {
        if (IsAuthority)
        {
            ServerDestroy();
        }
        else
        {
            //LocalDestroy();
        }
    }
}