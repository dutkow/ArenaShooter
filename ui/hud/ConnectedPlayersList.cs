using Godot;
using System;
using System.Collections.Generic;

public partial class ConnectedPlayersList : Control
{
    [Export] VBoxContainer _playerListContainer;

    public override void _Ready()
    {
        base._Ready();

        PopulateInitialPlayerList();

        MatchState.Instance.PlayerJoined += AddPlayerToList;
    }

    public void PopulateInitialPlayerList()
    {
        foreach(var child in _playerListContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach(var kvp in MatchState.Instance.ConnectedPlayers)
        {
            AddPlayerToList(kvp.Value);
        }

        // REFACTOR
        foreach (var kvp in MatchState.Instance.NewConnectedPlayers)
        {
            NewAddPlayerToList(kvp.Value.PublicState);
        }
    }

    public void AddPlayerToList(PlayerState playerState)
    {
        Label playerLabel = new();
        playerLabel.Text = playerState.PlayerName;
        _playerListContainer.AddChild(playerLabel);
    }

    public void NewAddPlayerToList(PublicPlayerState playerState)
    {
        Label playerLabel = new();
        playerLabel.Text = playerState.PlayerName;
        _playerListContainer.AddChild(playerLabel);
    }
}
