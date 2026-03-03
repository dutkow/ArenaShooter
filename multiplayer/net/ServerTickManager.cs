using Godot;
using System;

/// <summary>
/// Handles server-side ticking and snapshot sending for the match.
/// </summary>
public class ServerTickManager
{
    private double _accumulator = 0f;
    private const double TickInterval = 1f / 60f; // 60 Hz tick

    public void PhysicsTick(double delta)
    {
        _accumulator += delta;

        while (_accumulator >= TickInterval)
        {
            _accumulator -= TickInterval;
            TickServer();
        }
    }

    private void TickServer()
    {
        SendWorldSnapshot();
    }


    private void SendWorldSnapshot()
    {
        WorldSnapshot.Send();
    }
}