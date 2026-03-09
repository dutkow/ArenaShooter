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

    // History of snapshots, keyed by server tick


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

            /*
            if (MatchState.Instance.CurrentTick % NetworkConstants.SERVER_TICK_RATE == 0)
            {
                float mbps = (_bytesSentThisPeriod * 8f) / 1_000_000f;

                GD.Print($"Traffic: ~{mbps:F4} Mbps, {_bytesSentThisPeriod} bytes/sec (~{_bytesSentThisPeriod / 64.0f} bytes/tick)");

                _bytesSentThisPeriod = 0;
            }*/
        }
    }

    private void TickServer()
    {
        /*
        var newSnapshot = WorldSnapshot.Build();
        SendWorldSnapshotDeltas(newSnapshot);
        AddSnapshotToHistory(MatchState.Instance.CurrentTick, newSnapshot);*/
    }

}