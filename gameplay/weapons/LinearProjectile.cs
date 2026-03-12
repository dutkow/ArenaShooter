using Godot;

public partial class LinearProjectile : Projectile
{
    [Export] public float Speed = 50f;
    [Export] public float Gravity = 0f;

    private Vector3 _velocity;
    private Vector3 _gravityVector;

    public override void Initialize(Vector3 origin, Vector3 direction, ushort projectileID, bool isPredicted)
    {
        base.Initialize(origin, direction, projectileID, isPredicted);

        State.Position = GlobalPosition;
        _velocity = direction.Normalized() * Speed;
        LookAt(origin + direction, Vector3.Up);

        _gravityVector = new Vector3(0, -Gravity, 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // Normal physics step with collision and updating GlobalPosition
        State = Step(State, (float)delta, false);
        GlobalPosition = State.Position;
    }

    /// <summary>
    /// Single step of projectile motion
    /// </summary>
    public ProjectileState Step(ProjectileState state, float delta, bool skipCollision = false)
    {
        if (!_isAlive)
        {
            return state;
        }

        // Linear motion + gravity
        Vector3 motion = _velocity * delta;
        motion += _gravityVector * 0.5f * delta * delta;

        Vector3 newPos = state.Position + motion;

        // Collision check (skip if reconciling)
        if (!skipCollision)
        {
            var space = GetWorld3D().DirectSpaceState;
            var query = new PhysicsShapeQueryParameters3D
            {
                Shape = CollisionShape.Shape,
                Transform = new Transform3D(Basis.Identity, state.Position),
                Motion = motion,
                CollideWithBodies = false,
                CollideWithAreas = true
            };
            query.Exclude = new Godot.Collections.Array<Rid> { Area.GetRid() };

            var result = space.CastMotion(query);
            float unsafeFraction = result[1];

            if (unsafeFraction < 1.0f)
            {
                Vector3 unsafeMotion = motion * unsafeFraction;
                var newQuery = query;
                newQuery.Transform = newQuery.Transform.Translated(unsafeMotion);

                var restInfo = space.GetRestInfo(newQuery);
                if (restInfo.TryGetValue("collider_id", out var colliderIDObj))
                {
                    ulong colliderID = (ulong)colliderIDObj;
                    var obj = GodotObject.InstanceFromId(colliderID);
                    var colliderNode = obj as Node3D;

                    if (colliderNode?.Owner is IDamageable damageable)
                    {
                        damageable.ApplyDamage(Damage);
                    }

                    Destroy();
                    return state;
                }
            }
        }

        state.Position = newPos;
        return state;
    }

    public override void Reconcile(ProjectileSpawnData spawnData)
    {
        base.Reconcile(spawnData);

        int tickCount = ClientGame.Instance.LastServerTickProcessedByClient - spawnData.ServerTickOnSpawn;
        float tickDelta = NetworkConstants.SERVER_TICK_INTERVAL;

        // Start from spawn
        Vector3 estimatedPos = spawnData.SpawnLocation;
        ProjectileState tempState = new ProjectileState { ProjectileID = State.ProjectileID, Position = estimatedPos };

        // Simulate each tick without collision or applying to GlobalPosition
        for (int i = 0; i < tickCount; i++)
        {
            Step(tempState, tickDelta, skipCollision: true);
        }

        // Actual position from current state
        Vector3 actualPos = State.Position;
        Vector3 error = actualPos - tempState.Position;

        GD.Print($"Projectile [{State.ProjectileID}] Reconcile:");
        GD.Print($"  Estimated Pos: {tempState.Position}");
        GD.Print($"  Actual Pos:    {actualPos}");
        GD.Print($"  Error:         {error} (Length: {error.Length()} units)");
        GD.Print($"  Spawn Direction: {spawnData.SpawnDirection.Normalized()}");
    }
}