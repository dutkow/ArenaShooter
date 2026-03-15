using Godot;
using System;
using System.Collections.Generic;
using System.Data;

/// <summary>
/// NetworkHandler handles low-level networking for both server and client.
/// LAN only for now. Uses ENet for packet delivery. 
/// </summary>
public class NetworkPeer : ITickable
{
    public static NetworkPeer Instance;


    // ----------------------
    // Server events
    // ----------------------
    public Action? OnServerStarted;
    public Action<int>? OnPeerConnectedEvent;
    public Action<int>? OnPeerDisconnectedEvent;
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

    // ----------------------
    // LAN discovery
    // ----------------------
    private PacketPeerUdp lanBroadcastPeer;
    private PacketPeerUdp lanListenPeer;

    private bool isListeningForLan = false;
    private double lanListenTimer = 0.0;

    private double lanBroadcastTimer = 0.0;

    public Action<ServerInfo>? OnLanServerDiscovered;

    private Dictionary<string, ServerInfo> _discoveredLanServers = new();

    public HashSet<ENetPacketPeer> ReadyPeers = new();


    public NetworkPeer()
    {
        Instance = this;

        //_session = new();
        //_session.Initialize();

        // Initialize available peer IDs 255 -> 0
        for (int i = 255; i >= 0; i--)
        {
            availablePeerIds.Add(i);
        }
    }

    public virtual void Tick(double delta)
    {
        if (Connection == null) return;

        HandleEvents();
        NetworkSender.Process(delta);
    }

    // ----------------------
    // Event handling
    // ----------------------
    public virtual void HandleEvents()
    {
        GD.Print($"handle events running. net mode: {NetworkManager.Instance.NetworkMode}");

        var packetEvent = Connection.Service();
        ENetConnection.EventType eventType = (ENetConnection.EventType)(int)packetEvent[0];

        while (eventType != ENetConnection.EventType.None)
        {
            ENetPacketPeer peer = (ENetPacketPeer)packetEvent[1];

            switch (eventType)
            {
                case ENetConnection.EventType.Connect:
                    HandlePeerConnected(peer);
                    break;

                case ENetConnection.EventType.Disconnect:
                    HandlePeerDisconnected(peer);
                    break;

                case ENetConnection.EventType.Receive:
                    GD.Print($"HANDLING RECEIVED PACKET");
                    HandleReceivedPacket(peer);
                    break;

                case ENetConnection.EventType.Error:
                    HandleError();
                    break;
            }

            packetEvent = Connection.Service();
            eventType = (ENetConnection.EventType)(int)packetEvent[0];
        }
    }

    public virtual void HandleError()
    {
        GD.PushWarning("Network error occurred!");
    }

    public virtual void HandlePeerConnected(ENetPacketPeer peer)
    {

    }

    public virtual void HandlePeerDisconnected(ENetPacketPeer peer)
    {

    }

    private void HandleReceivedPacket(ENetPacketPeer peer)
    {
        HandleReceivedPacketFromPeer(peer, peer.GetPacket());
    }

    public virtual void HandleReceivedPacketFromPeer(ENetPacketPeer peer, byte[] packet)
    {

    }

    // ----------------------
    // Server functions
    // ----------------------


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
        NetworkManager.Instance.PeerIDsToPeers[peerId] = peer;

        GD.Print($"added peer with peer id {peerId} to dict. peer = {peer}");

        clientPeers[peerId] = peer;

        GD.Print($"Peer connected: {peerId}");
        OnPeerConnectedEvent?.Invoke(peerId);
    }

    private void PeerDisconnected(ENetPacketPeer peer)
    {
        int peerId = (int)peer.GetMeta("id");
        availablePeerIds.Add(peerId);
        clientPeers.Remove(peerId);

        GD.Print($"Peer disconnected: {peerId}");

        ReadyPeers.Remove(peer);

        OnPeerDisconnectedEvent?.Invoke(peerId);
    }

    // ----------------------
    // Client functions
    // ----------------------


    private bool _hasFiredConnected = false;

    private void ConnectedToServer(ENetPacketPeer peer)
    {
        if (_hasFiredConnected)
        {
            return;
        }

        _hasFiredConnected = true;
        GD.Print("Connected to server!");

        NetworkManager.Instance.HandleConnectedToServer(peer);
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
        lanListenPeer.Bind(NetworkConstants.DEFAULT_PORT);

        isListeningForLan = true;
        lanListenTimer = listenSeconds;
    }


}