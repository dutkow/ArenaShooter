using Godot;
using System;
using System.Linq;
using System.Xml.Linq;


public struct PlayerStats
{
    public ushort Kills;
    public ushort Deaths;
}

public struct CharacterMoveState
{
    public Vector3 Position;
    public float Yaw;
    public float Pitch;
    public Vector3 Velocity;
}

/// <summary>
/// Contains replicated player data
/// </summary>
public class PlayerState()
{
    // PERSISTENT PLAYER DATA

    // Player info. Replicated reliably to all on init and change, not included in world snapshots
    public string Name;
    public byte ID;

    // Player stats
    public short Kills; // signed for kill penalties to allow negative values (i.e., suicide or team-killing can result in negative kills)
    public ushort Deaths;
    public ushort Ping;

    // CHARACTER STATE
    public Character Character; // Character instance. Not replicated, just assigned on spawn for convenience

    // Replicated to all
    public bool IsSpawned; // since health is not sent to all, replicated to avoid needing to send spawn data (1) reliably, to existing players or (2) in the initial match state sent to joining players
    public CharacterMoveState MoveState;
    public byte EquippedWeaponIndex;

    // Replicated only to owner (and maybe eventually to clients which are viewing this character, although that would require a small delay to synchronize when a client spectates a new player)
    public byte Health;
    public byte MaxHealth;
    public byte Armor;
    public byte MaxArmor;

    public WeaponFlags HeldWeaponsFlags;
    public WeaponFlags AmmoChangedFlags;
    public byte[] Ammo;

    // Not replicated, purely initialized from Game Rules once as this is not a dynamic variable so clients can initialize. Eventually, will need to include game rules in initial match state
    public byte[] MaxAmmo; 

    // EVENTS
    public Action<string> NameChanged;

    public Action<int> KillsChanged;
    public Action<int> DeathsChanged;
    public Action<int> PingChanged;

    public Action<bool> IsSpawnedChanged;

    public Action<int> HealthChanged;
    public Action<int> MaxHealthChanged;

    public Action<int> ArmorChanged;
    public Action<int> MaxArmorChanged;

    public Action<int> GainedWeapon;
    public Action<int> LostWeapon;

    public Action<int, int> AmmoChanged;
    public Action<int, int> MaxAmmoChanged;


    // METHODS
    public void Initialize(byte id)
    {
        SetID(id);

        int numWeapons = GameRules.Instance.Weapons.Count;

        Ammo = new byte[numWeapons];
        MaxAmmo = new byte[numWeapons];

        for(int i = 0; i < numWeapons; ++i)
        {
            SetMaxAmmo(i, GameRules.Instance.MaxAmmoAmounts[i]);
        }
    }

    public void SetID(byte id)
    {
        ID = id;
    }

    public void SetName(string name)
    {
        if (Name != name)
        {
            Name = name;
            PlayerNameChanged?.Invoke(name);
        }
    }

    public void AddKill()
    {
        Kills++;

        KillsChanged?.Invoke(Kills);
    }

    public void SubtractKill() // i.e., penalty for suicide or team kill
    {
        Kills--;

        KillsChanged?.Invoke(Kills);
    }

    public void AddDeath()
    {
        Deaths++;

        DeathsChanged?.Invoke(Deaths);
    }

    public void SetPing(ushort ping)
    {
        if (Ping != ping)
        {
            Ping = ping;
            PingChanged?.Invoke(ping);
        }
    }

    public void SetIsSpawned(bool isSpawned)
    {
        if (IsSpawned != isSpawned)
        {
            IsSpawned = isSpawned;
            IsSpawnedChanged?.Invoke(IsSpawned);
        }
    }


    public void SetPosition(Vector3 position)
    {
        MoveState.Position = position;
    }


    public void SetVelocity(Vector3 velocity)
    {
        MoveState.Velocity = velocity;
    }


    public void SetYaw(float yaw)
    {
        MoveState.Yaw = yaw;
    }

    public void SetPitch(float pitch)
    {
        MoveState.Pitch = pitch;
    }

    public void AddWeapon(int weaponIndex)
    {
        if ((HeldWeaponsFlags & (WeaponFlags)(1 << weaponIndex)) == 0)
        {
            HeldWeaponsFlags |= (WeaponFlags)(1 << weaponIndex);
            GainedWeapon?.Invoke(weaponIndex);
        }
    }

    public void RemoveWeapon(int weaponIndex)
    {
        if ((HeldWeaponsFlags & (WeaponFlags)(1 << weaponIndex)) != 0)
        {
            HeldWeaponsFlags &= ~(WeaponFlags)(1 << weaponIndex);

            LostWeapon?.Invoke(weaponIndex);
        }
    }

    public void AddAmmo(int weaponIndex, int amount)
    {
        if (Ammo.Length > weaponIndex)
        {
            SetAmmo(weaponIndex, Ammo[weaponIndex] + amount);
        }
    }

    public void SubtractAmmo(int weaponIndex, int amount)
    {
        if (Ammo.Length > weaponIndex)
        {
            SetAmmo(weaponIndex, Ammo[weaponIndex] - amount);
        }
    }

    public void SetAmmo(int weaponIndex, int amount)
    {
        if (amount >= 0)
        {
            Ammo[weaponIndex] = (byte)amount;
            AmmoChanged?.Invoke(weaponIndex, amount);
        }
    }

    public void SetMaxAmmo(int weaponIndex, int amount)
    {
        if(amount >= 0 && MaxAmmo.Length > weaponIndex)
        {
            MaxAmmo[weaponIndex] = (byte)amount;
            MaxAmmoChanged?.Invoke(weaponIndex, amount);
        }
    }

    public void SetEquippedWeapon(int weaponIndex)
    {
        if(EquippedWeaponIndex != weaponIndex)
        {
            EquippedWeaponIndex = (byte)weaponIndex;
            EquippedWeaponChanged?.Invoke(weaponIndex);
        }
    }

    public void AddHealth(int amount)
    {
        SetHealth(Health + amount);
    }

    public void SubtractHealth(int amount)
    {
        SetHealth(Health - amount);
    }

    public void SetHealth(int value)
    {
        value = Math.Clamp(value, 0, MaxHealth);

        if (Health != value)
        {
            Health = (byte)value;

            HealthChanged?.Invoke(value);

            if (Health == 0)
            {
                HandleDeath();
            }
        }
    }

    public void SetMaxHealth(int value)
    {
        value = Math.Max(0, value);

        if (MaxHealth != value)
        {
            MaxHealth = (byte)value;
            MaxHealthChanged?.Invoke(value);
        }
    }

    public void AddArmor(int amount)
    {
        SetArmor(Armor + amount);
    }

    public void SubtractArmor(int amount)
    {
        SetArmor(Armor - amount);
    }

    public void SetArmor(int value)
    {
        value = Math.Clamp(value, 0, MaxArmor);

        if (Armor != value)
        {
            Armor = (byte)value;
            ArmorChanged?.Invoke(value);
        }
    }

    public void AddMaxArmor(int amount)
    {
        SetMaxArmor(MaxArmor + amount);
    }

    public void SubtractMaxArmor(int amount)
    {
        SetMaxArmor(MaxArmor - amount);
    }

    public void SetMaxArmor(int value)
    {
        value = Math.Max(0, value);

        if (CharacterPrivateState.MaxArmor != value)
        {
            CharacterPrivateState.MaxArmor = (byte)value;
            MaxArmorChanged?.Invoke(value);
        }
    }

    public void SetPing(int value)
    {
        if (Ping != value)
        {
            Ping = (ushort)value;
            PingChanged?.Invoke(value);
        }
    }

    public void HandleSpawn(Character character)
    {
        Character = character;

        SetIsSpawned(true);

        MoveState.Position = character.GlobalPosition;
        MoveState.Velocity = Vector3.Zero;
        MoveState.Yaw = character.GlobalRotation.Y;
        MoveState.Pitch = 0.0f;

        SetMaxHealth(GameRules.Instance.MaxHealth);
        SetHealth(GameRules.Instance.StartingHealth);

        SetMaxArmor(GameRules.Instance.MaxArmor);
        SetArmor(GameRules.Instance.MaxArmor);

        // Initialize starting weapons from game rules
        foreach(var startingWeaponData in GameRules.Instance.StartingWeapons)
        {
            AddWeapon(startingWeaponData.WeaponIndex);

            if (startingWeaponData.AmmoOverride >= 0)
            {
                SetAmmo(startingWeaponData.WeaponIndex, startingWeaponData.AmmoOverride);
            }
            else
            {
                if(GameRules.Instance.Weapons.Count > startingWeaponData.WeaponIndex)
                {
                    var weaponData = GameRules.Instance.Weapons[startingWeaponData.WeaponIndex];
                    if (weaponData != null)
                    {
                        SetAmmo(startingWeaponData.WeaponIndex, weaponData.DefaultStartingAmmo);
                    }
                }
            }
        }

        SetEquippedWeapon(GameRules.Instance.StartingWeaponIndex);
    }

    public void HandleDeath()
    {
        AddDeath();
        SetIsSpawned(false);
    }


    public PlayerInfo PlayerInfo;

    public CharacterPublicState CharacterPublicState = new();
    public CharacterPrivateState CharacterPrivateState = new();

    public PlayerStateFlags Flags;

    public PlayerStats Stats;

 

    public Action<string> PlayerNameChanged;
    public Action PlayerLeft;

    public Action<int, int> WeaponAmmoChanged;

    public Action<int> EquippedWeaponChanged;





    public void ClearFlags()
    {
        Flags = 0;
        if (Character != null)
        {
            Character.ClearFlags();
        }
    }

    internal void Add(Message msg, byte clientPlayerID, bool forceFull = false)
    {
        msg.Add(PlayerInfo.PlayerID);

        msg.AddEnum(Flags);


        msg.Add(PlayerInfo.PlayerName);

        msg.Add(Stats.Kills);
        msg.Add(Stats.Deaths);

        msg.Add(Ping);
        msg.Add(IsSpawned);

        CharacterPublicState.Add(msg);
        CharacterPrivateState.Add(msg);
    }

    internal void Write(Message msg, byte clientPlayerID)
    {
        msg.Write(PlayerInfo.PlayerID);

        msg.Write(PlayerInfo.PlayerName);

        msg.Write(Stats.Kills);
        msg.Write(Stats.Deaths);

        msg.Write(Ping);
        msg.Write(IsSpawned);

        CharacterPublicState.Write(msg);
        CharacterPrivateState.Write(msg);
    }

    internal void Read(Message msg, byte clientPlayerID)
    {
        msg.Read(out PlayerInfo.PlayerID);


        msg.Read(out PlayerInfo.PlayerName);

        msg.Read(out Stats.Kills);
        msg.Read(out Stats.Deaths);
        msg.Read(out Ping);
        msg.Read(out IsSpawned);

        CharacterPublicState.Read(msg);
        CharacterPrivateState.Read(msg);
    }
}