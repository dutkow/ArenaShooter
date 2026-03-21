using Godot;
using Godot.Collections;
using System;

public partial class WeaponBar : BoxContainer
{
    [Export] PackedScene _weaponBarEntryScene;

    public Dictionary<int, WeaponBarEntry> _weaponBarEntries = new();

    public void AssignToPlayerState(PlayerStateOld playerState)
    {
        playerState.GainedWeapon += OnGainedWeapon;
    }

    public void AddWeaponBarEntry()
    {
        var newEntry = (WeaponBarEntry)_weaponBarEntryScene.Instantiate();
        AddChild(newEntry);
    }

    public void RemoveWeaponBarEntry(int weaponIndex)
    {
        if (_weaponBarEntries.TryGetValue(weaponIndex, out var weaponBarEntry))
        {
            RemoveChild(weaponBarEntry);
        }
    }

    public void OnGainedWeapon(int weaponIndex)
    {
        if(_weaponBarEntries.TryGetValue(weaponIndex, out var weaponBarEntry))
        {
            weaponBarEntry.OnGainedWeapon();
        }
    }

    public void OnWeaponAmmoChanged(int weaponIndex, int ammoAmount)
    {
        if (_weaponBarEntries.TryGetValue(weaponIndex, out var weaponBarEntry))
        {
            weaponBarEntry.OnAmmoChanged(ammoAmount);
        }
    }
}
