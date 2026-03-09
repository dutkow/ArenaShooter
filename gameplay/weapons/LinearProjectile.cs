using Godot;
using System;
using System.Linq;
using System.Security.Cryptography;

/// <summary>
/// Linear projectile that moves manually and triggers OnCollision when hitting a body.
/// </summary>
public partial class LinearProjectile : Projectile
{
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


    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        var space = GetWorld3D().DirectSpaceState;
        Vector3 motion = _velocity * (float)delta;

        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = CollisionShape.Shape, // e.g., CapsuleShape3D or SphereShape3D
            Transform = new Transform3D(Basis.Identity, GlobalPosition),
            Motion = motion,
            CollideWithBodies = false,
            CollideWithAreas = true
        };
        query.Exclude = new Godot.Collections.Array<Rid> { Area.GetRid() };

        // Cast motion
        var result = space.CastMotion(query);

        float safeFraction = result[0];
        float unsafeFraction = result[1];

        if (unsafeFraction < 1.0f)
        {
            // We hit something, figure out what it was
            Vector3 unsafeMotion = motion * unsafeFraction;
            var newQuery = query;
            newQuery.Transform = newQuery.Transform.Translated(unsafeMotion);

            var restInfo = space.GetRestInfo(newQuery);

            if (restInfo.TryGetValue("collider_id", out var colliderIDObj))
            {
                ulong colliderID = (ulong)colliderIDObj;

                var obj = GodotObject.InstanceFromId(colliderID);
                var colliderNode = obj as Node3D;

                if (colliderNode.Owner != null && colliderNode.Owner is IDamageable damageable)
                {
                    GD.Print($"Collided with: {colliderNode.Name}. apply damage");
                    damageable.ApplyDamage(Damage);
                }
                else
                {
                    GD.Print($"Collided with: {colliderNode?.Name ?? "unknown"}. not damageable");
                }

                Destroy();
            }
        }
        else
        {
            GlobalPosition += motion;
        }
    }

    public override void Destroy()
    {
        base.Destroy();

        QueueFree();
    }
}