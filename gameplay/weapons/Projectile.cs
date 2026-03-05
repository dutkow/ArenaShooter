using Godot;
using System;

/// <summary>
/// Base class for all projectiles. Handles lifetime, damage, and collision API.
/// </summary>
public abstract partial class Projectile : Node3D
{
    [Export] public int Damage = 10;
    [Export] public float LifeTime = 5f;

    protected float _timeAlive = 0f;


    public virtual void Initialize(Vector3 origin, Vector3 direction)
    {
        GlobalPosition = origin;
        LookAt(origin + direction, Vector3.Up);
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;

        _timeAlive += deltaF;
        if (_timeAlive > LifeTime)
        {
            QueueFree();
        }
    }

    public virtual void OnCollision(Node3D hit)
    {
        if(hit is IDamageable damageable)
        {
            damageable.ApplyDamage(Damage);
        }

        QueueFree();
    }
}