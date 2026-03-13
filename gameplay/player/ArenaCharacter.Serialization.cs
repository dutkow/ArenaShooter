using Godot;
using System;

public class CharacterPublicState
{
    public CharacterPublicFlags Flags;

    public Vector3 Position;
    public Vector2 Rotation; // global yaw, local pitch
    public Vector3 Velocity;
    public CharacterMoveMode MovementMode;
    public WeaponType EquippedWeapon;

    internal void Add(Message msg)
    {
        msg.AddEnum(Flags);
        if ((Flags & CharacterPublicFlags.POSITION_CHANGED) != 0) msg.Add(Position);
        if ((Flags & CharacterPublicFlags.ROTATION_CHANGED) != 0) msg.Add(Rotation);// NEED TO ADD VEC 2 TO MESSAGE
        if ((Flags & CharacterPublicFlags.VELOCITY_CHANGED) != 0) msg.Add(Velocity);
        if ((Flags & CharacterPublicFlags.MOVEMENT_MODE_CHANGED) != 0) msg.AddEnum(MovementMode);
        if ((Flags & CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED) != 0) msg.AddEnum(EquippedWeapon);
    }

    internal void Write(Message msg)
    {
        msg.WriteEnum(Flags);

        if ((Flags & CharacterPublicFlags.POSITION_CHANGED) != 0) msg.Write(Position);
        if ((Flags & CharacterPublicFlags.ROTATION_CHANGED) != 0) msg.Write(Rotation);
        if ((Flags & CharacterPublicFlags.VELOCITY_CHANGED) != 0) msg.Write(Velocity);
        if ((Flags & CharacterPublicFlags.MOVEMENT_MODE_CHANGED) != 0) msg.WriteEnum(MovementMode);
        if ((Flags & CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED) != 0) msg.WriteEnum(EquippedWeapon);
    }
    internal static CharacterPublicState Read(Message msg)
    {
        var state = new CharacterPublicState();

        msg.ReadEnum(out state.Flags);

        if ((state.Flags & CharacterPublicFlags.POSITION_CHANGED) != 0) msg.Read(out state.Position);
        if ((state.Flags & CharacterPublicFlags.ROTATION_CHANGED) != 0) msg.Read(out state.Rotation);
        if ((state.Flags & CharacterPublicFlags.VELOCITY_CHANGED) != 0) msg.Read(out state.Velocity);
        if ((state.Flags & CharacterPublicFlags.MOVEMENT_MODE_CHANGED) != 0) msg.ReadEnum(out state.MovementMode);
        if ((state.Flags & CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED) != 0) msg.ReadEnum(out state.EquippedWeapon);

        return state;
    }
}