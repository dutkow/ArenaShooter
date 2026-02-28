using Godot;
using System;
using System.Collections.Generic;

public partial class ServerBrowser : Control
{
    [Export] VBoxContainer _serverResultsContainer;
    [Export] Label _serverRefreshLabel;

    [Export] PackedScene _serverResultEntryScene;

    public override void _EnterTree()
    {
        base._EnterTree();

        NetworkSession.Instance.OnServerRefreshFinished += OnServerRefreshFinished;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        NetworkSession.Instance.OnServerRefreshFinished -= OnServerRefreshFinished;
    }

    public void Open()
    {
        Show();
        RefreshServers();
    }

    public void RefreshServers()
    {
        ClearServerResults();

        _serverRefreshLabel.Text = "Refreshing servers...";
        _serverRefreshLabel.Show();

        NetworkSession.Instance.RefreshLanServers();
    }

    public void ClearServerResults()
    {
        foreach (var child in _serverResultsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    public void OnServerRefreshFinished(List<ServerInfo> serverResults)
    {
        GD.Print("on server refresh finished on server browser");

        if(serverResults.Count == 0)
        {
            _serverRefreshLabel.Text = "No servers found!";
            return;
        }

        _serverRefreshLabel.Hide();

        foreach (var serverInfo in serverResults)
        {
            AddServerResult(serverInfo);
        }
    }

    public void AddServerResult(ServerInfo serverInfo)
    {
        var serverResultEntry = (ServerResultEntry)_serverResultEntryScene.Instantiate();
        serverResultEntry.Initialize(serverInfo);
        _serverResultsContainer.AddChild(serverResultEntry);
    }

}
