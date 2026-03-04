using Godot;
using System;

public enum FireMode
{
    SEMI_AUTO,   // click = one shot
    FULL_AUTO,   // hold = repeated fire
    BURST,       // optional later
}

public partial class Weapon : Node3D
{
    private FireMode _fireMode = FireMode.FULL_AUTO;

    private bool _readyToFire => _cooldownTimer <= 0.0f;

    private bool _isTriggerHeld = false;
    private bool _hasFiredSincePress = false;
    private float _cooldownTimer = 0f;

    [Export] public MeshInstance3D FirstPersonWeaponMesh;
    [Export] public float PrimaryFireCooldown = 0.5f;
    [Export] PackedScene _projectileScene;

    public override void _Process(double delta)
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= (float)delta;
    }

    public void StartPrimaryFire()
    {
        _isTriggerHeld = true;

        if (_fireMode == FireMode.SEMI_AUTO && !_hasFiredSincePress)
        {
            TryFire();
            _hasFiredSincePress = true;
        }
    }

    public void StopPrimaryFire()
    {
        _isTriggerHeld = false;
        _hasFiredSincePress = false;
    }

    public void HandleInput(InputCommand cmd)
    {
        bool wantsPrimaryFire = cmd.HasFlag(InputCommand.FIRE_PRIMARY);

        if (wantsPrimaryFire)
        {
            StartPrimaryFire();
        }
        else
        {
            StopPrimaryFire();
        }
    }


    public void TickWeapon(double delta, Vector3 origin, Vector3 direction)
    {
        if (_cooldownTimer > 0.0f)
        {
            _cooldownTimer -= (float)delta;
        }

        if (!_isTriggerHeld)
        {
            return;
        }

        if (_fireMode == FireMode.FULL_AUTO)
        {
            TryFire(origin, direction);
        }
    }

    private void TryFire(Vector3 origin = default, Vector3 direction = default)
    {
        if(_readyToFire)
        {
            if (_projectileScene != null)
            {
                ProjectileManager.Instance.ServerSpawnProjectile(1, ProjectileType.DEFAULT, origin, direction);
            }
            _cooldownTimer = PrimaryFireCooldown;
        }
    }
}