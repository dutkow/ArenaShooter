using Godot;
using System;

public partial class MainMenu : Control
{
    [Export] Button _hostGameButton;
    [Export] Button _joinGameButton;

    [Export] HostGameMenu _hostGameMenu;

    [Export] ServerBrowser _serverBrowser;

    [Export] LineEdit _playerNameLineEdit;
    public override void _Ready()
    {
        base._Ready();

        _hostGameButton.Pressed += OnHostGameButtonPressed;
        _joinGameButton.Pressed += OnJoinGameButtonPressed;

        _playerNameLineEdit.Text = Settings.Instance.PlayerName;
    }

    public void OnHostGameButtonPressed()
    {
        _hostGameMenu.Open();
    }

    public void OnJoinGameButtonPressed()
    {
        _serverBrowser.Open();
    }
}
