using Godot;
using System;

public partial class GameMenu : Control
{
    [Export] Button _resumeButton;
    [Export] Button _settingsButton;
    [Export] Button _quitToMainMenuButton;
    [Export] Button _quitGameButton;

    public override void _Ready()
    {
        base._Ready();

        _resumeButton.Pressed += OnResumeButtonPressed;
        _settingsButton.Pressed += OnSettingsButtonPressed;
        _quitToMainMenuButton.Pressed += OnQuitToMainMenuButtonPressed;
        _quitGameButton.Pressed += OnQuitGameButtonPressed;
    }

    public void NavigateBack()
    {
        if(Visible)
        {
            Hide();
        }
    }

    public void OnResumeButtonPressed()
    {
        ClientGame.Instance.LocalPlayerController.SetInputMode(InputMode.GAME);
        Hide();
    }

    public void OnSettingsButtonPressed()
    {
        GameUI.Instance.ShowSettingsMenu();
    }

    public void HandleGameSessionExit()
    {
        if (NetworkManager.Instance.NetworkMode == NetworkMode.LISTEN_SERVER || NetworkManager.Instance.NetworkMode == NetworkMode.LISTEN_SERVER)
        {
            NetworkServer.Instance.StartServerShutdown();
        }
        else if (NetworkManager.Instance.NetworkMode == NetworkMode.CLIENT)
        {
            NetworkClient.Instance.DisconnectFromServer();
        }
    }

    public void OnQuitToMainMenuButtonPressed()
    {
        HandleGameSessionExit();

        SceneNavigator.OpenMainMenu();
    }

    public async void OnQuitGameButtonPressed()
    {
        HandleGameSessionExit();

        // Wait 2 seconds
        await ToSignal(GetTree().CreateTimer(1f), "timeout");

        SceneNavigator.QuitToDesktop();
    }
}
