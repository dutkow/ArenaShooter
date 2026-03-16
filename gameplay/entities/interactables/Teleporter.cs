using Godot;
using System;

public partial class Teleporter : Entity, ICharacterCollidable
{
    [Export] Area3D _area;
    [Export] CollisionShape3D _collisionShape;

    [Export] private Node3D _targetDestination;

    public void OnCollidedWith(Character character, CharacterPublicState state, bool isSimulating)
    {
        Teleport(character);
    }

    public void Teleport(Character character)
    {
        character.Teleport(_targetDestination.GlobalPosition, _targetDestination.GlobalRotation.Y);
    }
}
