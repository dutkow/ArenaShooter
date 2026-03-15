using Godot;
using System;
using System.Collections.Generic;

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
    public float Gravity = -20.0f;
    public float JumpSpeed = 10.0f;
    public float MaxStepHeight = 0.25f;

    public float GroundAcceleration = 60.0f;
    public float AirAcceleration = 0.5f;
    public float GroundDeceleration = 100f;
    public float AirDeceleration = 5f;

    public float MaxWalkableSlopeAngle = 45f;
    public bool PreserveHorizontalSpeedOnSlope = true;

    // Internal state
    private bool _isGrounded = false;
    private bool _isOnSlope = false;
    private Vector3 _groundNormal;
    private CapsuleShape3D _collisionCapsule;
    private Godot.Collections.Array<Rid> _characterCollisionRids = new();

    public Vector3 LaunchVector;
    public bool WasLaunched;

    private bool _jumpCooldownReady = true;
    public float HorizontalVelocity { get; private set; }

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
        // Process movement input
        Vector3 move = Vector3.Zero;
        if (cmd.Flags.HasFlag(InputFlags.FORWARD)) move.Z -= 1;
        if (cmd.Flags.HasFlag(InputFlags.BACKWARD)) move.Z += 1;
        if (cmd.Flags.HasFlag(InputFlags.STRAFE_LEFT)) move.X -= 1;
        if (cmd.Flags.HasFlag(InputFlags.STRAFE_RIGHT)) move.X += 1;

        state.Look += cmd.Look;

        move = move.Normalized();
        Basis basis = Basis.FromEuler(new Vector3(0, -state.Look.X, 0));

        Vector3 desiredMoveDir = (basis.Z * move.Z + basis.X * move.X).Normalized() * MaxGroundSpeed;

        // Jump
        if (cmd.Flags.HasFlag(InputFlags.JUMP) && _isGrounded)
        {
            Jump(state);
        }



        state.MovementMode = _isGrounded ? CharacterMoveMode.GROUNDED : CharacterMoveMode.FALLING;

        // Movement
        switch (state.MovementMode)
        {
            case CharacterMoveMode.GROUNDED:
                HandleGroundedMovement(state, desiredMoveDir, delta);
                break;
            case CharacterMoveMode.FALLING:
                HandleAerialMovement(state, desiredMoveDir, delta);
                break;
        }

        // Launch velocity
        if (cmd.Flags.HasFlag(InputFlags.WAS_LAUNCHED))
        {
            state.Velocity += cmd.LaunchVelocity;
            WasLaunched = false;
        }

        // Collision handling
        Vector3 safeMotion = HandleCollision(state, delta);
        state.Position += safeMotion;

        // Server-side collidable checks
        if (!isSimulating && _character.IsAuthority)
            CheckCollidables(state);

        CheckGrounded(state);

        // Gravity
        if (!_isGrounded)
        {
            state.Velocity.Y += Gravity * delta;
        }

        return state;
    }


    private void HandleGroundedMovement(CharacterPublicState state, Vector3 desiredMoveDir, float delta)
    {
        Vector3 horizontalVel = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

        if (desiredMoveDir.LengthSquared() > 0)
        {
            Vector3 dir = desiredMoveDir.Normalized();
            horizontalVel = horizontalVel.MoveToward(dir * MaxGroundSpeed, GroundAcceleration * delta);
        }
        else
        {
            float decel = GroundDeceleration * delta;
            if (horizontalVel.Length() <= decel)
                horizontalVel = Vector3.Zero;
            else
                horizontalVel -= horizontalVel.Normalized() * decel;
        }

        state.Velocity.X = horizontalVel.X;
        state.Velocity.Z = horizontalVel.Z;

        if (_isOnSlope)
            state.Velocity = ProjectVelocityOnSlope(state.Velocity);
        else
            state.Velocity.Y = Mathf.Max(state.Velocity.Y, 0.0f);
    }

    private void HandleAerialMovement(CharacterPublicState state, Vector3 desiredMoveDir, float delta)
    {
        Vector3 horizontalVel = new Vector3(state.Velocity.X, 0, state.Velocity.Z);

        if (desiredMoveDir.LengthSquared() > 0)
        {
            Vector3 dir = desiredMoveDir.Normalized();
            Vector3 velChange = dir * MaxGroundSpeed * AirAcceleration * delta;
            horizontalVel += velChange;
        }
        else
        {
            float decel = AirDeceleration * delta;
            if (horizontalVel.Length() <= decel)
                horizontalVel = Vector3.Zero;
            else
                horizontalVel -= horizontalVel.Normalized() * decel;
        }

        // Clamp horizontal speed
        if (horizontalVel.Length() > MaxGroundSpeed)
            horizontalVel = horizontalVel.Normalized() * MaxGroundSpeed;

        state.Velocity.X = horizontalVel.X;
        state.Velocity.Z = horizontalVel.Z;
    }

    private void Jump(CharacterPublicState state)
    {
        state.Velocity.Y = Math.Max(state.Velocity.Y, 0f) + JumpSpeed;
        _isGrounded = false;
    }

    private void CheckGrounded(CharacterPublicState state)
    {
        var space = _character.GetWorld3D().DirectSpaceState;

        Vector3 from = state.Position + Vector3.Down * (_collisionCapsule.MidHeight);
        Vector3 to = from + Vector3.Down * (0.2f);

        PhysicsRayQueryParameters3D query = new()
        {
            From = from,
            To = to,
            CollideWithBodies = true,
            CollideWithAreas = false
        };
        query.SetExclude(_characterCollisionRids);

        var result = space.IntersectRay(query);
        _isGrounded = result.Count > 0;

        if (_isGrounded)
        {
            HorizontalVelocity = new Vector2(state.Velocity.X, state.Velocity.Z).Length();

            if (HorizontalVelocity != 0 && result.TryGetValue("normal", out var normalObj))
            {
                _groundNormal = (Vector3)normalObj;
                _isOnSlope = OnSlope();
            }
        }
        else
        {
            _groundNormal = Vector3.Zero;
            _isOnSlope = false;
        }

        GD.Print($"grounded = {_isGrounded}");
    }

    private Vector3 ProjectVelocityOnSlope(Vector3 velocity)
    {
        return velocity - _groundNormal * velocity.Dot(_groundNormal);
    }

    private Vector3 HandleCollision(CharacterPublicState state, float delta)
    {
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

            if (slopeAngle <= MaxWalkableSlopeAngle)
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

    public bool OnSlope()
    {
        float slopeAngle = Mathf.RadToDeg(Mathf.Acos(_groundNormal.Dot(Vector3.Up)));
        return slopeAngle > 0f && slopeAngle < MaxWalkableSlopeAngle;
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