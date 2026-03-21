using Godot;
using System;
using System.Collections.Generic;

public partial class Scoreboard : Control
{
    [Export] VBoxContainer _playerListContainer;

    [Export] PackedScene _playerScoreboardEntry;

    public override void _EnterTree()
    {
        base._EnterTree();

        MatchState.Instance.PlayerJoined += AddPlayerToList;

        ClearPlayerList();
    }


    public void PopulateInitialPlayerList()
    {
        foreach (var kvp in MatchState.Instance.ConnectedPlayers)
        {
            AddPlayerToList(kvp.Value);
        }
    }


    public void ClearPlayerList()
    {
        foreach (var child in _playerListContainer.GetChildren())
        {
            child.QueueFree();
        }
    }
    public void AddPlayerToList(PlayerStateOld playerState)
    {
        var playerListEntry = (PlayerScoreboardEntry)_playerScoreboardEntry.Instantiate();
        playerListEntry.Initialize(playerState);
        _playerListContainer.AddChild(playerListEntry);
    }


}
