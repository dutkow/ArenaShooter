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


        // REFACTOR
        foreach (var kvp in MatchState.Instance.ConnectedPlayers)
        {
            AddPlayerToList(kvp.Value);
        }
    }

    public void AddPlayerToList(PlayerState playerState)
    {
        Label playerLabel = new();
        playerLabel.Text = playerState.PlayerName;
        _playerListContainer.AddChild(playerLabel);
    }


}
