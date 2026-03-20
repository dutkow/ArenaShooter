using Godot;
using System;

public enum WeaponFireType : byte
{
    HITSCAN,
    PROJECTILE,
}

public partial class WeaponData : Resource
{
    [Export] public PackedScene FirstPersonScene;
    [Export] public PackedScene ThirdPersonScene;

    [Export] public string WeaponName = "SHOTGUN";
    [Export] public int Damage = 10;
    [Export] public float FireInterval = 1.0f;
    [Export] public WeaponFireType FireType;
    [Export] FireMode FireMode = FireMode.FULL_AUTO;
    

}


public partial class ProjectileWeaponData : WeaponData
{
    [Export] public PackedScene ProjectileScene;
    [Export] public float ProjectileSpeed;
}


public partial class HitscanWeaponData : WeaponData
{
    [Export] public bool PenetratingShot = false;
}