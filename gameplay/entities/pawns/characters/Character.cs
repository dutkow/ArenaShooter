using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;


public partial class Character : Pawn, IDamageable
{
    CharacterMoveMode _mode;


    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;


    [Export] public Camera3D Camera; // assign in editor
    [Export] public float MouseSensitivity = 0.005f;

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

    public void HandleSpawn(Vector3 spawnPosition, float yaw, float pitch)
    {
        GlobalPosition = spawnPosition;
        GlobalRotation = new Vector3(0.0f, yaw, 0.0f);


        MovementComp.Initialize(this);

    }



    public override void ServerProcessNextClientInput(ClientInputCommand cmd)
    {
        base.ServerProcessNextClientInput(cmd);

        PlayerState.CharacterPublicState = MovementComp.Step(PlayerState.CharacterPublicState, cmd, NetworkConstants.SERVER_TICK_INTERVAL);

        _weapon.ProcessClientInput(cmd.Flags);
    }


    public override void _Process(double delta)
    {
        base._Process(delta);

        if (IsLocal)
        {

            if (IsAuthority)
            {
                LocalAuthorityInterpolation((float)delta);
            }
            else
            {
                LocalPredictiveInterpolation((float)delta);
            }
        }
        else
        {
            RemoteInterpolation((float)delta);
        }
    }


    public void LocalAuthorityInterpolation(float delta)
    {
        if (_useInterpolation)
        {
            InterpolatePosition(delta * LOCAL_SV_INTERP_RATE);
        }
        else
        {
            GlobalPosition = PlayerState.CharacterPublicState.Position;
        }
    }

    public void LocalPredictiveInterpolation(float delta)
    {
        if (_useInterpolation)
        {
            InterpolatePosition(delta * LOCAL_SV_INTERP_RATE);
        }
        else
        {
            GlobalPosition = PredictedPublicState.Position;
        }
    }

    public void RemoteInterpolation(float delta)
    {
        if(_useInterpolation)
        {
            InterpolatePosition(delta * REMOTE_CL_INTERP_RATE);
            InterpolateYaw(delta * 10.0f);
            InterpolatePitch(delta * 10.0f);
        }
        else
        {
            GlobalPosition = PlayerState.CharacterPublicState.Position;
            GlobalRotation = new Vector3(0.0f, -PlayerState.CharacterPublicState.Look.X, 0.0f);
            _thirdPersonWeaponSocket.Rotation = new Vector3(-PlayerState.CharacterPublicState.Look.Y, 0.0f, 0.0f);
        }
    }

    float LOCAL_SV_INTERP_RATE = 0.5f;
    float LOCAL_CL_INTERP_RATE = 0.5f;
    float REMOTE_CL_INTERP_RATE = 0.5f;


    public void InterpolatePosition(float interpSpeed)
    {

        var targetPosition = PlayerState.CharacterPublicState.Position + _visualContainerPosition;
        _visualContainer.GlobalPosition = _visualContainer.GlobalPosition.Lerp(targetPosition, LOCAL_SV_INTERP_RATE);
    }

    public void InterpolateYaw(float interpSpeed)
    {
        Vector3 rot = GlobalRotation;
        rot.Y = Mathf.LerpAngle(rot.Y, PlayerState.CharacterPublicState.Look.X, interpSpeed);
        GlobalRotation = rot;
    }

    public void InterpolatePitch(float interpSpeed)
    {
        Vector3 camRot = _thirdPersonWeaponMesh.GlobalRotation;
        camRot.X = Mathf.Lerp(camRot.X, PlayerState.CharacterPublicState.Look.Y, interpSpeed);
        _thirdPersonWeaponMesh.GlobalRotation = camRot;
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

        _weapon.OwnerPlayerID = PlayerState.PlayerInfo.PlayerID;
        _weapon.SetIsAuthority(IsAuthority);

        if(!IsAuthority)
        {
            PredictedPublicState = PlayerState.CharacterPublicState.Copy();
            PredictedPublicState.Look = new Vector2(GlobalRotation.Y, 0.0f);
            GD.Print($"client copied pub state");
        }
    }

    // Apply Replicated States
    public void ApplyAuthoritativePublicState(CharacterPublicState publicState)
    {
        CharacterPublicFlags flags = publicState.Flags;


        if ((flags & CharacterPublicFlags.POSITION_CHANGED) == 0)
        {
            publicState.Position = PredictedPublicState.Position;
        }

        if ((flags & CharacterPublicFlags.POSITION_CHANGED) == 0)
        {
            publicState.Look = PredictedPublicState.Look;
        }

        if ((flags & CharacterPublicFlags.VELOCITY_CHANGED) == 0)
        {
            publicState.Velocity = PredictedPublicState.Velocity;
        }

        if ((flags & CharacterPublicFlags.MOVEMENT_MODE_CHANGED) == 0)
        {
            publicState.MovementMode = PredictedPublicState.MovementMode;
        }

        if ((flags & CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED) == 0)
        {
            publicState.EquippedWeapon = PredictedPublicState.EquippedWeapon;
        }


        if(IsLocal)
        {
            //GD.Print($"Num unprocessed inputs: {ClientGame.Instance.UnprocessedClientInputs.Count}");
            foreach (var unprocessedInput in ClientGame.Instance.UnprocessedClientInputs)
            {
                publicState = MovementComp.Step(publicState, unprocessedInput, NetworkConstants.SERVER_TICK_INTERVAL, true);
            }

            ReconcileMoveState(publicState);
        }
        else
        {
            PlayerState.CharacterPublicState = publicState;
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

        GD.Print($"hiding third person view");
    }


    public void ReconcileMoveState(CharacterPublicState authoritativeState)
    {
        Vector3 delta = PredictedPublicState.Position - authoritativeState.Position;

        // Thresholds
        const float SNAP_THRESHOLD_H = 2.0f;        // Horizontal snap (X/Z)
        const float SNAP_THRESHOLD_V = 2.0f;        // Vertical snap (Y)
        const float INTERP_THRESHOLD_H = 0.1f;      // Horizontal lerp start
        const float INTERP_THRESHOLD_V = 0.1f;     // Vertical lerp start

        // Lerp speeds
        const float INTERP_SPEED_H = 0.25f;
        const float INTERP_SPEED_V = 0.25f;

        Vector3 targetPos = authoritativeState.Position;
        Vector3 currentPos = PredictedPublicState.Position;

        Vector2 deltaXZ = new Vector2(delta.X, delta.Z);
        float distXZ = deltaXZ.Length();

        float deltaY = Math.Abs(delta.Y);


        // --- Horizontal correction ---
        if (distXZ > SNAP_THRESHOLD_H)
        {
            //GD.Print($"snap correction horizontal, error {distXZ}");
            currentPos.X = targetPos.X;
            currentPos.Z = targetPos.Z;
        }
        else if (distXZ > INTERP_THRESHOLD_H)
        {
            GD.Print($"horizontal error: {distXZ}.");
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


    public override ClientInputCommand CaptureInput(ClientInputCommand cmd)
    {
        base.CaptureInput(cmd);

        if(_inputEnabled)
        {
            if (Input.IsActionPressed("move_forward")) cmd.Flags |= InputFlags.FORWARD;
            if (Input.IsActionPressed("move_back")) cmd.Flags |= InputFlags.BACKWARD;
            if (Input.IsActionPressed("move_left")) cmd.Flags |= InputFlags.STRAFE_LEFT;
            if (Input.IsActionPressed("move_right")) cmd.Flags |= InputFlags.STRAFE_RIGHT;
            if (Input.IsActionPressed("jump")) cmd.Flags |= InputFlags.JUMP;
            if (Input.IsActionPressed("primary_fire")) cmd.Flags |= InputFlags.FIRE_PRIMARY;
        }

        if (_weapon.FiredPredictedProjectile)
        {

        }

        Vector3 dir = -Camera.GlobalTransform.Basis.Z;
        _weapon.Tick(NetworkConstants.SERVER_TICK_INTERVAL, Camera.GlobalPosition, dir);


        cmd.Look = _accumulatedLookDelta;
        cmd.Flags |= InputFlags.LOOK;

        if(!IsAuthority)
        {
            PredictedPublicState = MovementComp.Step(PredictedPublicState, cmd, NetworkConstants.SERVER_TICK_INTERVAL);
            PredictedPublicState.Flags |= CharacterPublicFlags.POSITION_CHANGED;
        }

        _weapon.HandleInput(cmd.Flags);

        _accumulatedLookDelta = Vector2.Zero;
        return cmd;
    }

    private Vector2 _accumulatedLookDelta;

    private float _pitch = 0.0f;
    public void HandleMouseLook(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {
            Vector2 lookDelta = mouseEvent.Relative * MouseSensitivity;

            _accumulatedLookDelta += lookDelta;

            // Yaw (horizontal)
            if (Mathf.Abs(lookDelta.X) > 0.0f)
            {
                RotateY(-lookDelta.X);
            }

            // Pitch (vertical)
            if (Mathf.Abs(lookDelta.Y) > 0.0f)
            {
                _pitch -= lookDelta.Y;

                _pitch = Mathf.Clamp(_pitch, -Mathf.DegToRad(89.0f), Mathf.DegToRad(89.0f));

                Vector3 rot = _cameraPivot.Rotation;
                rot.X = _pitch;
                _cameraPivot.Rotation = rot;
            }
        }
    }

    public void DebugInput()
    {
        if (Input.IsActionJustPressed("toggle_cursor_lock"))
        {
            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                SetInputEnabled(false);
            }
            else if (Input.MouseMode == Input.MouseModeEnum.Visible)
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                SetInputEnabled(true);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if(!IsLocal)
        {
            return;
        }

        DebugInput();

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




    public void Launch(Vector3 velocity)
    {
        GD.Print($"launch ran on {NetworkManager.Instance.NetworkMode}");

        if(IsAuthority)
        {
            GD.Print($"fired launch on owner");
            MovementComp.Launch(PlayerState.CharacterPublicState, velocity);
        }
        else if(IsLocal)
        {
            GD.Print($"fired predicted on client");
            MovementComp.Launch(PredictedPublicState, velocity);
        }
        else
        {
            GD.Print("launch failed");
        }
    }

    public void Teleport(Vector3 position, float yawRotation)
    {
        PlayerState.CharacterPublicState.Position = position;
        PlayerState.CharacterPublicState.Look.X = yawRotation;
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

    public void UpdatePositionState(Vector3 position)
    {

        PlayerState.CharacterPublicState.Position = position;
        PlayerState.CharacterPublicState.Flags |= CharacterPublicFlags.POSITION_CHANGED;

        ApplyPosition(position);
    }

    public void ApplyPosition(Vector3 position)
    {
        GlobalPosition = position;
    }

    public void UpdateRotationState(Vector2 look)
    {
        PlayerState.CharacterPublicState.Look = new Vector2(look.X, look.Y);
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



    // Flags management
    public void ClearFlags()
    {
        PlayerState.CharacterPublicState.ClearFlags();
        PlayerState.CharacterPrivateState.ClearFlags();
    }
}