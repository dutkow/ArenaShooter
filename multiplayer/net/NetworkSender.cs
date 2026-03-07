using Godot;
using System;
using System.Collections.Generic;

public static class NetworkSender
{
    // -------------------------------------------------
    // Emulator Settings
    // -------------------------------------------------
    public static bool EmulationEnabled = true;

    // Client → Server
    public static float ClientToServerBasePingMs = 75.0f;
    public static float ClientToServerVarianceMs = 15.0f;
    public static float ClientToServerPacketLossPercent = 0.01f;

    // Server → Client
    public static float ServerToClientBasePingMs = 75.0f;
    public static float ServerToClientVarianceMs = 15.0f;
    public static float ServerToClientPacketLossPercent = 0.01f;

    private class QueuedPacket
    {
        public ENetPacketPeer Peer;
        public byte[] Data;
        public int Channel;
        public int Flags;
        public double SendTime;
    }

    private static readonly List<QueuedPacket> ClientToServerQueue = new();
    private static readonly List<QueuedPacket> ServerToClientQueue = new();
    private static readonly Random Rng = new();
    private static double CurrentTime = 0.0;

    // -------------------------------------------------
    // Frame Update
    // -------------------------------------------------
    public static void Process(double delta)
    {
        CurrentTime += delta;

        if (!EmulationEnabled)
            return;

        // Flush both queues
        FlushQueue(ClientToServerQueue);
        FlushQueue(ServerToClientQueue);
    }

    private static void FlushQueue(List<QueuedPacket> queue)
    {
        for (int i = queue.Count - 1; i >= 0; i--)
        {
            var pkt = queue[i];
            if (pkt.SendTime <= CurrentTime)
            {
                pkt.Peer.Send(pkt.Channel, pkt.Data, pkt.Flags);
                queue.RemoveAt(i);
            }
        }
    }

    // -------------------------------------------------
    // Core Send Wrapper
    // -------------------------------------------------
    private static void SendInternal(ENetPacketPeer peer, int channel, byte[] data, int flags, bool clientToServer)
    {
        if (!EmulationEnabled)
        {
            peer.Send(channel, data, flags);
            return;
        }

        bool reliable = (flags & (int)ENetPacketFlags.Reliable) != 0;

        float packetLoss = clientToServer ? ClientToServerPacketLossPercent : ServerToClientPacketLossPercent;
        float basePing = clientToServer ? ClientToServerBasePingMs : ServerToClientBasePingMs;
        float variance = clientToServer ? ClientToServerVarianceMs : ServerToClientVarianceMs;

        // drop only unreliable packets
        if (!reliable && Rng.NextDouble() < packetLoss)
            return;

        double delay = (basePing + (Rng.NextDouble() * 2 - 1) * variance) / 2000.0;

        var queue = clientToServer ? ClientToServerQueue : ServerToClientQueue;
        queue.Add(new QueuedPacket
        {
            Peer = peer,
            Data = data,
            Channel = channel,
            Flags = flags,
            SendTime = CurrentTime + delay
        });
    }

    // -------------------------------------------------
    // Client → Server
    // -------------------------------------------------
    public static void ToServer(Message message)
    {
        ENetPacketPeer serverPeer = NetworkSession.Instance.ServerPeer;
        if (serverPeer == null)
        {
            GD.PushError("ServerPeer is null.");
            return;
        }

        SendInternal(serverPeer, 0, message.WriteMessage(), (int)message.Flags, clientToServer: true);
    }

    // -------------------------------------------------
    // Server → Single Client
    // -------------------------------------------------
    public static void ToClient(ENetPacketPeer clientPeer, Message message)
    {
        if (clientPeer == null)
            return;

        SendInternal(clientPeer, 0, message.WriteMessage(), (int)message.Flags, clientToServer: false);
    }

    // -------------------------------------------------
    // Server → All Clients
    // -------------------------------------------------
    public static void Broadcast(Message message)
    {
        byte[] data = message.WriteMessage();
        int flags = (int)message.Flags;

        foreach (var peer in NetworkHandler.Instance.ReadyPeers)
        {
            if (peer != null)
                SendInternal(peer, 0, data, flags, clientToServer: false);
        }
    }

    // -------------------------------------------------
    // Server → All Clients Except One
    // -------------------------------------------------
    public static void BroadcastExcept(byte excludedPlayerID, Message message)
    {
        byte[] data = message.WriteMessage();
        int flags = (int)message.Flags;

        foreach (var kvp in NetworkSession.Instance.PlayerIDsToPeers)
        {
            if (kvp.Key == excludedPlayerID)
                continue;

            SendInternal(kvp.Value, 0, data, flags, clientToServer: false);
        }
    }

    // -------------------------------------------------
    // Server → Filtered Clients
    // -------------------------------------------------
    public static void BroadcastFiltered(IEnumerable<ENetPacketPeer> clients, Func<ENetPacketPeer, bool> filter, Message message)
    {
        byte[] data = message.WriteMessage();
        int flags = (int)message.Flags;

        foreach (var client in clients)
        {
            if (client != null && filter(client))
                SendInternal(client, 0, data, flags, clientToServer: false);
        }
    }
}