using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        GD.Print($"Client sent: {message.MessageType}.");

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
    public static void Broadcast(IEnumerable<ENetPacketPeer> clients, Message message)
    {
        byte[] data = message.WriteMessage();

        foreach (var client in clients)
        {
            if (client != null)
            {
                client.Send(0, data, (int)message.Flags);
            }
        }
    }

    // ----------------------
    // Server → All Clients Except One
    // ----------------------
    public static void BroadcastExcept(IEnumerable<ENetPacketPeer> clients, ENetPacketPeer ignoredPeer, Message message)
    {
        byte[] data = message.WriteMessage();

        foreach (var client in clients)
        {
            if (client != null && client != ignoredPeer)
            {
                client.Send(0, data, (int)message.Flags);
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

