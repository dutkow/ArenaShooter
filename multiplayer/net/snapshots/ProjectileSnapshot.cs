using Godot;
using System;

[Flags]
public enum ProjectileSnapshotFlags : byte
{
    None = 0,
    Position = 1 << 0,
    Velocity = 1 << 1,
    Yaw = 1 << 2,
    AimPitch = 1 << 3,
}

public class ProjectileSnapshot
{
    public int ID;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float AimPitch;

}