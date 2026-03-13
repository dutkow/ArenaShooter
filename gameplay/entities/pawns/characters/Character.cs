using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;


public partial class Character : Pawn, IDamageable
{
    CharacterMoveMode _mode;


    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;


    [Export] public Camera3D Camera; // assign in editor
    [Export] public float MouseSensitivity = 0.1f;

    List<ClientInputCommand> _unacknowledgedClientInputs = new();


    const int REDUNDANT_INPUTS = 4;

    public float Yaw => GlobalRotation.Y;
    public float Pitch { get; private set; }


    private uint _lastProcessedClientTick;


    // Components
    [Export] MeshInstance3D _characterMesh;
    [Export] Node3D _thirdPersonWeaponMesh;
    [Export] Node3D _cameraPivot;
    [Export] Weapon _weapon;
    [Export] Node3D _visualContainer;

    [Export] Node3D _thirdPersonWeaponSocket;

    private Vector3 _visualContainerPosition;

    public CharacterMovement MovementComp { get; private set; } = new();

    public HealthComponent HealthComp { get; private set; } = new();

    private SortedDictionary<ushort, ClientInputCommand> _unprocessedClientInputs = new();


    private ClientInputCommand _lastProcessedClientCommand;

    private bool _useInterpolation = false;

    private bool _lookDirty;

    public CharacterPublicState PredictedPublicState = new();

    public override void _Ready()
    {
        base._Ready();

        Camera.Current = false;
        SetProcessInput(false);
        ShowThirdPersonView();

        Input.MouseMode = Input.MouseModeEnum.Captured;

        _visualContainerPosition = _visualContainer.Position;

        Area.Owner = this;
    }

    public void ApplyDamage(int damage)
    {
        HealthComp.ApplyDamage(damage);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        Vector3 dir = -Camera.GlobalTransform.Basis.Z;
        _weapon.Tick(delta, Camera.GlobalPosition, dir);
    }

    public void HandleSpawn(Vector3 spawnPosition, float yaw, float pitch)
    {
        GlobalPosition = spawnPosition;
        GlobalRotation = new Vector3(0.0f, yaw, 0.0f);


        MovementComp.Initialize(this);

    }

    public override void ApplyInput(ClientInputCommand cmd)
    {
        base.ApplyInput(cmd);


        if(IsAuthority)
        {
            MovementComp.Step(PlayerState.CharacterPublicState, cmd, NetworkConstants.SERVER_TICK_INTERVAL);
            UpdatePositionState(PlayerState.CharacterPublicState.Position);

        }
        else if(IsLocal)
        {
            MovementComp.Step(PredictedPublicState, cmd, NetworkConstants.SERVER_TICK_INTERVAL);

            PredictedPublicState.Flags |= CharacterPublicFlags.POSITION_CHANGED;
        }

        _weapon.HandleInput(cmd.Mask);

    }

    public override void ProcessClientInput(ClientInputCommand cmd)
    {
        base.ProcessClientInput(cmd);

        if (cmd.Mask.HasFlag(ClientCommandMask.LOOK))
        {
            UpdateRotationState(cmd.Look);
        }

        PlayerState.CharacterPublicState = MovementComp.Step(PlayerState.CharacterPublicState, cmd, NetworkConstants.SERVER_TICK_INTERVAL);

        UpdatePositionState(PlayerState.CharacterPublicState.Position);

        _weapon.ProcessClientInput(cmd.Mask);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        InterpolateMovement((float)delta);
    }

    /// <summary>
    /// Interface functions
    /// </summary>
    public override void OnPossessed(Controller controller)
    {
        base.OnPossessed(controller);

        //_healthComp.Death += OnDeath;

        Input.MouseMode = Input.MouseModeEnum.Captured;

        SetProcessInput(true);
        ShowFirstPersonView();

        Camera.Current = true;

        SetRole(NetworkRole.LOCAL);
        UIRoot.Instance.OnPossessedCharacter(this);

        if(MatchState.Instance.ConnectedPlayers.TryGetValue(ClientGame.Instance.LocalPlayerID, out var foundPlayerState))
        {
            PlayerState = foundPlayerState;
        }
        else
        {
            GD.Print($"player state is null");
        }

        _weapon.OwnerPlayerID = PlayerState.PlayerID;
        _weapon.SetIsAuthority(IsAuthority);

        if(!IsAuthority)
        {
            PredictedPublicState = PlayerState.CharacterPublicState.Copy();
            PredictedPublicState.Rotation = new Vector2(GlobalRotation.Y, 0.0f);
            GD.Print($"client copied pub state");
        }
    }

    public override void OnUnpossessed()
    {
        base.OnUnpossessed();
    }

    public bool IsAlive()
    {
        return HealthComp.IsAlive;
    }

    public void ShowFirstPersonView()
    {
        HideThirdPersonView();
        _weapon.FirstPersonWeaponMesh.Visible = true;
    }

    public void HideFirstPersonView()
    {
        _weapon.FirstPersonWeaponMesh.Visible = false;
    }

    public void ShowThirdPersonView()
    {
        HideFirstPersonView();

        _characterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        //_thirdPersonWeaponMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        _thirdPersonWeaponMesh.Visible = true;
    }

    public void HideThirdPersonView()
    {
        _characterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
        //_thirdPersonWeaponMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
        _thirdPersonWeaponMesh.Visible = false;

    }

    float LOCAL_SV_INTERP_RATE = 0.5f;
    float LOCAL_CL_INTERP_RATE = 0.5f;
    float REMOTE_CL_INTERP_RATE = 0.5f;

    public void InterpolateMovement(float delta)
    {
        if(_useInterpolation)
        {
            if (IsLocal)
            {
                InterpolatePosition(delta * LOCAL_SV_INTERP_RATE);
            }
            else
            {
                InterpolatePosition(delta * REMOTE_CL_INTERP_RATE);
                InterpolateYaw(delta * 10.0f);
                InterpolatePitch(delta * 10.0f);
            }
        }
        else
        {
            if(IsLocal)
            {
                if(IsAuthority)
                {
                    GlobalPosition = PlayerState.CharacterPublicState.Position;
                }
                else
                {
                    GlobalPosition = PredictedPublicState.Position;
                }
            }
            else
            {
                GlobalPosition = PlayerState.CharacterPublicState.Position;
                GlobalRotation = new Vector3(0.0f, PlayerState.CharacterPublicState.Rotation.X, 0.0f);
                _thirdPersonWeaponSocket.Rotation = new Vector3(PlayerState.CharacterPublicState.Rotation.Y, 0.0f, 0.0f);
            }
        }

    }

    public void InterpolatePosition(float interpSpeed)
    {

        var targetPosition = PlayerState.CharacterPublicState.Position + _visualContainerPosition;
        _visualContainer.GlobalPosition = _visualContainer.GlobalPosition.Lerp(targetPosition, LOCAL_SV_INTERP_RATE);
    }

    public void InterpolateYaw(float interpSpeed)
    {
        Vector3 rot = GlobalRotation;
        rot.Y = Mathf.LerpAngle(rot.Y, PlayerState.CharacterPublicState.Rotation.X, interpSpeed);
        GlobalRotation = rot;
    }

    public void InterpolatePitch(float interpSpeed)
    {
        Vector3 camRot = _thirdPersonWeaponMesh.GlobalRotation;
        camRot.X = Mathf.Lerp(camRot.X, PlayerState.CharacterPublicState.Rotation.Y, interpSpeed);
        _thirdPersonWeaponMesh.GlobalRotation = camRot;
    }

    /*
    public override void ApplySnapshot(CharacterSnapshot snapshot)
    {
        base.ApplySnapshot(snapshot);


        // Reset any values which haven't changed
        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.POSITION))
        {
            snapshot.Position = MovementComp.State.Position;        
        }

        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.VELOCITY))
        {
            snapshot.Velocity = MovementComp.State.Velocity;
        }

        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.YAW))
        {
            snapshot.Yaw = MovementComp.State.Yaw;
        }

        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.PITCH))
        {
            snapshot.Pitch = MovementComp.State.Pitch;
        }

        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.MOVE_MODE))
        {
            snapshot.MoveMode = MovementComp.State.MoveMode;
        }


        var snapshotMoveState = snapshot.GetMoveState();

        if (IsLocal)
        {
            var reconciledState = snapshotMoveState;

            foreach (var cmd in ClientGame.Instance.UnprocessedClientInputs)
            {
                reconciledState = MovementComp.Step(reconciledState, cmd, NetworkConstants.SERVER_TICK_INTERVAL, true);
            }

            ReconcileMoveState(reconciledState);
        }
        else
        {
            MovementComp.State = snapshotMoveState;
        }
    }*/


    public void ReconcileMoveState(CharacterPublicState authoritativeState)
    {
        Vector3 delta = PredictedPublicState.Position - authoritativeState.Position;

        // Thresholds
        const float SNAP_THRESHOLD_H = 2.0f;        // Horizontal snap (X/Z)
        const float SNAP_THRESHOLD_V = 2.0f;        // Vertical snap (Y)
        const float INTERP_THRESHOLD_H = 0.025f;      // Horizontal lerp start
        const float INTERP_THRESHOLD_V = 0.025f;     // Vertical lerp start

        // Lerp speeds
        const float INTERP_SPEED_H = 0.15f;
        const float INTERP_SPEED_V = 0.15f;

        Vector3 targetPos = authoritativeState.Position;
        Vector3 currentPos = PredictedPublicState.Position;

        Vector2 deltaXZ = new Vector2(delta.X, delta.Z);
        float distXZ = deltaXZ.Length();

        float deltaY = Math.Abs(delta.Y);

        GD.Print($"horizontal error: {distXZ}. position: {PredictedPublicState.Position}. predictedp osition {authoritativeState.Position}");

        // --- Horizontal correction ---
        if (distXZ > SNAP_THRESHOLD_H)
        {
            GD.Print($"snap correction horizontal, error {distXZ}");
            currentPos.X = targetPos.X;
            currentPos.Z = targetPos.Z;
        }
        else if (distXZ > INTERP_THRESHOLD_H)
        {
            GD.Print($"lerp correction horizontal, error {distXZ}");
            currentPos.X = Mathf.Lerp(currentPos.X, targetPos.X, INTERP_SPEED_H);
            currentPos.Z = Mathf.Lerp(currentPos.Z, targetPos.Z, INTERP_SPEED_H);
        }

        // --- Vertical correction ---
        if (deltaY > SNAP_THRESHOLD_V)
        {
            //GD.Print($"snap correction vertical, error {deltaY}");
            currentPos.Y = targetPos.Y;
        }
        else if (deltaY > INTERP_THRESHOLD_V)
        {
            //GD.Print($"lerp correction vertical, error {deltaY}");
            currentPos.Y = Mathf.Lerp(currentPos.Y, targetPos.Y, INTERP_SPEED_V);
        }

        // Apply the corrected position and velocity
        PredictedPublicState.Position = currentPos;
        PredictedPublicState.Velocity = authoritativeState.Velocity;
    }


    public override ClientInputCommand AddInput(ClientInputCommand cmd)
    {
        base.AddInput(cmd);

        if(_inputEnabled)
        {
            if (Input.IsActionPressed("move_forward")) cmd.Mask |= ClientCommandMask.FORWARD;
            if (Input.IsActionPressed("move_back")) cmd.Mask |= ClientCommandMask.BACKWARD;
            if (Input.IsActionPressed("move_left")) cmd.Mask |= ClientCommandMask.STRAFE_LEFT;
            if (Input.IsActionPressed("move_right")) cmd.Mask |= ClientCommandMask.STRAFE_RIGHT;
            if (Input.IsActionPressed("jump")) cmd.Mask |= ClientCommandMask.JUMP;
            if (Input.IsActionPressed("primary_fire")) cmd.Mask |= ClientCommandMask.FIRE_PRIMARY;

            if (_lookDirty)
            {
                cmd.Mask |= ClientCommandMask.LOOK;
                cmd.Look = PredictedPublicState.Rotation;
                _lookDirty = false;
            }
        }

        if (MovementComp.WasLaunched)
        {
            cmd.Mask |= ClientCommandMask.WAS_LAUNCHED;
            cmd.LaunchVelocity = MovementComp.LaunchVector;
            MovementComp.WasLaunched = false;
        }

        if (_weapon.FiredPredictedProjectile)
        {
            cmd.Mask |= ClientCommandMask.FIRED_PREDICTED_PROJECTILE;
            cmd.PredictedProjectileClientID = ClientProjectileManager.Instance.GetNextAvailableClientProjectileID();
            _weapon.FiredPredictedProjectile = false;
        }

        MovementComp.LaunchVector = Vector3.Zero;

        return cmd;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if(!IsLocal)
        {
            return;
        }

        // always process for debugging
        if (Input.IsActionJustPressed("toggle_cursor_lock"))
        {
            if(Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                SetInputEnabled(false);
            }
            else if(Input.MouseMode == Input.MouseModeEnum.Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                SetInputEnabled(true);
            }
        }

        if(!_inputEnabled)
        {
            return;
        }

        HandleMouseLook(@event);
    }

    public override void HandleRemoteSpawn(byte playerID)
    {
        base.HandleRemoteSpawn(playerID);

        _weapon.OwnerPlayerID = playerID;
    }


    public void HandleMouseLook(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {

            if(Mathf.Abs(mouseEvent.Relative.X) > 0.0f)
            {
                RotateY(Mathf.DegToRad(-mouseEvent.Relative.X * MouseSensitivity));
            }

            if (Mathf.Abs(mouseEvent.Relative.Y) > 0.0f)
            {
                Pitch += -mouseEvent.Relative.Y * MouseSensitivity;
                Pitch = Mathf.Clamp(Pitch, -90, 90);

                if (_cameraPivot != null)
                {
                    _cameraPivot.RotationDegrees = new Vector3(Pitch, 0, 0);
                }

                _thirdPersonWeaponSocket.Rotation = _cameraPivot.Rotation;

            }

            if(mouseEvent.Relative != Vector2.Zero)
            {
                _lookDirty = true;
            }

            if(_lookDirty)
            {
                Vector2 newLookRotation = new Vector2(Yaw, _cameraPivot.Rotation.X);
                if (IsAuthority)
                {
                    UpdateRotationState(newLookRotation);
                }
                else
                {
                    PredictedPublicState.Rotation = newLookRotation;
                }
            }
        }
    }

    public void Launch(Vector3 velocity)
    {
        MovementComp.QueueLaunch(velocity);
    }

    public void Teleport(Vector3 position, float yawRotation)
    {
        PlayerState.CharacterPublicState.Position = position;
        PlayerState.CharacterPublicState.Rotation.X = yawRotation;
    }


    // Health & Armor
    public int GetHealth()
    {
        return PlayerState.CharacterPrivateState.Health;
    }

    public void SetHealth(int health)
    {
        PlayerState.CharacterPrivateState.Health = (byte)health;
        PlayerState.CharacterPrivateState.Flags |= CharacterPrivateFlags.HEALTH_CHANGED;
    }

    public int GetMaxHealth()
    {
        return PlayerState.CharacterPrivateState.MaxHealth;
    }

    public void SetMaxHealth(int maxHealth)
    {
        PlayerState.CharacterPrivateState.MaxHealth = (byte)maxHealth;
        PlayerState.CharacterPrivateState.Flags |= CharacterPrivateFlags.MAX_HEALTH_CHANGED;
    }

    public int GetArmor()
    {
        return PlayerState.CharacterPrivateState.Armor;
    }

    public void SetArmor(int armor)
    {
        PlayerState.CharacterPrivateState.Armor = (byte)armor;
        PlayerState.CharacterPrivateState.Flags |= CharacterPrivateFlags.ARMOR_CHANGED;
    }

    public int GetMaxArmor()
    {
        return PlayerState.CharacterPrivateState.MaxArmor;
    }

    public void SetMaxArmor(int maxArmor)
    {
        PlayerState.CharacterPrivateState.MaxArmor = (byte)maxArmor;
        PlayerState.CharacterPrivateState.Flags |= CharacterPrivateFlags.MAX_ARMOR_CHANGED;
    }

    // Public State Changes

    const float POSITION_EPSILON = 0.01f;
    const float ROTATION_EPSILON = 0.01f;

    public void UpdatePositionState(Vector3 position)
    {
        if (GlobalPosition.DistanceSquaredTo(position) > POSITION_EPSILON * POSITION_EPSILON)
        {
            PlayerState.CharacterPublicState.Position = position;
            PlayerState.CharacterPublicState.Flags |= CharacterPublicFlags.POSITION_CHANGED;
        }

        ApplyPosition(position);
    }

    public void ApplyPosition(Vector3 position)
    {
        GlobalPosition = position;
    }

    public void UpdateRotationState(Vector2 look)
    {
        PlayerState.CharacterPublicState.Rotation = new Vector2(look.X, look.Y);
        PlayerState.CharacterPublicState.Flags |= CharacterPublicFlags.ROTATION_CHANGED;
    }

    public void OnVelocityChanged(Vector3 velocity)
    {
        PlayerState.CharacterPublicState.Velocity = velocity;
        PlayerState.CharacterPublicState.Flags |= CharacterPublicFlags.VELOCITY_CHANGED;
    }

    public void OnMovementModeChanged(CharacterMoveMode movementMode)
    {
        PlayerState.CharacterPublicState.MovementMode = movementMode;
        PlayerState.CharacterPublicState.Flags |= CharacterPublicFlags.MOVEMENT_MODE_CHANGED;
    }

    public void OnEquippedWeaponChanged(WeaponType weaponType)
    {
        PlayerState.CharacterPublicState.EquippedWeapon = weaponType;
        PlayerState.CharacterPublicState.Flags |= CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED;
    }

    // Weapons & Ammo
    public void OnReceivedWeapon(WeaponType weaponType)
    {
        WeaponFlags mask = WeaponConstants.MaskFromWeapon(weaponType);
        PlayerState.CharacterPrivateState.HeldWeaponsFlags |= mask;
        PlayerState.CharacterPrivateState.Flags |= CharacterPrivateFlags.WEAPONS_CHANGED;
    }

    public void OnLostWeapon(WeaponType weaponType)
    {
        WeaponFlags mask = WeaponConstants.MaskFromWeapon(weaponType);
        PlayerState.CharacterPrivateState.HeldWeaponsFlags &= ~mask;
        PlayerState.CharacterPrivateState.Flags |= CharacterPrivateFlags.WEAPONS_CHANGED;
    }

    public void OnAmmoChanged(WeaponType weaponType, byte newAmmo)
    {
        int index = (int)weaponType;
        if (index < WeaponConstants.TOTAL_WEAPON_SLOTS)
        {
            PlayerState.CharacterPrivateState.Ammo[index] = newAmmo;
            PlayerState.CharacterPrivateState.AmmoChangedFlags |= WeaponConstants.MaskFromWeapon(weaponType);
            PlayerState.CharacterPrivateState.Flags |= CharacterPrivateFlags.AMMO_CHANGED;
        }
    }

    // Apply Replicated States
    public void ApplyAuthoritativePublicState(CharacterPublicState publicState)
    {
        CharacterPublicFlags flags = publicState.Flags;

        if ((flags & CharacterPublicFlags.POSITION_CHANGED) != 0)
        {
            if (IsLocal)
            {
                foreach (var cmd in ClientGame.Instance.UnprocessedClientInputs)
                {
                    publicState = MovementComp.Step(publicState, cmd, NetworkConstants.SERVER_TICK_INTERVAL, true);
                }

                ReconcileMoveState(publicState);
            }
            else
            {
                PlayerState.CharacterPublicState.Position = publicState.Position;
            }
        }


        if ((flags & CharacterPublicFlags.ROTATION_CHANGED) != 0)
        {
            GD.Print($"Running apply public state on player id: {PlayerState.PlayerID} and PLAYER ROTATED. ");

            PlayerState.CharacterPublicState.Rotation = publicState.Rotation;
        }

        if ((flags & CharacterPublicFlags.VELOCITY_CHANGED) != 0)
        {
            PlayerState.CharacterPublicState.Velocity = publicState.Velocity;
        }

        if ((flags & CharacterPublicFlags.MOVEMENT_MODE_CHANGED) != 0)
        {
            PlayerState.CharacterPublicState.MovementMode = publicState.MovementMode;
        }

        if ((flags & CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED) != 0)
        {
            PlayerState.CharacterPublicState.EquippedWeapon = publicState.EquippedWeapon;
        }
    }

    public void ApplyAuthoritativePrivateState(CharacterPrivateState privateState)
    {
        CharacterPrivateFlags flags = privateState.Flags;

        if ((flags & CharacterPrivateFlags.HEALTH_CHANGED) != 0)
        {
            PlayerState.CharacterPrivateState.Health = privateState.Health;
        }

        if ((flags & CharacterPrivateFlags.MAX_HEALTH_CHANGED) != 0)
        {
            PlayerState.CharacterPrivateState.MaxHealth = privateState.MaxHealth;
        }

        if ((flags & CharacterPrivateFlags.ARMOR_CHANGED) != 0)
        {
            PlayerState.CharacterPrivateState.Armor = privateState.Armor;
        }

        if ((flags & CharacterPrivateFlags.MAX_ARMOR_CHANGED) != 0)
        {
            PlayerState.CharacterPrivateState.MaxArmor = privateState.MaxArmor;
        }

        if ((flags & CharacterPrivateFlags.WEAPONS_CHANGED) != 0)
        {
            PlayerState.CharacterPrivateState.HeldWeaponsFlags = privateState.HeldWeaponsFlags;
        }

        if ((flags & CharacterPrivateFlags.AMMO_CHANGED) != 0)
        {
            WeaponFlags ammoFlags = privateState.AmmoChangedFlags;
            for (int i = 0; i < WeaponConstants.TOTAL_WEAPON_SLOTS; i++)
            {
                WeaponFlags mask = (WeaponFlags)(1 << i);
                if ((ammoFlags & mask) != 0)
                {
                    PlayerState.CharacterPrivateState.Ammo[i] = privateState.Ammo[i];
                }
            }

            PlayerState.CharacterPrivateState.AmmoChangedFlags = privateState.AmmoChangedFlags;
        }
    }

    // Flags management
    public void ClearFlags()
    {
        PlayerState.CharacterPublicState.ClearFlags();
        PlayerState.CharacterPrivateState.ClearFlags();
    }
}