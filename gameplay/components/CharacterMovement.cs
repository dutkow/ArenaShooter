using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using static Godot.WebSocketPeer;

public enum CharacterMoveMode : byte
{
    GROUNDED,
    FALLING,
}

public struct SlideResult
{
    public float SafePercent;
    public Vector3 Motion;
}

public struct SweepResult
{
    public float SafePercent;
    public float UnsafePercent;
    public Vector3 SafeMotion;
    public Vector3 UnsafeMotion;
    public Vector3 CollisionNormal;
    public Vector3 CollisionPoint;
    public CollisionType CollisionType;
}

public enum CollisionType
{
    NONE,
    FLOOR,
    WALL,
    CEILING,
}

public class CharacterMovement
{
    private Character _character;

    // Movement parameters
    public float MaxGroundSpeed = 10.0f;
    public float Gravity = 25.0f;
    public Vector3 _gravityVector => new Vector3(0.0f, -Gravity, 0.0f);
    public float JumpStrength = 10.0f;
    public const float MAX_STEP_HEIGHT = 0.45f;

    public float _walkAcceleration = 100.0f;
    public float _airAcceleration = 25.0f;
    public float _walkDeceleration = 100f;
    public float _airDeceleration = 0.0f;

    public bool PreserveHorizontalSpeedOnSlope = true;

    // Internal state

    private CapsuleShape3D _mainCollisionShape = new();

    private Godot.Collections.Array<Rid> _characterCollisionRids = new();



    public float HorizontalVelocity { get; private set; }
    public float VerticalVelocity { get; private set; }


    public int _ticksToIgnoreGroundPostJump = 10;

    private Vector3 _lastPosition;

    private bool _justJumped;

    public void Initialize(Character character)
    {
        _character = character;

        _characterCollisionRids.Add(_character.Area.GetRid());

        if (_character.CollisionShape.Shape is CapsuleShape3D collisionShape)
        {
            _mainCollisionShape = collisionShape;
            _characterCollisionRids.Add(_mainCollisionShape.GetRid());
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
        MoveAndSlide(state, delta);

        Vector3 deltaPos = state.Position - _lastPosition;

        HorizontalVelocity = new Vector2(deltaPos.X, deltaPos.Z).Length() / delta;
        VerticalVelocity = deltaPos.Y / delta;

        _lastPosition = state.Position;

        state.LastUnstuckPosition = state.Position;
        return state;
    }

    public void CheckGround(CharacterPublicState state, Vector3 startPosition, PhysicsDirectSpaceState3D space)
    {
        state.IsGrounded = false;

        Vector3 motion = Vector3.Down * GROUND_CLEARANCE;

        PhysicsShapeQueryParameters3D motionQuery = new()
        {
            Shape = _mainCollisionShape,
            Transform = new Transform3D(Basis.Identity, startPosition),
            Motion = motion,
            CollideWithBodies = true,
            CollideWithAreas = false
        };
        motionQuery.SetExclude(_characterCollisionRids);

        var motionQueryResult = space.CastMotion(motionQuery);

        var unsafeMotionPercent = motionQueryResult[1];

        if (unsafeMotionPercent < 1.0f)
        {
            var unsafeMotion = unsafeMotionPercent * motion;

            PhysicsShapeQueryParameters3D collisionQuery = new()
            {
                Shape = _mainCollisionShape,
                Transform = new Transform3D(Basis.Identity, startPosition + unsafeMotion),
                CollideWithBodies = true,
                CollideWithAreas = false
            };
            collisionQuery.SetExclude(_characterCollisionRids);

            var restInfo = space.GetRestInfo(collisionQuery);
            if (restInfo.TryGetValue("normal", out var normal))
            {
                state.GroundNormal = (Vector3)normal;

                if (state.GroundNormal.Dot(Vector3.Up) >= _walkableThreshold)
                {
                    state.IsGrounded = true;
                }
            }
        }
    }

    public CharacterPublicState MoveAndSlide(CharacterPublicState state, float delta)
    {
        var space = _character.GetWorld3D().DirectSpaceState;

        CheckGround(state, state.Position, space);

        if (state.IsGrounded)
        {
            MoveAndSlideGrounded(state, space, delta);
        }
        else
        {
            MoveAndSlideAir(state, space, delta);
        }

        state.ticksRemainingBeforeJump--;

        return state;
    }

    public CharacterPublicState MoveAndSlideGrounded(CharacterPublicState state, PhysicsDirectSpaceState3D space, float delta)
    {
        if (state.WantsToJump && state.ticksRemainingBeforeJump <= 0)
        {
            Jump(state);
            MoveAndSlideAir(state, space, delta);
            return state;
        }

        ApplyAcceleration(state, _walkAcceleration, _walkDeceleration, delta);

        if (state.Velocity == Vector3.Zero)
        {
            return state;
        }

        state.Velocity = state.Velocity.Slide(state.GroundNormal);

        StepAndSlide(state, space, delta, true);

        return state;
    }

    public void StepAndSlide(CharacterPublicState state, PhysicsDirectSpaceState3D space, float delta, bool groundedMove)
    {

        Vector3 remainingMotion = state.Velocity * delta;
        float remainingDistance = remainingMotion.Length();
        Vector3 direction = remainingMotion.Normalized();


        bool moveComplete = false;
        int maxSlides = 4;
        for (int i = 0; i < maxSlides; ++i)
        {
            if (moveComplete)
            {
                break;
            }

            Vector3 targetMotion = direction * remainingDistance;

            var sweepResult = Sweep(state, state.Position, space, targetMotion);

            if (sweepResult.SafePercent >= 1.0f)
            {
                state.Position += sweepResult.SafeMotion;
                remainingDistance -= sweepResult.SafeMotion.Length();
                moveComplete = true;

            }
            else
            {
                if (sweepResult.CollisionType == CollisionType.FLOOR)
                {
                    Vector3 motion = targetMotion.Slide(sweepResult.CollisionNormal);
                    motion = new Vector3(targetMotion.X, motion.Y, targetMotion.Z);
                    state.Position += sweepResult.SafeMotion;
                    remainingDistance -= sweepResult.SafeMotion.Length();
                }
                else if (sweepResult.CollisionType == CollisionType.WALL)
                {
                    GD.Print($"colliding with wall");
                    // TRY TO STEP FIRST
                    bool steppedUp = false;

                    if (groundedMove)
                    {
                        // Test that we can move up before stepping forward
                        Vector3 stepPosition = state.Position;

                        // step back from the wall slightly

                        float pushBackLength = 0.01f;
                        Vector3 pushBack = sweepResult.CollisionNormal * pushBackLength;
                        pushBack = pushBack.Slide(state.GroundNormal);
                        stepPosition += pushBack;


                        Vector3 upMotion = new Vector3(0.0f, MAX_STEP_HEIGHT, 0.0f);
                        var upSweep = Sweep(state, stepPosition, space, upMotion);

                        GD.Print($"up sweep safe motion length = {upSweep.SafeMotion.Length()}");

                        if (upSweep.SafePercent > 0.25f)
                        {
                            stepPosition += upSweep.SafeMotion;

                            // Try to move in the original intended direction from the step up position
                            float minForwardStep = 0.25f + pushBackLength;

                            if(targetMotion.Length() < minForwardStep)
                            {
                                targetMotion = targetMotion.Normalized() * minForwardStep;
                            }

                            var forwardSweep = Sweep(state, stepPosition, space, targetMotion);

                            GD.Print($"forward sweep safe percent= {forwardSweep.SafePercent}");

                            if (forwardSweep.SafePercent >= 1.0f)
                            {
                                GD.Print($"forward sweep success");
                                stepPosition += forwardSweep.SafeMotion;

                                // Finally, sweep back down to floor
                                var downMotion = new Vector3(0.0f, -MAX_STEP_HEIGHT, 0.0f);
                                var downSweep = Sweep(state, stepPosition, space, downMotion);


                                state.Position += upSweep.SafeMotion + forwardSweep.SafeMotion + downSweep.SafeMotion + new Vector3(0.0f, GROUND_CLEARANCE * 2.0f, 0.0f);

                                steppedUp = true;
                            }
                           
                        }
                    }

                    // STEP FAILED, THIS IS A WALL
                    if(!steppedUp)
                    {
                        GD.Print($"SLIDING ON WALL");
                        state.Position += sweepResult.SafeMotion;

                        remainingMotion = targetMotion - sweepResult.SafeMotion;

                        float pushAmount = 0.01f;
                        Vector3 pushVector = sweepResult.CollisionNormal * pushAmount;

                        if (groundedMove)
                        {
                            pushVector = pushVector.Slide(state.GroundNormal);
                        }

                        state.Position += pushVector;

                        remainingDistance = remainingMotion.Length();

                    }

                    CheckStuck(state, space);

                }
            }

            if (!moveComplete && remainingDistance < 0.001f)
            {
                moveComplete = true;
            }
        }
    }

    public void CheckStuck(CharacterPublicState state, PhysicsDirectSpaceState3D space)
    {
        PhysicsShapeQueryParameters3D query = new()
        {
            Shape = _mainCollisionShape,
            Transform = new Transform3D(Basis.Identity, state.Position),
            CollideWithBodies = true,
            CollideWithAreas = true
        };
        query.SetExclude(_characterCollisionRids);

        var trappedResult = space.GetRestInfo(query);

        if (trappedResult.Count > 0)
        {
            GD.Print($"we are stuck!");
            state.Position = state.LastUnstuckPosition;
        }
    }

    public SweepResult Sweep(CharacterPublicState state, Vector3 startPosition, PhysicsDirectSpaceState3D space, Vector3 motion)
    {

        PhysicsShapeQueryParameters3D motionQuery = new()
        {
            Shape = _mainCollisionShape,
            Transform = new Transform3D(Basis.Identity, startPosition),
            Motion = motion,
            CollideWithBodies = true,
            CollideWithAreas = true
        };
        motionQuery.SetExclude(_characterCollisionRids);

        SweepResult sweepResult = new();

        var queryResult = space.CastMotion(motionQuery);

        sweepResult.SafePercent = queryResult[0];
        sweepResult.SafeMotion = motion * sweepResult.SafePercent;

        // If safe motion >= 1.0f, we don't need the other results
        if (sweepResult.SafePercent < 1.0f)
        {
            sweepResult.UnsafePercent = queryResult[1];
            sweepResult.UnsafeMotion = motion * sweepResult.UnsafePercent;

            PhysicsShapeQueryParameters3D collisionQuery = new()
            {
                Shape = _mainCollisionShape,
                Transform = new Transform3D(Basis.Identity, state.Position + sweepResult.UnsafeMotion),
                CollideWithBodies = true,
                CollideWithAreas = true
            };
            collisionQuery.SetExclude(_characterCollisionRids);

            var restInfo = space.GetRestInfo(collisionQuery);
            if (restInfo.TryGetValue("normal", out var normal))
            {
                sweepResult.CollisionNormal = (Vector3)normal;

                if (sweepResult.CollisionNormal.Dot(Vector3.Up) >= _walkableThreshold)
                {
                    sweepResult.CollisionType = CollisionType.FLOOR;
                }
                else
                {
                    sweepResult.CollisionType = CollisionType.WALL;
                }
            }

            if (restInfo.TryGetValue("point", out var point))
            {
                sweepResult.CollisionPoint = (Vector3)point;
            }
        }
        return sweepResult;
    }


    public CharacterPublicState MoveAndSlideAir(CharacterPublicState state, PhysicsDirectSpaceState3D space, float delta)
    {
        ApplyAcceleration(state, _airAcceleration, _airDeceleration, delta);
        ApplyGravity(state, delta);

        if (state.Velocity == Vector3.Zero)
        {
            return state;
        }

        StepAndSlide(state, space, delta, false);

        return state;
    }

    public void ApplyInput(CharacterPublicState state, ClientInputCommand cmd, float delta)
    {
        Vector3 move = Vector3.Zero;
        if (cmd.Flags.HasFlag(InputFlags.FORWARD)) move.Z -= 1;
        if (cmd.Flags.HasFlag(InputFlags.BACKWARD)) move.Z += 1;
        if (cmd.Flags.HasFlag(InputFlags.STRAFE_LEFT)) move.X -= 1;
        if (cmd.Flags.HasFlag(InputFlags.STRAFE_RIGHT)) move.X += 1;

        state.WantsToJump = cmd.Flags.HasFlag(InputFlags.JUMP);

        state.Yaw += cmd.Look.X;
        state.Pitch += cmd.Look.Y;

        Basis basis = Basis.FromEuler(new Vector3(0, state.Yaw, 0));

        if (move != Vector3.Zero)
        {
            state.DesiredDirection = (basis.Z * move.Z + basis.X * move.X).Normalized();
            state.DesiredSpeed = MaxGroundSpeed;
        }
        else
        {
            state.DesiredDirection = Vector3.Zero;
            state.DesiredSpeed = 0.0f;
        }
    }

    const float GROUND_CLEARANCE = 0.5f;
    const float MAX_WALKABLE_GROUND_ANGLE = 35.0f;
    float _walkableThreshold = MathF.Cos(Mathf.DegToRad(MAX_WALKABLE_GROUND_ANGLE));



    public void ApplyAcceleration(CharacterPublicState state, float acceleration, float deceleration, float delta)
    {
        // Correctly clamped acceleration
        Vector3 horizontalVel = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

        if (state.DesiredSpeed > 0)
        {
            Vector3 desiredVelocity = state.DesiredDirection * state.DesiredSpeed;

            Vector3 deltaV = desiredVelocity - horizontalVel;

            float maxDelta = acceleration * delta;

            if (deltaV.Length() > maxDelta)
            {
                deltaV = deltaV.Normalized() * maxDelta;
            }

            horizontalVel += deltaV;
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

    const int MinTicksBetweenJumps = 20;

    private void Jump(CharacterPublicState state)
    {
        state.Velocity.Y = Math.Max(state.Velocity.Y, 0f) + JumpStrength;
        state.MovementMode = CharacterMoveMode.FALLING;
        state.IsGrounded = false;
        state.ticksRemainingBeforeJump = MinTicksBetweenJumps;
        GD.Print($"JUMP");
    }

    public void ApplyGravity(CharacterPublicState state, float delta)
    {
        state.Velocity += _gravityVector * delta;
    }

    const float SAFE_MOTION_PADDING = 0.05f;
   
    private void CheckCollidables(CharacterPublicState state, bool isSimulating)
    {

        if(isSimulating)
        {
            foreach (var collidable in state.NewlyOverlappedCollidables)
            {
                collidable.OnCollidedWith(_character, state, true);
            }
        }
        else
        {
            state.NewlyOverlappedCollidables.Clear();

            var space = _character.GetWorld3D().DirectSpaceState;

            PhysicsShapeQueryParameters3D query = new()
            {
                Shape = _mainCollisionShape,
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
                            collidable.OnCollidedWith(_character, state, isSimulating);
                            state.NewlyOverlappedCollidables.Add(collidable);
                        }
                    }
                }
            }
            state.CurrentCollidables = newCollidables;
        }
    }


    public CharacterPublicState QueueLaunch(CharacterPublicState state, Vector3 launchVelocity)
    {
        state.WasLaunched = true;
        state.LaunchVelocity = launchVelocity;

        return state;
    }

    public bool CheckLaunch(CharacterPublicState state, bool isSimulating)
    {
        if (state.WasLaunched)
        {
            GD.Print($"launching on client. is simulating: {isSimulating}. num collidables = {state.NewlyOverlappedCollidables.Count}");


            state.Velocity += state.LaunchVelocity;
            state.MovementMode = CharacterMoveMode.FALLING;

            state.WasLaunched = false;
            state.IsGrounded = false;

            return true;
        }
        return false;
    }

    public CharacterPublicState PreReconciliationReset(CharacterPublicState state)
    {
        state.WasLaunched = false;
        state.CurrentCollidables.Clear();

        return state;
    }
}