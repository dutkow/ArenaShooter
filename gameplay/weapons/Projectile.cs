using Godot;
using System;

public partial class Projectile : Node3D
{
    [Export] public float Speed = 50f;          // units per second
    [Export] public float LifeTime = 5f;        // seconds before projectile is deleted
    [Export] public float Gravity = 0.0f; // optional gravity, default no gravity
    Vector3 _gravityVector;

    private Vector3 _velocity;
    private float _timeAlive = 0f;

    // Call this immediately after instancing the projectile
    public void Initialize(Vector3 origin, Vector3 direction)
    {
        GlobalPosition = origin;
        LookAt(origin + direction, Vector3.Up);
        _velocity = direction.Normalized() * Speed;

        _gravityVector = new Vector3(0, -Gravity, 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        // Apply gravity
        _velocity += _gravityVector * (float)delta;

        // Move forward
        GlobalTranslate(_velocity * (float)delta);

        // Track lifetime
        _timeAlive += (float)delta;
        if (_timeAlive > LifeTime)
        {
            QueueFree(); // destroy the projectile after lifetime ends
        }
    }
}