using Godot;
using System;

[Flags]
public enum ArenaCharacterSnapshotFlags : byte
{
    None = 0,
    Position = 1 << 0,
    Velocity = 1 << 1,
    Yaw = 1 << 2,
    AimPitch = 1 << 3,
}

[Flags]
public enum ArenaCharacterStatFlags : byte
{
    None = 0,
    Health = 1 << 0,
    Armor = 1 << 1,
}


