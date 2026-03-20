using Godot;
using System;

public enum WeaponFireType : byte
{
    HITSCAN,
    PROJECTILE,
}

[GlobalClass]
public partial class WeaponData : Resource
{
    [ExportCategory("Visual")]
    [Export] public PackedScene FirstPersonScene;
    [Export] public PackedScene ThirdPersonScene;
    [Export] public Mesh PickupMesh;
    [Export] public Mesh AmmoPickupMesh;

    [ExportCategory("Interface")]
    [Export] public string Name = "UNNAMED WEAPON";
    [Export] public Texture2D WeaponIcon;
    [Export] public Texture2D AmmoIcon;

    [ExportCategory("Weapon Data")]
    [Export] public int Damage = 10;
    [Export] public float FireInterval = 1.0f;
    [Export] public WeaponFireType FireType;
    [Export] FireMode FireMode = FireMode.FULL_AUTO;
    [Export] public int DefaultStartingAmmo = 50;
    [Export] public int MaxAmmo = 150;
    [Export] public int DefaultPickupAmmo = 25;
    [Export] public int BulletsPerFire = 1;

    // Only used with projectile weapons
    [ExportCategory("Projectiles")]
    [Export] public PackedScene ProjectileScene;
    [Export] public float ProjectileSpeed = 50.0f;

    // Only used with hitscan weapons
    [ExportCategory("Hitscan")]
    [Export] public bool PenetratingShot = false;

}
