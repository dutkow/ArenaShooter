using Godot;
using System;
using System.Collections.Generic;

public partial class ConnectedPlayersList : Control
{
    [Export] VBoxContainer _playerListContainer;

    public override void _Ready()
    {
        base._Ready();

        MatchState.Instance.PlayerJoined += OnPlayerJoined;
    }

    public void OnPlayerJoined(PlayerState playerState)
    {
        Label playerLabel = new();
        playerLabel.Text = playerState.PlayerName;
        _playerListContainer.AddChild(playerLabel);

        GD.Print($"Played joined ran on connected players list. Network Mode = {NetworkSession.Instance.NetworkMode}");

    }
}
