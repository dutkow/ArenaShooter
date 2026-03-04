using Godot;
using System;

public partial class GameManagers : Node
{
    public static GameManagers Instance;

    [Export] private PackedScene _projectileManagerScene;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        ProjectileManager projectileManager = (ProjectileManager)_projectileManagerScene.Instantiate();
        AddChild(projectileManager);
    }
}
