using Godot;
using System;
using System.Collections.Generic;

public partial class Level : Node3D
{
    public static Level Instance { get; private set; }

    [Export] private PackedScene _gameModeScene;

    public List<ITickable> Tickables = new();

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



        // this should only be client
        var levelUI = (LevelUI)gameMode.LevelUIScene.Instantiate();
        AddChild(levelUI);


        if (NetworkManager.Instance.NetworkMode != NetworkMode.CLIENT)
        {
            Tickables.Add(TickManager.Create());
            ServerGame.Instance.PostLoad();
        }
        if(NetworkManager.Instance.NetworkMode != NetworkMode.DEDICATED_SERVER)
        {
            ClientGame.Instance.PostLoad();
        }

        CallDeferred(nameof(PostInit));
    }

    public void PostInit()
    {
        ServerGame.Instance?.StartMatch();

    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        foreach(var tickable in Tickables)
        {
            tickable.Tick(delta);
        }
    }
}
