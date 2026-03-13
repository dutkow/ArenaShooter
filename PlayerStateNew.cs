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

[Flags]
public enum PlayerStateFlags : byte
{
    NONE = 0,

    KILLS_CHANGED = 1 << 0,
    DEATHS_CHANGED = 1 << 1,
    PING_CHANGED = 1 << 2,
    IS_ALIVE_CHANGED = 1 << 3,
}

[Flags]
public enum CharacterPublicFlags : ushort
{
    NONE = 0,

    // movement replication
    POSITION_CHANGED = 1 << 0,
    ROTATION_CHANGED = 1 << 1,
    VELOCITY_CHANGED = 1 << 2,
    MOVEMENT_MODE_CHANGED = 1 << 3,

    EQUIPPED_WEAPON_CHANGED = 1 << 4,

    // eventually animation stuff, etc.
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
public enum CharacterPrivateFlags : byte
{
    NONE = 0,

    HEALTH_CHANGED = 1 << 0,
    MAX_HEALTH_CHANGED = 1 << 1,

    ARMOR_CHANGED = 1 << 2,
    MAX_ARMOR_CHANGED = 1 << 3,

    WEAPONS_CHANGED = 1 << 4,
    AMMO_CHANGED = 1 << 5,
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

public class ExamplePlayerState
{
    public byte PlayerID;
    public  string PlayerName;
    public ExampleCharacter Character; // instance, not replicated

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

public class CharacterPublicState
{
    public CharacterPublicFlags Flags;

    public Vector3 Position;
    public Vector2 Rotation; // global yaw, local pitch
    public Vector3 Velocity;
    public CharacterMoveMode MovementMode;
    public WeaponType EquippedWeapon;
}

public class CharacterPrivateState
{
    public CharacterPrivateFlags Flags;

    public byte Health;
    public  byte MaxHealth;
    public byte Armor;
    public  byte MaxArmor;
    public WeaponFlags HeldWeaponsFlags;
    public WeaponFlags AmmoChangedFlags;
    public byte[] Ammo = new byte[10]; // 10 weapons for now, like a classic arena FPS

}

public partial class ExampleCharacter : Node3D, ILifeEntity
{
    private CharacterPublicState PublicState = new CharacterPublicState();
    private CharacterPrivateState PrivateState = new CharacterPrivateState();

    // Health & Armor
    public int GetHealth()
    {
        return PrivateState.Health;
    }

    public void SetHealth(int health)
    {
        byte clamped = (byte)Math.Clamp(health, 0, PrivateState.MaxHealth);
        FlagStatics.SetAndFlag(ref PrivateState.Health, clamped, ref PrivateState.Flags, CharacterPrivateFlags.HEALTH_CHANGED);
    }

    public int GetMaxHealth()
    {
        return PrivateState.MaxHealth;
    }

    public void SetMaxHealth(int maxHealth)
    {
        byte clamped = (byte)maxHealth;
        FlagStatics.SetAndFlag(ref PrivateState.MaxHealth, clamped, ref PrivateState.Flags, CharacterPrivateFlags.MAX_HEALTH_CHANGED);
    }

    public int GetArmor()
    {
        return PrivateState.Armor;
    }

    public void SetArmor(int armor)
    {
        byte clamped = (byte)Math.Clamp(armor, 0, PrivateState.MaxArmor);
        FlagStatics.SetAndFlag(ref PrivateState.Armor, clamped, ref PrivateState.Flags, CharacterPrivateFlags.ARMOR_CHANGED);
    }

    public int GetMaxArmor()
    {
        return PrivateState.MaxArmor;
    }

    public void SetMaxArmor(int maxArmor)
    {
        byte clamped = (byte)maxArmor;
        FlagStatics.SetAndFlag(ref PrivateState.MaxArmor, clamped, ref PrivateState.Flags, CharacterPrivateFlags.MAX_ARMOR_CHANGED);
    }

    // Public State Changes
    public void OnPositionChanged(Vector3 position)
    {
        FlagStatics.SetAndFlag(ref PublicState.Position, position, ref PublicState.Flags, CharacterPublicFlags.POSITION_CHANGED);
    }

    public void OnRotationChanged(float globalYaw, float localPitch)
    {
        Vector2 rotation = new Vector2(globalYaw, localPitch);
        FlagStatics.SetAndFlag(ref PublicState.Rotation, rotation, ref PublicState.Flags, CharacterPublicFlags.ROTATION_CHANGED);
    }

    public void OnVelocityChanged(Vector3 velocity)
    {
        FlagStatics.SetAndFlag(ref PublicState.Velocity, velocity, ref PublicState.Flags, CharacterPublicFlags.VELOCITY_CHANGED);
    }

    public void OnMovementModeChanged(CharacterMoveMode movementMode)
    {
        PublicState.MovementMode = movementMode;
        PublicState.Flags |= CharacterPublicFlags.MOVEMENT_MODE_CHANGED;
    }

    public void OnEquippedWeaponChanged(WeaponType weaponType)
    {
        PublicState.EquippedWeapon = weaponType;
        PublicState.Flags |= CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED;
    }

    // Weapons & Ammo
    public void OnReceivedWeapon(WeaponType weaponType)
    {
        WeaponFlags mask = WeaponConstants.MaskFromWeapon(weaponType);
        PrivateState.HeldWeaponsFlags |= mask;
        PrivateState.Flags |= CharacterPrivateFlags.WEAPONS_CHANGED;
    }

    public void OnLostWeapon(WeaponType weaponType)
    {
        WeaponFlags mask = WeaponConstants.MaskFromWeapon(weaponType);
        PrivateState.HeldWeaponsFlags &= ~mask;
        PrivateState.Flags |= CharacterPrivateFlags.WEAPONS_CHANGED;
    }

    public void OnAmmoChanged(WeaponType weaponType, byte newAmmo)
    {
        int index = (int)weaponType;
        if (index < WeaponConstants.TOTAL_WEAPON_SLOTS)
        {
            PrivateState.Ammo[index] = newAmmo;
            PrivateState.AmmoChangedFlags |= WeaponConstants.MaskFromWeapon(weaponType);
            PrivateState.Flags |= CharacterPrivateFlags.AMMO_CHANGED;
        }
    }

    // Apply Replicated States
    public void ApplyPublicState(CharacterPublicState publicState)
    {
        CharacterPublicFlags flags = publicState.Flags;

        if ((flags & CharacterPublicFlags.POSITION_CHANGED) != 0)
        {
            PublicState.Position = publicState.Position;
        }

        if ((flags & CharacterPublicFlags.ROTATION_CHANGED) != 0)
        {
            PublicState.Rotation = publicState.Rotation;
        }

        if ((flags & CharacterPublicFlags.VELOCITY_CHANGED) != 0)
        {
            PublicState.Velocity = publicState.Velocity;
        }

        if ((flags & CharacterPublicFlags.MOVEMENT_MODE_CHANGED) != 0)
        {
            PublicState.MovementMode = publicState.MovementMode;
        }

        if ((flags & CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED) != 0)
        {
            PublicState.EquippedWeapon = publicState.EquippedWeapon;
        }
    }

    public void ApplyPrivateState(CharacterPrivateState privateState)
    {
        CharacterPrivateFlags flags = privateState.Flags;

        if ((flags & CharacterPrivateFlags.HEALTH_CHANGED) != 0)
        {
            PrivateState.Health = privateState.Health;
        }

        if ((flags & CharacterPrivateFlags.MAX_HEALTH_CHANGED) != 0)
        {
            PrivateState.MaxHealth = privateState.MaxHealth;
        }

        if ((flags & CharacterPrivateFlags.ARMOR_CHANGED) != 0)
        {
            PrivateState.Armor = privateState.Armor;
        }

        if ((flags & CharacterPrivateFlags.MAX_ARMOR_CHANGED) != 0)
        {
            PrivateState.MaxArmor = privateState.MaxArmor;
        }

        if ((flags & CharacterPrivateFlags.WEAPONS_CHANGED) != 0)
        {
            PrivateState.HeldWeaponsFlags = privateState.HeldWeaponsFlags;
        }

        if ((flags & CharacterPrivateFlags.AMMO_CHANGED) != 0)
        {
            WeaponFlags ammoFlags = privateState.AmmoChangedFlags;
            for (int i = 0; i < WeaponConstants.TOTAL_WEAPON_SLOTS; i++)
            {
                WeaponFlags mask = (WeaponFlags)(1 << i);
                if ((ammoFlags & mask) != 0)
                {
                    PrivateState.Ammo[i] = privateState.Ammo[i];
                }
            }

            PrivateState.AmmoChangedFlags = privateState.AmmoChangedFlags;
        }
    }

    // Flags management
    public void ClearFlags()
    {
        PublicState.Flags = 0;
        PrivateState.Flags = 0;
    }
}

public partial class PlayerStateNew
{
    byte PlayerID;
    string PlayerName = string.Empty;









    public PublicPlayerState PublicState = new();
    public PrivatePlayerState PrivateState = new();

    public void TickPlayerState()
    {
        if ((PublicState.IsAlive && PublicState.Character != null))
        {
            PublicState.Position = PublicState.Character.MovementComp.State.Position;
            PublicState.Yaw = PublicState.Character.MovementComp.State.Yaw;
            PublicState.Pitch = PublicState.Character.MovementComp.State.Pitch;
        }
    }
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

    public CharacterMoveState GetMoveState()
    {
        CharacterMoveState moveState = new();
        moveState.Position = Position;
        moveState.Velocity = Velocity;
        moveState.Yaw = Yaw;
        moveState.Pitch = Pitch;
        moveState.MoveMode = MoveMode;
        return moveState;
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