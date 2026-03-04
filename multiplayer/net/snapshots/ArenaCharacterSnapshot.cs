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

public class ArenaCharacterSnapshot
{
    public byte PlayerID;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float AimPitch;

    public ArenaCharacterSnapshot() { }

    public ArenaCharacterSnapshot(byte playerID, Vector3 pos, Vector3 velocity, float yaw, float pitch)
    {
        PlayerID = playerID;
        Position = pos;
        Velocity = velocity;
        Yaw = yaw;
        AimPitch = pitch;
    }
}