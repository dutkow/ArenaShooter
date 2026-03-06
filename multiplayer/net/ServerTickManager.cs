using Godot;
using System;

/// <summary>
/// Handles server-side ticking and snapshot sending for the match.
/// </summary>
public class ServerTickManager
{
    private double _accumulator = 0f;
    public ushort ServerTick { get; private set; }

    public void PhysicsTick(double delta)
    {
        _accumulator += delta;

        while (_accumulator >= NetworkConstants.SERVER_TICK_INTERVAL)
        {
            _accumulator -= NetworkConstants.SERVER_TICK_INTERVAL;
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