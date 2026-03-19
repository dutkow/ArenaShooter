using Godot;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

public enum NetworkMode : byte
{
    OFFLINE,
    DEDICATED_SERVER,
    CLIENT,
    LISTEN_SERVER,
}

public enum NetworkRole : byte
{
    NONE,
    LOCAL,
    REMOTE
}

public class NetworkManager : ITickable
{
    public static NetworkManager Instance { get; private set; }

    public NetworkMode NetworkMode { get; private set; } = NetworkMode.OFFLINE;
    public bool IsClient => NetworkMode == NetworkMode.CLIENT;
    public bool IsListenServer => NetworkMode == NetworkMode.LISTEN_SERVER;
    public bool IsDedicatedServer => NetworkMode == NetworkMode.DEDICATED_SERVER;

    public bool IsServer => IsListenServer || IsDedicatedServer;

    // ----------------------
    // Connected player info
    // ----------------------
    public int MaxPlayers { get; private set; } = 8;

    public Dictionary<byte, ENetPacketPeer> PlayerIDsToPeers = new();
    private Queue<byte> _availablePlayerIDs = new();

    private NetworkPeer _networkPeer;
    private LanServerAdvertiser _lanBroadcaster;

    public ServerInfo ServerInfo;
    private bool _isHosting = false;

    // ----------------------
    // Events
    // ----------------------
    public Action<ServerInfo> OnSessionStarted;
    public Action? OnSessionStopped;
    public Action<byte, string>? OnPlayerJoined;
    public Action<byte>? OnPlayerLeft;

    public Action<List<ServerInfo>>? OnServerRefreshFinished;
    public Action? OnConnectedToServer;
    public Action? OnFailedToConnect;
    public Action? OnDisconnectedFromServer;

    public Action? OnConnectionToServerAccepted;
    public Action<NetworkMode> OnRoleChanged;

    public ENetPacketPeer ServerPeer;

    public Action<ServerInfo> JoinedServer;


    // ----------------------
    // Initialization
    // ----------------------

    public static void Create()
    {
        Instance = new NetworkManager();

        Instance.Initialize();
    }

    public void Initialize()
    {
        Instance = this;


        _availablePlayerIDs.Clear();
        for (byte i = 1; i <= MaxPlayers; i++)
        {
            _availablePlayerIDs.Enqueue(i);
        }
    }

    public virtual void Tick(double delta)
    {
        _networkPeer?.Tick(delta);
    }

    public void SetMode(NetworkMode mode)
    {
        if (NetworkMode == mode)
        {
            return;
        }

        NetworkMode = mode;

        switch (NetworkMode)
        {
            case NetworkMode.DEDICATED_SERVER:
                _networkPeer = NetworkServer.Initialize();
                ServerGame.Initialize();
                break;
            case NetworkMode.OFFLINE:
            case NetworkMode.LISTEN_SERVER:
                _networkPeer = NetworkServer.Initialize();
                ServerGame.Initialize();
                ClientGame.Initialize();
                break;

            case NetworkMode.CLIENT:
                _networkPeer = NetworkClient.Initialize();
                ClientGame.Initialize();
                break;
        }

        //_router.Initialize(mode);
        OnRoleChanged?.Invoke(NetworkMode);
    }

    // ----------------------
    // Hosting / Server
    // ----------------------
    public void HostLanServer(ServerInfo serverInfo)
    {
        SetMode(NetworkMode.LISTEN_SERVER);

        ServerInfo = serverInfo;
        var error = NetworkServer.Instance.StartLanServer(serverInfo.IP, serverInfo.Port, serverInfo);

        if(error == Error.Ok)
        {
            BroadcastServerJoined();
        }

        _isHosting = true;
    }

    // ----------------------
    // Joining / Client
    // ----------------------
    public void JoinServer(ServerInfo serverInfo)
    {
        SetMode(NetworkMode.CLIENT);

        ServerInfo = serverInfo;

        if (_isHosting)
        {
            OnFailedToConnect?.Invoke();
            return;
        }

        NetworkClient.Instance.JoinServer(serverInfo.IP, serverInfo.Port);
    }

    public void BroadcastServerJoined()
    {
        JoinedServer?.Invoke(ServerInfo);

        CommandConsole.Instance.AddConsoleLogEntry($"Connection request accepted. Joining server... Server Name: {ServerInfo.Name}.");

    }

    public async void ShutdownServer()
    {
        await Task.Delay(NetworkConstants.SERVER_SHUTDOWN_DELAY_MS);

        ShutdownNetworkPeer();
    }

    public void ShutdownNetworkPeer()
    {
        _networkPeer.Shutdown();
        _networkPeer = null;
    }

    public void DisconnectFromServer()
    {
        _networkPeer = null;
    }
}