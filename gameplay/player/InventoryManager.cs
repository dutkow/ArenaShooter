using Godot;
using System;

[Flags]
public enum InventoryStateFlags : byte
{
    HELD_WEAPONS_CHANGED,
    AMMO_CHANGED,
    EQUIPPED_WEAPON_CHANGED,
}

public struct InventoryState
{
    public InventoryStateFlags Flags;

    public WeaponFlags HeldWeaponsFlags;
    public WeaponFlags AmmoChangedFlags;
    public byte[] Ammo;
    public byte EquippedWeaponIndex;

    public byte[] MaxAmmo; // not replicated
}

public class InventoryManager
{
    InventoryState State;

    private Character _character;

    public WeaponFlags HeldWeaponsFlags;

    private int _equippedWeaponIndex = -1;

    public Weapon[] Weapons = new Weapon[GameRules.Instance.Weapons.Count];

    private Weapon _currentWeapon;

    public Action<int> GainedWeapon;
    public Action<int> LostWeapon;

    public Action<int, int> AmmoChanged;
    public Action<int, int> MaxAmmoChanged;

    public Action<int> EquippedWeaponChanged;


    public void Initialize(Character character)
    {
        _character = character;
    }

    public void ShowFirstPerson()
    {
        _currentWeapon?.ShowFirstPerson();
    }
    public void ShowThirdPerson()
    {
        _currentWeapon?.ShowThirdPerson();
    }

    public void ProcessClientInput(InputFlags cmd)
    {
        HandleInput(cmd);
    }

    public void HandleInput(InputFlags cmd)
    {
        bool wantsPrimaryFire = cmd.HasFlag(InputFlags.FIRE_PRIMARY);

        if (wantsPrimaryFire)
        {
            _currentWeapon?.HandlePrimaryFirePressed();
        }
        else
        {
            _currentWeapon?.HandlePrimaryFireReleased();
        }
    }
    public void EquipWeapon(int weaponIndex)
    {
        if (weaponIndex == _equippedWeaponIndex || weaponIndex < 0 || weaponIndex >= GameRules.Instance.Weapons.Count)
        {
            return;
        }

        //WeaponFlags weaponFlag = (WeaponFlags)(1 << weaponIndex);

        var weaponToEquip = Weapons[weaponIndex];
        if (weaponToEquip == null)
        {
            weaponToEquip = new();
            Weapons[weaponIndex] = weaponToEquip;
            weaponToEquip.Initialize(_character, GameRules.Instance.Weapons[weaponIndex]);
        }
        // if is local

        weaponToEquip.ShowFirstPerson();

        // else show third person
        _equippedWeaponIndex = weaponIndex;
    }

    public void OnSpawned()
    {
        foreach (var startingWeaponData in GameRules.Instance.StartingWeapons)
        {
            AddWeapon(startingWeaponData.WeaponIndex);

            if (startingWeaponData.AmmoOverride >= 0)
            {
                SetAmmo(startingWeaponData.WeaponIndex, startingWeaponData.AmmoOverride);
            }
            else
            {
                if (GameRules.Instance.Weapons.Count > startingWeaponData.WeaponIndex)
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
        if (State.Ammo.Length > weaponIndex)
        {
            SetAmmo(weaponIndex, State.Ammo[weaponIndex] + amount);
        }
    }

    public void SubtractAmmo(int weaponIndex, int amount)
    {
        if (State.Ammo.Length > weaponIndex)
        {
            SetAmmo(weaponIndex, State.Ammo[weaponIndex] - amount);
        }
    }

    public void SetAmmo(int weaponIndex, int amount)
    {
        if (amount >= 0)
        {
            State.Ammo[weaponIndex] = (byte)amount;
            AmmoChanged?.Invoke(weaponIndex, amount);
        }
    }

    public void SetMaxAmmo(int weaponIndex, int amount)
    {
        if (amount >= 0 && State.MaxAmmo.Length > weaponIndex)
        {
            State.MaxAmmo[weaponIndex] = (byte)amount;
            MaxAmmoChanged?.Invoke(weaponIndex, amount);
        }
    }

    public void SetEquippedWeapon(int weaponIndex)
    {
        if (State.EquippedWeaponIndex != weaponIndex)
        {
            State.EquippedWeaponIndex = (byte)weaponIndex;
            EquippedWeaponChanged?.Invoke(weaponIndex);
        }
    }
}
