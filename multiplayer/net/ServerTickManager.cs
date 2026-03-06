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

    public void PhysicsTick(double delta)
    {
        _accumulator += delta;

        while (_accumulator >= NetworkConstants.SERVER_TICK_INTERVAL)
        {
            _accumulator -= NetworkConstants.SERVER_TICK_INTERVAL;
            TickServer();
            ServerTick++;
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
        // Cache calculated deltas for this send
        var snapshotDeltas = new Dictionary<uint, WorldSnapshot>();

        foreach (var kvp in MatchState.Instance.LastAckedTickByPeerID)
        {
            int peerID = kvp.Key;
            uint lastAckedTick = kvp.Value;

            if (!NetworkSession.Instance.PeerIDsToPeers.TryGetValue(peerID, out var peer) || peer == null)
            {
                continue;
            }

            // Use cached delta if already calculated
            if (snapshotDeltas.TryGetValue(lastAckedTick, out var deltaSnapshot))
            {
                NetworkSender.ToClient(peer, deltaSnapshot);
            }
            // Build delta from previous snapshot if it exists
            else if (_snapshotHistory.TryGetValue((ushort)lastAckedTick, out var previousSnapshot))
            {
                var snapDelta = newSnapshot.BuildDelta(previousSnapshot);
                snapshotDeltas[lastAckedTick] = snapDelta;
                NetworkSender.ToClient(peer, snapDelta);
            }
            // Otherwise, send full snapshot
            else
            {
                NetworkSender.ToClient(peer, newSnapshot);
            }
        }
    }

    private void AddSnapshotToHistory(ushort tick, WorldSnapshot snapshot)
    {
        _snapshotHistory[tick] = snapshot;
        _snapshotQueue.Enqueue(tick);

        // Trim old snapshots
        while (_snapshotQueue.Count > MaxSnapshotHistory)
        {
            var oldTick = _snapshotQueue.Dequeue();
            _snapshotHistory.Remove(oldTick);
        }
    }
}