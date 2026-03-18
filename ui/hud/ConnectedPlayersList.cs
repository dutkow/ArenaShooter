using Godot;
using System;
using System.Collections.Generic;

public partial class ConnectedPlayersList : Control
{
    [Export] VBoxContainer _playerListContainer;

    [Export] PackedScene _playerListEntryScene;

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

        foreach (var kvp in MatchState.Instance.ConnectedPlayers)
        {
            AddPlayerToList(kvp.Value);
        }
    }

    public void AddPlayerToList(PlayerState playerState)
    {
        var playerListEntry = (PlayerListEntry)_playerListEntryScene.Instantiate();
        playerListEntry.Initialize(playerState);
        _playerListContainer.AddChild(playerListEntry);
    }


}
