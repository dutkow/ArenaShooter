using Godot;
using System;

/// <summary>
/// Base class for all projectiles. Handles lifetime, damage, and collision API.
/// </summary>
public abstract partial class Projectile : Node3D
{
    [Export] public int Damage = 75;
    [Export] public float LifeTime = 5f;

    protected float _timeAlive = 0f;

    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;

    public virtual void Initialize(Vector3 origin, Vector3 direction)
    {
        GlobalPosition = origin;
        LookAt(origin + direction, Vector3.Up);

        if (NetworkSession.Instance.IsServer && Area != null)
        {
            Area.AreaEntered += OnCollision;
        }
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
    }

    public virtual void Destroy()
    {

    }
}