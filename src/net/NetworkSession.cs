using Godot;
using System;

public partial class NetworkSession : Node
{
    public static NetworkSession Instance { get; private set; }

    // ----------------------
    // Dependencies
    // ----------------------
    [Export] private NetworkHandler _networkHandler;
    private LanServerBroadcaster _lanBroadcaster;

    // ----------------------
    // Session info
    // ----------------------
    private ServerInfo _serverInfo;
    private bool _isHosting = false;

    // ----------------------
    // Session events
    // ----------------------
    public Action? OnSessionStarted;
    public Action? OnSessionStopped;
    public Action<int>? OnPlayerJoined;
    public Action<int>? OnPlayerLeft;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        _networkHandler = NetworkHandler.Instance;
    }

    // ----------------------
    // Public API
    // ----------------------

    public void HostLanServer(ServerInfo info)
    {
        _serverInfo = info;

        GD.Print("Starting LAN server...");

        _networkHandler.StartServer();

        if(_lanBroadcaster == null)
        {
            _lanBroadcaster = new();
        }

        _lanBroadcaster.StartBroadcast(info);

        _networkHandler.OnServerStarted += HandleServerStarted;
        _networkHandler.OnPeerConnected += HandlePeerConnected;
        _networkHandler.OnPeerDisconnected += HandlePeerDisconnected;

        _isHosting = true;
    }


    public void StopHosting()
    {
        if (!_isHosting) return;

        GD.Print("Stopping LAN session...");

        _lanBroadcaster = null;

        _isHosting = false;
        OnSessionStopped?.Invoke();
    }

    // ----------------------
    // Event handlers
    // ----------------------
    private void HandleServerStarted()
    {
        GD.Print("NetworkHandler reports server started!");
        OnSessionStarted?.Invoke();
    }

    private void HandlePeerConnected(int peerId)
    {
        GD.Print($"Player joined: {peerId}");
        OnPlayerJoined?.Invoke(peerId);
    }

    private void HandlePeerDisconnected(int peerId)
    {
        GD.Print($"Player left: {peerId}");
        OnPlayerLeft?.Invoke(peerId);
    }
}