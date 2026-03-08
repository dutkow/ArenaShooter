using Godot;
using Godot.Collections;
using System;
using System.Linq;
using static Godot.Image;

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

    public float MaxGroundSpeed = 10.0f;
    public float Gravity = -20.0f;
    public float JumpSpeed = 10.0f;

    public float GroundAcceleration = 60.0f;
    public float AirAcceleration = 1.0f;

    public float GroundDeceleration = 100f;
    public float AirDeceleration = 5f;

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

        move = move.Normalized() * MaxGroundSpeed;
        var basis = Basis.FromEuler(new Vector3(0, state.Yaw, 0));
        Vector3 desiredMoveDirection = (basis.Z * move.Z + basis.X * move.X).Normalized() * MaxGroundSpeed;

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

        state.MoveMode = _isGrounded ? CharacterMoveMode.GROUNDED : CharacterMoveMode.FALLING;

        switch(state.MoveMode)
        {
            case CharacterMoveMode.GROUNDED:
                HandleGroundedMovement(desiredMoveDirection, delta);
                break;

            case CharacterMoveMode.FALLING:
                HandleAerialMovement(desiredMoveDirection, delta);
                break;
        }

        Vector3 safeMotion = HandleCollision(state, delta);

        if (_isGrounded)
        {
            state.Position += safeMotion;
        }
        else
        {
            float deltaY = state.Velocity.Y * delta - safeMotion.Y;
            safeMotion.Y += deltaY;
            state.Position += safeMotion;
        }

        return state;
    }

    private Vector3 HandleCollision(CharacterMoveState state, float delta)
    {
        var space = _character.CollisionShape.GetWorld3D().DirectSpaceState;

        Vector3 motion = state.Velocity * delta;

        // Initial cast from current position along desired motion
        var initialQuery = new PhysicsShapeQueryParameters3D
        {
            Shape = _collisionCapsule,
            Transform = new Transform3D(Basis.Identity, _character.CollisionShape.GlobalPosition),
            Motion = motion,
            CollideWithBodies = true,
            CollideWithAreas = false
        };
        initialQuery.SetExclude(_characterCollisionRids);

        var result = space.CastMotion(initialQuery);

        Vector3 safeMotion = motion * result[0];
        Vector3 unsafeMotion = motion * result[1];

        // Cast at the point of collision to get the collision normal
        var normalQuery = new PhysicsShapeQueryParameters3D
        {
            Shape = _collisionCapsule,
            Transform = initialQuery.Transform.Translated(unsafeMotion),
            Motion = Vector3.Zero,
            CollideWithBodies = true,
            CollideWithAreas = false
        };
        normalQuery.SetExclude(_characterCollisionRids);

        var collisions = space.GetRestInfo(normalQuery);
        if (collisions.Count > 0 && collisions.TryGetValue("normal", out var value))
        {
            Vector3 normal = (Vector3)value;

            // Only slide along horizontal surfaces, ignore vertical-only normals (ground/ceiling)
            Vector3 horizontalNormal = new Vector3(normal.X, 0, normal.Z);
            if (horizontalNormal.LengthSquared() > 0.01f)
            {
                horizontalNormal = horizontalNormal.Normalized();

                // Project motion along the wall plane to slide
                safeMotion = motion - horizontalNormal * motion.Dot(horizontalNormal);

                // Optional: recast the adjusted motion to prevent clipping
                initialQuery.Motion = safeMotion;
                result = space.CastMotion(initialQuery);
                safeMotion = safeMotion * result[0];
            }
        }

        // Return safe motion; main loop decides how to apply it
        return safeMotion;
    }

    private void CheckGrounded()
    {
        var spaceState = _character.GetWorld3D().DirectSpaceState;

        float beginTraceOffset = 0.25f;
        float groundTraceDistance = 0.25f;
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

    private void HandleGroundedMovement(Vector3 desiredMoveDirection, float delta)
    {
        Vector3 horizontalVel = new Vector3(State.Velocity.X, 0, State.Velocity.Z);

        if (desiredMoveDirection.LengthSquared() > 0)
        {
            Vector3 desiredDir = desiredMoveDirection.Normalized();

            // Split velocity into components parallel and perpendicular to input
            float forwardSpeed = horizontalVel.Dot(desiredDir);
            Vector3 forwardVel = desiredDir * forwardSpeed;
            Vector3 perpVel = horizontalVel - forwardVel;

            // Cancel perpendicular momentum immediately
            horizontalVel = forwardVel;

            // Accelerate along desired input direction
            float accelAmount = GroundAcceleration * delta;
            float targetSpeed = MaxGroundSpeed;
            float newForwardSpeed = forwardSpeed + accelAmount;
            if (newForwardSpeed > targetSpeed)
                newForwardSpeed = targetSpeed;

            horizontalVel = desiredDir * newForwardSpeed;
        }
        else
        {
            // No input → decelerate all horizontal movement
            float decelAmount = GroundDeceleration * delta;
            if (horizontalVel.Length() <= decelAmount)
                horizontalVel = Vector3.Zero;
            else
                horizontalVel -= horizontalVel.Normalized() * decelAmount;
        }

        State.Velocity.X = horizontalVel.X;
        State.Velocity.Z = horizontalVel.Z;
    }

    private void HandleAerialMovement(Vector3 desiredMoveDirection, float delta)
    {
        Vector3 horizontalVel = new Vector3(State.Velocity.X, 0, State.Velocity.Z);

        if (desiredMoveDirection.LengthSquared() > 0)
        {
            Vector3 desiredDir = desiredMoveDirection.Normalized();
            float accelSpeed = MaxGroundSpeed * AirAcceleration;
            Vector3 velChange = desiredDir * accelSpeed * delta;
            horizontalVel += velChange;
        }
        else
        {
            // Decelerate slowly in air
            float decelAmount = AirDeceleration * delta;
            if (horizontalVel.Length() <= decelAmount)
                horizontalVel = Vector3.Zero;
            else
                horizontalVel -= horizontalVel.Normalized() * decelAmount;
        }

        // Clamp horizontal speed
        if (horizontalVel.Length() > MaxGroundSpeed)
            horizontalVel = horizontalVel.Normalized() * MaxGroundSpeed;

        State.Velocity.X = horizontalVel.X;
        State.Velocity.Z = horizontalVel.Z;
    }
}