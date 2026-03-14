using Godot;

public class CharacterPublicState
{
    public CharacterPublicFlags Flags;

    public Vector3 Position;
    public Vector2 Look; // global yaw, local pitch
    public Vector3 Velocity;
    public CharacterMoveMode MovementMode;
    public WeaponType EquippedWeapon;

    public void ClearFlags()
    {
        Flags = 0;
    }

    public CharacterPublicState Copy()
    {
        return new CharacterPublicState
        {
            Flags = Flags,
            Position = Position,
            Velocity = Velocity,
            Look = Look,
            MovementMode = MovementMode,
        };
    }

    internal void Add(Message msg, bool forceFull = false)
    {
        if (forceFull)
        {
            msg.Add(Position);
            msg.Add(Look);
            msg.Add(Velocity);
            msg.AddEnum(MovementMode);
            msg.AddEnum(EquippedWeapon);
        }
        else
        {
            msg.AddEnum(Flags);

            if ((Flags & CharacterPublicFlags.POSITION_CHANGED) != 0) msg.Add(Position);
            if ((Flags & CharacterPublicFlags.ROTATION_CHANGED) != 0) msg.Add(Look);
            if ((Flags & CharacterPublicFlags.VELOCITY_CHANGED) != 0) msg.Add(Velocity);
            if ((Flags & CharacterPublicFlags.MOVEMENT_MODE_CHANGED) != 0) msg.AddEnum(MovementMode);
            if ((Flags & CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED) != 0) msg.AddEnum(EquippedWeapon);
        }
    }

    internal void Write(Message msg, bool forceFull = false)
    {
        if (forceFull)
        {
            msg.Write(Position);
            msg.Write(Look);
            msg.Write(Velocity);
            msg.WriteEnum(MovementMode);
            msg.WriteEnum(EquippedWeapon);
        }
        else
        {
            msg.WriteEnum(Flags);


            if ((Flags & CharacterPublicFlags.POSITION_CHANGED) != 0) msg.Write(Position);
            if ((Flags & CharacterPublicFlags.ROTATION_CHANGED) != 0) msg.Write(Look);
            if ((Flags & CharacterPublicFlags.VELOCITY_CHANGED) != 0) msg.Write(Velocity);
            if ((Flags & CharacterPublicFlags.MOVEMENT_MODE_CHANGED) != 0) msg.WriteEnum(MovementMode);
            if ((Flags & CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED) != 0) msg.WriteEnum(EquippedWeapon);
        }
    }

    internal void Read(Message msg, bool forceFull = false)
    {
        if (forceFull)
        {
            msg.Read(out Position);
            msg.Read(out Look);
            msg.Read(out Velocity);
            msg.ReadEnum(out MovementMode);
            msg.ReadEnum(out EquippedWeapon);
        }
        else
        {
            msg.ReadEnum(out Flags);

            if ((Flags & CharacterPublicFlags.POSITION_CHANGED) != 0) msg.Read(out Position);
            if ((Flags & CharacterPublicFlags.ROTATION_CHANGED) != 0) msg.Read(out Look);
            if ((Flags & CharacterPublicFlags.VELOCITY_CHANGED) != 0) msg.Read(out Velocity);
            if ((Flags & CharacterPublicFlags.MOVEMENT_MODE_CHANGED) != 0) msg.ReadEnum(out MovementMode);
            if ((Flags & CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED) != 0) msg.ReadEnum(out EquippedWeapon);
        }
    }
}