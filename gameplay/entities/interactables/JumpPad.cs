using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class JumpPad : Node3D, ICharacterCollidable
{
    [Export] private Area3D _area3D;
    [Export] public float JumpStrength = 8.0f;


    [Export] public bool OverrideHorizontalVelocity = true;
    [Export] public Vector3 LaunchDirection = Vector3.Up;
    

    public Vector3 LaunchVector;

    [Export] private CollisionShape3D _collisionShape;

    // Track which characters are currently inside the jump pad
    private HashSet<Node3D> _intersectingNodes = new();

    public List<Character> _seenCharacters = new();

    public override void _Ready()
    {
        base._Ready();
        LaunchVector = LaunchDirection * JumpStrength;

        _area3D.Owner = this;
    }

    public void OnCollidedWith(Character character)
    {
        character?.Launch(LaunchVector);
    }

    public void OnStoppedCollidingWith(Character character)
    {
        
    }
}