using Godot;
using System;



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


public partial class ArenaCharacter : Node3D, ILifeEntity
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
        PrivateState.Health = (byte)health;
        PrivateState.Flags |= CharacterPrivateFlags.HEALTH_CHANGED;
    }

    public int GetMaxHealth()
    {
        return PrivateState.MaxHealth;
    }

    public void SetMaxHealth(int maxHealth)
    {
        PrivateState.MaxHealth = (byte)maxHealth;
        PrivateState.Flags |= CharacterPrivateFlags.MAX_HEALTH_CHANGED;
    }

    public int GetArmor()
    {
        return PrivateState.Armor;
    }

    public void SetArmor(int armor)
    {
        PrivateState.Armor = (byte)armor;
        PrivateState.Flags |= CharacterPrivateFlags.ARMOR_CHANGED;
    }

    public int GetMaxArmor()
    {
        return PrivateState.MaxArmor;
    }

    public void SetMaxArmor(int maxArmor)
    {
        PrivateState.MaxArmor = (byte)maxArmor;
        PrivateState.Flags |= CharacterPrivateFlags.MAX_ARMOR_CHANGED;
    }

    // Public State Changes
    public void OnPositionChanged(Vector3 position)
    {
        PublicState.Position = position;
        PublicState.Flags |= CharacterPublicFlags.POSITION_CHANGED;
    }

    public void OnRotationChanged(float globalYaw, float localPitch)
    {
        //PublicState.Look = new Vector2(globalYaw, localPitch);
        PublicState.Flags |= CharacterPublicFlags.ROTATION_CHANGED;
    }

    public void OnVelocityChanged(Vector3 velocity)
    {
        PublicState.Velocity = velocity;
        PublicState.Flags |= CharacterPublicFlags.VELOCITY_CHANGED;
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
            //PublicState.Look = publicState.Look;
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