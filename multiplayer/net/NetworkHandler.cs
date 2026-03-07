using Godot;
using System;
using System.Collections.Generic;
using System.Data;

/// <summary>
/// NetworkHandler handles low-level networking for both server and client.
/// LAN only for now. Uses ENet for packet delivery. 
/// </summary>
public partial class NetworkHandler : Node
{
    public static NetworkHandler Instance;

    private NetworkSession _session;

    // ----------------------
    // Server events
    // ----------------------
    public Action? OnServerStarted;
    public Action<int>? OnPeerConnected;
    public Action<int>? OnPeerDisconnected;
    public Action<int, byte[]>? OnServerPacket;

    // ----------------------
    // Client events
    // ----------------------
    public Action? OnConnectedToServer;
    public Action? OnDisconnectedFromServer;
    public Action<byte[]>? OnClientPacket;

    // ----------------------
    // Server state
    // ----------------------
    private List<int> availablePeerIds = new List<int>();
    private Dictionary<int, ENetPacketPeer> clientPeers = new Dictionary<int, ENetPacketPeer>();

    // ----------------------
    // Client state
    // ----------------------
    private ENetPacketPeer serverPeer;

    // ----------------------
    // General state
    // ----------------------
    public ENetConnection Connection;
    private bool isServer = false;

    // ----------------------
    // LAN discovery
    // ----------------------
    private PacketPeerUdp lanBroadcastPeer;
    private PacketPeerUdp lanListenPeer;

    private bool isListeningForLan = false;
    private double lanListenTimer = 0.0;

    private double lanBroadcastTimer = 0.0;

    public int LanBroadcastPort = 42070;

    public Action<ServerInfo>? OnLanServerDiscovered;

    private Dictionary<string, ServerInfo> _discoveredLanServers = new();

    public HashSet<ENetPacketPeer> ReadyPeers = new();


    public NetworkHandler()
    {
        Instance = this;

        _session = new();
        _session.Initialize();

        // Initialize available peer IDs 255 -> 0
        for (int i = 255; i >= 0; i--)
        {
            availablePeerIds.Add(i);
        }
    }

    public override void _Process(double delta)
    {
        if (Connection == null) return;

        HandleEvents();
        NetworkSender.Process(delta);
    }

    // ----------------------
    // Event handling
    // ----------------------
    private void HandleEvents()
    {
        var packetEvent = Connection.Service();
        ENetConnection.EventType eventType = (ENetConnection.EventType)(int)packetEvent[0];

        while (eventType != ENetConnection.EventType.None)
        {
            ENetPacketPeer peer = (ENetPacketPeer)packetEvent[1];

            switch (eventType)
            {
                case ENetConnection.EventType.Error:
                    GD.PushWarning("Network error occurred!");
                    return;

                case ENetConnection.EventType.Connect:
                    if (isServer)
                    {
                        PeerConnected(peer);
                    }
                    else
                    {
                        ConnectedToServer(peer);
                    }
                    break;

                case ENetConnection.EventType.Disconnect:
                    if (isServer)
                    {
                        PeerDisconnected(peer);
                    }
                    else
                    {
                        DisconnectedFromServer();
                        return;
                    }
                    break;

                case ENetConnection.EventType.Receive:
                    if(_session == null)
                    {
                        GD.PushError("NetworkSession returned null.");
                        return;
                    }
                    _session.HandleReceivedMessage(peer, peer.GetPacket());
                    break;
            }

            // Service again to handle remaining packets in current frame
            packetEvent = Connection.Service();
            eventType = (ENetConnection.EventType)(int)packetEvent[0];
        }
    }

    // ----------------------
    // Server functions
    // ----------------------
    public void StartServer(string ipAddress = "127.0.0.1", int port = 42069)
    {
        GD.Print($"Attempting to start server. IP = {ipAddress}. Port = {port}.");
        Connection = new ENetConnection();
        var error = Connection.CreateHostBound(ipAddress, port);
        if (error != Error.Ok)
        {
            GD.Print($"Server failed to start: {error}");
            Connection = null;
            return;
        }

        GD.Print("Server started");
        isServer = true;

        OnServerStarted?.Invoke();
    }

    private void PeerConnected(ENetPacketPeer peer)
    {
        if (availablePeerIds.Count == 0)
        {
            GD.Print("No available peer IDs!");
            return;
        }

        int peerId = availablePeerIds[availablePeerIds.Count - 1];
        availablePeerIds.RemoveAt(availablePeerIds.Count - 1);

        peer.SetMeta("id", peerId);
        NetworkSession.Instance.PeerIDsToPeers[peerId] = peer;

        GD.Print($"added peer with peer id {peerId} to dict. peer = {peer}");

        clientPeers[peerId] = peer;

        GD.Print($"Peer connected: {peerId}");
        OnPeerConnected?.Invoke(peerId);
    }

    private void PeerDisconnected(ENetPacketPeer peer)
    {
        int peerId = (int)peer.GetMeta("id");
        availablePeerIds.Add(peerId);
        clientPeers.Remove(peerId);

        GD.Print($"Peer disconnected: {peerId}");

        ReadyPeers.Remove(peer);

        OnPeerDisconnected?.Invoke(peerId);
    }

    // ----------------------
    // Client functions
    // ----------------------
    public void StartClient(string ipAddress = "127.0.0.1", int port = 42069)
    {
        if (Connection != null)
        {
            GD.Print("Client already running!");
            return;
        }

        Connection = new ENetConnection();
        var error = Connection.CreateHost(1); // 1 client
        if (error != Error.Ok)
        {
            GD.Print($"Client failed to start: {error}");
            Connection = null;
            return;
        }

        GD.Print($"Client started, connecting to {ipAddress}:{port}...");
        serverPeer = Connection.ConnectToHost(ipAddress, port);

        // Optional: reset flags to track connection
        _hasFiredConnected = false;
    }

    private bool _hasFiredConnected = false;

    private void ConnectedToServer(ENetPacketPeer peer)
    {
        if (_hasFiredConnected)
        {
            return;
        }

        _hasFiredConnected = true;
        GD.Print("Connected to server!");

        _session.HandleConnectedToServer(peer);
        //OnConnectedToServer?.Invoke();
    }

    private void DisconnectedFromServer()
    {
        GD.Print("Disconnected from server!");
        OnDisconnectedFromServer?.Invoke();

        // Cleanup
        Connection = null;
        serverPeer = null;
        _hasFiredConnected = false;
    }

    // ----------------------
    // Server discovery functions
    // ----------------------
    public void RefreshLanServers(double listenSeconds = 2.0)
    {
        _discoveredLanServers.Clear();

        lanListenPeer = new PacketPeerUdp();
        lanListenPeer.Bind(LanBroadcastPort);

        isListeningForLan = true;
        lanListenTimer = listenSeconds;
    }


}