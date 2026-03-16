using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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
    public float Gravity = 25.0f;
    public Vector3 _gravityVector => new Vector3(0.0f, -Gravity, 0.0f);
    public float JumpSpeed = 10.0f;
    public float MaxStepHeight = 0.25f;

    public float _walkAcceleration = 100.0f;
    public float _airAcceleration = 5.0f;
    public float _walkDeceleration = 100f;
    public float _airDeceleration = 0.0f;

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

        if(!CheckLaunch(state))
        {
            CheckGrounded(state);
        }

        switch (state.MovementMode)
            {
                case CharacterMoveMode.GROUNDED:
                    HandleGroundedMovement(state, delta);
                    break;
                case CharacterMoveMode.FALLING:
                    HandleAerialMovement(state, delta);
                    break;
            }

        PerformMove(state, delta);

        HorizontalVelocity = new Vector2(state.Velocity.X, state.Velocity.Z).Length();
        VerticalVelocity = state.Velocity.Y;

        CheckCollidables(state, isSimulating);

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
            state.MovementMode = CharacterMoveMode.FALLING;
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
        ApplyAcceleration(state, _walkAcceleration, _walkDeceleration, delta);

        ProjectVelocityOnGround(state);
    }

    private void HandleAerialMovement(CharacterPublicState state, float delta)
    {
        ApplyAcceleration(state, _airAcceleration, _airDeceleration, delta);
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
            float decel = _airDeceleration * delta;
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
        {
            horizontalVel = horizontalVel.Normalized() * MaxGroundSpeed;
        }

        state.Velocity.X = horizontalVel.X;
        state.Velocity.Z = horizontalVel.Z;

        state.Velocity.Y += Gravity * delta;

    }

    public void ApplyAcceleration(CharacterPublicState state, float acceleration, float deceleration, float delta)
    {
        // Correctly clamped acceleration
        Vector3 horizontalVel = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

        // apply acceleration
        if (_desiredSpeed > 0)
        {
            float currentSpeedInDir = horizontalVel.Dot(_desiredDirection);
            float addSpeed = _desiredSpeed - currentSpeedInDir;
            if (addSpeed > 0)
            {
                float accelAmount = MathF.Min(acceleration * delta, addSpeed);
                horizontalVel += _desiredDirection * accelAmount;
            }
        }
        // apply deceleration if no input
        else
        {
            float speed = horizontalVel.Length();
            if (speed > 0)
            {
                float decelAmount = deceleration * delta;
                if (speed <= decelAmount)
                    horizontalVel = Vector3.Zero;
                else
                    horizontalVel -= horizontalVel.Normalized() * decelAmount;
            }
        }

        float totalSpeed = horizontalVel.Length();
        if (totalSpeed > MaxGroundSpeed)
        {
            horizontalVel = horizontalVel.Normalized() * MaxGroundSpeed;
        }


        state.Velocity.X = horizontalVel.X;
        state.Velocity.Z = horizontalVel.Z;
    }


    private void Jump(CharacterPublicState state)
    {
        state.Velocity.Y = Math.Max(state.Velocity.Y, 0f) + JumpSpeed;
        state.MovementMode = CharacterMoveMode.FALLING;
        _isGrounded = false;
    }

    public void ApplyGravity(CharacterPublicState state, float delta)
    {
        state.Velocity += _gravityVector * delta;
    }

    const float SAFE_MOTION_PADDING = 0.01f;
    public void PerformMove(CharacterPublicState state, float delta)
    {
        var space = _character.CollisionShape.GetWorld3D().DirectSpaceState;

        Vector3 remainingMotion = state.Velocity * delta;
        int maxIterations = 4; // handle multiple collisions in one frame

        for (int i = 0; i < maxIterations; i++)
        {
            if (remainingMotion.Length() < 0.001f)
            {
                break; // nothing left to move
            }

            // Check movement
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

            Vector3 safeMotion;
            if(i == 0)
            {
                safeMotion = remainingMotion * (safeFraction);
            }
            else
            {
                safeMotion = remainingMotion * (safeFraction - SAFE_MOTION_PADDING);

            }

            // Move as far as possible safely
            state.Position += safeMotion;

            // No collision? Done!
            if (safeFraction >= 1.0f)
            {
                break;
            }

            // We collided, check collision

            float unsafeFraction = result[1];
            Vector3 unsafeMotion = remainingMotion * unsafeFraction;

            PhysicsShapeQueryParameters3D collisionQuery = new()
            {
                Shape = _collisionCapsule,
                Transform = new Transform3D(Basis.Identity, state.Position + unsafeMotion),
                Motion = remainingMotion,
                CollideWithBodies = true,
                CollideWithAreas = false
            };
            collisionQuery.SetExclude(_characterCollisionRids);

            var collisionResult = space.CastMotion(collisionQuery);

            // Collision happened — get the normal
            if (!space.GetRestInfo(collisionQuery).TryGetValue("normal", out var value))
            {
                break;
            }

            Vector3 collisionNormal = (Vector3)value;


            remainingMotion = remainingMotion * (1.0f - safeFraction);


            remainingMotion = remainingMotion - collisionNormal * remainingMotion.Dot(collisionNormal);
        }
    }

    private void ProjectVelocityOnGround(CharacterPublicState state)
    {
        state.Velocity -= _groundNormal * state.Velocity.Dot(_groundNormal);
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


    private void CheckCollidables(CharacterPublicState state, bool isSimulating)
    {
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

        List<ICharacterCollidable> newCollidables = new();

        foreach (var result in results)
        {
            if (result.TryGetValue("collider_id", out var idObj))
            {
                ulong id = (ulong)idObj;
                var node = GodotObject.InstanceFromId(id) as Node3D;
                if (node?.Owner is ICharacterCollidable collidable)
                {
                    
                    newCollidables.Add(collidable);

                    if (!state.CurrentCollidables.Contains(collidable))
                    {
                        collidable.OnCollidedWith(_character);
                    }

                }

            }
        }

        state.CurrentCollidables = newCollidables;

    }





    public CharacterPublicState QueueLaunch(CharacterPublicState state, Vector3 launchVelocity)
    {
        state.WasLaunched = true;
        state.LaunchVelocity = launchVelocity;

        return state;
    }

    public bool CheckLaunch(CharacterPublicState state)
    {
        if (state.WasLaunched)
        {

            state.Velocity += state.LaunchVelocity;
            state.MovementMode = CharacterMoveMode.FALLING;

            state.WasLaunched = false;
            state.IsGrounded = false;

            return true;
        }
        return false;
    }

    public void PreConciliationReset(CharacterPublicState state)
    {
        state.WasLaunched = false;
        state.CurrentCollidables.Clear();

    }
}