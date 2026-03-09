using Godot;
using System;

public partial class ServerResultEntry : Control
{
    ServerInfo _serverInfo;

    [Export] Label _serverNameLabel;
    [Export] Label _mapLabel;
    [Export] Label _playerCountLabel;
    [Export] Label _pingLabel;



    [Export] Button _joinButton;


    public override void _Ready()
    {
        base._Ready();

        _joinButton.Pressed += OnJoinButtonPressed;
    }

    public void Initialize(ServerInfo serverInfo)
    {
        _serverInfo = serverInfo;

        _serverNameLabel.Text = _serverInfo.Name;
        _mapLabel.Text = GameData.Instance.MultiplayerMapsByID[serverInfo.MapID].Name;
        _playerCountLabel.Text = $"{serverInfo.ConnectedPlayersCount}/{serverInfo.MaxPlayers}";
        _pingLabel.Text = "999";

        //_serverInfo.PrintInfo();
    }

    public void OnJoinButtonPressed()
    {
        NetworkSession.Instance.JoinServer(_serverInfo);
    }
}
