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


    public Projectile LocalSpawnProjectile(ushort projectileID, ProjectileType type, Vector3 position, Vector3 rotation)
    {
        GD.Print($"local spawn proj ran on {NetworkSession.Instance.NetworkMode}");
        if(_projectilesByType.TryGetValue(type, out var projectileScene))
        {
            var spawnedProjectile = (Projectile)projectileScene.Instantiate();

            if(spawnedProjectile == null)
            {
                GD.PushError($"Projectile Type [{type}] was found in projectiles dictionary but no projectile spawned. Ensure the projectile scene has a projectile script attached to it.");
                return null;
            }

            Level.Instance.AddChild(spawnedProjectile);

            spawnedProjectile.GlobalPosition = position;
            spawnedProjectile.GlobalRotation = rotation;


            spawnedProjectile.Initialize(position, rotation, projectileID);
            return spawnedProjectile;
        }
        else
        {
            GD.PushError($"Projectile Type [{type}] not found in projectiles dictionary. It should be set in the editor.");
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
