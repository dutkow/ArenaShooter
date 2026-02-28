using Godot;
using System;

public partial class ServerBrowser : Control
{
    [Export] VBoxContainer _serverResultsContainer;
    [Export] Label _serverRefreshLabel;

    [Export] PackedScene _serverResultEntryScene;

    public void Open()
    {
        Show();
        RefreshServers();
    }

    public void RefreshServers()
    {
        ClearServerResults();

        _serverRefreshLabel.Show();

    }

    public void ClearServerResults()
    {
        foreach (var child in _serverResultsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    public void PopulateServerResults()
    {

    }

    public void AddServerResult(ServerInfo serverInfo)
    {
        var serverResultEntry = (ServerResultEntry)_serverResultEntryScene.Instantiate();
        _serverResultsContainer.AddChild(serverResultEntry);
    }

}
