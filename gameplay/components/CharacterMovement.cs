using Godot;
using Godot.Collections;
using System;
using System.Linq;

public enum CharacterMoveMode : byte
{
    GROUNDED,
    FALLING,
}

public class CharacterMoveState
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

    public CharacterMoveState State = new();

    private CapsuleShape3D _collisionCapsule;
    private Array<Rid> _characterCollisionRids = new();

    public void Initialize(Character character)
    {
        _character = character;

        if(_character.CollisionShape.Shape is CapsuleShape3D collisionCapsule)
        {
            _collisionCapsule = collisionCapsule;
            _characterCollisionRids.Add(collisionCapsule.GetRid());
        }
        else
        {
            GD.PushError("Character does not have a CapsuleShape3D set as its collision shape.");
        }
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
        CheckGrounded();

        // Jump
        if (inputCommand.HasFlag(InputCommand.JUMP) && _isGrounded)
        {
            state.Velocity.Y = JumpSpeed;
            _isGrounded = false;
        }
        else if (!_isGrounded)
        {
            state.Velocity.Y += Gravity * delta;
        }
        else
        {
            if (state.Velocity.Y < 0) state.Velocity.Y = 0;
        }

        //state.Position += state.Velocity * delta;
        MoveWithCollision(state, delta);

        state.MoveMode = _isGrounded ? CharacterMoveMode.GROUNDED : CharacterMoveMode.FALLING;

        return state;
    }

    private void MoveWithCollision(CharacterMoveState state, float delta)
    {
        var space = _character.GetWorld3D().DirectSpaceState;

        Vector3 motion = state.Velocity * delta;

        var query = new PhysicsShapeQueryParameters3D();
        query.Shape = _collisionCapsule;
        query.Transform = new Transform3D(Basis.Identity, state.Position);
        query.Motion = motion;
        query.CollideWithBodies = true;
        query.CollideWithAreas = false;
        query.SetExclude(_characterCollisionRids);

        var result = space.CastMotion(query);

        state.Position += motion * result[0];
        //GD.Print($"Safe collision: {result[0]}. Unsafe collision: {result[1]}. Desired motion = {motion}");
    }
    private void CheckGrounded()
    {
        var spaceState = _character.GetWorld3D().DirectSpaceState;

        float traceBeginOffset = 0.1f;
        float groundedEpsilon = 1.0f;
        Vector3 from = State.Position + new Vector3(0.0f, -_collisionCapsule.MidHeight, 0.0f);
        Vector3 to = from + Vector3.Down * (groundedEpsilon - traceBeginOffset);

        PhysicsRayQueryParameters3D query = new PhysicsRayQueryParameters3D
        {
            From = from,
            To = to,
            CollideWithBodies = true,
            CollideWithAreas = false
        };

        query.SetExclude(_characterCollisionRids);

        var result = spaceState.IntersectRay(query);
        bool grounded = result.Count > 0;

        _isGrounded = grounded;
    }

    public void HandleInput(InputCommand input, float delta)
    {
        State = Step(State, input, delta);
    }
}