using Godot;
using System;

public partial class Projectile : Node3D
{
    [Export] public float Speed = 50f;      // units per second
    [Export] public float LifeTime = 5f;    // seconds before projectile is deleted

    private Vector3 _velocity;
    private float _timeAlive = 0f;

    // Call this immediately after instancing the projectile

    public void Initialize(Vector3 direction)
    {
        _velocity = direction.Normalized() * Speed;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Move forward
        GlobalTranslate(_velocity * (float)delta);

        // Keep track of lifetime
        _timeAlive += (float)delta;
        if (_timeAlive > LifeTime)
        {
            QueueFree(); // destroy the projectile after lifetime ends
        }
    }
}