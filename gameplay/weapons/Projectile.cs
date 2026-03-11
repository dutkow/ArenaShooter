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

    bool _isAlive = true;

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
        ServerProjectileManager.Instance.UpdateProjectileState(_state);
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

        ClientProjectileManager.Instance.OnLocalProjectileDestroyed(_state.ProjectileID);

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
}