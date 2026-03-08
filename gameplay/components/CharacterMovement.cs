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

    public float AirControl = 0.3f;

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
        Vector3 desiredMoveDirection = (basis.Z * move.Z + basis.X * move.X).Normalized() * Speed;

        // --- Gravity & jump ---
        CheckGrounded();

        GD.Print($"Grounded: {_isGrounded}");

        

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

        state.MoveMode = _isGrounded ? CharacterMoveMode.GROUNDED : CharacterMoveMode.FALLING;

        switch(state.MoveMode)
        {
            case CharacterMoveMode.GROUNDED:
                HandleGroundedMovement(desiredMoveDirection);
                break;

            case CharacterMoveMode.FALLING:
                HandleAerialMovement(desiredMoveDirection);
                break;
        }

        HandleCollision(state, delta);

        return state;
    }

    private void HandleCollision(CharacterMoveState state, float delta)
    {
        var space = _character.CollisionShape.GetWorld3D().DirectSpaceState;

        Vector3 motion = state.Velocity * delta;

        var query = new PhysicsShapeQueryParameters3D();
        query.Shape = _collisionCapsule;
        query.Transform = new Transform3D(Basis.Identity, _character.CollisionShape.GlobalPosition);
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

        float beginTraceOffset = 0.25f;
        float groundTraceDistance = 0.02f;
        Vector3 from = _character.CollisionShape.GlobalPosition + Vector3.Down * (_collisionCapsule.MidHeight - beginTraceOffset);
        Vector3 to = from + Vector3.Down * (beginTraceOffset + groundTraceDistance);

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

    private void HandleGroundedMovement(Vector3 desiredMoveDirection)
    {
        State.Velocity.X = desiredMoveDirection.X;
        State.Velocity.Z = desiredMoveDirection.Z;
    }

    private void HandleAerialMovement(Vector3 desiredMoveDirection)
    {
        Vector3 horizontalVel = new Vector3(State.Velocity.X, 0, State.Velocity.Z);

        Vector3 accel = desiredMoveDirection - horizontalVel;

        float accelFactor = _isGrounded ? 1f : AirControl;
        horizontalVel += accel * accelFactor;

        if (horizontalVel.Length() > Speed)
            horizontalVel = horizontalVel.Normalized() * Speed;

        State.Velocity.X = horizontalVel.X;
        State.Velocity.Z = horizontalVel.Z;
    }
}