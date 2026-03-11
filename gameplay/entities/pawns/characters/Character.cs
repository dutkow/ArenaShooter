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
    [Export] public Weapon Weapon;
    [Export] Node3D _visualContainer;

    [Export] Node3D _thirdPersonWeaponSocket;

    private Vector3 _visualContainerPosition;

    public CharacterMovement MovementComp { get; private set; } = new();

    public HealthComponent HealthComp { get; private set; } = new();

    private SortedDictionary<ushort, ClientInputCommand> _unprocessedClientInputs = new();


    private ClientInputCommand _lastProcessedClientCommand;

    private bool _useInterpolation = false;

    private bool _yawDirty;
    private bool _pitchDirty;

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

        Vector3 direction = -Camera.GlobalTransform.Basis.Z;
        Weapon.Tick(delta, Camera.GlobalPosition, direction);

        GlobalPosition = MovementComp.State.Position + new Vector3(0.0f, 0.0f, 0.1f);
    }


    public void HandleSpawn(Vector3 spawnPosition, float yaw, float pitch)
    {
        GlobalPosition = spawnPosition;
        GlobalRotation = new Vector3(0.0f, yaw, 0.0f);

        MovementComp.State.Position = spawnPosition;
        MovementComp.State.Yaw = yaw;
        MovementComp.State.Pitch = pitch;

        MovementComp.Initialize(this);

    }

    public override void ApplyInput(ClientInputCommand cmd)
    {
        base.ApplyInput(cmd);

        MovementComp.HandleInput(cmd, NetworkConstants.SERVER_TICK_INTERVAL);

        Weapon.HandleInput(cmd.Mask);

    }

    public override void ProcessClientInput(ClientInputCommand cmd)
    {
        base.ProcessClientInput(cmd);

        if (cmd.Mask.HasFlag(ClientCommandMask.YAW))
        {
            MovementComp.State.Yaw = cmd.Yaw;
            GlobalRotation = new Vector3(0.0f, cmd.Yaw, 0.0f);
        }

        if (cmd.Mask.HasFlag(ClientCommandMask.PITCH))
        {
            MovementComp.State.Pitch = cmd.Pitch;
            _cameraPivot.Rotation = new Vector3(cmd.Pitch, 0.0f, 0.0f);
        }

        MovementComp.State = MovementComp.Step(MovementComp.State, cmd, NetworkConstants.SERVER_TICK_INTERVAL);

        Weapon.ProcessClientInput(cmd.Mask);
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

        if(PlayerState == null)
        {
            GD.Print($"player state is null");
        }
        Weapon.OwnerPlayerID = PlayerState.PlayerID;
        Weapon.SetIsAuthority(IsAuthority);

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
        Weapon.FirstPersonWeaponMesh.Visible = true;
    }

    public void HideFirstPersonView()
    {
        Weapon.FirstPersonWeaponMesh.Visible = false;
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
                GlobalPosition = MovementComp.State.Position;
            }
            else
            {
                GlobalPosition = MovementComp.State.Position;
                GlobalRotation = new Vector3(0.0f, MovementComp.State.Yaw, 0.0f);
                _thirdPersonWeaponSocket.Rotation = new Vector3(MovementComp.State.Pitch, 0.0f, 0.0f);
            }
        }

    }

    public void InterpolatePosition(float interpSpeed)
    {

        var targetPosition = MovementComp.State.Position + _visualContainerPosition;
        _visualContainer.GlobalPosition = _visualContainer.GlobalPosition.Lerp(targetPosition, LOCAL_SV_INTERP_RATE);
    }

    public void InterpolateYaw(float interpSpeed)
    {
        Vector3 rot = GlobalRotation;
        rot.Y = Mathf.LerpAngle(rot.Y, MovementComp.State.Yaw, interpSpeed);
        GlobalRotation = rot;
    }

    public void InterpolatePitch(float interpSpeed)
    {
        Vector3 camRot = _thirdPersonWeaponMesh.GlobalRotation;
        camRot.X = Mathf.Lerp(camRot.X, MovementComp.State.Pitch, interpSpeed);
        _thirdPersonWeaponMesh.GlobalRotation = camRot;
    }

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
    }


    public void ReconcileMoveState(CharacterMoveState newPredictedState)
    {
        Vector3 delta = MovementComp.State.Position - newPredictedState.Position;

        // Thresholds
        const float SNAP_THRESHOLD_H = 2.0f;        // Horizontal snap (X/Z)
        const float SNAP_THRESHOLD_V = 2.0f;        // Vertical snap (Y)
        const float INTERP_THRESHOLD_H = 0.025f;      // Horizontal lerp start
        const float INTERP_THRESHOLD_V = 0.025f;     // Vertical lerp start

        // Lerp speeds
        const float INTERP_SPEED_H = 0.15f;
        const float INTERP_SPEED_V = 0.15f;

        Vector3 targetPos = newPredictedState.Position;
        Vector3 currentPos = MovementComp.State.Position;

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
            //GD.Print($"lerp correction horizontal, error {distXZ}");
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
        MovementComp.State.Position = currentPos;
        MovementComp.State.Velocity = newPredictedState.Velocity;
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

            if (_yawDirty)
            {
                cmd.Mask |= ClientCommandMask.YAW;
                cmd.Yaw = GlobalRotation.Y;
                _yawDirty = false;
            }

            if (_pitchDirty)
            {
                cmd.Mask |= ClientCommandMask.PITCH;
                cmd.Pitch = _thirdPersonWeaponSocket.Rotation.X;
                _pitchDirty = false;
            }
        }

        if (MovementComp.WasLaunched)
        {
            cmd.Mask |= ClientCommandMask.WAS_LAUNCHED;
            cmd.LaunchVelocity = MovementComp.LaunchVector;
            MovementComp.WasLaunched = false;
        }

        cmd = ClientProjectileManager.Instance.AddInfoToClientInputCommand(cmd);


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

        Weapon.OwnerPlayerID = playerID;
    }


    public void HandleMouseLook(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {

            if(Mathf.Abs(mouseEvent.Relative.X) > 0.0f)
            {
                RotateY(Mathf.DegToRad(-mouseEvent.Relative.X * MouseSensitivity));
                MovementComp.State.Yaw = Yaw;
                _yawDirty = true;
            }

            if (Mathf.Abs(mouseEvent.Relative.Y) > 0.0f)
            {
                Pitch += -mouseEvent.Relative.Y * MouseSensitivity;
                Pitch = Mathf.Clamp(Pitch, -90, 90);

                if (_cameraPivot != null)
                {
                    _cameraPivot.RotationDegrees = new Vector3(Pitch, 0, 0);
                }

                MovementComp.State.Pitch = _cameraPivot.Rotation.X;
                _thirdPersonWeaponSocket.Rotation = _cameraPivot.Rotation;

                _pitchDirty = true;
            }
        }
    }

    public void Launch(Vector3 velocity)
    {
        MovementComp.QueueLaunch(velocity);
    }

    public void Teleport(Vector3 position, float yawRotation)
    {
        MovementComp.State.Position = position;
        MovementComp.State.Yaw = yawRotation;
    }
}