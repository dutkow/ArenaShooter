using Godot;
using System;

[Flags]
public enum CharacterSnapshotFlags : ushort
{
    NONE = 0,

    // MOVEMENT
    POSITION = 1 << 0,
    VELOCITY = 1 << 1,
    YAW = 1 << 2,
    PITCH = 1 << 3,

    // I can actually remove move mode I think, but won't yet
    MOVE_MODE = 1 << 4,

    // Events
    FIRE_PRIMARY = 1 << 5,
    FIRE_SECONDARY = 1 << 6,

    HEALTH = 1 << 7,
    ARMOR = 1 << 8,
}

public struct CharacterSnapshot
{
    public byte PlayerID;
    public CharacterSnapshotFlags DirtyFlags;

    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float Pitch;
    public CharacterMovementMode MoveMode;
    public byte Health;
    public byte Armor;


    public CharacterSnapshot(byte playerID, Vector3 position, Vector3 velocity,
                             float yaw, float pitch, CharacterMovementMode moveMode,
                             byte health, byte shield,
                             CharacterSnapshotFlags dirtyFlags)
    {
        PlayerID = playerID;
        Position = position;
        Velocity = velocity;
        Yaw = yaw;
        Pitch = pitch;
        MoveMode = moveMode;
        Health = health;
        Armor = shield;
        DirtyFlags = dirtyFlags;
    }

    public static CharacterSnapshotFlags ComputeDirtyFlags(CharacterSnapshot current, CharacterSnapshot? previous)
    {
        if (previous == null)
            return CharacterSnapshotFlags.POSITION |
                   CharacterSnapshotFlags.VELOCITY |
                   CharacterSnapshotFlags.YAW |
                   CharacterSnapshotFlags.PITCH |
                   CharacterSnapshotFlags.MOVE_MODE |
                   CharacterSnapshotFlags.HEALTH |
                   CharacterSnapshotFlags.ARMOR;

        CharacterSnapshotFlags flags = CharacterSnapshotFlags.NONE;

        const float EPSILON_SQ = 0.0001f;

        if ((current.Position - previous.Value.Position).LengthSquared() > EPSILON_SQ)
            flags |= CharacterSnapshotFlags.POSITION;

        if ((current.Velocity - previous.Value.Velocity).LengthSquared() > EPSILON_SQ)
            flags |= CharacterSnapshotFlags.VELOCITY;

        if (Mathf.Abs(current.Yaw - previous.Value.Yaw) > EPSILON_SQ)
            flags |= CharacterSnapshotFlags.YAW;

        if (Mathf.Abs(current.Pitch - previous.Value.Pitch) > EPSILON_SQ)
            flags |= CharacterSnapshotFlags.PITCH;

        if (current.MoveMode != previous.Value.MoveMode)
            flags |= CharacterSnapshotFlags.MOVE_MODE;

        if (current.Health != previous.Value.Health)
            flags |= CharacterSnapshotFlags.HEALTH;

        if (current.Armor != previous.Value.Armor)
            flags |= CharacterSnapshotFlags.ARMOR;

        return flags;
    }
}