using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
using static Godot.WebSocketPeer;

public enum CharacterMovementMode : byte
{
    GROUNDED,
    FALLING,
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



[Flags]
public enum CharacterMoveStateFlags : byte
{
    POSITION_CHANGED,
    VELOCITY_CHANGED,
    ROTATION_CHANGED,
}
public struct CharacterMoveState
{
    // Replicated
    public CharacterMoveStateFlags Flags;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float Pitch;

    // Non-replicated, internal state simulation
    public bool IsGrounded;
    public Vector3 GroundNormal;
    public int TicksRemainingBeforeJump;
    public Vector3 LastUnstuckPosition;
    public Vector3 DesiredDirection;
    public float DesiredSpeed;
    public CharacterMovementMode Mode;
    public List<ICharacterCollidable> NewlyOverlappedCollidables;
    public List<ICharacterCollidable> CurrentCollidables;
    public bool WasLaunched;
    public Vector3 LaunchVelocity;
}



public class CharacterMovement
{
    public CharacterMoveState State;
    public CharacterMoveState PredictedState;

    private bool _isSkippingReconciliation;
    private int _ticksUntilReconciliationResume;

    public Character Character;
    private PhysicsDirectSpaceState3D _space;

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


    public void OnCharacterSpawned(Character character)
    {
        Character = character;

        _space = Character.GetWorld3D().DirectSpaceState;

        _characterCollisionRids.Add(Character.Area.GetRid());

        if (Character.CollisionShape.Shape is CapsuleShape3D collisionShape)
        {
            _mainCollisionShape = collisionShape;
            _characterCollisionRids.Add(_mainCollisionShape.GetRid());
        }

        SetPosition(Character.GlobalPosition);
        SetVelocity(Vector3.Zero);
        SetRotation(Character.GlobalRotation.Y, 0.0f);
    }

    public void ServerProcessNextClientInput(ClientInputCommand cmd, float delta)
    {
        Step(ref State, cmd, delta);
    }

    public void Step(ref CharacterMoveState state, ClientInputCommand cmd, float delta, bool isSimulating = false)
    {
        ProcessInput(ref state, cmd, delta);
        MoveAndSlide(ref state, cmd, delta);

        Vector3 deltaPos = state.Position - _lastPosition;

        HorizontalVelocity = new Vector2(deltaPos.X, deltaPos.Z).Length() / delta;
        VerticalVelocity = deltaPos.Y / delta;

        _lastPosition = state.Position;

        state.LastUnstuckPosition = state.Position;
    }

    public void CheckGround(ref CharacterMoveState state, Vector3 startPosition)
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

        var motionQueryResult = _space.CastMotion(motionQuery);

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

            var restInfo = _space.GetRestInfo(collisionQuery);
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

    public void MoveAndSlide(ref CharacterMoveState state, ClientInputCommand cmd, float delta)
    {
        CheckGround(ref state, state.Position);

        if (state.IsGrounded)
        {
            MoveAndSlideGrounded(ref state, cmd, delta);
        }
        else
        {
            MoveAndSlideAir(ref state, cmd, delta);
        }

        state.TicksRemainingBeforeJump--;
    }

    public void MoveAndSlideGrounded(ref CharacterMoveState state, ClientInputCommand cmd, float delta)
    {
        if ((cmd.Flags & InputFlags.JUMP) != 0 && state.TicksRemainingBeforeJump <= 0)
        {
            Jump(ref state);
            MoveAndSlideAir(ref state, cmd, delta);
            return;
        }

        ApplyAcceleration(ref state, _walkAcceleration, _walkDeceleration, delta);

        if (state.Velocity == Vector3.Zero)
        {
            return;
        }

        state.Velocity = state.Velocity.Slide(state.GroundNormal);

        StepAndSlide(ref state, delta, true);

        return;
    }

    public void StepAndSlide(ref CharacterMoveState state, float delta, bool groundedMove)
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

            var sweepResult = Sweep(ref state, state.Position, targetMotion);

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
                    bool steppedUp = false;

                    // TRY TO STEP FIRST

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
                        var upSweep = Sweep(ref state, stepPosition, upMotion);

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

                            var forwardSweep = Sweep(ref state, stepPosition, targetMotion);

                            GD.Print($"forward sweep safe percent= {forwardSweep.SafePercent}");

                            if (forwardSweep.SafePercent >= 1.0f)
                            {
                                GD.Print($"forward sweep success");
                                stepPosition += forwardSweep.SafeMotion;

                                // Finally, sweep back down to floor
                                var downMotion = new Vector3(0.0f, -MAX_STEP_HEIGHT, 0.0f);
                                var downSweep = Sweep(ref state, stepPosition, downMotion);


                                state.Position += upSweep.SafeMotion + forwardSweep.SafeMotion + downSweep.SafeMotion + new Vector3(0.0f, GROUND_CLEARANCE * 2.0f, 0.0f);

                                steppedUp = true;
                            }
                           
                        }
                    }
                    
                    // STEP FAILED, THIS IS A WALL
                    if (!steppedUp)
                    {
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

                    CheckStuck(ref state);

                }
            }

            if (!moveComplete && remainingDistance < 0.001f)
            {
                moveComplete = true;
            }
        }
    }

    public void CheckStuck(ref CharacterMoveState state)
    {
        PhysicsShapeQueryParameters3D query = new()
        {
            Shape = _mainCollisionShape,
            Transform = new Transform3D(Basis.Identity, state.Position),
            CollideWithBodies = true,
            CollideWithAreas = true
        };
        query.SetExclude(_characterCollisionRids);

        var trappedResult = _space.GetRestInfo(query);

        if (trappedResult.Count > 0)
        {
            GD.Print($"we are stuck!");
            state.Position = state.LastUnstuckPosition;
        }
    }

    public SweepResult Sweep(ref CharacterMoveState state, Vector3 startPosition, Vector3 motion)
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

        var queryResult = _space.CastMotion(motionQuery);

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

            var restInfo = _space.GetRestInfo(collisionQuery);
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


    public void MoveAndSlideAir(ref CharacterMoveState state, ClientInputCommand cmd, float delta)
    {
        ApplyAcceleration(ref state, _airAcceleration, _airDeceleration, delta);
        ApplyGravity(ref state, delta);

        if (state.Velocity == Vector3.Zero)
        {
            return;
        }

        StepAndSlide(ref state, delta, false);

        return;
    }

    public void ProcessInput(ref CharacterMoveState state, ClientInputCommand cmd, float delta)
    {
        Vector3 move = Vector3.Zero;
        if ((cmd.Flags & InputFlags.FORWARD) != 0) move.Z -= 1;
        if ((cmd.Flags & InputFlags.BACKWARD) != 0) move.Z += 1;
        if ((cmd.Flags & InputFlags.STRAFE_LEFT) != 0) move.X -= 1;
        if ((cmd.Flags & InputFlags.STRAFE_RIGHT) != 0) move.X += 1;

        state.Yaw -= cmd.Look.X;
        state.Pitch -= cmd.Look.Y;

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



    public void ApplyAcceleration(ref CharacterMoveState state, float acceleration, float deceleration, float delta)
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

    private void Jump(ref CharacterMoveState state)
    {
        state.Velocity.Y = Math.Max(state.Velocity.Y, 0f) + JumpStrength;
        state.Mode = CharacterMovementMode.FALLING;
        state.IsGrounded = false;
        state.TicksRemainingBeforeJump = MinTicksBetweenJumps;
        GD.Print($"JUMP");
    }

    public void ApplyGravity(ref CharacterMoveState state, float delta)
    {
        state.Velocity += _gravityVector * delta;
    }

    const float SAFE_MOTION_PADDING = 0.05f;
   
    private void CheckCollidables(ref CharacterMoveState state, bool isSimulating)
    {

        if(isSimulating)
        {
            foreach (var collidable in state.NewlyOverlappedCollidables)
            {
                collidable.OnCollidedWith(Character, state, true);
            }
        }
        else
        {
            state.NewlyOverlappedCollidables.Clear();

            PhysicsShapeQueryParameters3D query = new()
            {
                Shape = _mainCollisionShape,
                Transform = new Transform3D(Basis.Identity, state.Position),
                CollideWithBodies = true,
                CollideWithAreas = true,
                CollisionMask = PhysicsConstants.CHARACTER_COLLIDABLES_MASK
            };

            var results = _space.IntersectShape(query, 8);

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
                            collidable.OnCollidedWith(Character, state, isSimulating);
                            state.NewlyOverlappedCollidables.Add(collidable);
                        }
                    }
                }
            }
            state.CurrentCollidables = newCollidables;
        }
    }


    public CharacterMoveState QueueLaunch(CharacterMoveState state, Vector3 launchVelocity)
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
            state.MovementMode = CharacterMovementMode.FALLING;

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

    public void OnSpawned()
    {

    }


    public void SetPosition(Vector3 position, bool markDirty = true)
    {
        State.Position = position;

        if (markDirty)
        {
            State.Flags |= CharacterMoveStateFlags.POSITION_CHANGED;
        }
    }

    public void SetVelocity(Vector3 velocity, bool markDirty = true)
    {
        State.Velocity = velocity;

        if (markDirty)
        {
            State.Flags |= CharacterMoveStateFlags.VELOCITY_CHANGED;
        }
    }

    public void SetRotation(float yaw, float pitch, bool markDirty = true)
    {
        State.Yaw = yaw;
        State.Pitch = pitch;

        if (markDirty)
        {
            State.Flags |= CharacterMoveStateFlags.ROTATION_CHANGED;
        }
    }

    public void ApplyState(CharacterMoveState state, float delta, bool markDirty = true)
    {
        if ((state.Flags & CharacterMoveStateFlags.POSITION_CHANGED) != 0)
        {
            SetPosition(state.Position, false);
        }

        if ((state.Flags & CharacterMoveStateFlags.VELOCITY_CHANGED) != 0)
        {
            SetVelocity(state.Velocity, false);
        }

        if ((state.Flags & CharacterMoveStateFlags.ROTATION_CHANGED) != 0)
        {
            SetRotation(state.Yaw, state.Pitch, false);
        }

        
        if(Character.IsLocal)
        {
            foreach (var clientInputCommand in ClientGame.Instance.UnprocessedClientInputCommands)
            {
                Step(ref PredictedState, clientInputCommand, delta, true);
            }

            if (_isSkippingReconciliation)
            {
                _ticksUntilReconciliationResume--;
            }
            else
            {
                ReconcileState();
            }
        }
    }

    public void ReconcileState()
    {
        Vector3 delta = PredictedState.Position - State.Position;

        // Thresholds
        const float SNAP_THRESHOLD_H = 2.0f;        // Horizontal snap (X/Z)
        const float SNAP_THRESHOLD_V = 2.0f;        // Vertical snap (Y)
        const float INTERP_THRESHOLD_H = 0.01f;      // Horizontal lerp start
        const float INTERP_THRESHOLD_V = 0.01f;     // Vertical lerp start

        // Lerp speeds
        const float INTERP_SPEED_H = 0.25f;
        const float INTERP_SPEED_V = 0.25f;

        Vector3 targetPos = State.Position;
        Vector3 currentPos = PredictedState.Position;

        Vector2 deltaXZ = new Vector2(delta.X, delta.Z);
        float distXZ = deltaXZ.Length();

        float deltaY = Math.Abs(delta.Y);

        //GD.Print($"horizontal error: {distXZ}.");

        // --- Horizontal correction ---
        if (distXZ > SNAP_THRESHOLD_H)
        {
            //GD.Print($"snap correction horizontal, error {distXZ}");
            currentPos.X = targetPos.X;
            currentPos.Z = targetPos.Z;
        }
        else if (distXZ > INTERP_THRESHOLD_H)
        {
            //GD.Print($"horizontal error: {distXZ}.");
            currentPos.X = Mathf.Lerp(currentPos.X, targetPos.X, INTERP_SPEED_H);
            currentPos.Z = Mathf.Lerp(currentPos.Z, targetPos.Z, INTERP_SPEED_H);
        }

        // --- Vertical correction ---
        if (deltaY > SNAP_THRESHOLD_V)
        {
            //GD.Print($"snap correction vertical, error {deltaY}");
            currentPos.Y = targetPos.Y;
        }
        else if (deltaY > INTERP_THRESHOLD_V)
        {
            //GD.Print($"lerp correction vertical, error {deltaY}");
            currentPos.Y = Mathf.Lerp(currentPos.Y, targetPos.Y, INTERP_SPEED_V);
        }
    
        PredictedState.Position = currentPos;
        PredictedState.Velocity = State.Velocity;
    }

    public void HandlePredictedInput(ClientInputCommand cmd, float delta)
    {
        Step(ref PredictedState, cmd, delta);
    }
}