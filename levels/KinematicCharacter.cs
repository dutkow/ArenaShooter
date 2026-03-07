using Godot;
using System;
using System.Linq;

public enum CharacterMoveState
{
    GROUNDED,
    FALLING,
}
public partial class KinematicCharacter : Node3D
{
    CharacterMoveState _moveState;

    [Export] public float Speed = 10.0f;
    [Export] public float Gravity = -20.0f;      // Units per second^2
    [Export] public float JumpSpeed = 5.0f;
    [Export] public Area3D Area;
    [Export] public CollisionShape3D CollisionShape;

    // State
    private Vector3 _position = Vector3.Zero;
    private Vector3 _velocity = Vector3.Zero;
    private bool _isGrounded = false;


    [Export] public Camera3D Camera; // assign in editor
    [Export] public float MouseSensitivity = 0.1f;

    private float _pitch = 0f; // rotation around X
    public override void _Ready()
    {
        base._Ready();

        _position = GlobalPosition;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
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

    public override void _Process(double delta)
    {
        base._Process(delta);

        RaycastGrounded();
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        InputCommand inputCommand = InputCommand.NONE;

        if (Input.IsActionPressed("move_forward"))
            inputCommand |= InputCommand.MOVE_FORWARD;
        if (Input.IsActionPressed("move_back"))
            inputCommand |= InputCommand.MOVE_BACK;
        if (Input.IsActionPressed("move_left"))
            inputCommand |= InputCommand.MOVE_LEFT;
        if (Input.IsActionPressed("move_right"))
            inputCommand |= InputCommand.MOVE_RIGHT;
        if (Input.IsActionPressed("jump"))
            inputCommand |= InputCommand.JUMP;

        Step(inputCommand, (float)delta);
    }

    public void Step(InputCommand inputCommand, float delta)
    {
        Vector3 move = Vector3.Zero;

        if (inputCommand.HasFlag(InputCommand.MOVE_FORWARD))
            move.Z -= 1;
        if (inputCommand.HasFlag(InputCommand.MOVE_BACK))
            move.Z += 1;
        if (inputCommand.HasFlag(InputCommand.MOVE_LEFT))
            move.X -= 1;
        if (inputCommand.HasFlag(InputCommand.MOVE_RIGHT))
            move.X += 1;

        move = move.Normalized() * Speed;

        Vector3 moveDirection = Vector3.Zero;

        Vector3 forward = GlobalTransform.Basis.Z; 
        Vector3 right = GlobalTransform.Basis.X;

        moveDirection += forward * move.Z;
        moveDirection += right * move.X;

        moveDirection = moveDirection.Normalized() * Speed;

        // Gravity
        if (!_isGrounded)
            _velocity.Y += Gravity * delta;
        else if (_velocity.Y < 0)
            _velocity.Y = 0;

        if (inputCommand.HasFlag(InputCommand.JUMP))
            _velocity.Y = JumpSpeed;

        _velocity.X = moveDirection.X;
        _velocity.Z = moveDirection.Z;

        _position += _velocity * delta;

        if (_isGrounded && _velocity.Y < 0)
            _velocity.Y = 0;

        GlobalPosition = _position;
    }

    private bool RaycastGrounded()
    {
        // Get the physics space
        var spaceState = GetWorld3D().DirectSpaceState;

        // Start at the character position
        Vector3 from = GlobalPosition;
        Vector3 to = from + Vector3.Down * 0.5f; // 1000 meters down

        // Build raycast parameters
        PhysicsRayQueryParameters3D query = new PhysicsRayQueryParameters3D
        {
            From = from,
            To = to,
            CollideWithBodies = true,
            CollideWithAreas = true,
            Exclude = new Godot.Collections.Array<Rid> { Area.GetRid() } // ignore self
        };

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            SetIsGrounded(true);
        }
        else
        {
            SetIsGrounded(false);
        }

        return _isGrounded;
    }

    public void OnLanded()
    {
        GD.Print("landed");
        SetMoveState(CharacterMoveState.GROUNDED);
    }

    public void OnStartedFalling()
    {
        SetMoveState(CharacterMoveState.FALLING);
    }

    public void SetMoveState(CharacterMoveState moveState)
    {
        if(_moveState != moveState)
        {
            _moveState = moveState;
        }
    }

    public void SetIsGrounded(bool isGrounded)
    {
        if( _isGrounded != isGrounded )
        {
            _isGrounded = isGrounded;

            if(_isGrounded)
            {
                OnLanded();
            }
            else
            {
                OnStartedFalling();
            }
        }
    }
}