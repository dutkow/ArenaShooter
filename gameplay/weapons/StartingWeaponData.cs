using Godot;
using System;

[GlobalClass]
public partial class StartingWeaponData : Resource
{
    [Export] public int WeaponIndex = 0;
    [Export] public int AmmoOverride = -1; // If left as negative one, the default set in weapon data is used
}
