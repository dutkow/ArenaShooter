using Godot;
using System;

public interface IPlayerEntity
{
    public byte GetPlayerID();
    public bool IsPlayerControlled();
}