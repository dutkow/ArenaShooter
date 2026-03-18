using Godot;
using System;

public partial class PlayerListEntry : Control
{
    [Export] Label _playerNameLabel;

    public void Initialize(PlayerState playerState)
    {
        _playerNameLabel.Text = playerState.PlayerInfo.PlayerName;

        playerState.PlayerNameChanged += OnPlayerNameChanged;

    }

    public void OnPlayerNameChanged(string playerName)
    {
        _playerNameLabel.Text = playerName;
    }
}
