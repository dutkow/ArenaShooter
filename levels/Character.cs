using Godot;
using System.Collections.Generic;


public partial class Character : Pawn
{
    CharacterMoveMode _mode;


    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;

    // State
    private Vector3 _position = Vector3.Zero;
    private Vector3 _velocity = Vector3.Zero;
    private bool _isGrounded = false;


    [Export] public Camera3D Camera; // assign in editor
    [Export] public float MouseSensitivity = 0.1f;

    List<ClientInputCommand> _unacknowledgedInputs = new();

    private CharacterMovement _charMovement = new();

    const int REDUNDANT_INPUTS = 4;


    private float _pitch = 0f; // rotation around X


    ArenaCharacterSnapshot _lastServerSnapshot;

    private HealthComponent _healthComp = new();

    private uint _lastProcessedClientTick;

    private CharacterMoveState _predictedMoveState;

    public override void _Ready()
    {
        base._Ready();

        Role = NetworkRole.LOCAL;

        _position = GlobalPosition;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        _charMovement.Initialize(this);
    }

    public virtual void Tick(float delta)
    {
        base.Tick(delta);

        if(IsLocal)
        {
            var input = CaptureInput();
            HandleInput(input, delta);

            if(!IsAuthority)
            {
                SendClientInput(input);
            }
        }
        else
        {
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        InterpolateMovement((float)delta);
    }

    float LOCAL_SV_INTERP_RATE = 0.5f;
    float LOCAL_CL_INTERP_RATE = 0.5f;
    float REMOTE_CL_INTERP_RATE = 0.5f;

    public void InterpolateMovement(float delta)
    {
        if(IsAuthority)
        {
            if(IsLocal)
            {
                GlobalPosition.Lerp(_predictedMoveState.Position, LOCAL_SV_INTERP_RATE);
            }
        }
        else
        {
            if(IsLocal)
            {
                GlobalPosition.Lerp(_predictedMoveState.Position, LOCAL_CL_INTERP_RATE);
            }
            else
            {
                GlobalPosition.Lerp(_lastServerSnapshot.Position, REMOTE_CL_INTERP_RATE);
                GlobalRotation.Lerp(new Vector3(0.0f, _lastServerSnapshot.Yaw, REMOTE_CL_INTERP_RATE), REMOTE_CL_INTERP_RATE);
            }
        }
    }

    public void ApplyServerSnapshot(ArenaCharacterSnapshot snapshot, uint lastProcessedClientTick)
    {
        _lastServerSnapshot = snapshot;
        _unacknowledgedInputs.RemoveAll(cmd => cmd.TickNumber <= lastProcessedClientTick);

        if (IsLocal)
        {
            var reconciledState = snapshot.GetMoveState();

            foreach (var cmd in _unacknowledgedInputs)
            {
                reconciledState = _charMovement.Step(reconciledState, cmd.Input, NetworkConstants.SERVER_TICK_INTERVAL);
            }

            ReconcileMoveState(reconciledState);
        }
    }

    public void ReconcileMoveState(CharacterMoveState newPredictedState)
    {
        float positionDelta = (_predictedMoveState.Position - newPredictedState.Position).Length();

        const float SNAP_THRESHOLD = 1.0f;
        const float INTERP_THRESHOLD = 0.05f;

        if (positionDelta > SNAP_THRESHOLD)
        {
            _predictedMoveState.Position = newPredictedState.Position;
            _predictedMoveState.Velocity = newPredictedState.Velocity;
        }
        else if (positionDelta > INTERP_THRESHOLD)
        {
            _predictedMoveState.Position = _predictedMoveState.Position.Lerp(newPredictedState.Position, 0.5f);
            _predictedMoveState.Velocity = newPredictedState.Velocity;
        }
    }

    // predicting on the server so the server can also interpolate to this, just without snapshot based reconciliation
    public void HandleInput(InputCommand input, float delta)
    {
        _predictedMoveState = _charMovement.Step(_predictedMoveState, input, delta);
    }

    public void SendClientInput(InputCommand newInput)
    {
        var inputCommand = new ClientInputCommand();

        inputCommand.TickNumber = MatchState.Instance.CurrentTick;
        inputCommand.Input = newInput;
        inputCommand.Yaw = GlobalRotation.Y;
        inputCommand.Pitch = Camera.GlobalRotation.Y;

        _unacknowledgedInputs.Add(inputCommand);
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
    }


    public void HandleMouseLook(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent)
        {
            // Yaw: rotate the character around Y
            RotateY(Mathf.DegToRad(-mouseEvent.Relative.X * MouseSensitivity));

            // Pitch: rotate camera around X
            _pitch += -mouseEvent.Relative.Y * MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, -90, 90);

            if (Camera != null)
                Camera.RotationDegrees = new Vector3(_pitch, 0, 0);
        }
    }
}