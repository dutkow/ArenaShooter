using Godot;
using System;

public enum FireMode
{
    SEMI_AUTO,   // click = one shot
    FULL_AUTO,   // hold = repeated fire
    BURST,       // optional later
}

public partial class Weapon : Entity
{
    public byte OwnerPlayerID;

    private FireMode _fireMode = FireMode.FULL_AUTO;

    private bool _readyToFire => _cooldownTimer <= 0.0f;

    private bool _isTriggerHeld = false;
    private bool _hasFiredSincePress = false;
    private float _cooldownTimer = 0f;

    [Export] public Node3D FirstPersonWeaponMesh;
    [Export] public float PrimaryFireCooldown = 1.0f;
    [Export] PackedScene _projectileScene;

    public bool IsPredictingProjectiles { get; private set; } = true;

    public bool FiredPredictedProjectile;

    public override void _Process(double delta)
    {
        base._Process(delta);

        if(IsAuthority || IsPredictingProjectiles)
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= (float)delta;
            }
        }
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

    public void ProcessClientInput(ClientCommandMask cmd)
    {
        HandleInput(cmd);
    }

    public void HandleInput(ClientCommandMask cmd)
    {
        bool wantsPrimaryFire = cmd.HasFlag(ClientCommandMask.FIRE_PRIMARY);

        if (wantsPrimaryFire)
        {
            StartPrimaryFire();
        }
        else
        {
            StopPrimaryFire();
        }
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
        if (!IsAuthority && IsPredictingProjectiles)
        {
            ClientProjectileManager.Instance?.SpawnPredictedProjectile(spawnData);
            FiredPredictedProjectile = true;
        }

        if(NetworkSession.Instance.IsServer)
        {
            spawnData.ServerTickOnSpawn = MatchState.Instance.CurrentTick;
            ServerProjectileManager.Instance.CreateProjectilePendingSpawn(spawnData, IsPredictingProjectiles);
        }

        _cooldownTimer = PrimaryFireCooldown;
    }
}