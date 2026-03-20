using Godot;
using System;

public class WeaponManager
{
    private Character _character;

    public WeaponFlags HeldWeaponsFlags;

    private int _equippedWeaponIndex = -1;

    public Weapon[] Weapons = new Weapon[GameRules.Instance.Weapons.Count];

    private Weapon _currentWeapon;

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
}
