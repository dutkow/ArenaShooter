using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using static Godot.HttpRequest;
using static Godot.WebSocketPeer;

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



    public float HorizontalVelocity { get; private set; }
    public float VerticalVelocity { get; private set; }


    public int _ticksToIgnoreGroundPostJump = 10;

    private bool _justJumped;

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

        MoveAndSlide(state, delta);


        HorizontalVelocity = new Vector2(state.Velocity.X, state.Velocity.Z).Length();
        VerticalVelocity = state.Velocity.Y;

        /*
        if(!CheckLaunch(state, isSimulating))
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
        */
        return state;
    }

    public void CheckGround(CharacterPublicState state, PhysicsDirectSpaceState3D space)
    {
        Vector3 motion = Vector3.Down * GROUNDED_CHECK_DISTANCE;

        PhysicsShapeQueryParameters3D motionQuery = new()
        {
            Shape = _collisionCapsule,
            Transform = new Transform3D(Basis.Identity, state.Position),
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
                Shape = _collisionCapsule,
                Transform = new Transform3D(Basis.Identity, state.Position + unsafeMotion),
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

        if(state.IsGrounded)
        {
            SnapToGround(state, space);

        }
    }

    public void SnapToGround(CharacterPublicState state, PhysicsDirectSpaceState3D space)
    {
        Vector3 start = state.Position;
        Vector3 end = state.Position + Vector3.Down * 3.0f;
        var rayParams = new PhysicsRayQueryParameters3D()
        {
            From = start,
            To = end,
            CollideWithBodies = true,
            CollideWithAreas = false
        };
        rayParams.SetExclude(_characterCollisionRids);

        var result = space.IntersectRay(rayParams);

        if(result.TryGetValue("position", out var value))
        {
            var position = (Vector3)value;          

            state.Position = new Vector3(state.Position.X, position.Y + GROUNDED_CHECK_DISTANCE , state.Position.Z);
            GD.Print($"snap to ground ran. new position: {state.Position.Y}!");
        }
    }

    public CharacterPublicState MoveAndSlide(CharacterPublicState state, float delta)
    {
        var space = _character.GetWorld3D().DirectSpaceState;

        CheckGround(state, space);


        if (state.IsGrounded)
        {
            if (state.WantsToJump)
            {
                Jump(state);
                MoveAndSlideAir(state, space, delta);
            }
            else
            {
                MoveAndSlideGrounded(state, space, delta);
            }
        }
        else
        {
            MoveAndSlideAir(state, space, delta);
        }

        CheckGround(state, space);


        return state;
    }

    public void ProjectVelocityToGround(CharacterPublicState state)
    {
        state.Velocity = state.Velocity.Slide(state.GroundNormal);
    }

    public CharacterPublicState MoveAndSlideGrounded(CharacterPublicState state, PhysicsDirectSpaceState3D space, float delta)
    {
        if (state.WantsToJump && state.IsGrounded)
        {
            Jump(state);
            MoveAndSlideAir(state, space, delta);
            return state;
        }

        ApplyAcceleration(state, _walkAcceleration, _walkDeceleration, delta);
        ProjectVelocityToGround(state);

        Vector3 remainingMotion = state.Velocity * delta;

        bool moveComplete = false;
        int maxSlides = 4;
        for (int i = 0; i < maxSlides; ++i)
        {
            if(moveComplete)
            {
                break;
            }

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

            var safeMovePercent = result[0];
            var safeMotion = remainingMotion * safeMovePercent;

            if(safeMovePercent >= 1.0f)
            {
                moveComplete = true;
            }

            if (safeMovePercent < 1.0f)
            {
                var unsafeMovePercent = result[1];
                var unsafeMotion = remainingMotion * unsafeMovePercent;

                // Evaluate the collision normal
                PhysicsShapeQueryParameters3D collisionQuery = new()
                {
                    Shape = _collisionCapsule,
                    Transform = new Transform3D(Basis.Identity, state.Position + unsafeMotion),
                    CollideWithBodies = true,
                    CollideWithAreas = false
                };
                collisionQuery.SetExclude(_characterCollisionRids);

                if(space.GetRestInfo(collisionQuery).TryGetValue("collider_id", out var objectID))
                {
                    var id = (ulong)objectID;

                    var collisionObject = GodotObject.InstanceFromId(id);

                    if(collisionObject is Node node)
                    {
                        GD.Print($"collided with {node.Name}");
                    }

                    // how do i et the object and print its name
                }

                if (space.GetRestInfo(collisionQuery).TryGetValue("point", out var positionValue))
                {
                    var position = (Vector3)positionValue;
                    GD.Print($"collision position: {position}");
                }



                if (space.GetRestInfo(collisionQuery).TryGetValue("normal", out var value))
                {
                    var normal = (Vector3)value;
                    GD.Print($"NORMAL = {normal} ");

                    // this is a walkable slope
                    if (normal.Dot(Vector3.Up) >= _walkableThreshold)
                    {
                        GD.Print($"WALKABLE SLOPE. ");

                        Vector3 slideVector = remainingMotion.Slide(normal);
                        // only apply projection on the Y to maintain constant horizontal velocity
                        remainingMotion = new Vector3(remainingMotion.X, slideVector.Y, remainingMotion.Z);

                        PhysicsShapeQueryParameters3D slopeQuery = new()
                        {
                            Shape = _collisionCapsule,
                            Transform = new Transform3D(Basis.Identity, state.Position),
                            Motion = remainingMotion,
                            CollideWithBodies = true,
                            CollideWithAreas = false
                        };
                        slopeQuery.SetExclude(_characterCollisionRids);

                        var slopeResult = space.CastMotion(slopeQuery);

                        var slopeSafeMovePercent = slopeResult[0];
                        var slopeSafeMotion = remainingMotion * slopeSafeMovePercent;

                        remainingMotion -= safeMotion;
                        state.Position += safeMotion;

                        if (slopeSafeMovePercent >= 1.0f)
                        {
                            moveComplete = true;
                        }
                    }
                    // this is not a walkable angle
                    else
                    {

                        GD.Print($"not walkable slope");
                        if(space.GetRestInfo(collisionQuery).TryGetValue("point", out var collisionPointValue))
                        {
                            // check if this height is steppable
                            var collisionPoint = (Vector3)collisionPointValue;

                            GD.Print($"collision at Y: {collisionPoint.Y}. state pos + step height = {state.Position.Y + MaxStepHeight - _collisionCapsule.MidHeight}");

                           
                            // This is not steppable, treat it like a wall
                            if (collisionPoint.Y > state.Position.Y -_collisionCapsule.MidHeight + MaxStepHeight)
                            {
                                GD.Print("NOT STEPPABLE, we should slide");
                                Vector3 horizontalMotion = new Vector3(safeMotion.X, 0, safeMotion.Z);
                                Vector3 slide = safeMotion.Slide(normal);
                                safeMotion = new Vector3(slide.X, safeMotion.Y, slide.Z);
                            }
                            // This is a steppable collision height
                            else
                            {
                                // what's the right way to allow for stepping over it? like I could just project the Y height or something but i'm afraid that could inadvertently cause errors if there's a collision anyways. not sure if we need to like
                                // cast motion again to test or something
                                GD.Print($"STEPPABLE");

                            }
                        }
                    }
                }
                else
                {
                    GD.Print("unable to move but did not find normal");
                }
            }

            remainingMotion -= safeMotion;
            state.Position += safeMotion;

            if(!moveComplete && remainingMotion.Length() < 0.01f)
            {
                moveComplete = true;
            }
        }

        return state;
    }

    public CharacterPublicState MoveAndSlideAir(CharacterPublicState state, PhysicsDirectSpaceState3D space, float delta)
    {
        ApplyAcceleration(state, _airAcceleration, _airDeceleration, delta);

        Vector3 remainingMotion = state.Velocity * delta;


        bool moveComplete = false;
        int maxSlides = 4;
        for (int i = 0; i < maxSlides; ++i)
        {
            if (moveComplete)
            {
                break;
            }

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

            var safeMovePercent = result[0];
            var safeMotion = remainingMotion * safeMovePercent;            
            
            if (safeMovePercent >= 1.0f)
            {
                moveComplete = true;
            }
            else
            {
                var unsafeMovePercent = result[1];
                var unsafeMotion = remainingMotion * unsafeMovePercent;

                // Evaluate the collision normal
                PhysicsShapeQueryParameters3D collisionQuery = new()
                {
                    Shape = _collisionCapsule,
                    Transform = new Transform3D(Basis.Identity, state.Position + unsafeMotion),
                    CollideWithBodies = true,
                    CollideWithAreas = false
                };
                collisionQuery.SetExclude(_characterCollisionRids);

                if (space.GetRestInfo(collisionQuery).TryGetValue("normal", out var value))
                {
                    var normal = (Vector3)value;
                    GD.Print($"HIT SOMETHING IN AIR MOVE");

                    // we have landed on a walkable slope
                    if (normal.Dot(Vector3.Up) >= _walkableThreshold)
                    {
                        GD.Print($"LANDED ON A WALKABLE SLOPE. ");
                        remainingMotion = unsafeMotion;
                        moveComplete = true;
                    }
                    // this is not a walkable angle
                    else
                    {
                        Vector3 horizontalMotion = new Vector3(safeMotion.X, 0, safeMotion.Z);
                        Vector3 slide = safeMotion.Slide(normal);
                        safeMotion = new Vector3(slide.X, safeMotion.Y, slide.Z);
                    }
                }
                else
                {
                    GD.Print("unable to move but did not find normal");
                }
            }

            remainingMotion -= safeMotion;
            state.Position += safeMotion;

            if (!moveComplete && remainingMotion.Length() < 0.01f)
            {
                moveComplete = true;
            }
        }
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

        state.Look += cmd.Look;
        Basis basis = Basis.FromEuler(new Vector3(0, -state.Look.X, 0));

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


    const float GROUNDED_CHECK_DISTANCE = 0.5f;
    const float MAX_WALKABLE_GROUND_ANGLE = 35.0f;
    float _walkableThreshold = MathF.Cos(Mathf.DegToRad(MAX_WALKABLE_GROUND_ANGLE));



    public void ApplyAcceleration(CharacterPublicState state, float acceleration, float deceleration, float delta)
    {
        ApplyGravity(state, delta);
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


    private void Jump(CharacterPublicState state)
    {
        state.Velocity.Y = Math.Max(state.Velocity.Y, 0f) + JumpSpeed;
        state.MovementMode = CharacterMoveMode.FALLING;
        state.IsGrounded = false;
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