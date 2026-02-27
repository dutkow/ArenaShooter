using Godot;
using System;

public partial class ServerBrowser : Control
{
    [Export] VBoxContainer _serverResultsContainer;
    [Export] Label _serverRefreshLabel;

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

}
