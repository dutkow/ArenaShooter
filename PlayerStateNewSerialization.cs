
using System;

[Flags]
public enum PublicPlayerFlags : ushort
{
    NONE = 0,

    KILLS = 1 << 1,
    DEATHS = 1 << 2,

    IS_ALIVE = 1 << 3,

    POSITION = 1 << 4,
    YAW = 1 << 5,
    PITCH = 1 << 6,

    EQUIPPED_WEAPON = 1 << 7,

    FIRED_PRIMARY = 1 << 8,
    FIRED_SECONDARY = 1 << 9,
}



[Flags]
public enum PrivatePlayerFlags : uint
{
    NONE = 0,

    HEALTH = 1 << 0,
    MAX_HEALTH = 1 << 1,

    ARMOR = 1 << 2,
    MAX_ARMOR = 1 << 3,

    WEAPON_1 = 1 << 4,
    WEAPON_2 = 1 << 5,
    WEAPON_3 = 1 << 6,
    WEAPON_4 = 1 << 7,
    WEAPON_5 = 1 << 8,
    WEAPON_6 = 1 << 9,
    WEAPON_7 = 1 << 10,
    WEAPON_8 = 1 << 11,
    WEAPON_9 = 1 << 12,
    WEAPON_TOTAL_WEAPON_SLOTS = 1 << 13,

    AMMO_WEAPON_1 = 1 << 14,
    AMMO_WEAPON_2 = 1 << 15,
    AMMO_WEAPON_3 = 1 << 16,
    AMMO_WEAPON_4 = 1 << 17,
    AMMO_WEAPON_5 = 1 << 18,
    AMMO_WEAPON_6 = 1 << 19,
    AMMO_WEAPON_7 = 1 << 20,
    AMMO_WEAPON_8 = 1 << 21,
    AMMO_WEAPON_9 = 1 << 22,
    AMMO_WEAPON_TOTAL_WEAPON_SLOTS = 1 << 23,
}


public partial class PublicPlayerState
{
    internal void Add(Message msg)
    {
        msg.Add(PlayerID);
        msg.AddEnum(Flags);

        if ((Flags & PublicPlayerFlags.KILLS) != 0) msg.Add(Kills);
        if ((Flags & PublicPlayerFlags.DEATHS) != 0) msg.Add(Deaths);

        if ((Flags & PublicPlayerFlags.IS_ALIVE) != 0)
        {
            msg.Add(IsAlive);

            if (IsAlive)
            {
                if ((Flags & PublicPlayerFlags.POSITION) != 0) msg.Add(Position);
                if ((Flags & PublicPlayerFlags.YAW) != 0) msg.Add(Yaw);
                if ((Flags & PublicPlayerFlags.PITCH) != 0) msg.Add(Pitch);
                if ((Flags & PublicPlayerFlags.EQUIPPED_WEAPON) != 0) msg.AddEnum(EquippedWeapon);
            }
        }
    }

    internal void Write(Message msg)
    {
        msg.Write(PlayerID);
        msg.WriteEnum(Flags);

        if ((Flags & PublicPlayerFlags.KILLS) != 0) msg.Write(Kills);
        if ((Flags & PublicPlayerFlags.DEATHS) != 0) msg.Write(Deaths);

        if ((Flags & PublicPlayerFlags.IS_ALIVE) != 0)
        {
            msg.Write(IsAlive);

            if (IsAlive)
            {
                if ((Flags & PublicPlayerFlags.POSITION) != 0) msg.Write(Position);
                if ((Flags & PublicPlayerFlags.YAW) != 0) msg.Write(Yaw);
                if ((Flags & PublicPlayerFlags.PITCH) != 0) msg.Write(Pitch);
                if ((Flags & PublicPlayerFlags.EQUIPPED_WEAPON) != 0) msg.WriteEnum(EquippedWeapon);
            }
        }
    }

    internal static PublicPlayerState Read(Message msg)
    {
        var state = new PublicPlayerState();

        msg.Read(out state.PlayerID);
        msg.ReadEnum(out state.Flags);

        if ((state.Flags & PublicPlayerFlags.KILLS) != 0) msg.Read(out state.Kills);
        if ((state.Flags & PublicPlayerFlags.DEATHS) != 0) msg.Read(out state.Deaths);

        if ((state.Flags & PublicPlayerFlags.IS_ALIVE) != 0)
        {
            msg.Read(out state.IsAlive);

            if (state.IsAlive)
            {
                if ((state.Flags & PublicPlayerFlags.POSITION) != 0) msg.Read(out state.Position);
                if ((state.Flags & PublicPlayerFlags.YAW) != 0) msg.Read(out state.Yaw);
                if ((state.Flags & PublicPlayerFlags.PITCH) != 0) msg.Read(out state.Pitch);
                if ((state.Flags & PublicPlayerFlags.EQUIPPED_WEAPON) != 0) msg.ReadEnum(out state.EquippedWeapon);
            }
        }

        return state;
    }
}

public partial class PrivatePlayerState
{
    public const int FIRST_WEAPON_BIT = 4;
    public const int TOTAL_WEAPON_SLOTS = 10;
    public const int FIRST_AMMO_BIT = 14;

    internal void Add(Message msg)
    {
        msg.AddEnum(Flags);

        if ((Flags & PrivatePlayerFlags.HEALTH) != 0) msg.Add(Health);
        if ((Flags & PrivatePlayerFlags.MAX_HEALTH) != 0) msg.Add(MaxHealth);
        if ((Flags & PrivatePlayerFlags.ARMOR) != 0) msg.Add(Armor);
        if ((Flags & PrivatePlayerFlags.MAX_ARMOR) != 0) msg.Add(MaxArmor);

        // Weapons (just presence, no data)
        for (int w = 0; w < TOTAL_WEAPON_SLOTS; w++)
        {
            if ((Flags & (PrivatePlayerFlags)(1 << (FIRST_WEAPON_BIT + w))) != 0)
                msg.Add((byte)1); // placeholder if needed
        }

        // Ammo (actual data)
        for (int w = 0; w < TOTAL_WEAPON_SLOTS; w++)
        {
            if ((Flags & (PrivatePlayerFlags)(1 << (FIRST_AMMO_BIT + w))) != 0)
                msg.Add(Ammo[w]);
        }
    }

    internal void Write(Message msg)
    {
        msg.WriteEnum(Flags);

        if ((Flags & PrivatePlayerFlags.HEALTH) != 0) msg.Write(Health);
        if ((Flags & PrivatePlayerFlags.MAX_HEALTH) != 0) msg.Write(MaxHealth);
        if ((Flags & PrivatePlayerFlags.ARMOR) != 0) msg.Write(Armor);
        if ((Flags & PrivatePlayerFlags.MAX_ARMOR) != 0) msg.Write(MaxArmor);

        // Weapons (just presence)
        for (int w = 0; w < TOTAL_WEAPON_SLOTS; w++)
        {
            if ((Flags & (PrivatePlayerFlags)(1 << (FIRST_WEAPON_BIT + w))) != 0)
                msg.Write((byte)1);
        }

        // Ammo
        for (int w = 0; w < TOTAL_WEAPON_SLOTS; w++)
        {
            if ((Flags & (PrivatePlayerFlags)(1 << (FIRST_AMMO_BIT + w))) != 0)
                msg.Write(Ammo[w]);
        }
    }

    internal static PrivatePlayerState Read(Message msg)
    {
        msg.Read(out uint flagsRaw);
        var flags = (PrivatePlayerFlags)flagsRaw;

        var priv = new PrivatePlayerState { Flags = flags };

        if ((flags & PrivatePlayerFlags.HEALTH) != 0) msg.Read(out priv.Health);
        if ((flags & PrivatePlayerFlags.MAX_HEALTH) != 0) msg.Read(out priv.MaxHealth);
        if ((flags & PrivatePlayerFlags.ARMOR) != 0) msg.Read(out priv.Armor);
        if ((flags & PrivatePlayerFlags.MAX_ARMOR) != 0) msg.Read(out priv.MaxArmor);

        // Weapons
        for (int w = 0; w < TOTAL_WEAPON_SLOTS; w++)
        {
            if ((flags & (PrivatePlayerFlags)(1 << (FIRST_WEAPON_BIT + w))) != 0)
            {
                msg.Read(out byte _); // discard
            }
        }

        // Ammo
        for (int w = 0; w < TOTAL_WEAPON_SLOTS; w++)
        {
            if ((flags & (PrivatePlayerFlags)(1 << (FIRST_AMMO_BIT + w))) != 0)
                msg.Read(out priv.Ammo[w]);
        }

        return priv;
    }
}