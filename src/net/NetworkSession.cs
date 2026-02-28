using Godot;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

    // ----------------------
    // Server discovery
    // ----------------------
    public Action<List<ServerInfo>>? OnServerRefreshFinished;

    public NetworkSession()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        base._Ready();


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

    // ----------------------
    // LAN Discovery
    // ----------------------
    public async void RefreshLanServers(float listenSeconds = 2f)
    {
        var servers = await ListenForLanServersAsync(listenSeconds);
        OnServerRefreshFinished?.Invoke(servers);
    }

    private async Task<List<ServerInfo>> ListenForLanServersAsync(float listenSeconds)
    {
        var discoveredServers = new List<ServerInfo>();
        var seenServerIDs = new HashSet<string>();

        using var listener = new UdpClient(_networkHandler.LanBroadcastPort);
        listener.EnableBroadcast = true;

        var timeout = DateTime.Now.AddSeconds(listenSeconds);

        while (DateTime.Now < timeout)
        {
            if (listener.Available > 0)
            {
                var result = await listener.ReceiveAsync();
                var data = Encoding.UTF8.GetString(result.Buffer);
                var serverInfo = ServerInfo.FromString(data);

                if (!seenServerIDs.Contains(serverInfo.ServerID))
                {
                    discoveredServers.Add(serverInfo);
                    seenServerIDs.Add(serverInfo.ServerID);
                }
            }

            await Task.Delay(50);
        }

        return discoveredServers;
    }
}