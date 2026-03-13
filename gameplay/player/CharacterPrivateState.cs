using Godot;
using System;

public class CharacterPrivateState
{
    public CharacterPrivateFlags Flags;

    public byte Health;
    public byte MaxHealth;
    public byte Armor;
    public byte MaxArmor;
    public WeaponFlags HeldWeaponsFlags;
    public WeaponFlags AmmoChangedFlags;
    public byte[] Ammo = new byte[WeaponConstants.TOTAL_WEAPON_SLOTS];

    public void ClearFlags()
    {
        Flags = 0;
        HeldWeaponsFlags = 0;
        AmmoChangedFlags = 0;
    }

    internal void Add(Message msg)
    {
        msg.AddEnum(Flags);

        if ((Flags & CharacterPrivateFlags.HEALTH_CHANGED) != 0) msg.Add(Health);
        if ((Flags & CharacterPrivateFlags.MAX_HEALTH_CHANGED) != 0) msg.Add(MaxHealth);
        if ((Flags & CharacterPrivateFlags.ARMOR_CHANGED) != 0) msg.Add(Armor);
        if ((Flags & CharacterPrivateFlags.MAX_ARMOR_CHANGED) != 0) msg.Add(MaxArmor);
        if ((Flags & CharacterPrivateFlags.WEAPONS_CHANGED) != 0) msg.AddEnum(HeldWeaponsFlags);
        if ((Flags & CharacterPrivateFlags.AMMO_CHANGED) != 0) msg.AddEnum(AmmoChangedFlags);

    }

    internal void Write(Message msg)
    {
        msg.WriteEnum(Flags);

        if ((Flags & CharacterPrivateFlags.HEALTH_CHANGED) != 0) msg.Write(Health);
        if ((Flags & CharacterPrivateFlags.MAX_HEALTH_CHANGED) != 0) msg.Write(MaxHealth);
        if ((Flags & CharacterPrivateFlags.ARMOR_CHANGED) != 0) msg.Write(Armor);
        if ((Flags & CharacterPrivateFlags.MAX_ARMOR_CHANGED) != 0) msg.Write(MaxArmor);
        if ((Flags & CharacterPrivateFlags.WEAPONS_CHANGED) != 0) msg.WriteEnum(HeldWeaponsFlags);
        if ((Flags & CharacterPrivateFlags.AMMO_CHANGED) != 0) msg.WriteEnum(AmmoChangedFlags);
    }
    internal CharacterPrivateState Read(Message msg)
    {
        var state = new CharacterPrivateState();

        msg.ReadEnum(out Flags);

        if ((Flags & CharacterPrivateFlags.HEALTH_CHANGED) != 0) msg.Read(out Health);
        if ((Flags & CharacterPrivateFlags.MAX_HEALTH_CHANGED) != 0) msg.Read(out MaxHealth);
        if ((Flags & CharacterPrivateFlags.ARMOR_CHANGED) != 0) msg.Read(out Armor);
        if ((Flags & CharacterPrivateFlags.MAX_ARMOR_CHANGED) != 0) msg.Read(out MaxArmor);
        if ((Flags & CharacterPrivateFlags.WEAPONS_CHANGED) != 0) msg.ReadEnum(out HeldWeaponsFlags);
        if ((Flags & CharacterPrivateFlags.AMMO_CHANGED) != 0) msg.ReadEnum(out AmmoChangedFlags);

        return state;
    }
}