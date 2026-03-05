using Godot;
using System;

public partial class UIRoot : Control
{
    public static UIRoot Instance { get; private set; }

    // Scenes
    [Export] PackedScene _loadingScreenScene;
    private LoadingScreen _loadingScreen;

    [Export] PackedScene _mainMenuScene;
    private MainMenu _mainMenu;

    [Export] PackedScene _playerHudScene;
    private PlayerHud _playerHud;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        _mainMenu = (MainMenu)_mainMenuScene.Instantiate();
        AddChild(_mainMenu);

        _loadingScreen = (LoadingScreen)_loadingScreenScene.Instantiate();
        AddChild(_loadingScreen);
        _loadingScreen.Visible = false;
    }

    public void ShowLoadingScreen()
    {
        if (_loadingScreen != null)
        {
            _loadingScreen.Visible = true;
            _loadingScreen.ShowLoading();
        }
    }

    public void HideLoadingScreen()
    {
        if (_loadingScreen != null)
        {
            _loadingScreen.Visible = false;
            _loadingScreen.HideLoading();
        }
    }

    public void SetProgress(float value)
    {
        _loadingScreen?.SetProgress(value);
    }

    public void OnPossessedArenaCharacter(ArenaCharacter character)
    {
        EnsurePlayerHud();

        _playerHud.AssignToArenaCharacter(character);

        ShowPlayerHud();
    }

    public void EnsurePlayerHud()
    {
        if (_playerHud == null)
        {
            _playerHud = (PlayerHud)_playerHudScene.Instantiate();
            AddChild(_playerHud);
        }
    }

    public void ShowPlayerHud()
    {
        _playerHud.Show();
    }

    public void HidePlayerHud()
    {
        _playerHud.Hide();
    }
}
