using Godot;
using System;
using System.Collections.Generic;
using static Godot.HttpRequest;

public enum CharacterMoveMode : byte
{
    GROUNDED,
    FALLING,
}

public class CharacterMovement
{
    private Character _character;

    // Movement parameters
    public float MaxGroundSpeed = 10.0f;
    public float Gravity = -5.0f;
    public Vector3 _gravityVector => new Vector3(0.0f, Gravity, 0.0f);
    public float JumpSpeed = 10.0f;
    public float MaxStepHeight = 0.25f;

    public float GroundAcceleration = 60.0f;
    public float _airAcceleration = 0.5f;
    public float GroundDeceleration = 100f;
    public float AirDeceleration = 5f;

    public bool PreserveHorizontalSpeedOnSlope = true;

    // Internal state

    private CapsuleShape3D _collisionCapsule;
    private Godot.Collections.Array<Rid> _characterCollisionRids = new();

    public Vector3 LaunchVector;
    public bool WasLaunched;

    private bool _jumpCooldownReady = true;
    public float HorizontalVelocity { get; private set; }
    public float VerticalVelocity { get; private set; }


    public int _ticksToIgnoreGroundPostJump = 10;

    public void Initialize(Character character)
    {
        _character = character;



        if (_character.CollisionShape.Shape is CapsuleShape3D capsule)
        {
            _collisionCapsule = capsule;
            _characterCollisionRids.Add(capsule.GetRid());
        }
        else
        {
            GD.PushError("Character does not have a CapsuleShape3D set as its collision shape.");
        }
    }



    // Step function now takes CharacterPublicState and returns it
    public CharacterPublicState Step(CharacterPublicState state, ClientInputCommand cmd, float delta, bool isSimulating = false)
    {
        ApplyInput(state, cmd, delta);

        CheckGrounded(state);

        switch (state.MovementMode)
        {
            case CharacterMoveMode.GROUNDED:
                HandleGroundedMovement(state, delta);
                break;
            case CharacterMoveMode.FALLING:
                HandleAerialMovement(state, delta);
                break;
        }

        GD.Print($"Desired direction: {_desiredDirection}. desired speed: {_desiredSpeed}. is grounded = {_isGrounded}. ground normal: {_groundNormal}. move mode: {state.MovementMode}");
        PerformMove(state, delta);

        return state;

        state.MovementMode = _isGrounded ? CharacterMoveMode.GROUNDED : CharacterMoveMode.FALLING;

        // Movement


        // Launch velocity
        if (cmd.Flags.HasFlag(InputFlags.WAS_LAUNCHED))
        {
            state.Velocity = new Vector3(state.Velocity.X, cmd.LaunchVelocity.Y, state.Velocity.Z);
            WasLaunched = false;
        }

        // Collision handling
        Vector3 safeMotion = HandleCollision(state, delta);

        state.Position += safeMotion;

        // Server-side collidable checks
        if (!isSimulating && _character.IsAuthority)
        {
            CheckCollidables(state);
        }

        HorizontalVelocity = new Vector2(state.Velocity.X, state.Velocity.Z).Length();
        VerticalVelocity = state.Velocity.Y;

        return state;
    }

    Vector3 _desiredDirection;
    float _desiredSpeed;
    bool _wantsToJump;


    public void ApplyInput(CharacterPublicState state, ClientInputCommand cmd, float delta)
    {
        Vector3 move = Vector3.Zero;
        if (cmd.Flags.HasFlag(InputFlags.FORWARD)) move.Z -= 1;
        if (cmd.Flags.HasFlag(InputFlags.BACKWARD)) move.Z += 1;
        if (cmd.Flags.HasFlag(InputFlags.STRAFE_LEFT)) move.X -= 1;
        if (cmd.Flags.HasFlag(InputFlags.STRAFE_RIGHT)) move.X += 1;

        _wantsToJump = cmd.Flags.HasFlag(InputFlags.JUMP);

        state.Look += cmd.Look;
        Basis basis = Basis.FromEuler(new Vector3(0, -state.Look.X, 0));

        if (move != Vector3.Zero)
        {
            _desiredDirection = (basis.Z * move.Z + basis.X * move.X).Normalized();
            _desiredSpeed = MaxGroundSpeed;
        }
        else
        {
            _desiredDirection = Vector3.Zero;
            _desiredSpeed = 0.0f;
        }
    }


    const float GROUNDED_CHECK_DISTANCE = 0.01f;
    const float MAX_WALKABLE_GROUND_ANGLE = 35.0f;
    float _walkableThreshold = MathF.Cos(Mathf.DegToRad(MAX_WALKABLE_GROUND_ANGLE));

    private bool _isGrounded = false;
    private Vector3 _groundNormal;
    private void CheckGrounded(CharacterPublicState state)
    {
        _isGrounded = false;

        var space = _character.GetWorld3D().DirectSpaceState;

        Vector3 motion = Vector3.Down * GROUNDED_CHECK_DISTANCE;

        PhysicsShapeQueryParameters3D query = new()
        {
            Shape = _collisionCapsule,
            Transform = new Transform3D(Basis.Identity, state.Position),
            Motion = motion,
            CollideWithBodies = true,
            CollideWithAreas = false
        };
        query.SetExclude(_characterCollisionRids);

        var result = space.CastMotion(query);
        bool hasGroundBelow = false;

        if (result[1] < 1.0f) // check if we would collide
        {
            hasGroundBelow = true;
        }

        if (hasGroundBelow)
        {
            PhysicsShapeQueryParameters3D groundedQuery = new()
            {
                Shape = _collisionCapsule,
                Transform = new Transform3D(Basis.Identity, state.Position + result[1] * motion),
                Motion = motion,
                CollideWithBodies = true,
                CollideWithAreas = false
            };
            groundedQuery.SetExclude(_characterCollisionRids);

            if (space.GetRestInfo(groundedQuery).TryGetValue("normal", out var normal))
            {
                _groundNormal = (Vector3)normal;

                if (_groundNormal.Dot(Vector3.Up) >= _walkableThreshold)
                {
                    _isGrounded = true;
                }
            }
        }
        else
        {
            _groundNormal = Vector3.Zero;
        }

        if(_isGrounded)
        {
            state.MovementMode = CharacterMoveMode.GROUNDED;
        }
        else
        {
            state.MovementMode |= CharacterMoveMode.FALLING;
        }
    }


    private void HandleGroundedMovement(CharacterPublicState state, float delta)
    {
        // Jump
        if (_wantsToJump && _isGrounded)
        {
            Jump(state);
            GD.Print($"JUMPED!");
            HandleAerialMovement(state, delta);
            return;
        }

        Vector3 horizontalVel = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

        if (_desiredDirection.LengthSquared() > 0)
        {
            horizontalVel = horizontalVel.MoveToward(_desiredDirection * MaxGroundSpeed, GroundAcceleration * delta);
        }
        else
        {
            float decel = GroundDeceleration * delta;
            if (horizontalVel.Length() <= decel)
            {
                horizontalVel = Vector3.Zero;
            }
            else
            {
                horizontalVel -= horizontalVel.Normalized() * decel;
            }
        }

        state.Velocity.X = horizontalVel.X;
        state.Velocity.Z = horizontalVel.Z;

        state.Velocity = ProjectVelocityOnGround(state.Velocity);
    }

    private void HandleAerialMovement(CharacterPublicState state, float delta)
    {
        ApplyAcceleration(state, _airAcceleration, delta);
        ApplyGravity(state, delta);


        return;
        Vector3 horizontalVel = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

        if (_desiredDirection.LengthSquared() > 0)
        {
            Vector3 velChange = _desiredDirection * MaxGroundSpeed * _airAcceleration * delta;
            horizontalVel += velChange;
        }
        else
        {
            float decel = AirDeceleration * delta;
            if (horizontalVel.Length() <= decel)
            {
                horizontalVel = Vector3.Zero;
            }
            else
            {
                horizontalVel -= horizontalVel.Normalized() * decel;
            }
        }

        // Clamp horizontal speed
        if (horizontalVel.Length() > MaxGroundSpeed)
            horizontalVel = horizontalVel.Normalized() * MaxGroundSpeed;

        state.Velocity.X = horizontalVel.X;
        state.Velocity.Z = horizontalVel.Z;

        state.Velocity.Y += Gravity * delta;

        state.Velocity = ProjectVelocityOnGround(state.Velocity);

    }

    public void ApplyAcceleration(CharacterPublicState state, float acceleration, float delta)
    {
        if (_desiredSpeed <= 0) return;

        float currentSpeed = state.Velocity.Dot(_desiredDirection);
        float addSpeed = _desiredSpeed - currentSpeed;

        if (addSpeed <= 0) return;

        float accelAmount = MathF.Min(acceleration, addSpeed);
        state.Velocity += _desiredDirection * accelAmount;
    }
    private void Jump(CharacterPublicState state)
    {
        state.Velocity.Y = Math.Max(state.Velocity.Y, 0f) + JumpSpeed;
        _isGrounded = false;
    }

    public void ApplyGravity(CharacterPublicState state, float delta)
    {
        state.Velocity += _gravityVector * delta;
    }

    public void PerformMove(CharacterPublicState state, float delta)
    {
        var space = _character.CollisionShape.GetWorld3D().DirectSpaceState;

        Vector3 remainingMotion = state.Velocity * delta;
        int maxIterations = 4; // handle multiple collisions in one frame

        _isGrounded = false;

        for (int i = 0; i < maxIterations; i++)
        {
            if (remainingMotion.Length() < 0.001f)
                break; // nothing left to move

            // Cast capsule along remaining motion
            PhysicsShapeQueryParameters3D query = new()
            {
                Shape = _collisionCapsule,
                Transform = new Transform3D(Basis.Identity, state.Position),
                Motion = remainingMotion,
                CollideWithBodies = true,
                CollideWithAreas = false
            };
            query.SetExclude(_characterCollisionRids);

            var result = space.CastMotion(query);

            float safeFraction = result[0];
            Vector3 safeMotion = remainingMotion * safeFraction;

            // Move as far as possible safely
            state.Position += safeMotion;

            // No collision? Done!
            if (safeFraction >= 1.0f)
                break;

            // Collision happened — get the normal
            if (!space.GetRestInfo(query).TryGetValue("normal", out var value))
                break;

            Vector3 collisionNormal = (Vector3)value;
            float collisionDot = collisionNormal.Dot(Vector3.Up);

            if (collisionDot >= _walkableThreshold)
            {
                // Landed on floor
                state.Velocity.Y = 0.0f;
                _isGrounded = true;

                // Slide remaining motion along the floor plane
                remainingMotion = SlideAlongSurface(remainingMotion * (1.0f - safeFraction), collisionNormal);
            }
            else
            {
                // Wall / steep slope → slide along it
                state.Velocity = SlideAlongSurface(state.Velocity, collisionNormal);
                remainingMotion = SlideAlongSurface(remainingMotion * (1.0f - safeFraction), collisionNormal);
            }
        }
    }
    public Vector3 SlideAlongSurface(Vector3 vector, Vector3 normal)
    {
        float dot = vector.Dot(normal);

        if (dot < 0f)
        {
            vector -= normal * dot;
        }

        return vector;
    }

    private Vector3 ProjectVelocityOnGround(Vector3 velocity)
    {
        return velocity - _groundNormal * velocity.Dot(_groundNormal);
    }

    private Vector3 HandleCollision(CharacterPublicState state, float delta)
    {
        return state.Velocity * delta;

        var space = _character.CollisionShape.GetWorld3D().DirectSpaceState;
        Vector3 motion = state.Velocity * delta;

        PhysicsShapeQueryParameters3D query = new()
        {
            Shape = _collisionCapsule,
            Transform = new Transform3D(Basis.Identity, state.Position),
            Motion = motion,
            CollideWithBodies = true,
            CollideWithAreas = false
        };
        query.SetExclude(_characterCollisionRids);

        var result = space.CastMotion(query);
        Vector3 safeMotion = motion * result[0];
        Vector3 unsafeMotion = motion * result[1];

        if (result[1] < 1.0f && space.GetRestInfo(query).TryGetValue("normal", out var n))
        {
            Vector3 normal = (Vector3)n;
            float slopeAngle = Mathf.RadToDeg(Mathf.Acos(normal.Dot(Vector3.Up)));

            if (slopeAngle <= MAX_WALKABLE_GROUND_ANGLE)
            {
                Vector3 slopeRight = normal.Cross(Vector3.Up).Normalized();
                Vector3 slopeForward = slopeRight.Cross(normal).Normalized();
                Vector3 slopeMotion = slopeForward * motion.Dot(slopeForward) + slopeRight * motion.Dot(slopeRight);

                if (!PreserveHorizontalSpeedOnSlope)
                    slopeMotion = slopeMotion.Normalized() * motion.Length();

                safeMotion = slopeMotion * result[0];
            }
            else
            {
                Vector3 stepUp = Vector3.Up * MaxStepHeight;
                query.Transform = new Transform3D(Basis.Identity, state.Position + stepUp);
                query.Motion = motion;
                result = space.CastMotion(query);
                safeMotion = (result[0] > 0.99f) ? stepUp + motion * result[0] : motion - new Vector3(normal.X, 0, normal.Z) * motion.Dot(new Vector3(normal.X, 0, normal.Z));
            }
        }

        return safeMotion;
    }

    public void QueueLaunch(Vector3 vector)
    {
        LaunchVector = vector;
        WasLaunched = true;
    }



    private void CheckCollidables(CharacterPublicState state)
    {
        if (_character == null || _collisionCapsule == null)
            return;

        var space = _character.GetWorld3D().DirectSpaceState;

        PhysicsShapeQueryParameters3D query = new()
        {
            Shape = _collisionCapsule,
            Transform = new Transform3D(Basis.Identity, state.Position),
            CollideWithBodies = true,
            CollideWithAreas = true,
            CollisionMask = PhysicsConstants.CHARACTER_COLLIDABLES_MASK
        };

        var results = space.IntersectShape(query, 8);

        foreach (var result in results)
        {
            if (result.TryGetValue("collider_id", out var idObj))
            {
                ulong id = (ulong)idObj;
                var node = GodotObject.InstanceFromId(id) as Node3D;
                if (node?.Owner is ICharacterCollidable collidable)
                    collidable.OnCollidedWith(_character);
            }
        }
    }
}