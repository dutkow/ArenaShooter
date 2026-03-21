using Godot;
using System;

public interface ICharacterCollidable
{
    void OnCollidedWith(Character character, CharacterMoveState state, bool isSimulating);

}