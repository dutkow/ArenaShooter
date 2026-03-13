using Godot;
using System;
using System.Runtime.ExceptionServices;

public static class WeaponConstants
{
    public const byte TOTAL_WEAPON_SLOTS = 10;

    public static WeaponFlags MaskFromWeapon(WeaponType weapon)
    {
        return (WeaponFlags)(1 << (byte)weapon);
    }
}

public enum WeaponType : byte
{
    WEAPON_1,
    WEAPON_2,
    WEAPON_3,
    WEAPON_4,
    WEAPON_5,
    WEAPON_6,
    WEAPON_7,
    WEAPON_8,
    WEAPON_9,
    WEAPON_10,
}


public enum WeaponFlags : ushort
{
    NONE = 0,

    WEAPON_1_CHANGED = 1 << 0,
    WEAPON_2_CHANGED = 1 << 1,
    WEAPON_3_CHANGED = 1 << 2,
    WEAPON_4_CHANGED  = 1 << 3,
    WEAPON_5_CHANGED = 1 << 4,
    WEAPON_6_CHANGED = 1 << 5,
    WEAPON_7_CHANGED = 1 << 6,
    WEAPON_8_CHANGED = 1 << 7,
    WEAPON_9_CHANGED = 1 << 8,
    WEAPON_10_CHANGED = 1 << 9,
}

[Flags]
public enum PlayerStateFlags : byte
{
    NONE = 0,

    KILLS_CHANGED = 1 << 0,
    DEATHS_CHANGED = 1 << 1,
    PING_CHANGED = 1 << 2,
    IS_ALIVE_CHANGED = 1 << 3,
}

public static class FlagStatics
{
    public static void SetAndFlag<TField, TFlag>(ref TField field, TField value, ref TFlag flags, TFlag flag)
        where TField : struct, IEquatable<TField>
        where TFlag : struct, Enum
    {
        if (!field.Equals(value))
        {
            field = value;
            flags = (TFlag)(object)(((ushort)(object)flags) | ((ushort)(object)flag));
        }
    }


}

public class ArenaPlayerState
{
    public byte PlayerID;
    public  string PlayerName;
    public ArenaCharacter Character; // instance, not replicated

    public PlayerStateFlags Flags;

    public byte Kills;
    public byte Deaths;
    public ushort Ping;
    public bool IsAlive; // Used so clients know who they need to spawn

    public void ClearFlags()
    {
        Flags = 0;
        if(Character != null)
        {
            Character.ClearFlags();
        }
    }
} 



public partial class PlayerStateNew
{
    byte PlayerID;
    string PlayerName = string.Empty;









    public PublicPlayerState PublicState = new();
    public PrivatePlayerState PrivateState = new();


}

public partial class PublicPlayerState
{
    // Not replicated
    public Character Character;

    public byte PlayerID;
    public string PlayerName = "UNNAMED_PLAYER";

    public PublicPlayerFlags Flags;

    public ushort Kills;
    public ushort Deaths;

    public bool IsAlive;

    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float Pitch;
    public CharacterMoveMode MoveMode;


    public WeaponType EquippedWeapon;

    public void ApplyState(PublicPlayerState state)
    {
        var flags = state.Flags;

        if ((flags & PublicPlayerFlags.KILLS) != 0)
            Kills = state.Kills;

        if ((flags & PublicPlayerFlags.DEATHS) != 0)
            Deaths = state.Deaths;

        if ((flags & PublicPlayerFlags.IS_ALIVE) != 0)
            IsAlive = state.IsAlive;

        if ((flags & PublicPlayerFlags.POSITION) != 0)
            Position = state.Position;

        if ((flags & PublicPlayerFlags.VELOCITY) != 0)
            Velocity = state.Velocity;

        if ((flags & PublicPlayerFlags.YAW) != 0)
            Yaw = state.Yaw;

        if ((flags & PublicPlayerFlags.PITCH) != 0)
            Pitch = state.Pitch;

        if ((flags & PublicPlayerFlags.MOVE_MODE) != 0)
            MoveMode = state.MoveMode;

        if ((flags & PublicPlayerFlags.EQUIPPED_WEAPON) != 0)
            EquippedWeapon = state.EquippedWeapon;
    }

    public static PublicPlayerFlags ComputeDirtyFlags(PublicPlayerState current, PublicPlayerState previous)
    {
        if (previous == null)
        {
            return PublicPlayerFlags.KILLS |
                   PublicPlayerFlags.DEATHS |
                   PublicPlayerFlags.IS_ALIVE |
                   PublicPlayerFlags.POSITION |
                   PublicPlayerFlags.VELOCITY |
                   PublicPlayerFlags.YAW |
                   PublicPlayerFlags.PITCH |
                   PublicPlayerFlags.MOVE_MODE |
                   PublicPlayerFlags.EQUIPPED_WEAPON;
        }

        PublicPlayerFlags flags = PublicPlayerFlags.NONE;

        const float EPSILON_SQ = 0.0001f;

        if (current.Kills != previous.Kills)
            flags |= PublicPlayerFlags.KILLS;

        if (current.Deaths != previous.Deaths)
            flags |= PublicPlayerFlags.DEATHS;

        if (current.IsAlive != previous.IsAlive)
            flags |= PublicPlayerFlags.IS_ALIVE;

        if ((current.Position - previous.Position).LengthSquared() > EPSILON_SQ)
            flags |= PublicPlayerFlags.POSITION;

        if ((current.Velocity - previous.Velocity).LengthSquared() > EPSILON_SQ)
            flags |= PublicPlayerFlags.VELOCITY;

        if (Mathf.Abs(current.Yaw - previous.Yaw) > EPSILON_SQ)
            flags |= PublicPlayerFlags.YAW;

        if (Mathf.Abs(current.Pitch - previous.Pitch) > EPSILON_SQ)
            flags |= PublicPlayerFlags.PITCH;

        if (current.MoveMode != previous.MoveMode)
            flags |= PublicPlayerFlags.MOVE_MODE;

        if (current.EquippedWeapon != previous.EquippedWeapon)
            flags |= PublicPlayerFlags.EQUIPPED_WEAPON;

        return flags;
    }
}

public partial class PrivatePlayerState()
{
    public PrivatePlayerFlags Flags;

    public byte Health;
    public byte MaxHealth;

    public byte Armor;
    public byte MaxArmor;

    public byte[] Ammo = new byte[10];

    public void ApplyState(PrivatePlayerState state)
    {

    }
}