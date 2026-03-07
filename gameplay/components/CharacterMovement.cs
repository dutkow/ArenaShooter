using Godot;
using System;

public enum CharacterMoveMode
{
    GROUNDED,
    FALLING,
}

public struct CharacterMoveState
{
    public Vector3 Position;
    public float Yaw;
    public float Pitch;
    public Vector3 Velocity;
    public bool IsGrounded;
}


public class CharacterMovement
{
    Character _character;

    public float Speed = 15.0f;
    public float Gravity = -12.0f;
    public float JumpSpeed = 5.0f;

    private bool _isGrounded = false;

    private CharacterMoveMode _mode;

    public CharacterMoveState State;

    public void Initialize(Character character)
    {
        _character = character;
    }

    public CharacterMoveState Step(CharacterMoveState state, InputCommand inputCommand, float delta)
    {
        Vector3 move = Vector3.Zero;

        if (inputCommand.HasFlag(InputCommand.MOVE_FORWARD))
        {
            move.Z -= 1;
        }
        if (inputCommand.HasFlag(InputCommand.MOVE_BACK))
        {
            move.Z += 1;
        }
        if (inputCommand.HasFlag(InputCommand.MOVE_LEFT))
        {
            move.X -= 1;
        }
        if (inputCommand.HasFlag(InputCommand.MOVE_RIGHT))
        {
            move.X += 1;
        }

        move = move.Normalized() * Speed;

        Vector3 moveDirection = Vector3.Zero;

        var rotation = new Vector3(0.0f, state.Yaw, 0.0f);
        var basis = Basis.FromEuler(rotation);

        Vector3 forward = basis.Z;
        Vector3 right = basis.X;

        moveDirection += forward * move.Z;
        moveDirection += right * move.X;

        moveDirection = moveDirection.Normalized() * Speed;

        // Gravity
        if (!IsGrounded())
        {
            state.Velocity.Y += Gravity * delta;
        }
        else if (state.Velocity.Y < 0)
        {
            state.Velocity.Y = 0;
        }

        if (inputCommand.HasFlag(InputCommand.JUMP))
        {
            state.Velocity.Y = JumpSpeed;
        }

        state.Velocity.X = moveDirection.X;
        state.Velocity.Z = moveDirection.Z;

        state.Position += state.Velocity * delta;

        if (_isGrounded && state.Velocity.Y < 0)
        {
            state.Velocity.Y = 0;
        }
        return state;
    }


    private bool IsGrounded()
    {
        // Get the physics space
        var spaceState = _character.GetWorld3D().DirectSpaceState;

        // Start at the character position
        Vector3 from = State.Position;
        Vector3 to = from + Vector3.Down * 0.5f; // 1000 meters down

        // Build raycast parameters
        PhysicsRayQueryParameters3D query = new PhysicsRayQueryParameters3D
        {
            From = from,
            To = to,
            CollideWithBodies = true,
            CollideWithAreas = true,
            Exclude = new Godot.Collections.Array<Rid> { _character.Area.GetRid() } // ignore self
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
        SetMode(CharacterMoveMode.GROUNDED);
    }

    public void OnStartedFalling()
    {
        SetMode(CharacterMoveMode.FALLING);
    }

    public void SetMode(CharacterMoveMode mode)
    {
        if (_mode != mode)
        {
            _mode = mode;
        }
    }

    public void SetIsGrounded(bool isGrounded)
    {
        if (_isGrounded != isGrounded)
        {
            _isGrounded = isGrounded;

            if (_isGrounded)
            {
                OnLanded();
            }
            else
            {
                OnStartedFalling();
            }
        }
    }

    public void HandleInput(InputCommand input, float delta)
    {
        State = Step(State, input, delta);
    }
}
