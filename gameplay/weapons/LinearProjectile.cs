using Godot;
using System;

/// <summary>
/// Linear projectile that moves manually and triggers OnCollision when hitting a body.
/// </summary>
public partial class LinearProjectile : Projectile
{
    [Export] public Area3D CollisionArea;
    [Export] public float Speed = 50f;
    [Export] public float Gravity = 0f;

    private Vector3 _velocity;
    private Vector3 _gravityVector;

    public override void Initialize(Vector3 origin, Vector3 direction)
    {
        base.Initialize(origin, direction);

        _velocity = direction.Normalized() * Speed;
        _gravityVector = new Vector3(0, -Gravity, 0);
    }

    public override void _Ready()
    {
        base._Ready();

        if(NetworkSession.Instance.IsServer && CollisionArea != null)
        {
            CollisionArea.BodyEntered += OnCollision;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        float deltaF = (float)delta;

        _velocity += _gravityVector * deltaF;

        GlobalTranslate(_velocity * deltaF);

        base._PhysicsProcess(delta);
    }

}