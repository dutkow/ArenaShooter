using Godot;
using System;
using System.Collections.Generic;


[Flags]
public enum CharacterStateFlags : byte
{
    NONE = 0,

    MOVE_STATE_CHANGED,
    HEALTH_STATE_CHANGED,
    INVENTORY_STATE_CHANGED,
}

public struct CharacterState
{
    public CharacterStateFlags Flags;

    public CharacterMoveState MoveState;

    public HealthState HealthState;

    public InventoryState InventoryState;
}

public partial class Character : Pawn, IDamageable
{
    public CharacterState State;

    CharacterMovementMode _mode;


    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;


    [Export] private Camera3D _camera; // assign in editor
    [Export] public float MouseSensitivity = 0.005f;

    List<ClientInputCommand> _unacknowledgedClientInputs = new();


    const int REDUNDANT_INPUTS = 4;


    public Player Player;

    public float Yaw => GlobalRotation.Y;
    public float Pitch { get; private set; }


    private uint _lastProcessedClientTick;


    // Components
    [Export] MeshInstance3D _characterMesh;
    [Export] Node3D _cameraPivot;
    [Export] Node3D _visualContainer;

    [Export] public Node3D WeaponSocketFP;

    [Export] public Node3D WeaponSocketTP;

    private Vector3 _visualContainerPosition;

    public CharacterMovement MovementComp { get; private set; } = new();

    public HealthComponent HealthComp { get; private set; } = new();



    private ClientInputCommand _lastProcessedClientCommand;

    private bool _useInterpolation = false;

    private bool _lookDirty;

    //public CharacterPublicState PredictedPublicState = new();

    private InventoryManager InventoryManager;

    public override void _Ready()
    {
        base._Ready();


        _camera.Current = false;

        if(_useInterpolation)
        {
            _camera.Reparent(Level.Instance, true);
        }

        SetProcessInput(false);
        ShowThirdPersonView();

        Input.MouseMode = Input.MouseModeEnum.Captured;

        _visualContainerPosition = _visualContainer.Position;

        Area.Owner = this;


        //InventoryManager.EquipWeapon(PlayerState.CharacterPublicState.EquippedWeaponIndex);
    }


    public void ApplyDamage(int damage)
    {
        HealthComp.ApplyDamage(damage);
    }

 



    public override void TickWithNextClientCommand(ClientInputCommand cmd, float delta)
    {
        base.TickWithNextClientCommand(cmd, delta);

        MovementComp.ServerProcessNextClientInput(cmd, true, delta);
        InventoryManager?.ProcessClientInput(cmd.Flags);
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
            //InterpolatePosition(10.0f, delta);
            //GlobalPosition = MovementComp.AuthoritativeState.Position;
        }
        else
        {
            GlobalPosition = MovementComp.AuthoritativeState.Position;
        }
    }


    public void LocalPredictiveInterpolation(float delta)
    {
        if (_useInterpolation)
        {
            InterpolatePosition(1.0f, delta);
        }
        else
        {
            GlobalPosition = MovementComp.PredictedState.Position;
        }
    }

    public void RemoteInterpolation(float delta)
    {
        if(_useInterpolation)
        {
            InterpolatePosition(1.0f, delta);
            InterpolateYaw(delta * 10.0f);
            InterpolatePitch(delta * 10.0f);
        }
        else
        {
            GlobalPosition = MovementComp.AuthoritativeState.Position;
            GlobalRotation = new Vector3(0.0f, MovementComp.AuthoritativeState.Yaw, 0.0f);
            WeaponSocketTP.Rotation = new Vector3(MovementComp.AuthoritativeState.Pitch, 0.0f, 0.0f);
        }
    }

    float LOCAL_SV_INTERP_RATE = 0.5f;
    float LOCAL_CL_INTERP_RATE = 0.5f;
    float REMOTE_CL_INTERP_RATE = 0.5f;


    public void InterpolatePosition(float interpSpeed, float delta)
    {
        _camera.GlobalPosition = _camera.Position.Lerp(_cameraPivot.GlobalPosition, 0.25f);
        _camera.GlobalRotation = _cameraPivot.GlobalRotation;
    }

    public void InterpolateYaw(float interpSpeed)
    {
        Vector3 rot = GlobalRotation;
        //rot.Y = Mathf.LerpAngle(rot.Y, PlayerState.CharacterPublicState.Yaw, interpSpeed);
        GlobalRotation = rot;
    }

    public void InterpolatePitch(float interpSpeed)
    {

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

        _camera.Current = true;

        SetRole(NetworkRole.LOCAL);
        UIRoot.Instance.OnPossessedCharacter(this);

        if(MatchState.Instance.Players.TryGetValue(ClientGame.Instance.LocalPlayerID, out var player))
        {
            Player = player;
            //PlayerState = player;
        }
        else
        {
            GD.Print($"player state is null");
        }



        //_weapon.OwnerPlayerID = PlayerStateOld.PlayerInfo.PlayerID;
        //_weapon.SetIsAuthority(IsAuthority);

        if(!IsAuthority)
        {
            //PredictedPublicState = PlayerState.CharacterPublicState.Copy();
            //PredictedPublicState.Yaw = GlobalRotation.Y;
            GD.Print($"client copied pub state");
        }

        //EquipStartingWeapon();
    }


    // Apply Replicated States
    public void ApplyAuthoritativePublicState(CharacterPublicState publicState, float delta)
    {
        if (IsLocal)
        {
            //GD.Print($"Num unprocessed inputs: {ClientGame.Instance.UnprocessedClientInputs.Count}");

            foreach (var clientPredictionTick in ClientGame.Instance.UnprocessedClientInputCommands)
            {
                //publicState = MovementComp.Step(publicState, cmd.InputCommand, delta, true);

            }

            if(!_skipReconciliation)
            {
                //ReconcileMoveState(publicState);
            }
            else
            {
                _ticksUntilReconciliationResume--;

            }
        }
        else
        {
            //PlayerState.CharacterPublicState = publicState;
        }

        _lastSimulatedState = publicState.Copy();
    }

    private CharacterPublicState _lastSimulatedState = new();

    /*
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
    }*/

    /*
    public void EquipStartingWeapon()
    {
        var weaponDataArray = GameRules.Instance.Weapons;

        if (weaponDataArray.Count <= PlayerState.CharacterPublicState.EquippedWeaponIndex)
        {
            GD.PushError($"Weapon index {PlayerState.CharacterPublicState.EquippedWeaponIndex} not found");
            return;
        }

        if (IsLocal)
        {

            GD.Print($"weapon flags on spawn: {PlayerState.CharacterPrivateState.HeldWeaponsFlags}. Equipped weapon: {PlayerState.CharacterPublicState.EquippedWeaponIndex}");
        }
    }*/

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
    }

    public void HideFirstPersonView()
    {
    }

    public void ShowThirdPersonView()
    {
        HideFirstPersonView();

        _characterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        //_thirdPersonWeaponMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        //_thirdPersonWeaponMesh.Visible = true;
    }

    public void HideThirdPersonView()
    {
        _characterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
        //_thirdPersonWeaponMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
        //_thirdPersonWeaponMesh.Visible = false;

        GD.Print($"hiding third person view");
    }


    /*
    public void ReconcileMoveState(CharacterPublicState authoritativeState)
    {
        Vector3 delta = PredictedPublicState.Position - authoritativeState.Position;

        // Thresholds
        const float SNAP_THRESHOLD_H = 2.0f;        // Horizontal snap (X/Z)
        const float SNAP_THRESHOLD_V = 2.0f;        // Vertical snap (Y)
        const float INTERP_THRESHOLD_H = 0.01f;      // Horizontal lerp start
        const float INTERP_THRESHOLD_V = 0.01f;     // Vertical lerp start

        // Lerp speeds
        const float INTERP_SPEED_H = 0.25f;
        const float INTERP_SPEED_V = 0.25f;

        Vector3 targetPos = authoritativeState.Position;
        Vector3 currentPos = PredictedPublicState.Position;

        Vector2 deltaXZ = new Vector2(delta.X, delta.Z);
        float distXZ = deltaXZ.Length();

        float deltaY = Math.Abs(delta.Y);

        //GD.Print($"horizontal error: {distXZ}.");

        // --- Horizontal correction ---
        if (distXZ > SNAP_THRESHOLD_H)
        {
            //GD.Print($"snap correction horizontal, error {distXZ}");
            currentPos.X = targetPos.X;
            currentPos.Z = targetPos.Z;
        }
        else if (distXZ > INTERP_THRESHOLD_H)
        {
            //GD.Print($"horizontal error: {distXZ}.");
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
    */

    public override ClientInputCommand GetClientInputCommand(ClientInputCommand cmd)
    {
        base.GetClientInputCommand(cmd);

        if (_inputEnabled)
        {
            if (Input.IsActionPressed("move_forward")) cmd.Flags |= InputFlags.FORWARD;
            if (Input.IsActionPressed("move_back")) cmd.Flags |= InputFlags.BACKWARD;
            if (Input.IsActionPressed("move_left")) cmd.Flags |= InputFlags.STRAFE_LEFT;
            if (Input.IsActionPressed("move_right")) cmd.Flags |= InputFlags.STRAFE_RIGHT;
            if (Input.IsActionPressed("jump")) cmd.Flags |= InputFlags.JUMP;
            if (Input.IsActionPressed("primary_fire")) cmd.Flags |= InputFlags.FIRE_PRIMARY;
        }

        /*
        if (_weapon.FiredPredictedProjectile)
        {

        }*/

        Vector3 dir = -_camera.GlobalTransform.Basis.Z;
        //InventoryManager.Tick((float)TickManager.Instance.ServerTickInterval, _camera.GlobalPosition, dir);

        if(_accumulatedLookDelta != Vector2.Zero)
        {
            cmd.Look = _accumulatedLookDelta;
            cmd.Flags |= InputFlags.LOOK;
        }

        if (!IsAuthority)
        {
            float delta = (float)TickManager.Instance.ServerTickInterval;
            MovementComp.HandlePredictedInput(cmd, delta);
        }

        InventoryManager.HandleInput(cmd.Flags);

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


    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if(!IsLocal)
        {
            return;
        }

        if(!_inputEnabled)
        {
            return;
        }

        HandleMouseLook(@event);
    }

    bool _skipReconciliation => _ticksUntilReconciliationResume > 0;

    public override void HandleRemoteSpawn(byte playerID)
    {
        base.HandleRemoteSpawn(playerID);

    }

    int _ticksToSkipReconiliationPostLaunch = 10;
    int _ticksUntilReconciliationResume = 10;
    int _accumulatedSkipReconiliationTicks;
    public void Launch(Vector3 velocity, CharacterMoveState state, bool isSimulating)
    {
        if(IsAuthority)
        {
            MovementComp.AuthoritativeState = MovementComp.QueueLaunch(MovementComp.AuthoritativeState, velocity);
        }
        else if(IsLocal)
        {
            state = MovementComp.QueueLaunch(state, velocity);
            _ticksUntilReconciliationResume = _ticksToSkipReconiliationPostLaunch;
            GD.Print($"launch queued on client. is simulating: {isSimulating}");
        }
    }

    /*
    public void Teleport(Vector3 position, float yawRotation)
    {
        PlayerState.CharacterPublicState.Position = position;
        PlayerState.CharacterPublicState.Yaw = yawRotation;
    }


    // Health & ARMOR
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

    // Public AuthoritativeState Changes

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
        //PlayerStateOld.CharacterPublicState.Yaw = new Vector2(look.X, look.Y);
        PlayerState.CharacterPublicState.Flags |= CharacterPublicFlags.ROTATION_CHANGED;
    }

    public void OnVelocityChanged(Vector3 velocity)
    {
        PlayerState.CharacterPublicState.Velocity = velocity;
        PlayerState.CharacterPublicState.Flags |= CharacterPublicFlags.VELOCITY_CHANGED;
    }

    public void OnMovementModeChanged(CharacterMovementMode movementMode)
    {
        PlayerState.CharacterPublicState.MovementMode = movementMode;
        PlayerState.CharacterPublicState.Flags |= CharacterPublicFlags.MOVEMENT_MODE_CHANGED;
    }

    public void OnEquippedWeaponChanged(byte weaponIndex)
    {
        PlayerState.CharacterPublicState.EquippedWeaponIndex = weaponIndex;
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
    }    */


    public void HandleDeath()
    {
        QueueFree();
    }


    public void OnSpawned()
    {
        MovementComp.OnCharacterSpawned(this);

        InventoryManager = new();
        InventoryManager.Initialize(this);
    }



    public CharacterState GetState()
    {
        CharacterState state = new();

        state.MoveState = MovementComp.AuthoritativeState;
        state.HealthState = HealthComp.State;
        state.InventoryState = InventoryManager.State;

        if(state.MoveState.Flags != 0)
        {
            state.Flags |= CharacterStateFlags.MOVE_STATE_CHANGED;
        }

        if (state.HealthState.Flags != 0)
        {
            state.Flags |= CharacterStateFlags.HEALTH_STATE_CHANGED;
        }

        if (state.InventoryState.Flags != 0)
        {
            state.Flags |= CharacterStateFlags.INVENTORY_STATE_CHANGED;
        }

        return state;
    }

    public void ApplyState(CharacterState state, float delta)
    {
        if (state.Flags == 0)
        {
            return;
        }

        if ((state.Flags & CharacterStateFlags.MOVE_STATE_CHANGED) != 0)
        {
            MovementComp.ApplyAuthoritativeState(state.MoveState, delta);
        }

        if ((state.Flags & CharacterStateFlags.HEALTH_STATE_CHANGED) != 0)
        {
            HealthComp.ApplyState(state.HealthState);
        }

        if ((state.Flags & CharacterStateFlags.INVENTORY_STATE_CHANGED) != 0)
        {
            InventoryManager.ApplyState(state.InventoryState);
        }
    }
}