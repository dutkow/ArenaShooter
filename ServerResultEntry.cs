using Godot;
using System;

public partial class ServerResultEntry : Control
{
    [Export] Label _serverNameLabel;

    public void Initialize(ServerInfo serverInfo)
    {
        _serverNameLabel.Text = serverInfo.Name;
    }
}
