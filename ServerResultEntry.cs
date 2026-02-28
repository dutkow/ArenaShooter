using Godot;
using System;

public partial class ServerResultEntry : Control
{
    ServerInfo _serverInfo;

    [Export] Label _serverNameLabel;
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

        _serverInfo.PrintInfo();
    }

    public void OnJoinButtonPressed()
    {
        NetworkSession.Instance.JoinServer(_serverInfo);
        GD.Print("join button pressed ran");
    }
}
