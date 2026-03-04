using Godot;
using System;

public enum MovementState
{
    GROUNDED,
    FALLING
}

public partial class ArenaCharacter : Pawn
{
    // ----------------------
    // Exports & Components
    // ----------------------
    [Export] public CharacterBody3D Body;
    [Export] public Camera3D Camera;
    [Export] public Marker3D CameraPivot;
    [Export] private Weapon _equippedWeapon;

    [Export] public int Speed { get; set; } = 14;
    [Export] public int FallAcceleration { get; set; } = 50;
    [Export] public float _jumpVelocity { get; set; } = 20f;
    [Export] public float _airControlAcceleration { get; set; } = 6f;
    [Export] public float MouseSens = 0.09f;
    [Export] public float MouseSmooth = 50f;

    // ----------------------
    // State
    // ----------------------
    public bool IsAlive;
    public PlayerState State { get; private set; }

    private MovementState _movementState;
    private bool _weaponsEnabled = true;
    private bool _isMouseCaptured = true;

    private Vector3 _targetVelocity = Vector3.Zero;
    private Vector2 _cameraInput = Vector2.Zero;
    private Vector2 _rotVelocity = Vector2.Zero;

    private bool _canJump => Body.IsOnFloor();
    public float Yaw => Body.GlobalRotation.Y;
    public float AimPitch => CameraPivot.Rotation.X;

    // ----------------------
    // Initialization
    // ----------------------
    public override void _Ready()
    {
        base._Ready();

        Camera.Current = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void OnPossessed(Controller controller)
    {
        base.OnPossessed(controller);
        Camera.Current = true;
    }

    public void Initialize(PlayerState state)
    {
        if(state == null)
        {
            GD.PushError("State was null on arena character initialization");
            return;
        }
        State = state;
        State.Character = this;
    }

    public void TeleportTo(Transform3D t)
    {
        GlobalTransform = t;
        Body.Velocity = Vector3.Zero;
    }

    public void ResetMovement()
    {
        Body.Velocity = Vector3.Zero;
    }

    public void SetWeaponsEnabled(bool enabled) => _weaponsEnabled = enabled;

    // ----------------------
    // Input & Mouse
    // ----------------------
    public override void _Input(InputEvent @event)
    {
        if(!InputActive)
        {
            return;
        }

        if (@event is InputEventMouseMotion mouseEvent && Input.MouseMode == Input.MouseModeEnum.Captured)
            _cameraInput = mouseEvent.Relative;

        if (_weaponsEnabled && Input.IsActionPressed("primary_fire"))
            TryPrimaryFire();

        if (Input.IsActionJustPressed("toggle_cursor_lock"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }
    }

    public override void _Process(double delta)
    {
        // Mouse smoothing
        _rotVelocity = _rotVelocity.Lerp(_cameraInput * MouseSens, (float)delta * MouseSmooth);

        if (CameraPivot != null)
        {
            CameraPivot.RotateX(-Mathf.DegToRad(_rotVelocity.Y));
            CameraPivot.Rotation = new Vector3(
                Mathf.Clamp(CameraPivot.Rotation.X, -1.5f, 1.5f),
                CameraPivot.Rotation.Y,
                CameraPivot.Rotation.Z
            );
        }

        // Rotate horizontally
        Body.RotateY(-Mathf.DegToRad(_rotVelocity.X));

        // Reset mouse input for next frame
        _cameraInput = Vector2.Zero;
    }

    // ----------------------
    // Movement & Jumping
    // ----------------------
    public void HandleMovement(double delta)
    {
        if (!InputActive)
        {
            return;
        }

        Vector3 inputDir = Vector3.Zero;
        inputDir.X = Input.GetActionStrength("move_right") - Input.GetActionStrength("move_left");
        inputDir.Z = Input.GetActionStrength("move_back") - Input.GetActionStrength("move_forward");

        if (inputDir != Vector3.Zero)
        {
            inputDir = inputDir.Normalized();
            if (CameraPivot != null)
                inputDir = inputDir.Rotated(Vector3.Up, CameraPivot.GlobalRotation.Y);
        }

        UpdateMovementState();

        if (Input.IsActionJustPressed("jump"))
            TryJump();

        if (_movementState == MovementState.GROUNDED)
        {
            _targetVelocity.X = inputDir.X * Speed;
            _targetVelocity.Z = inputDir.Z * Speed;
        }
        else
        {
            _targetVelocity.X += inputDir.X * _airControlAcceleration * (float)delta;
            _targetVelocity.Z += inputDir.Z * _airControlAcceleration * (float)delta;
        }

        // Gravity
        if (!Body.IsOnFloor())
        {
            _targetVelocity.Y -= FallAcceleration * (float)delta;
        }
        else if (_targetVelocity.Y < 0)
        {
            _targetVelocity.Y = 0;
        }

        // Apply movement
        Body.Velocity = _targetVelocity;
        Body.MoveAndSlide();
    }

    public void UpdateMovementState()
    {
        MovementState newState = Body.IsOnFloor() ? MovementState.GROUNDED : MovementState.FALLING;
        if (_movementState != newState)
        {
            _movementState = newState;
        }
    }

    public void TryPrimaryFire()
    {
        if (_equippedWeapon == null) return;
        Vector3 dir = -Camera.GlobalTransform.Basis.Z;
        _equippedWeapon.TryPrimaryFire(Camera.GlobalPosition, dir);
    }

    public void TryJump()
    {
        if (!_canJump) return;
        _targetVelocity.Y = _jumpVelocity;
        _movementState = MovementState.FALLING;
    }

    // ----------------------
    // Client Command Sending
    // ----------------------
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        HandleMovement(delta);
        SendClientCommand();
    }

    private void SendClientCommand()
    {
        if (!IsPossessedLocally || !NetworkSession.Instance.IsClient)
        {
            return;
        }

        InputCommand buttons = InputCommand.NONE;

        if (Input.IsActionPressed("move_forward")) buttons |= InputCommand.MOVE_FORWARD;
        if (Input.IsActionPressed("move_back")) buttons |= InputCommand.MOVE_BACK;
        if (Input.IsActionPressed("move_left")) buttons |= InputCommand.MOVE_LEFT;
        if (Input.IsActionPressed("move_right")) buttons |= InputCommand.MOVE_RIGHT;
        if (Input.IsActionJustPressed("jump")) buttons |= InputCommand.JUMP;
        if (Input.IsActionPressed("primary_fire")) buttons |= InputCommand.FIRE_PRIMARY;

        var cmd = new ClientCommand()
        {
            PlayerID = State.PlayerID,
            TickNumber = MatchState.Instance.CurrentTick,
            Buttons = buttons,
            YawDelta = _rotVelocity.X,
            PitchDelta = _rotVelocity.Y
        };

        ClientCommand.Send(cmd, null);
    }

    public void ApplyClientCommand(ClientCommand cmd, float delta)
    {
        Vector3 moveDir = Vector3.Zero;

        if (cmd.Buttons.HasFlag(InputCommand.MOVE_FORWARD)) moveDir.Z -= 1f;
        if (cmd.Buttons.HasFlag(InputCommand.MOVE_BACK)) moveDir.Z += 1f;
        if (cmd.Buttons.HasFlag(InputCommand.MOVE_LEFT)) moveDir.X -= 1f;
        if (cmd.Buttons.HasFlag(InputCommand.MOVE_RIGHT)) moveDir.X += 1f;

        // Apply camera rotation

        RotateY(-cmd.YawDelta);
        if (CameraPivot != null)
        {
            CameraPivot.RotateX(-cmd.PitchDelta);
            var r = CameraPivot.Rotation;
            CameraPivot.Rotation = new Vector3(
                Mathf.Clamp(r.X, -1.5f, 1.5f),
                r.Y,
                r.Z
            );
        }

        if (moveDir != Vector3.Zero) moveDir = moveDir.Normalized();

        // Movement
        if (_movementState == MovementState.GROUNDED)
        {
            _targetVelocity.X = moveDir.X * Speed;
            _targetVelocity.Z = moveDir.Z * Speed;
        }
        else
        {
            _targetVelocity.X += moveDir.X * _airControlAcceleration * delta;
            _targetVelocity.Z += moveDir.Z * _airControlAcceleration * delta;
        }

        // Jump
        if (cmd.Buttons.HasFlag(InputCommand.JUMP) && _canJump)
        {
            _targetVelocity.Y = _jumpVelocity;
            _movementState = MovementState.FALLING;
        }

        // Gravity
        if (!Body.IsOnFloor())
        {
            _targetVelocity.Y -= FallAcceleration * delta;
        }
        else if (_targetVelocity.Y < 0)
        {
            _targetVelocity.Y = 0;
        }

        Body.Velocity = _targetVelocity;
        Body.MoveAndSlide();
    }
}