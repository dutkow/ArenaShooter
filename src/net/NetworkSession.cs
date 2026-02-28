using Godot;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public partial class NetworkSession : Node
{
    public static NetworkSession Instance { get; private set; }

    [Export] private NetworkHandler _networkHandler;
    private LanServerBroadcaster _lanBroadcaster;

    private ServerInfo _serverInfo;
    private bool _isHosting = false;

    public Action? OnSessionStarted;
    public Action? OnSessionStopped;
    public Action<int>? OnPlayerJoined;
    public Action<int>? OnPlayerLeft;

    public Action<List<ServerInfo>>? OnServerRefreshFinished;
    public Action? OnConnectedToServer;
    public Action? OnFailedToConnect;

    public NetworkSession()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        base._Ready();
        _networkHandler = NetworkHandler.Instance;
    }

    public void HostLanServer(ServerInfo info)
    {
        _serverInfo = info;
        _networkHandler.StartServer(info.HostIP, info.Port);

        if (_lanBroadcaster == null)
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

        _lanBroadcaster = null;
        _isHosting = false;
        OnSessionStopped?.Invoke();
    }

    private void HandleServerStarted()
    {
        OnSessionStarted?.Invoke();
    }

    private void HandlePeerConnected(int peerId)
    {
        OnPlayerJoined?.Invoke(peerId);
    }

    private void HandlePeerDisconnected(int peerId)
    {
        OnPlayerLeft?.Invoke(peerId);
    }

    // ----------------------
    // Join server
    // ----------------------
    public void JoinServer(ServerInfo server)
    {
        GD.Print("Attempting to join server...");
        if (_isHosting)
        {
            GD.Print("Cannot join a server while hosting!");
            OnFailedToConnect?.Invoke();
            return;
        }

        _networkHandler.StartClient(server.HostIP, server.Port);
        _networkHandler.OnConnectedToServer += HandleConnectedToServer;
        _networkHandler.OnDisconnectedFromServer += HandleFailedToConnect;
    }

    private void HandleConnectedToServer()
    {
        GD.Print("Successfully connected to server");
        _networkHandler.OnConnectedToServer -= HandleConnectedToServer;
        _networkHandler.OnDisconnectedFromServer -= HandleFailedToConnect;

        OnConnectedToServer?.Invoke();
    }

    private void HandleFailedToConnect()
    {
        GD.Print("Failed to connect to server");

        _networkHandler.OnConnectedToServer -= HandleConnectedToServer;
        _networkHandler.OnDisconnectedFromServer -= HandleFailedToConnect;

        OnFailedToConnect?.Invoke();
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