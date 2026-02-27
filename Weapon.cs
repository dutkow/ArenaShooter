using Godot;
using System;

public partial class Weapon : Node3D
{
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
        else
        {
            GD.Print("cannot fire, cooldown active");
        }
    }

    public void PrimaryFire(Vector3 origin, Vector3 direction)
    {
        GD.Print("fire!");
        SpawnProjectile(origin, direction);
    }


    public void SpawnProjectile(Vector3 origin, Vector3 direction)
    {
        var newProjectile = (Projectile)_projectileScene.Instantiate();
        newProjectile.Initialize(origin, direction);
        GetTree().Root.AddChild(newProjectile);
    }

}
