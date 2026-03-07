using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using static Godot.WebSocketPeer;


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

    [Export] Weapon _weapon;

    public CharacterMovement MovementComp { get; private set; } = new();

    public HealthComponent HealthComp { get; private set; } = new();

    private SortedDictionary<uint, ClientInputCommand> _unprocessedClientInputs = new SortedDictionary<uint, ClientInputCommand>();

    private uint _lastAckedClientCommandTick;

    private ClientInputCommand _lastProcessedClientCommand;

    private bool _useInterpolation = false;

    public override void _Ready()
    {
        base._Ready();

        Input.MouseMode = Input.MouseModeEnum.Captured;

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
            uint nextTick = _unprocessedClientInputs.Keys.Min();
            cmd = _unprocessedClientInputs[nextTick];
            _lastProcessedClientCommand = cmd;
            _unprocessedClientInputs.Remove(nextTick);
            _lastAckedClientCommandTick = nextTick;
        }
        // replay last input. TODO: consider replaying for a specific time and tracking state of missed inputs
        else
        {
            cmd = _lastProcessedClientCommand;
        }

        MovementComp.State = MovementComp.Step(MovementComp.State, cmd.Input, NetworkConstants.SERVER_TICK_INTERVAL);

        MovementComp.State.Yaw = cmd.Yaw;
        MovementComp.State.Pitch = cmd.Pitch;
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
                GlobalRotation = new Vector3(0.0f, MovementComp.State.Yaw, 0.0f);
                _thirdPersonWeaponMesh.GlobalRotation = new Vector3(MovementComp.State.Pitch, 0.0f, 0.0f);
            }
        }

    }

    public void InterpolatePosition(float interpSpeed)
    {
        GlobalPosition = GlobalPosition.Lerp(MovementComp.State.Position, LOCAL_SV_INTERP_RATE);
    }

    public void InterpolateYaw(float interpSpeed)
    {
        /*
        Vector3 rot = GlobalRotation;
        rot.Y = Mathf.LerpAngle(rot.Y, MovementComp.State.Yaw, interpSpeed);
        GlobalRotation = rot;*/
    }

    public void InterpolatePitch(float interpSpeed)
    {
        Vector3 camRot = _thirdPersonWeaponMesh.GlobalRotation;
        camRot.X = Mathf.Lerp(camRot.X, MovementComp.State.Pitch, interpSpeed);
        _thirdPersonWeaponMesh.GlobalRotation = camRot;
    }

    public void ApplyServerSnapshot(ArenaCharacterSnapshot snapshot, ushort lastProcessedClientTick)
    {
        _unacknowledgedClientInputs.RemoveAll(cmd => cmd.TickNumber <= lastProcessedClientTick);

        var snapshotMoveState = snapshot.GetMoveState();

        if (IsLocal)
        {
            var reconciledState = snapshotMoveState;
            foreach (var cmd in _unacknowledgedClientInputs)
            {
                reconciledState = MovementComp.Step(reconciledState, cmd.Input, NetworkConstants.SERVER_TICK_INTERVAL);
            }

            ReconcileMoveState(reconciledState);
        }
        else
        {
            MovementComp.State = snapshot.GetMoveState();
        }
    }

    public void ReceiveClientCommand(ClientCommand command)
    {
        GD.Print("Receiving client command entry");

        foreach (var cmd in command.Commands)
        {
            if (!_unprocessedClientInputs.ContainsKey(cmd.TickNumber))
            {
                _unprocessedClientInputs.Add(cmd.TickNumber, cmd);
                GD.Print("Receiving client command and adding command to unprocessed inputs");
            }
        }
    }

    public void ReconcileMoveState(CharacterMoveState newPredictedState)
    {
        float positionDelta = (MovementComp.State.Position - newPredictedState.Position).Length();

        const float SNAP_THRESHOLD = 1.0f;
        const float INTERP_THRESHOLD = 0.05f;

        if (positionDelta > SNAP_THRESHOLD)
        {
            MovementComp.State.Position = newPredictedState.Position;
            MovementComp.State.Velocity = newPredictedState.Velocity;
        }
        else if (positionDelta > INTERP_THRESHOLD)
        {
            MovementComp.State.Position = MovementComp.State.Position.Lerp(newPredictedState.Position, 0.5f);
            MovementComp.State.Velocity = newPredictedState.Velocity;
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
            TickNumber = MatchState.Instance.CurrentTick,
            Input = newInput,
            Yaw = GlobalRotation.Y,
            Pitch = Camera.GlobalRotation.Y
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
        if (Input.IsActionJustPressed("jump")) cmd |= InputCommand.JUMP;
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

            if (Camera != null)
            {
                Camera.RotationDegrees = new Vector3(Pitch, 0, 0);
            }

            MovementComp.State.Yaw = Yaw;
            MovementComp.State.Pitch = Pitch;
        }
    }
}