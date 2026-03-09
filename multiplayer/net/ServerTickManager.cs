using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles server-side ticking and snapshot sending for the match.
/// Manages its own snapshot history dictionary and queue.
/// </summary>
public partial class ServerTickManager : Node
{
    private double _accumulator = 0f;

    private const int MaxSnapshotHistory = 250;

    // History of snapshots, keyed by server tick
    private readonly Dictionary<ushort, WorldSnapshot> _snapshotHistory = new();
    private readonly Queue<ushort> _snapshotQueue = new();

    // ---- For traffic tracking ----
    private int _bytesSentThisPeriod = 0;

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        _accumulator += delta;

        while (_accumulator >= NetworkConstants.SERVER_TICK_INTERVAL)
        {
            _accumulator -= NetworkConstants.SERVER_TICK_INTERVAL;

            MatchState.Instance.Tick();

            if (MatchState.Instance.CurrentTick % NetworkConstants.SERVER_TICK_RATE == 0)
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
        AddSnapshotToHistory(MatchState.Instance.CurrentTick, newSnapshot);
    }

    private void SendWorldSnapshotDeltas(WorldSnapshot newSnapshot)
    {
        var snapshotDeltas = new Dictionary<uint, WorldSnapshot>();

        foreach (var kvp in ServerGame.Instance.LastProcessedServerTicksByPlayerID)
        {
            byte playerID = kvp.Key;
            ushort lastReceivedServerTick = kvp.Value;
            ushort lastProcessedClientTick = ServerGame.Instance.LastProcessedServerTicksByPlayerID[playerID];

            if (!NetworkSession.Instance.PlayerIDsToPeers.TryGetValue(playerID, out var peer))
            {
                GD.Print($"not found. Peer id: {playerID}. Peer: {peer}");
                continue;
            }

            if (snapshotDeltas.TryGetValue(lastReceivedServerTick, out var deltaSnapshot))
            {
                newSnapshot = deltaSnapshot;
            }
            else if (_snapshotHistory.TryGetValue((ushort)lastReceivedServerTick, out var previousSnapshot))
            {
                newSnapshot = newSnapshot.BuildDelta(previousSnapshot);
                snapshotDeltas[lastReceivedServerTick] = newSnapshot;
            }

            var bytes = newSnapshot.WriteMessage();
            _bytesSentThisPeriod += bytes.Length;

            newSnapshot.LastProcessedClientTick = lastProcessedClientTick;

            NetworkSender.ToClient(peer, newSnapshot);
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