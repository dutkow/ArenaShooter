using Godot;
using System;
using System.Collections.Generic;

public static class NetworkSender
{
    // -------------------------------------------------
    // Emulator Settings
    // -------------------------------------------------

    public static bool EmulationEnabled = true;

    public static float BasePingMs = 500f;
    public static float PingVarianceMs = 20f;

    public static float PacketLossPercent = 1.0f;

    private class QueuedPacket
    {
        public ENetPacketPeer Peer;
        public byte[] Data;
        public int Channel;
        public int Flags;
        public double SendTime;
    }

    private static readonly List<QueuedPacket> Queue = new();
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

        for (int i = Queue.Count - 1; i >= 0; i--)
        {
            var pkt = Queue[i];

            if (pkt.SendTime <= CurrentTime)
            {
                pkt.Peer.Send(pkt.Channel, pkt.Data, pkt.Flags);
                Queue.RemoveAt(i);
            }
        }
    }

    // -------------------------------------------------
    // Core Send Wrapper
    // -------------------------------------------------

    private static void SendInternal(ENetPacketPeer peer, int channel, byte[] data, int flags)
    {
        if (!EmulationEnabled)
        {
            peer.Send(channel, data, flags);
            return;
        }

        bool reliable = (flags & (int)ENetPacketFlags.Reliable) != 0;

        // drop only unreliable packets
        if (!reliable && Rng.NextDouble() < PacketLossPercent / 100.0)
            return;

        double delay = (BasePingMs + (Rng.NextDouble() * 2 - 1) * PingVarianceMs) / 2000.0;

        Queue.Add(new QueuedPacket
        {
            Peer = peer,
            Data = data,
            Channel = channel,
            Flags = flags,
            SendTime = CurrentTime + delay
        });
    }

    private static double RandRange(double min, double max)
    {
        return Rng.NextDouble() * (max - min) + min;
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

        SendInternal(serverPeer, 0, message.WriteMessage(), (int)message.Flags);
    }

    // -------------------------------------------------
    // Server → Single Client
    // -------------------------------------------------

    public static void ToClient(ENetPacketPeer clientPeer, Message message)
    {
        if (clientPeer == null)
            return;

        SendInternal(clientPeer, 0, message.WriteMessage(), (int)message.Flags);
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
                SendInternal(peer, 0, data, flags);
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

            SendInternal(kvp.Value, 0, data, flags);
        }
    }

    // -------------------------------------------------
    // Server → Filtered Clients
    // -------------------------------------------------

    public static void BroadcastFiltered(
        IEnumerable<ENetPacketPeer> clients,
        Func<ENetPacketPeer, bool> filter,
        Message message)
    {
        byte[] data = message.WriteMessage();
        int flags = (int)message.Flags;

        foreach (var client in clients)
        {
            if (client != null && filter(client))
                SendInternal(client, 0, data, flags);
        }
    }
}