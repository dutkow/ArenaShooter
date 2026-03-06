using Godot;

public class CharacterMovementComponent
{
    private ArenaCharacter _character;

    public Vector3 Velocity;
    private Vector3 _targetVelocity;

    public MovementState MovementState;

    public int Speed = 15;
    public int FallAcceleration = 50;
    public float JumpVelocity = 20;
    public float AirControlAcceleration = 0.5f;

    public void SetCharacter(ArenaCharacter character)
    {
        _character = character;
    }

    public void Tick(InputCommand cmd, double delta, Node3D cameraPivot)
    {
        ApplyInput(cmd, delta, cameraPivot);

        _character.Velocity = Velocity;

        _character.MoveAndSlide();
        HandleFallAcceleration(delta);

    }

    public void ApplyInput(InputCommand cmd, double delta, Node3D cameraPivot)
    {
        Vector3 moveDir = Vector3.Zero;

        if (cmd.HasFlag(InputCommand.MOVE_FORWARD)) moveDir.Z -= 1f;
        if (cmd.HasFlag(InputCommand.MOVE_BACK)) moveDir.Z += 1f;
        if (cmd.HasFlag(InputCommand.MOVE_LEFT)) moveDir.X -= 1f;
        if (cmd.HasFlag(InputCommand.MOVE_RIGHT)) moveDir.X += 1f;

        if (moveDir != Vector3.Zero)
        {
            moveDir = moveDir.Normalized();

            if (cameraPivot != null)
                moveDir = moveDir.Rotated(Vector3.Up, cameraPivot.GlobalRotation.Y);
        }

        UpdateMovementState();

        if (cmd.HasFlag(InputCommand.JUMP) && CanJump())
        {
            TryJump();
        }

        if (MovementState == MovementState.GROUNDED)
        {
            _targetVelocity.X = moveDir.X * Speed;
            _targetVelocity.Z = moveDir.Z * Speed;
        }
        else
        {
            _targetVelocity.X += moveDir.X * AirControlAcceleration * (float)delta;
            _targetVelocity.Z += moveDir.Z * AirControlAcceleration * (float)delta;
        }

        Velocity = _targetVelocity;
    }

    private void HandleFallAcceleration(double delta)
    {
        if (!_character.IsOnFloor())
        {
            _targetVelocity.Y -= FallAcceleration * (float)delta;
        }
        else if (_targetVelocity.Y < 0)
        {
            _targetVelocity.Y = 0;
        }
    }

    private void UpdateMovementState()
    {
        MovementState = _character.IsOnFloor() ? MovementState.GROUNDED : MovementState.FALLING;
    }

    private bool CanJump()
    {
        return _character.IsOnFloor();
    }

    private void TryJump()
    {
        _targetVelocity.Y = JumpVelocity;
        MovementState = MovementState.FALLING;
    }

    public void Launch(Vector3 vel)
    {
        _targetVelocity += vel;
        Velocity = _targetVelocity;
        MovementState = MovementState.FALLING;
    }

    public void ResetVelocity()
    {
        _targetVelocity = Vector3.Zero;
        Velocity = Vector3.Zero;

        if (_character != null)
            _character.Velocity = Vector3.Zero;
    }
}