using Godot;
using Godot.Collections;
using System;

public enum FireMode
{
    SEMI_AUTO,   // click = one shot
    FULL_AUTO,   // hold = repeated fire
    BURST,       // optional later
}

public partial class Weapon : ITickable
{
    public byte OwnerPlayerID;

    public Character Character;

    private FireMode _fireMode = FireMode.FULL_AUTO;

    private bool _readyToFire => _cooldownTimer <= 0.0f;

    private bool _isTriggerHeld = false;
    private bool _hasFiredSincePress = false;
    private float _cooldownTimer = 0f;

    [Export] public Node3D _weaponScene;
    [Export] public float PrimaryFireCooldown = 1.0f;
    [Export] PackedScene _projectileScene;

    Node3D _weaponFP;
    Node3D _weaponTP;

    public bool IsPredictingProjectiles { get; private set; } = true;

    public bool FiredPredictedProjectile;

    public void Initialize(Character character, WeaponData weaponData)
    {
        Character = character;

        _weaponFP = (Node3D)weaponData.FirstPersonScene.Instantiate();
        _weaponTP = (Node3D)weaponData.ThirdPersonScene.Instantiate();

        _weaponFP.Visible = true;
        _weaponTP.Visible = false; // will cusotmize later by net role etc.

        Character.WeaponSocketFP.AddChild(_weaponFP);
        Character.WeaponSocketTP.AddChild(_weaponTP);
    }

    public void Tick(double delta)
    {
        if(Character.IsAuthority || IsPredictingProjectiles)
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= (float)delta;
            }
        }
    }

    public virtual void HandlePrimaryFirePressed()
    {
        _isTriggerHeld = true;

        if (_fireMode == FireMode.SEMI_AUTO && !_hasFiredSincePress)
        {
            TryFire();
            _hasFiredSincePress = true;
        }
    }

    public virtual void HandlePrimaryFireReleased()
    {
        _isTriggerHeld = false;
        _hasFiredSincePress = false;
    }

    public void Tick(double delta, Vector3 origin, Vector3 direction)
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
        if (!_readyToFire)
        {
            return;
        }

        if (_projectileScene == null)
        {
            return;
        }

        Vector3 spawnPosition = origin + direction.Normalized() * 2.0f;

        ProjectileSpawnData spawnData = new();
        spawnData.ownerPlayerID = OwnerPlayerID;
        spawnData.SpawnLocation = spawnPosition;
        spawnData.SpawnDirection = direction;

        Fire(spawnData);
    }

    public void Fire(ProjectileSpawnData spawnData)
    {
        if (!Character.IsAuthority && IsPredictingProjectiles)
        {
            ClientProjectileManager.Instance?.SpawnPredictedProjectile(spawnData);
            FiredPredictedProjectile = true;
        }

        if(Character.IsAuthority)
        {
            spawnData.ServerTickOnSpawn = MatchState.Instance.CurrentTick;
            ServerProjectileManager.Instance.CreateProjectilePendingSpawn(spawnData, IsPredictingProjectiles);
        }

        _cooldownTimer = PrimaryFireCooldown;
    }

    public void ShowFirstPerson()
    {
        HideThirdPerson();
        _weaponFP.Show();
    }

    public void ShowThirdPerson()
    {
        HideFirstPerson();
        _weaponTP.Show();
    }

    public void HideFirstPerson()
    {
        _weaponFP.Hide();
    }

    public void HideThirdPerson()
    {
        _weaponTP.Hide();
    }
}