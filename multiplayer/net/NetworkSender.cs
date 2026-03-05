using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Channels;

/// <summary>
/// NetworkSend handles all network sending logic in a centralized way.
/// Supports Client → Server, Server → Client, and broadcasts.
/// </summary>
public static class NetworkSender
{
    // ----------------------
    // Client → Server
    // ----------------------
    public static void ToServer(Message message)
    {
        ENetPacketPeer serverPeer = NetworkSession.Instance.ServerPeer;
        if (serverPeer == null)
        {
            GD.PushError("ServerPeer is null.");
            return;
        }
        serverPeer.Send(0, message.WriteMessage(), (int)message.Flags);
    }

    // ----------------------
    // Server → Single Client
    // ----------------------
    public static void ToClient(ENetPacketPeer clientPeer, Message message)
    {
        if (clientPeer != null)
        {
            clientPeer.Send(0, message.WriteMessage(), (int)message.Flags);
        }
    }

    // ----------------------
    // Server → All Clients
    // ----------------------
    public static void Broadcast(Message message)
    {
        byte[] data = message.WriteMessage();

        foreach (var peer in NetworkHandler.Instance.ReadyPeers) // by using this instead of GetPeers() we can avoid broadcasting to irrelevant peers (i.e., peers that haven't loaded yet)
        {
            if(peer != null)
            {
                peer.Send(0, data, (int)message.Flags);
            }
        }
    }

    // ----------------------
    // Server → All Clients Except One
    // ----------------------
    public static void BroadcastExcept(byte excludedPlayerID, Message message)
    {
        byte[] data = message.WriteMessage();

        foreach (var kvp in NetworkSession.Instance.PlayerIDsToPeers)
        {
            byte playerID = kvp.Key;
            ENetPacketPeer peer = kvp.Value;

            if (playerID != excludedPlayerID)
            {
                peer.Send(0, data, (int)message.Flags);
            }

        }
    }

    // ----------------------
    // Server → Filtered Clients (e.g., team-based)
    // ----------------------
    public static void BroadcastFiltered(IEnumerable<ENetPacketPeer> clients, Func<ENetPacketPeer, bool> filter, Message message)
    {
        byte[] data = message.WriteMessage();

        foreach (var client in clients)
        {
            if (client != null && filter(client))
            {
                client.Send(0, data, (int)message.Flags);
            }
        }
    }
}

