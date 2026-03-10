using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class JumpPad : Node3D
{
    [Export] private Area3D _area3D;
    [Export] public float JumpStrength = 8.0f;


    [Export] public bool OverrideHorizontalVelocity = true;
    [Export] public Vector3 LaunchDirection = Vector3.Up;
    

    public Vector3 LaunchVector;

    [Export] private CollisionShape3D _collisionShape;

    // Track which characters are currently inside the jump pad
    private HashSet<Node3D> _intersectingNodes = new();

    public override void _Ready()
    {
        base._Ready();
        LaunchVector = LaunchDirection * JumpStrength;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_collisionShape == null)
            return;

        // Create query parameters
        PhysicsShapeQueryParameters3D query = new PhysicsShapeQueryParameters3D
        {
            Shape = _collisionShape.Shape,
            Transform = _collisionShape.GlobalTransform,
            CollisionMask = 1, // adjust as needed
            CollideWithAreas = true,
            CollideWithBodies = false
        };

        var spaceState = GetWorld3D().DirectSpaceState;
        var results = spaceState.IntersectShape(query, 32);

        // Temporary set of characters detected this tick
        HashSet<Character> currentTick = new();

        foreach (var result in results)
        {
            var collider = (Node3D)result["collider"];
            if (collider != null && collider.Owner is Character character)
            {
                currentTick.Add(character);

                // Only launch if character wasn't already inside
                if (!_intersectingNodes.Contains(character))
                {
                    character.Launch(LaunchVector);
                    _intersectingNodes.Add(character);
                }
            }
        }

        // Remove characters who left the pad
        _intersectingNodes.RemoveWhere(collider => !currentTick.Contains(collider));
    }
}