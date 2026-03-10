using Godot;
using System;

[Flags]
public enum CharacterSnapshotFlags : ushort
{
    NONE = 0,
    POSITION = 1 << 0,
    VELOCITY = 1 << 1,
    YAW = 1 << 2,
    PITCH = 1 << 3,
    MOVE_MODE = 1 << 4,
    HEALTH = 1 << 5,
    SHIELD = 1 << 6,
}

public struct CharacterSnapshot
{
    public byte PlayerID;
    public CharacterSnapshotFlags DirtyFlags;

    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float Pitch;
    public CharacterMoveMode MoveMode;
    public byte Health;
    public byte Shield;

    public CharacterMoveState GetMoveState()
    {
        CharacterMoveState state = new();
        state.Position = Position;
        state.Velocity = Velocity;
        state.Yaw = Yaw;
        state.Pitch = Pitch;
        state.MoveMode = MoveMode;
        return state;
    }

    public CharacterSnapshot(byte playerID, Vector3 position, Vector3 velocity,
                             float yaw, float pitch, CharacterMoveMode moveMode,
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
        Shield = shield;
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
                   CharacterSnapshotFlags.SHIELD;

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

        if (current.Shield != previous.Value.Shield)
            flags |= CharacterSnapshotFlags.SHIELD;

        return flags;
    }
}