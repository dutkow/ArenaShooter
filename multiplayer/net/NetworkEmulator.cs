using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public static class NetworkEmulator
{
    public static bool Enabled = false;
    public static float MinPingMs = 50.0f;
    public static float MaxPingMs = 60.0f;
    public static float PacketLossPercent = 0.01f;

    private class QueuedPacket
    {
        public ENetPacketPeer Peer;
        public byte[] Data;
        public double DeliveryTime;
        public Action<ENetPacketPeer, byte[]> DeliverAction;
    }

    private static readonly List<QueuedPacket> IncomingQueue = new();
    public static readonly Random Rng = new();
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

    // Call this in your HandleReceivedMessage
    public static void Receive(ENetPacketPeer peer, byte[] data, Action<ENetPacketPeer, byte[]> deliverAction)
    {
        // simulate packet loss
        if (Rng.NextDouble() < PacketLossPercent / 100.0)
            return;

        // simulate latency
        double delay = RandfRange(MinPingMs / 1000.0, MaxPingMs / 1000.0);
        double now = GetTimeSeconds();
        IncomingQueue.Add(new QueuedPacket
        {
            Peer = peer,
            Data = data,
            DeliveryTime = now + delay,
            DeliverAction = deliverAction
        });
    }

    // Call this every frame to flush delayed packets
    public static void ProcessQueue()
    {
        double now = GetTimeSeconds(); // use absolute time, NOT delta

        for (int i = IncomingQueue.Count - 1; i >= 0; i--)
        {
            var pkt = IncomingQueue[i];
            if (pkt.DeliveryTime <= now)
            {
                pkt.DeliverAction(pkt.Peer, pkt.Data);
                IncomingQueue.RemoveAt(i);
            }
        }
    }

    private static double GetTimeSeconds()
    {
        return Stopwatch.Elapsed.TotalSeconds;
    }

    public static double RandfRange(double min, double max)
    {
        return Rng.NextDouble() * (max - min) + min;
    }
}