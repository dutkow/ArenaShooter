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
        NetworkHandler.Instance.OnServerStarted += OnServerStarted;
    }

    public void Open()
    {
        Visible = true;
    }

    public void OnHostGameButtonPressed()
    {
        ServerInfo serverInfo = new();
        serverInfo.Name = "Test Server";
        NetworkSession.Instance.HostLanServer(serverInfo);
    }

    public void OnServerStarted()
    {
        MatchState.Instance.StartPhase(MatchPhase.WARMUP);
        PlayerState playerState = new();
        PlayerManager.Instance.RegisterPlayer(playerState);
        GetTree().ChangeSceneToPacked(_testLevel);
    }
}
