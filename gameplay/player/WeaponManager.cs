using Godot;
using System;

public class WeaponManager
{
    public WeaponFlags HeldWeaponsFlags;

    public void ChangeWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex > WeaponConstants.TOTAL_WEAPON_SLOTS - 1) return;

    }
}
