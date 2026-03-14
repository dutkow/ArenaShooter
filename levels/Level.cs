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

        MatchState matchState = new();
        AddChild(matchState);

        ServerTickManager serverTickManager = new();
        AddChild(serverTickManager);

        SpawnManager spawnManager = new();
        AddChild(spawnManager);

        var levelUI = (LevelUI)gameMode.LevelUIScene.Instantiate();
        AddChild(levelUI);

        

        CallDeferred(nameof(PostInit));
    }

    public void PostInit()
    {
        if (NetworkManager.Instance.IsServer)
        {
            MatchState.Instance.Initialize();

        }
        else if (NetworkManager.Instance.IsClient)
        {
            ClientLoaded.Send();
            GD.Print($"sending client loaded");
        }
    }
}
