using Godot;
using System;

public enum CharacterMoveMode : byte
{
    GROUNDED,
    FALLING,
}

public struct CharacterMoveState
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float Pitch;
    public CharacterMoveMode MoveMode;
}

public class CharacterMovement
{
    private Character _character;

    public float Speed = 10.0f;
    public float Gravity = -15.0f;
    public float JumpSpeed = 10.0f;

    // Only used for server-side grounded logic
    private bool _isGrounded = false;

    public CharacterMoveState State;

    public void Initialize(Character character)
    {
        _character = character;
    }

    public CharacterMoveState Step(CharacterMoveState state, InputCommand inputCommand, float delta)
    {
        // --- Movement input ---
        Vector3 move = Vector3.Zero;
        if (inputCommand.HasFlag(InputCommand.MOVE_FORWARD)) move.Z -= 1;
        if (inputCommand.HasFlag(InputCommand.MOVE_BACK)) move.Z += 1;
        if (inputCommand.HasFlag(InputCommand.MOVE_LEFT)) move.X -= 1;
        if (inputCommand.HasFlag(InputCommand.MOVE_RIGHT)) move.X += 1;
        move = move.Normalized() * Speed;

        var basis = Basis.FromEuler(new Vector3(0, state.Yaw, 0));
        Vector3 moveDir = (basis.Z * move.Z + basis.X * move.X).Normalized() * Speed;

        state.Velocity.X = moveDir.X;
        state.Velocity.Z = moveDir.Z;

        // --- Gravity & jump ---
        bool grounded;
        if (_character.IsAuthority)
        {
            // Server uses real raycast
            grounded = CheckGroundedServer(state.Position);
        }
        else
        {
            // Client prediction: assume floor at Y = 0
            grounded = state.Position.Y <= 0f && state.Velocity.Y <= 0f;
        }

        // Jump
        if (inputCommand.HasFlag(InputCommand.JUMP) && grounded)
        {
            state.Velocity.Y = JumpSpeed;
            grounded = false; // leaving ground
        }
        else if (!grounded)
        {
            // Apply gravity only when falling
            state.Velocity.Y += Gravity * delta;
        }
        else
        {
            // Stop downward velocity when grounded
            if (state.Velocity.Y < 0) state.Velocity.Y = 0;
        }

        // --- Apply movement ---
        state.Position += state.Velocity * delta;

        // --- Update MoveMode ---
        state.MoveMode = grounded ? CharacterMoveMode.GROUNDED : CharacterMoveMode.FALLING;

        return state;
    }

    private bool CheckGroundedServer(Vector3 position)
    {
        var spaceState = _character.GetWorld3D().DirectSpaceState;

        Vector3 from = position;
        Vector3 to = from + Vector3.Down * 0.5f;

        PhysicsRayQueryParameters3D query = new PhysicsRayQueryParameters3D
        {
            From = from,
            To = to,
            CollideWithBodies = true,
            CollideWithAreas = true,
            Exclude = new Godot.Collections.Array<Rid> { _character.Area.GetRid() }
        };

        var result = spaceState.IntersectRay(query);
        bool grounded = result.Count > 0;

        _isGrounded = grounded;
        return grounded;
    }

    public void HandleInput(InputCommand input, float delta)
    {
        State = Step(State, input, delta);
    }
}