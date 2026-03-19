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
    // General state
    // ----------------------
    public ENetConnection Connection;

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

    public virtual void Disconnect()
    {

    }

    public virtual void Shutdown() { }

}