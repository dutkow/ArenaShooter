using Godot;
using System;

public partial class Weapon : Node3D
{
    [Export] public MeshInstance3D FirstPersonWeaponMesh;

    [Export] public float PrimaryFireCooldown = 0.5f;
    protected float _cooldown = 0.0f;

    [Export] PackedScene _projectileScene;

    public override void _Process(double delta)
    {
        if (_cooldown > 0f)
        {
            _cooldown -= (float)delta;
        }
    }

    public void TryPrimaryFire(Vector3 origin, Vector3 direction)
    {
        if (_cooldown <= 0f)
        {
            PrimaryFire(origin, direction);
            _cooldown = PrimaryFireCooldown;
        }
    }

    public void PrimaryFire(Vector3 origin, Vector3 direction)
    {
        //SpawnProjectile(origin, direction);

        ProjectileManager.Instance.ServerSpawnProjectile(1, ProjectileType.DEFAULT, origin, direction);
    }


    public void SpawnProjectile(Vector3 origin, Vector3 direction)
    {
        var newProjectile = (Projectile)_projectileScene.Instantiate();
        Level.Instance.AddChild(newProjectile);

        newProjectile.Initialize(origin, direction);
    }

}
