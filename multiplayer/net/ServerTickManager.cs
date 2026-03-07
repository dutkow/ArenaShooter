using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles server-side ticking and snapshot sending for the match.
/// Manages its own snapshot history dictionary and queue.
/// </summary>
public class ServerTickManager
{
    private double _accumulator = 0f;
    public ushort ServerTick { get; private set; }

    private const int MaxSnapshotHistory = 128;

    // History of snapshots, keyed by server tick
    private readonly Dictionary<ushort, WorldSnapshot> _snapshotHistory = new();
    private readonly Queue<ushort> _snapshotQueue = new();

    // ---- For traffic tracking ----
    private int _bytesSentThisPeriod = 0;

    public void PhysicsTick(double delta)
    {
        _accumulator += delta;

        while (_accumulator >= NetworkConstants.SERVER_TICK_INTERVAL)
        {
            _accumulator -= NetworkConstants.SERVER_TICK_INTERVAL;
            TickServer();
            ServerTick++;

            if (ServerTick % NetworkConstants.SERVER_TICK_RATE == 0)
            {
                float mbps = (_bytesSentThisPeriod * 8f) / 1_000_000f;
                GD.Print($"Traffic: ~{mbps:F4} Mbps, {_bytesSentThisPeriod} bytes/sec (~{_bytesSentThisPeriod / 64.0f} bytes/tick)");
                _bytesSentThisPeriod = 0;
            }
        }
    }

    private void TickServer()
    {
        var newSnapshot = WorldSnapshot.Build();
        SendWorldSnapshotDeltas(newSnapshot);
        AddSnapshotToHistory(ServerTick, newSnapshot);
    }

    private void SendWorldSnapshotDeltas(WorldSnapshot newSnapshot)
    {
        var snapshotDeltas = new Dictionary<uint, WorldSnapshot>();

        foreach (var kvp in MatchState.Instance.LastProcessedTickByPlayerID)
        {
            int peerID = kvp.Key;
            uint lastAckedTick = kvp.Value;

            if (!NetworkSession.Instance.PeerIDsToPeers.TryGetValue(peerID, out var peer) || peer == null)
                continue;

            WorldSnapshot snapshotToSend;

            if (snapshotDeltas.TryGetValue(lastAckedTick, out var deltaSnapshot))
            {
                snapshotToSend = deltaSnapshot;
            }
            else if (_snapshotHistory.TryGetValue((ushort)lastAckedTick, out var previousSnapshot))
            {
                snapshotToSend = newSnapshot.BuildDelta(previousSnapshot);
                snapshotDeltas[lastAckedTick] = snapshotToSend;
            }
            else
            {
                snapshotToSend = newSnapshot;
            }

            // Write message to calculate bytes
            var bytes = snapshotToSend.WriteMessage();

            // ---- Accumulate bytes sent ----
            _bytesSentThisPeriod += bytes.Length;


            NetworkSender.ToClient(peer, snapshotToSend);
        }
    }

    private void AddSnapshotToHistory(ushort tick, WorldSnapshot snapshot)
    {
        _snapshotHistory[tick] = snapshot;
        _snapshotQueue.Enqueue(tick);

        while (_snapshotQueue.Count > MaxSnapshotHistory)
        {
            var oldTick = _snapshotQueue.Dequeue();
            _snapshotHistory.Remove(oldTick);
        }
    }
}