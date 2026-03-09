using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles UI functionality of server browser.
/// </summary>
public partial class ServerBrowser : Control
{
    // top bar buttons
    [Export] Button _lanButton;
    [Export] Button _internetButton;


    [Export] VBoxContainer _serverResultsContainer;
    [Export] Label _serverRefreshLabel;

    [Export] PackedScene _serverResultEntryScene;

    [Export] Button _backButton;

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

    public override void _Ready()
    {
        base._Ready();

        _lanButton.Pressed += OnLanButtonPressed;
        _internetButton.Pressed += OnInternetButtonPressed;

        _backButton.Pressed += OnBackButtonPressed;
    }

    public void Open()
    {
        Show();
        RefreshLanServers();
    }

    public void RefreshLanServers()
    {
        ClearServerResults();

        _serverRefreshLabel.Text = "Refreshing LAN servers...";
        _serverRefreshLabel.Show();

        ServerBrowserRequests.Instance.RefreshLanServersAsync();
    }

    public void RefreshInternetServers()
    {
        ClearServerResults();

        _serverRefreshLabel.Text = "Refreshing internet servers...";
        _serverRefreshLabel.Show();

        ServerBrowserRequests.Instance.RefreshInternetServersAsync();
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

    public void OnBackButtonPressed()
    {
        Hide();
    }

    public void OnLanButtonPressed()
    {

    }

    public void OnInternetButtonPressed()
    {

    }
}
