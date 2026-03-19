using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node, ITickable
{
    public static Main Instance { get; private set; }

    public Node _mainScene;

    private SceneNavigator _sceneNavigator;

    [Export] PackedScene _mainMenuScene;
    private MainMenu _mainMenu;

    [Export] PackedScene _loadingScreenScene;
    private LoadingScreen _loadingScreen;

    [Export] PackedScene _uiRootScene;

    [Export] Node GameRoot;
    ClientInput _clientInput;

    public List<ITickable> Tickables = new();


    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        NetworkManager.Create();
        _sceneNavigator = new();

        // client
        if(true)
        {
            _clientInput = new();
            UserSettings userSettings = new();

            UIRoot UIRoot = (UIRoot)_uiRootScene.Instantiate();
            AddChild(UIRoot);

            CommandConsole.Instance.AddConsoleLogEntry("=== INITIALIZING CLIENT ===");

            OpenMainMenu();

        }



 
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Tick(delta);

        foreach(var tickable in Tickables)
        {
            tickable.Tick(delta);
        }
    }

    public virtual void Tick(double delta)
    {
        NetworkManager.Instance?.Tick(delta);
    }

    public void OpenMainMenu()
    {
        _mainMenu = (MainMenu)_mainMenuScene.Instantiate();
        SetMainScene(_mainMenu);
    }

    public void OpenLoadingScreen()
    {
        _loadingScreen = (LoadingScreen)_loadingScreenScene.Instantiate();
        SetMainScene(_loadingScreen);
    }

    public void UnloadMainScene()
    {
        if(_mainScene != null)
        {
            _mainScene.QueueFree();
        }
        _mainScene = null;
    }

    public void SetMainScene(Node mainScene)
    {
        if(_mainScene == mainScene)
        {
            return;
        }

        CommandConsole.Instance.AddConsoleLogEntry($"Setting main scene to {mainScene.Name}");

        UnloadMainScene();

        _mainScene = mainScene;
        GameRoot.AddChild(_mainScene);
    }

    public void SetLoadingScreenProgress(float value)
    {
        _loadingScreen?.SetProgress(value);
    }

    public void OpenMultiplayerMap(string mapID, float delayBeforeLoad = 0.5f)
    {
        _sceneNavigator.OpenMultiplayerMap(mapID, delayBeforeLoad);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        _clientInput?.HandleInput(@event);
    }
}
