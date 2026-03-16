using Godot;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

public static class NetworkSender
{
    // -------------------------------------------------
    // Emulator Settings
    // -------------------------------------------------
    public static bool EmulationEnabled = true;

    public static float ClientToServerBasePingMs = 20.0f;
    public static float ClientToServerVarianceMs = 0.0f;
    public static float ClientToServerPacketLossPercent = 0.0f;

    public static float ServerToClientBasePingMs = 20.0f;
    public static float ServerToClientVarianceMs = 0.0f;
    public static float ServerToClientPacketLossPercent = 0.0f;

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
    // Predefined Network Profiles
    // -------------------------------------------------
    public enum NetworkProfile
    {
        GREAT,
        GOOD,
        AVERAGE,
        BAD
    }

    private static readonly Dictionary<NetworkProfile, (float basePing, float variance, float packetLoss)> ProfileSettings
        = new()
    {
        // Base ping = RTT (round trip), variance in ms, packet loss %
        { NetworkProfile.GREAT,   (40f, 5f, 0f) },    // ~20 ms one-way, very stable
        { NetworkProfile.GOOD,    (60f, 10f, 0.01f) }, // ~30 ms one-way, slight jitter
        { NetworkProfile.AVERAGE, (100f, 15f, 0.02f) }, // ~50 ms one-way, normal internet
        { NetworkProfile.BAD,     (200f, 20f, 0.05f) }  // ~100 ms one-way, noticeable latency
    };

    // -------------------------------------------------
    // Network Emulation Control
    // -------------------------------------------------
    public static void ToggleNetEmulation(bool enabled)
    {
        EmulationEnabled = enabled;
    }

    public static void SetNetworkProfile(NetworkProfile profile)
    {
        var settings = ProfileSettings[profile];

        ClientToServerBasePingMs = settings.basePing;
        ClientToServerVarianceMs = settings.variance;
        ClientToServerPacketLossPercent = settings.packetLoss;

        ServerToClientBasePingMs = settings.basePing;
        ServerToClientVarianceMs = settings.variance;
        ServerToClientPacketLossPercent = settings.packetLoss;
    }

    // -------------------------------------------------
    // Frame Update
    // -------------------------------------------------
    public static void Process(double delta)
    {
        CurrentTime += delta;

        if (!EmulationEnabled)
            return;

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

        // Drop only unreliable packets
        if (!reliable && Rng.NextDouble() < packetLoss)
            return;

        // Delay in seconds (half RTT to simulate one-way)
        double delay = (basePing / 2.0 + (Rng.NextDouble() * 2 - 1) * variance / 2.0) / 1000.0;

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
        ENetPacketPeer serverPeer = NetworkClient.Instance.ServerPeer;

        if (serverPeer == null)
        {
            GD.PushError("ServerPeer is null.");
            return;
        }

        SendInternal(serverPeer, 0, message.WriteMessage(), (int)message.Flags, true);
    }

    // -------------------------------------------------
    // Server → Single Client
    // -------------------------------------------------
    public static void ToClient(ENetPacketPeer clientPeer, Message message)
    {
        if (clientPeer == null)
        {
            return;
        }

        SendInternal(clientPeer, 0, message.WriteMessage(), (int)message.Flags, false);
    }

    // -------------------------------------------------
    // Server → All Clients
    // -------------------------------------------------
    public static void Broadcast(Message message)
    {
        byte[] data = message.WriteMessage();
        int flags = (int)message.Flags;

        foreach (var peer in NetworkPeer.Instance.ReadyPeers)
        {
            if (peer != null)
                SendInternal(peer, 0, data, flags, false);
        }
    }

    // -------------------------------------------------
    // Server → All Clients Except One
    // -------------------------------------------------
    public static void BroadcastExcept(byte excludedPlayerID, Message message)
    {
        byte[] data = message.WriteMessage();
        int flags = (int)message.Flags;

        foreach (var kvp in NetworkManager.Instance.PlayerIDsToPeers)
        {
            if (kvp.Key == excludedPlayerID)
                continue;

            SendInternal(kvp.Value, 0, data, flags, false);
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
                SendInternal(client, 0, data, flags, false);
        }
    }
}