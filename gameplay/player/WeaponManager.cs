using Godot;
using System;

public class WeaponManager
{
    public WeaponFlags HeldWeaponsFlags;

    public int EquippedWeaponIndex;

    public Weapon[] Weapons = new Weapon[WeaponConstants.TOTAL_WEAPON_SLOTS];

    private Weapon _currentWeapon;

    public void ChangeWeapon(int weaponIndex)
    {
        if (weaponIndex == EquippedWeaponIndex || weaponIndex < 0 || weaponIndex >= WeaponConstants.TOTAL_WEAPON_SLOTS)
        {
            return;
        }

        WeaponFlags weaponFlag = (WeaponFlags)(1 << weaponIndex);

        if ((HeldWeaponsFlags & weaponFlag) != 0)
        {
            EquipWeapon(weaponIndex);
        }
    }

    public void EquipWeapon(int weaponIndex)
    {
        if (weaponIndex >= GameRules.Instance.Weapons.Count)
        {
            return;
        }

        PackedScene weaponScene = GameRules.Instance.Weapons[weaponIndex].FirstPersonScene;
        if(weaponScene == null)
        {
            return;
        }

        if (Weapons[weaponIndex] == null)
        {
            var weapon = (Weapon)weaponScene.Instantiate();
            Weapons[weaponIndex] = weapon;
        }
        else
        {
            // fetch the weapon, set it visible, etc.
        }
        //WeaponSlot.AddChild(weapon);

        // anims or whatever

        //
        EquippedWeaponIndex = weaponIndex;
    }
}
