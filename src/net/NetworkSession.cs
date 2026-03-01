using Godot;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

public enum NetRole : byte
{
    LOCAL,
    CLIENT,
    SERVER,
}


public partial class NetworkSession : Node
{

    public static NetworkSession Instance { get; private set; }

    public NetRole Role { get; private set; } = NetRole.LOCAL;


    [Export] private NetworkHandler _networkHandler;

    private MessageRouter _messageRouter;


    private LanServerBroadcaster _lanBroadcaster;



    public ServerInfo ServerInfo;
    private bool _isHosting = false;

    public Action<ServerInfo> OnSessionStarted;
    public Action? OnSessionStopped;
    public Action<int>? OnPlayerJoined;
    public Action<int>? OnPlayerLeft;

    public Action<List<ServerInfo>>? OnServerRefreshFinished;
    public Action? OnConnectedToServer;
    public Action? OnFailedToConnect;
    public Action? OnDisconnectedFromServer;

    public Action<NetRole> OnRoleChanged;


    public NetworkSession()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        base._Ready();
        
        _networkHandler = NetworkHandler.Instance;

        _messageRouter = new();
        _messageRouter.Initialize(Role);

        _networkHandler.OnServerStarted += HandleServerStarted;
        _networkHandler.OnPeerConnected += HandlePeerConnected;
        _networkHandler.OnPeerDisconnected += HandlePeerDisconnected;

        _networkHandler.OnConnectedToServer += HandleConnectedToServer;
        _networkHandler.OnDisconnectedFromServer += HandleFailedToConnect;


    }

    public void SetRole(NetRole role)
    {
        if(Role == role)
        {
            return;
        }

        Role = role;
        OnRoleChanged?.Invoke(Role);
    }

    public void HostLanServer(ServerInfo info)
    {
        ServerInfo = info;

        _networkHandler.StartServer(info.HostIP, info.Port);
        SetRole(NetRole.SERVER);

        if (_lanBroadcaster == null)
        {
            _lanBroadcaster = new();
        }

        _lanBroadcaster.StartBroadcast(info);


        _isHosting = true;
    }

    public void StopHosting()
    {
        if (!_isHosting)
        {
            return;
        }

        SetRole(NetRole.LOCAL);

        _lanBroadcaster = null;
        _isHosting = false;
        OnSessionStopped?.Invoke();
    }

    private void HandleServerStarted()
    {
        OnSessionStarted?.Invoke(ServerInfo);
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
    public void JoinServer(ServerInfo serverInfo)
    {
        ServerInfo = serverInfo;

        GD.Print("Attempting to join server...");
        if (_isHosting)
        {
            GD.Print("Cannot join a server while hosting!");
            OnFailedToConnect?.Invoke();
            return;
        }

        _networkHandler.StartClient(serverInfo.HostIP, serverInfo.Port);
    }

    private void HandleConnectedToServer()
    {
        GD.Print("Successfully connected to server");
        SetRole(NetRole.CLIENT);
        OnConnectedToServer?.Invoke();
    }

    private void HandleFailedToConnect()
    {
        GD.Print("Failed to connect to server");

        OnFailedToConnect?.Invoke();
    }

    public void HandleDisconnectedFromServer()
    {
        OnDisconnectedFromServer?.Invoke();
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


    private async Task<int> PingServerAsync(ServerInfo server, int timeoutMs = 1000)
    {
        try
        {
            using var udp = new UdpClient();
            udp.Connect(server.HostIP, server.Port);

            byte[] pingPacket = new byte[] { 0 }; // dummy ping
            var stopwatch = Stopwatch.StartNew();

            await udp.SendAsync(pingPacket, pingPacket.Length);

            // wait for reply or timeout
            var task = udp.ReceiveAsync();
            if (await Task.WhenAny(task, Task.Delay(timeoutMs)) == task)
            {
                stopwatch.Stop();
                return (int)stopwatch.ElapsedMilliseconds;
            }
            else
            {
                return -1; // timeout
            }
        }
        catch
        {
            return -1; // error
        }
    }

 
}