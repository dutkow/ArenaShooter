using Godot;
using System;

/// <summary>
/// Physics projectile using RigidBody3D for movement and Area3D for collision.
/// </summary>
public partial class PhysicsProjectile : Projectile
{
    [Export] public RigidBody3D RigidBody; // assign in editor
    [Export] public Area3D CollisionArea;  // assign in editor
    [Export] public float InitialSpeed = 50f;

    public override void Initialize(Vector3 origin, Vector3 direction)
    {
        base.Initialize(origin, direction);

        if (RigidBody != null)
        {
            RigidBody.GlobalPosition = origin;
            RigidBody.LookAt(origin + direction, Vector3.Up);
            RigidBody.LinearVelocity = direction.Normalized() * InitialSpeed;
        }

        if (CollisionArea != null)
        {
            CollisionArea.BodyEntered += (Node3D body) =>
            {
                OnCollision(body);
                QueueFree();
            };
        }
    }
}