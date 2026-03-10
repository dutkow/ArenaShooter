using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Resources;
using static Godot.Image;
using static Godot.WebSocketPeer;

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

    public float MaxStepHeight = 0.25f;

    public float GroundAcceleration = 60.0f;
    public float AirAcceleration = 0.5f;

    public float GroundDeceleration = 100f;
    public float AirDeceleration = 5f;

    // Only used for server-side grounded logic
    private bool _isGrounded = false;
    private bool _isOnSlope = false;

    public CharacterMoveState State = new();

    private CapsuleShape3D _collisionCapsule;
    private Array<Rid> _characterCollisionRids = new();

    public float MaxWalkableSlopeAngle = 45f;
    public bool PreserveHorizontalSpeedOnSlope = true;

    public Vector3 LaunchVector;

    private float _jumpDelay = 0.2f;
    private float _jumpAccumulator;

    private bool _jumpCooldownReady => _jumpAccumulator >= _jumpDelay;

    // private move state variables
    Vector3 _groundNormal;

    public float HorizontalVelocity { get; private set; }



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

    public CharacterMoveState Step(CharacterMoveState state, ClientInputCommand cmd, float delta, bool isSimulating = false)
    {
        _jumpAccumulator += delta;

        // Movement input
        Vector3 move = Vector3.Zero;
        if (cmd.Input.HasFlag(InputCommand.MOVE_FORWARD)) move.Z -= 1;
        if (cmd.Input.HasFlag(InputCommand.MOVE_BACK)) move.Z += 1;
        if (cmd.Input.HasFlag(InputCommand.MOVE_LEFT)) move.X -= 1;
        if (cmd.Input.HasFlag(InputCommand.MOVE_RIGHT)) move.X += 1;

        move = move.Normalized();
        var basis = Basis.FromEuler(new Vector3(0, state.Yaw, 0));
        Vector3 desiredMoveDirection = (basis.Z * move.Z + basis.X * move.X).Normalized() * MaxGroundSpeed;

        if(_jumpCooldownReady)
        {
            CheckGrounded(state);
        }

        // Jump
        if (cmd.Input.HasFlag(InputCommand.JUMP) && _isGrounded && _jumpCooldownReady)
        {
            Jump(state);
        }
        else if (!_isGrounded)
        {
            state.Velocity.Y += Gravity * delta;
        }

        state.MoveMode = _isGrounded ? CharacterMoveMode.GROUNDED : CharacterMoveMode.FALLING;

        switch(state.MoveMode)
        {
            case CharacterMoveMode.GROUNDED:
                HandleGroundedMovement(state, desiredMoveDirection, delta);
                break;

            case CharacterMoveMode.FALLING:
                HandleAerialMovement(state, desiredMoveDirection, delta);
                break;
        }

        state.Velocity += cmd.LaunchVelocity;
        Vector3 safeMotion = HandleCollision(state, delta);
        state.Position += safeMotion;


        return state;
    }

    private Vector3 ProjectVelocityOnSlope(Vector3 velocity)
    {
        return velocity - _groundNormal * velocity.Dot(_groundNormal);
    }

    private void Jump(CharacterMoveState state)
    {
        state.Velocity.Y = Math.Max(state.Velocity.Y, 0f);
        state.Velocity.Y += JumpSpeed;
        _isGrounded = false;
        _jumpAccumulator = 0.0f;
    }

    private Vector3 HandleCollision(CharacterMoveState state, float delta)
    {
        var space = _character.CollisionShape.GetWorld3D().DirectSpaceState;
        Vector3 motion = state.Velocity * delta;

        var query = new PhysicsShapeQueryParameters3D
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

        if (result[1] < 1.0f) // hit something
        {
            // Cast at collision point to get normal
            var normalQuery = new PhysicsShapeQueryParameters3D
            {
                Shape = _collisionCapsule,
                Transform = query.Transform.Translated(unsafeMotion),
                Motion = Vector3.Zero,
                CollideWithBodies = true,
                CollideWithAreas = false
            };
            normalQuery.SetExclude(_characterCollisionRids);

            var collisions = space.GetRestInfo(normalQuery);
            if (collisions.Count > 0 && collisions.TryGetValue("normal", out var n))
            {
                Vector3 normal = (Vector3)n;

                float slopeAngle = Mathf.RadToDeg(Mathf.Acos(normal.Dot(Vector3.Up)));

                if (slopeAngle <= MaxWalkableSlopeAngle)
                {
                    // Walkable slope: slide along slope
                    Vector3 slopeRight = normal.Cross(Vector3.Up).Normalized();
                    Vector3 slopeForward = slopeRight.Cross(normal).Normalized();

                    Vector3 slopeMotion = slopeForward * motion.Dot(slopeForward) + slopeRight * motion.Dot(slopeRight);

                    if (!PreserveHorizontalSpeedOnSlope)
                        slopeMotion = slopeMotion.Normalized() * motion.Length();

                    safeMotion = slopeMotion;

                    // Optional: recast along slope
                    query.Motion = safeMotion;
                    result = space.CastMotion(query);
                    safeMotion *= result[0];
                }
                else
                {
                    // Too steep: try stepping up if possible
                    Vector3 stepUp = Vector3.Up * MaxStepHeight;

                    // Test if stepping up avoids collision
                    query.Transform = new Transform3D(Basis.Identity, state.Position + stepUp);
                    query.Motion = motion;
                    result = space.CastMotion(query);

                    if (result[0] > 0.99f) // can step up fully
                    {
                        safeMotion = stepUp + motion * result[0];
                    }
                    else
                    {
                        // slide along horizontal as fallback
                        Vector3 horizontalNormal = new Vector3(normal.X, 0, normal.Z);
                        if (horizontalNormal.LengthSquared() > 0.01f)
                        {
                            horizontalNormal = horizontalNormal.Normalized();
                            safeMotion = motion - horizontalNormal * motion.Dot(horizontalNormal);

                            query.Motion = safeMotion;
                            result = space.CastMotion(query);
                            safeMotion *= result[0];
                        }
                    }
                }
            }
        }

        return safeMotion;
    }

    private void CheckGrounded(CharacterMoveState state)
    {
        var spaceState = _character.GetWorld3D().DirectSpaceState;

        float beginTraceOffset = 0.25f;
        float groundTraceDistance = 0.25f;
        Vector3 from = state.Position + Vector3.Down * (_collisionCapsule.MidHeight - beginTraceOffset);
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
        _isGrounded = result.Count > 0;

        if(_isGrounded)
        {
            HorizontalVelocity = new Vector2(state.Velocity.X, state.Velocity.Z).Length();

            if (HorizontalVelocity != 0.0f)
            {
                if (result.TryGetValue("normal", out var normal))
                {
                    _groundNormal = (Vector3)normal;
                    _isOnSlope = OnSlope();
                }
            }
        }
        else
        {
            _groundNormal = Vector3.Zero;
            _isOnSlope = false;
        }
        
    }

    public void HandleInput(ClientInputCommand cmd, float delta)
    {
        State = Step(State, cmd, delta);
    }

    private void HandleGroundedMovement(CharacterMoveState state, Vector3 desiredMoveDirection, float delta)
    {
        Vector3 horizontalVel = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

        if (desiredMoveDirection.LengthSquared() > 0)
        {
            Vector3 desiredDir = desiredMoveDirection.Normalized();

            float speed = horizontalVel.Length();

            Vector3 newVel = horizontalVel.MoveToward(desiredDir * MaxGroundSpeed, GroundAcceleration * delta);

            horizontalVel = newVel;
        }
        else
        {
            float decelAmount = GroundDeceleration * delta;
            if (horizontalVel.Length() <= decelAmount)
                horizontalVel = Vector3.Zero;
            else
                horizontalVel -= horizontalVel.Normalized() * decelAmount;
        }

        state.Velocity.X = horizontalVel.X;
        state.Velocity.Z = horizontalVel.Z;

        if (_isOnSlope)
        {
            state.Velocity = ProjectVelocityOnSlope(state.Velocity);
        }
        else
        {
            state.Velocity.Y = Mathf.Max(state.Velocity.Y, 0.0f);
        }
    }


    private void HandleAerialMovement(CharacterMoveState state, Vector3 desiredMoveDirection, float delta)
    {
        Vector3 horizontalVel = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

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

        state.Velocity.X = horizontalVel.X;
        state.Velocity.Z = horizontalVel.Z;
    }

    public void QueueLaunch(Vector3 vector)
    {
        LaunchVector = vector;
    }

    public bool OnSlope()
    {
        float slopeAngle = Mathf.RadToDeg(Mathf.Acos(_groundNormal.Dot(Vector3.Up)));
        return slopeAngle > 0.0f && slopeAngle < MaxWalkableSlopeAngle;
    }

    // Quake approximations


    private void HandleQuakeGroundMovement(CharacterMoveState state, Vector3 desiredMoveDirection, float delta)
    {
        const float frictionFactor = 10.0f;

        // --- Horizontal velocity ---
        Vector3 velocity = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

        float wishSpeed = MaxGroundSpeed + 1.0f / (1.0f - frictionFactor * delta);
        Vector3 wishDir = desiredMoveDirection.Normalized();

        if (desiredMoveDirection.LengthSquared() > 0)
        {
            // --- Quake-style acceleration ---
            float accel = GroundAcceleration;
            float currentSpeed = velocity.Dot(wishDir);
            float addSpeed = wishSpeed - currentSpeed;
            if (addSpeed > 0)
            {
                float accelSpeed = accel * delta * wishSpeed;
                if (accelSpeed > addSpeed) accelSpeed = addSpeed;
                velocity += wishDir * accelSpeed;
            }
        }

        // --- Apply ground friction ---
        float speed = velocity.Length();
        if (speed > 0)
        {
            float drop = speed * frictionFactor * delta; // friction coefficient, tweakable
            float newSpeed = speed - drop;
            if (newSpeed < 0) newSpeed = 0;
            velocity *= newSpeed / speed;
        }

        // --- Update final velocity ---
        state.Velocity.X = velocity.X;
        state.Velocity.Z = velocity.Z;

        // --- Project on slope ---
        if (_isOnSlope)
        {
            state.Velocity = ProjectVelocityOnSlope(state.Velocity);
        }
        else
        {
            state.Velocity.Y = Mathf.Max(state.Velocity.Y, 0.0f);
        }
    }
    private void HandleQuakeAerialMovement(CharacterMoveState state, Vector3 desiredMoveDirection, float delta)
    {
        // --- Horizontal velocity ---
        Vector3 velocity = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

        if (desiredMoveDirection.LengthSquared() > 0)
        {
            Vector3 wishDir = desiredMoveDirection.Normalized();
            float currentSpeed = velocity.Dot(wishDir);
            float addSpeed = MaxGroundSpeed - currentSpeed; // base "max speed" reference

            if (addSpeed > 0)
            {
                float accelSpeed = AirAcceleration * MaxGroundSpeed * delta;
                if (accelSpeed > addSpeed) accelSpeed = addSpeed;
                velocity += wishDir * accelSpeed;
            }
        }

        // --- Update final velocity ---
        state.Velocity.X = velocity.X;
        state.Velocity.Z = velocity.Z;

        // Keep vertical velocity untouched
    }

}