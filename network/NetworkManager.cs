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
    public byte LocalPlayerID = 0;
    public int MaxPlayers { get; private set; } = 8;
    public Dictionary<byte, PlayerState> PlayerIDsToPlayerStates = new();
    public Dictionary<int, byte> PeerIDsToPlayerIDs = new();
    public Dictionary<int, ENetPacketPeer> PeerIDsToPeers = new();

    public Dictionary<byte, ENetPacketPeer> PlayerIDsToPeers = new();
    private Queue<byte> _availablePlayerIDs = new();

    public bool IsServerFull => PlayerIDsToPlayerStates.Count >= MaxPlayers;

    private NetworkPeer _networkPeer;
    private MessageRouter _router;
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

        _networkPeer = new();

        _networkPeer.OnServerStarted += HandleServerStarted;
        //_networkHandler.OnPeerConnected += HandlePeerConnected;
        _networkPeer.OnPeerDisconnectedEvent += HandlePeerDisconnected;
        _networkPeer.OnDisconnectedFromServer += HandleFailedToConnect;

        _router = new MessageRouter();

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

        if(NetworkMode == NetworkMode.LISTEN_SERVER)
        {
            LocalPlayerID = 0;
        }

        NetworkMode = mode;

        switch (NetworkMode)
        {
            case NetworkMode.OFFLINE:
            case NetworkMode.DEDICATED_SERVER:
            case NetworkMode.LISTEN_SERVER:
                ServerGame.Initialize();
                break;

            case NetworkMode.CLIENT:
                break;
        }

        _router.Initialize(mode);
        OnRoleChanged?.Invoke(NetworkMode);
    }

    // ----------------------
    // Hosting / Server
    // ----------------------
    public void HostLanServer(ServerInfo info)
    {
        SetMode(NetworkMode.LISTEN_SERVER);

        ServerInfo = info;

        _networkPeer.StartLanServer(info.IP, info.Port);

        ServerGame.Instance.InitializeLanServer(ServerInfo);

        _isHosting = true;
    }

    public void StopHosting()
    {
        SetMode(NetworkMode.OFFLINE);

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

            PeerIDsToPeers.Remove(_peerID);

            PlayerIDsToPeers.Remove(_playerID);

            _availablePlayerIDs.Enqueue(_playerID);

            GD.Print($"Player disconnected: _peerID={_peerID}, _playerID={_playerID}");
            OnPlayerLeft?.Invoke(_playerID);
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
        SetMode(NetworkMode.CLIENT);


        ServerInfo = serverInfo;

        GD.Print("Attempting to join server...");
        if (_isHosting)
        {
            GD.Print("Cannot join a server while hosting!");
            OnFailedToConnect?.Invoke();
            return;
        }

        _networkPeer.StartClient(serverInfo.IP, serverInfo.Port);
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

        PeerIDsToPeers[peerID] = peer;

        PlayerIDsToPlayerStates[playerID] = new PlayerState()
        {
            PlayerID = playerID,
            PlayerName = playerName
        };

        GameMode.Instance.AddPlayerController(playerID);

        GD.Print($"Connection request accepted: peerID={peerID}, playerID={playerID}, name={playerName}");
        OnPlayerJoined?.Invoke(playerID, playerName);

        ConnectionAccepted.Send(peer, playerID);

        //ServerInfo.Players++;
    }

    public void HandlePlayerJoined(byte playerID, string playerName)
    {
        OnPlayerJoined?.Invoke(playerID, playerName);
    }

}