using Godot;
using System;
using System.Collections.Generic;

public partial class ConnectedPlayersList : Control
{
    [Export] VBoxContainer _playerListContainer;

    public override void _Ready()
    {
        base._Ready();

        PopulateConnectedPlayersList();
    }
    public void PopulateConnectedPlayersList()
    {
        foreach(var kvp in MatchState.Instance.ConnectedPlayers)
        {
            Label playerLabel = new();
            playerLabel.Text = kvp.Value.PlayerName;
            _playerListContainer.AddChild(playerLabel);
        }
    }
}
