using Godot;
using System;

public enum FireMode
{
    SEMI_AUTO,   // click = one shot
    FULL_AUTO,   // hold = repeated fire
    BURST,       // optional later
}

public struct FireTransform
{
    public Vector3 Position;
    public Vector3 Direction;
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

    public void Tick(double delta, Vector3 position, Vector3 direction)
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
            TryFire(position, direction);
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
        spawnData.SpawnRotation = direction;

        Fire(spawnData);
    }

    public static FireTransform GetFireTransform(Vector3 characterPosition, float yawDegrees, float pitchDegrees)
    {
        // Convert yaw/pitch to a forward vector
        Vector3 direction = new Vector3(
            Mathf.Cos(Mathf.DegToRad(pitchDegrees)) * Mathf.Sin(Mathf.DegToRad(yawDegrees)),
            Mathf.Sin(Mathf.DegToRad(pitchDegrees)),
            Mathf.Cos(Mathf.DegToRad(pitchDegrees)) * Mathf.Cos(Mathf.DegToRad(yawDegrees))
        ).Normalized();

        // Apply weapon muzzle offset if needed
        // You can adjust 2.0f to whatever the weapon's barrel length / offset is
        Vector3 spawnPosition = characterPosition + direction * 2.0f;

        return new FireTransform
        {
            Position = spawnPosition,
            Direction = direction
        };
    }

    public void Fire(ProjectileSpawnData spawnData)
    {
        if (IsPredictingProjectiles)
        {
            ClientProjectileManager.Instance?.SpawnPredictedProjectile(spawnData);
            FiredPredictedProjectile = true;
        }

        ServerProjectileManager.Instance?.CreateProjectilePendingSpawn(spawnData, IsPredictingProjectiles);

        _cooldownTimer = PrimaryFireCooldown;
    }


}