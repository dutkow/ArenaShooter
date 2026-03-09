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

        if (!NetworkSession.Instance.IsDedicatedServer)
        {

            //byte localPlayerID = NetworkSession.Instance.LocalPlayerID;
            //GameMode.Instance.AddPlayerController(localPlayerID);
        }

        CallDeferred(nameof(PostInit));
    }

    public void PostInit()
    {
        if (NetworkSession.Instance.IsServer)
        {
            MatchState.Instance.Initialize();

        }
        else if (NetworkSession.Instance.IsClient)
        {
            ClientLoaded.Send();
            GD.Print($"sending client loaded");
        }
    }
}
