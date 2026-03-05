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

public partial class NetworkSession : Node
{
    public static NetworkSession Instance { get; private set; }

    public NetworkMode NetworkMode { get; private set; } = NetworkMode.OFFLINE;
    public bool IsClient => NetworkMode == NetworkMode.CLIENT;
    public bool IsListenServer => NetworkMode == NetworkMode.LISTEN_SERVER;
    public bool IsDedicatedServer => NetworkMode == NetworkMode.DEDICATED_SERVER;

    public bool IsServer => IsListenServer || IsDedicatedServer;

    // ----------------------
    // Connected player info
    // ----------------------
    public byte LocalPlayerID = 0;
    public int MaxPlayers { get; private set; } = 8;
    public Dictionary<byte, PlayerState> PlayerIDsToPlayerStates = new();
    public Dictionary<int, byte> PeerIDsToPlayerIDs = new();
    public Dictionary<byte, ENetPacketPeer> PlayerIDsToPeers = new();
    private Queue<byte> _availablePlayerIDs = new();

    public bool IsServerFull => PlayerIDsToPlayerStates.Count >= MaxPlayers;

    private NetworkHandler _networkHandler;
    private MessageRouter _router;
    private LanServerBroadcaster _lanBroadcaster;

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

    // ----------------------
    // Initialization
    // ----------------------
    public void Initialize()
    {
        Instance = this;

        _router = new MessageRouter();

        // Fill the pool of available _playerIDs
        _availablePlayerIDs.Clear();
        for (byte i = 1; i <= MaxPlayers; i++)
        {
            _availablePlayerIDs.Enqueue(i);
        }

        _networkHandler = NetworkHandler.Instance;

        _networkHandler.OnServerStarted += HandleServerStarted;
        //_networkHandler.OnPeerConnected += HandlePeerConnected;
        _networkHandler.OnPeerDisconnected += HandlePeerDisconnected;
        _networkHandler.OnDisconnectedFromServer += HandleFailedToConnect;
    }

    public void SetRole(NetworkMode role)
    {
        if (NetworkMode == role)
        {
            return;
        }

        if(NetworkMode == NetworkMode.LISTEN_SERVER)
        {
            LocalPlayerID = 0;
        }

        NetworkMode = role;
        _router.Initialize(role);
        OnRoleChanged?.Invoke(NetworkMode);
    }

    // ----------------------
    // Hosting / Server
    // ----------------------
    public void HostLanServer(ServerInfo info)
    {
        SetRole(NetworkMode.LISTEN_SERVER);

        ServerInfo = info;

        _networkHandler.StartServer(info.HostIP, info.Port);
        ServerInfo.Players++;


        if (_lanBroadcaster == null)
        {
            _lanBroadcaster = new();
        }

        _lanBroadcaster.StartBroadcast(info);

        _isHosting = true;
    }

    public void StopHosting()
    {
        SetRole(NetworkMode.OFFLINE);

        if (!_isHosting)
        {
            return;
        }

        _lanBroadcaster = null;
        _isHosting = false;
        OnSessionStopped?.Invoke();
    }

    private void HandleServerStarted()
    {
        OnSessionStarted?.Invoke(ServerInfo);
    }

    private void HandlePeerDisconnected(int _peerID)
    {
        if (PeerIDsToPlayerIDs.TryGetValue(_peerID, out byte _playerID))
        {
            PeerIDsToPlayerIDs.Remove(_peerID);
            PlayerIDsToPlayerStates.Remove(_playerID);
            PlayerIDsToPeers.Remove(_playerID);

            _availablePlayerIDs.Enqueue(_playerID);

            GD.Print($"Player disconnected: _peerID={_peerID}, _playerID={_playerID}");
            OnPlayerLeft?.Invoke(_playerID);

            ServerInfo.Players--;
        }
        else
        {
            GD.PushError($"_peerID {_peerID} disconnected but no _playerID was assigned");
        }
    }

    // ----------------------
    // Joining / Client
    // ----------------------
    public void JoinServer(ServerInfo serverInfo)
    {
        SetRole(NetworkMode.CLIENT);

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

    private void HandleFailedToConnect()
    {
        GD.Print("Failed to connect to server");
        OnFailedToConnect?.Invoke();
    }

    public void HandleDisconnectedFromServer()
    {
        OnDisconnectedFromServer?.Invoke();
    }

    public void HandleConnectedToServer(ENetPacketPeer peer)
    {
        if (peer == null || ServerPeer == peer)
        {
            return;
        }

        GD.Print("Connected to server peer set");
        ServerPeer = peer;

        ConnectionRequest.Send(Settings.Instance.PlayerName);
    }

    // ----------------------
    // Message Routing
    // ----------------------
    public void HandleReceivedMessage(ENetPacketPeer peer, byte[] data)
    {
        if (_router == null)
        {
            GD.PrintErr("No message router assigned!");
            return;
        }

        if (NetworkEmulation.Enabled)
        {
           
            NetworkEmulation.Receive(peer, data, DeliverMessage);
            return;
        }

        DeliverMessage(peer, data);
    }

    private void DeliverMessage(ENetPacketPeer peer, byte[] data)
    {
        switch (NetworkMode)
        {
            case NetworkMode.LISTEN_SERVER:
                _router.RouteClientMessage(peer, data);
                break;

            case NetworkMode.CLIENT:
                _router.RouteServerMessage(data);
                break;
        }
    }

    public void HandleConnectionToServerAccepted()
    {
        OnConnectionToServerAccepted?.Invoke();
    }

    // ----------------------
    // LAN Discovery
    // ----------------------
    public async void RefreshLanServers(float listenSeconds = 0.3f)
    {
        var servers = await ListenForLanServersAsync(listenSeconds);
        OnServerRefreshFinished?.Invoke(servers);
    }

    private async Task<List<ServerInfo>> ListenForLanServersAsync(float listenSeconds)
    {
        var discoveredServers = new List<ServerInfo>();
        var seenServerIDs = new HashSet<string>();

        using (var listener = new UdpClient(_networkHandler.LanBroadcastPort))
        {
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
        }

        return discoveredServers;
    }

    public void HandleConnectionRequest(ENetPacketPeer peer, string playerName)
    {
        int peerID = (int)peer.GetMeta("id");

        if (IsServerFull)
        {
            GD.Print($"Connection request from peer {peerID} denied: server full");
            ConnectionDenied.Send(peer, "Server full");
            return;
        }

        byte playerID = _availablePlayerIDs.Dequeue();

        PeerIDsToPlayerIDs[peerID] = playerID;
        PlayerIDsToPeers[playerID] = peer;

        PlayerIDsToPlayerStates[playerID] = new PlayerState(playerID)
        {
            PlayerName = playerName
        };

        GameMode.Instance.AddPlayerController(playerID);

        GD.Print($"Connection request accepted: peerID={peerID}, playerID={playerID}, name={playerName}");
        OnPlayerJoined?.Invoke(playerID, playerName);

        ConnectionAccepted.Send(peer, playerID);

        ServerInfo.Players++;
    }

    public void HandlePlayerJoined(byte playerID, string playerName)
    {
        OnPlayerJoined?.Invoke(playerID, playerName);
    }

}