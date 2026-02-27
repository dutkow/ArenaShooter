using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// NetworkHandler handles low-level networking for both server and client.
/// LAN only for now. Uses ENet for packet delivery. 
/// </summary>
public partial class NetworkHandler : Node
{
    public static NetworkHandler Instance;

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
    private ENetConnection connection;
    private bool isServer = false;

    public NetworkHandler()
    {
        // Initialize available peer IDs 255 -> 0
        for (int i = 255; i >= 0; i--)
        {
            availablePeerIds.Add(i);
        }
    }

    public override void _Ready()
    {
        base._Ready();

        Instance = this;
    }

    public override void _Process(double delta)
    {
        if (connection == null) return;

        HandleEvents();
    }

    // ----------------------
    // Event handling
    // ----------------------
    private void HandleEvents()
    {
        var packetEvent = connection.Service();
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
                        PeerConnected(peer);
                    else
                        ConnectedToServer();
                    break;

                case ENetConnection.EventType.Disconnect:
                    if (isServer)
                        PeerDisconnected(peer);
                    else
                    {
                        DisconnectedFromServer();
                        return; // connection is now null
                    }
                    break;

                case ENetConnection.EventType.Receive:
                    if (isServer)
                        OnServerPacket?.Invoke((int)peer.GetMeta("id"), peer.GetPacket());
                    else
                        OnClientPacket?.Invoke(peer.GetPacket());
                    break;
            }

            // Service again to handle remaining packets in current frame
            packetEvent = connection.Service();
            eventType = (ENetConnection.EventType)(int)packetEvent[0];
        }
    }

    // ----------------------
    // Server functions
    // ----------------------
    public void StartServer(string ipAddress = "127.0.0.1", int port = 42069)
    {
        connection = new ENetConnection();
        var error = connection.CreateHostBound(ipAddress, port);
        if (error != Error.Ok)
        {
            GD.Print($"Server failed to start: {error}");
            connection = null;
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
        OnPeerDisconnected?.Invoke(peerId);
    }

    // ----------------------
    // Client functions
    // ----------------------
    public void StartClient(string ipAddress = "127.0.0.1", int port = 42069)
    {
        connection = new ENetConnection();
        var error = connection.CreateHost(1); // 1 client
        if (error != Error.Ok)
        {
            GD.Print($"Client failed to start: {error}");
            connection = null;
            return;
        }

        GD.Print("Client started");
        serverPeer = connection.ConnectToHost(ipAddress, port);
    }

    public void DisconnectClient()
    {
        if (isServer || serverPeer == null) return;
        serverPeer.PeerDisconnect();
    }

    private void ConnectedToServer()
    {
        GD.Print("Connected to server!");
        OnConnectedToServer?.Invoke();
    }

    private void DisconnectedFromServer()
    {
        GD.Print("Disconnected from server!");
        OnDisconnectedFromServer?.Invoke();
        connection = null;
    }
}