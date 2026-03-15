using Godot;
using System;
using System.Security.Cryptography;

public partial class Level : Node3D
{
    public static Level Instance { get; private set; }

    [Export] private PackedScene _gameModeScene;


    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;

        Initialize();
    }

    public void Initialize()
    {
        var gameMode = (GameMode)_gameModeScene.Instantiate();
        AddChild(gameMode);

        SpawnManager.Initialize();
        PickupManager.Initialize();
        MatchState.Initialize();


        ServerTickManager serverTickManager = new();
        AddChild(serverTickManager);


        var levelUI = (LevelUI)gameMode.LevelUIScene.Instantiate();
        AddChild(levelUI);

    }
}
