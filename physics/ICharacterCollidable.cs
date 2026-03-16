using Godot;
using System;

public interface ICharacterCollidable
{
    void OnCollidedWith(Character character, CharacterPublicState state, bool isSimulating);

}