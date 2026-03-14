using Godot;
using System;

public partial class Main : Node, ITickable
{
    public static Main Instance { get; private set; }

    public Node _mainScene;

    private SceneNavigator _sceneNavigator;

    [Export] PackedScene _mainMenuScene;
    private MainMenu _mainMenu;

    [Export] PackedScene _loadingScreenScene;
    private LoadingScreen _loadingScreen;



    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        NetworkManager.Create();
        _sceneNavigator = new();

        OpenMainMenu();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Tick(delta);
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

        UnloadMainScene();

        _mainScene = mainScene;
        AddChild(_mainScene);
    }

    public void SetLoadingScreenProgress(float value)
    {
        _loadingScreen?.SetProgress(value);
    }

    public void OpenMultiplayerMap(string mapID, float delayBeforeLoad = 0.5f)
    {
        _sceneNavigator.OpenMultiplayerMap(mapID, delayBeforeLoad);
    }
}
