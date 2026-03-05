using Godot;
using System;

public partial class JumpPad : Node3D
{
    [Export] private Area3D _area3D;
    [Export] public float JumpStrength = 20f;
    [Export] public Vector3 LaunchDirection = Vector3.Up;
    public Vector3 LaunchVector;

    public override void _Ready()
    {
        base._Ready();

        _area3D.BodyEntered += OnBodyEntered;

        LaunchVector = LaunchDirection * JumpStrength;
    }

    private void OnBodyEntered(Node3D body)
    {
        GD.Print($"body entered jump pad. {body}");
        if (body is ArenaCharacter character)
        {
            character.LaunchCharacter(LaunchVector);
        }
    }
}