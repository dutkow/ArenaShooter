using Godot;
using Godot.Collections;
using System;
using static Godot.TextServer;

public partial class ProjectileManager : Node3D
{
    public static ProjectileManager Instance { get; private set; }

    [Export] private Dictionary<ProjectileType, PackedScene> _projectilesByType = new();

    private Dictionary<ushort, Projectile> _projectilesByID = new();

    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;
    }


    public Projectile LocalSpawnProjectile(ushort projectileID, ProjectileType type, Vector3 position, Vector3 direction, bool isPredicted)
    {
        if (_projectilesByType.TryGetValue(type, out var projectileScene))
        {
            var spawnedProjectile = (Projectile)projectileScene.Instantiate();
            if (spawnedProjectile == null)
            {
                GD.PushError($"Projectile Type [{type}] found but no projectile spawned.");
                return null;
            }

            Level.Instance.AddChild(spawnedProjectile);

            spawnedProjectile.GlobalPosition = position;

            // Convert direction to rotation for visuals
            if (direction.LengthSquared() > 0.001f)
            {
                spawnedProjectile.GlobalRotation = direction.Normalized().ToEuler();
            }

            // Initialize using direction
            spawnedProjectile.Initialize(position, direction, projectileID, isPredicted);
            return spawnedProjectile;
        }
        else
        {
            GD.PushError($"Projectile Type [{type}] not found in dictionary.");
            return null;
        }
    }

    public void DestroyProjectile(ushort projectileID)
    {
        if(_projectilesByID.TryGetValue(projectileID, out var projectile))
        {
            projectile.LocalDestroy();
        }
    }
}
