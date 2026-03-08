using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;


public partial class Character : Pawn
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
    [Export] MeshInstance3D _thirdPersonWeaponMesh;
    [Export] Node3D _cameraPivot;
    [Export] Weapon _weapon;
    [Export] Node3D _visualContainer;

    private Vector3 _visualContainerPosition;


    public CharacterMovement MovementComp { get; private set; } = new();

    public HealthComponent HealthComp { get; private set; } = new();

    private SortedDictionary<ushort, ClientInputCommand> _unprocessedClientInputs = new();

    private ushort _lastAckedClientCommandTick;

    private ClientInputCommand _lastProcessedClientCommand;

    private bool _useInterpolation = false;

    public override void _Ready()
    {
        base._Ready();

        Camera.Current = false;
        SetProcessInput(false);
        ShowThirdPersonView();

        Input.MouseMode = Input.MouseModeEnum.Captured;

        _visualContainerPosition = _visualContainer.Position;
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

    // Ticking using physics process for now for simplicity, will move to server tick
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        Tick((float)delta);
    }

    public override void Tick(float delta)
    {
        base.Tick(delta);

        if(IsAuthority)
        {
            if(IsLocal)
            {
                HandleInput(CaptureInput(), delta);
            }
            else
            {
                ProcessNextClientInput();
            }
        }
        else if (IsLocal)
        {
            var input = CaptureInput();
            HandleInput(input, delta);
            SendClientInput(input);
        }
    }

    public void ProcessNextClientInput()
    {

        ClientInputCommand cmd = new();

        if (_unprocessedClientInputs.Count > 0)
        {
            ushort tickToProcess = _unprocessedClientInputs.Keys.Min();
            cmd = _unprocessedClientInputs[tickToProcess];
            _lastProcessedClientCommand = cmd;
            _unprocessedClientInputs.Remove(tickToProcess);
            _lastAckedClientCommandTick = tickToProcess;

            MatchState.Instance.LastProcessedTickByPlayerID[PlayerState.PlayerID] = tickToProcess;
        }
        else
        {
            cmd = _lastProcessedClientCommand;
        }

        MovementComp.State.Yaw = cmd.Yaw;
        MovementComp.State.Pitch = cmd.Pitch;

        MovementComp.State = MovementComp.Step(MovementComp.State, cmd.Input, NetworkConstants.SERVER_TICK_INTERVAL);


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
        _thirdPersonWeaponMesh.Visible = true;
    }

    public void HideThirdPersonView()
    {
        _characterMesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly;
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
                _thirdPersonWeaponMesh.GlobalRotation = new Vector3(MovementComp.State.Pitch, 0.0f, 0.0f);
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

    public void ApplyServerSnapshot(CharacterSnapshot snapshot, ushort lastProcessedClientTick)
    {
        _lastAckedClientCommandTick = lastProcessedClientTick;

        // Reset any values which haven't changed
        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.POSITION))
        {
            snapshot.Position = MovementComp.State.Position;
        }

        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.VELOCITY))
        {
            snapshot.Velocity = MovementComp.State.Velocity;
        }

        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.MOVE_MODE))
        {
            snapshot.MoveMode = MovementComp.State.MoveMode;
        }

        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.YAW))
        {
            snapshot.Yaw = MovementComp.State.Yaw;
        }

        if (!snapshot.DirtyFlags.HasFlag(CharacterSnapshotFlags.PITCH))
        {
            snapshot.Pitch = MovementComp.State.Pitch;
        }

        // if has flag -> jump then like pass the value of jumped this tick and then like snapshot move state gets 'jumped'


        var snapshotMoveState = snapshot.GetMoveState();

        if (IsLocal)
        {
            _unacknowledgedClientInputs.RemoveAll(cmd => cmd.ClientTick <= _lastAckedClientCommandTick);
            var reconciledState = snapshotMoveState;

            foreach (var cmd in _unacknowledgedClientInputs)
            {
                reconciledState = MovementComp.Step(reconciledState, cmd.Input, NetworkConstants.SERVER_TICK_INTERVAL);
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
        const float INTERP_THRESHOLD_H = 0.1f;      // Horizontal lerp start
        const float INTERP_THRESHOLD_V = 0.2f;     // Vertical lerp start

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
            GD.Print($"Snapping horizontal, error {distXZ}");
            currentPos.X = targetPos.X;
            currentPos.Z = targetPos.Z;
        }
        else if (distXZ > INTERP_THRESHOLD_H)
        {
            GD.Print($"Lepring horizontal, error {distXZ}");
            currentPos.X = Mathf.Lerp(currentPos.X, targetPos.X, INTERP_SPEED_H);
            currentPos.Z = Mathf.Lerp(currentPos.Z, targetPos.Z, INTERP_SPEED_H);
        }

        // --- Vertical correction ---
        if (deltaY > SNAP_THRESHOLD_V)
        {
            GD.Print($"Snapping vertical, error {deltaY}");
            currentPos.Y = targetPos.Y;
        }
        else if (deltaY > INTERP_THRESHOLD_V)
        {
            GD.Print($"Lerping vertical, error {deltaY}");
            currentPos.Y = Mathf.Lerp(currentPos.Y, targetPos.Y, INTERP_SPEED_V);
        }

        // Apply the corrected position and velocity
        MovementComp.State.Position = currentPos;
        MovementComp.State.Velocity = newPredictedState.Velocity;
    }

    public void ReceiveClientCommand(ClientCommand command)
    {
        foreach (var cmd in command.Commands)
        {
            if (!_unprocessedClientInputs.ContainsKey(cmd.ClientTick))
            {
                _unprocessedClientInputs.Add(cmd.ClientTick, cmd);
            }
        }
    }


    // predicting on the server so the server can also interpolate to this, just without snapshot based reconciliation
    public void HandleInput(InputCommand input, float delta)
    {
        MovementComp.HandleInput(input, delta);
    }

    public void SendClientInput(InputCommand newInput)
    {
        var inputCommand = new ClientInputCommand
        {
            ClientTick = MatchState.Instance.CurrentTick,
            Input = newInput,
            Yaw = GlobalRotation.Y,
            Pitch = _cameraPivot.GlobalRotation.X
        };

        _unacknowledgedClientInputs.Add(inputCommand);

        var commandsToSend = _unacknowledgedClientInputs
            .Skip(Math.Max(0, _unacknowledgedClientInputs.Count - REDUNDANT_INPUTS))
            .ToArray();


        ClientCommand.Send(commandsToSend, MatchState.Instance.CurrentTick);
    }

    public InputCommand CaptureInput()
    {
        InputCommand cmd = InputCommand.NONE;

        if (Input.IsActionPressed("move_forward")) cmd |= InputCommand.MOVE_FORWARD;
        if (Input.IsActionPressed("move_back")) cmd |= InputCommand.MOVE_BACK;
        if (Input.IsActionPressed("move_left")) cmd |= InputCommand.MOVE_LEFT;
        if (Input.IsActionPressed("move_right")) cmd |= InputCommand.MOVE_RIGHT;
        if (Input.IsActionPressed("jump")) cmd |= InputCommand.JUMP;
        if (Input.IsActionPressed("primary_fire")) cmd |= InputCommand.FIRE_PRIMARY;

        //LastInputCommand = cmd;

        //Weapon.HandleInput(cmd);

        return cmd;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if(!IsLocal)
        {
            return;
        }

        HandleMouseLook(@event);

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
    }


    public void HandleMouseLook(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {
            RotateY(Mathf.DegToRad(-mouseEvent.Relative.X * MouseSensitivity));

            Pitch += -mouseEvent.Relative.Y * MouseSensitivity;
            Pitch = Mathf.Clamp(Pitch, -90, 90);

            if (_cameraPivot != null)
            {
                _cameraPivot.RotationDegrees = new Vector3(Pitch, 0, 0);
            }

            MovementComp.State.Yaw = Yaw;
            MovementComp.State.Pitch = Pitch;
        }
    }
}