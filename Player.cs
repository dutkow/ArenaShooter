using Godot;
using System;

public enum MovementState
{
    GROUNDED,
    FALLING
}

public partial class Player : CharacterBody3D
{
    // Movement
    [Export] public int Speed { get; set; } = 14;                     // Ground speed
    [Export] public int FallAcceleration { get; set; } = 50;          // Gravity
    private Vector3 _targetVelocity = Vector3.Zero;
    [Export] public float _jumpVelocity { get; set; } = 20.0f;
    [Export] public float _airControlAcceleration { get; set; } = 6f; // Arena-style air control (per second)

    // Mouse look
    [Export] public Camera3D Camera;
    [Export] public Marker3D CameraPivot; // the part that tilts up/down
    [Export] public float MouseSens = 0.09f;
    [Export] public float MouseSmooth = 50f;

    private Vector2 _cameraInput = Vector2.Zero;
    private Vector2 _rotVelocity = Vector2.Zero;
    private bool _isMouseCaptured = true;

    private MovementState _movementState;
    private bool _canJump => IsOnFloor();

    [Export] private Weapon _equippedWeapon;
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseEvent && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _cameraInput = mouseEvent.Relative;
        }
    }

    public override void _Process(double delta)
    {
        // Toggle mouse capture
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            _isMouseCaptured = !_isMouseCaptured;
            Input.MouseMode = _isMouseCaptured
                ? Input.MouseModeEnum.Captured
                : Input.MouseModeEnum.Visible;
        }

        // Smooth mouse look
        _rotVelocity = _rotVelocity.Lerp(_cameraInput * MouseSens, (float)delta * MouseSmooth);

        if (CameraPivot != null)
        {
            CameraPivot.RotateX(-Mathf.DegToRad(_rotVelocity.Y));
            CameraPivot.Rotation = new Vector3(
                Mathf.Clamp(CameraPivot.Rotation.X, -1.5f, 1.5f), // clamp vertical rotation
                CameraPivot.Rotation.Y,
                CameraPivot.Rotation.Z
            );
        }

        // Rotate player horizontally
        RotateY(-Mathf.DegToRad(_rotVelocity.X));

        // Reset mouse input
        _cameraInput = Vector2.Zero;
    }

    public override void _PhysicsProcess(double delta)
    {
        var inputDir = Vector3.Zero;

        if (Input.IsActionPressed("move_right")) inputDir.X += 1.0f;
        if (Input.IsActionPressed("move_left")) inputDir.X -= 1.0f;
        if (Input.IsActionPressed("move_back")) inputDir.Z += 1.0f;
        if (Input.IsActionPressed("move_forward")) inputDir.Z -= 1.0f;

        if (inputDir != Vector3.Zero)
        {
            inputDir = inputDir.Normalized();
            if (CameraPivot != null)
                inputDir = inputDir.Rotated(Vector3.Up, CameraPivot.GlobalRotation.Y);
        }

        UpdateMovementState();

        // Jump input
        if (Input.IsActionJustPressed("jump"))
        {
            TryJump();
        }

        // Horizontal movement
        if (_movementState == MovementState.GROUNDED)
        {
            // Full control on ground
            _targetVelocity.X = inputDir.X * Speed;
            _targetVelocity.Z = inputDir.Z * Speed;
        }
        else
        {
            // Air control (arena style)
            // Preserve momentum, add input as acceleration
            _targetVelocity.X += inputDir.X * _airControlAcceleration * (float)delta;
            _targetVelocity.Z += inputDir.Z * _airControlAcceleration * (float)delta;
        }

        // Gravity
        if (!IsOnFloor())
        {
            _targetVelocity.Y -= FallAcceleration * (float)delta;
        }
        else if (_targetVelocity.Y < 0)
        {
            // Reset downward velocity when grounded
            _targetVelocity.Y = 0;
        }

        // Move the character
        Velocity = _targetVelocity;
        MoveAndSlide();
    }

    public void UpdateMovementState()
    {
        MovementState newMovementState = IsOnFloor() ? MovementState.GROUNDED : MovementState.FALLING;

        if (_movementState != newMovementState)
        {
            _movementState = newMovementState;
            if (_movementState == MovementState.FALLING)
            {
                GD.Print("Player left the ground!");
            }
            else
            {
                GD.Print("Player landed!");
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);

        if (Input.IsActionPressed("primary_fire"))
        {
            TryPrimaryFire();
        }
    }

    public void TryPrimaryFire()
    {
        Vector3 fireDirection = -Camera.GlobalTransform.Basis.Z; // camera forward
        _equippedWeapon.TryPrimaryFire(Camera.GlobalPosition, fireDirection);
    }

    public void TryJump()
    {
        if (_canJump)
        {
            _targetVelocity.Y = _jumpVelocity;
            _movementState = MovementState.FALLING; // immediately set to falling after jump
        }
    }
}