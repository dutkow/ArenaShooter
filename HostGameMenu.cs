using Godot;
using System;

public partial class HostGameMenu : Control
{
    [Export] PackedScene _testLevel;

    [Export] Button _hostGameButton;

    public override void _Ready()
    {
        base._Ready();

        _hostGameButton.Pressed += OnHostGameButtonPressed;
    }

    public void Open()
    {
        Visible = true;
    }

    public void OnHostGameButtonPressed()
    {
        var serverInfo = new ServerInfo();
        serverInfo.Name = "Test Server";
        NetworkSession.Instance.HostLanServer(serverInfo);
    }
}
