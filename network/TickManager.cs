using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles server-side ticking and snapshot sending for the match.
/// Manages its own snapshot history dictionary and queue.
/// </summary>
public class TickManager : ITickable
{
    public static TickManager Instance { get; private set; }
    private double _accumulator = 0f;

    // History of snapshots, keyed by server tick

    public int ServerTickRate = 64;
    public double ServerTickInterval = 0.125;

    // ---- For traffic tracking ----
    private int _bytesSentThisPeriod = 0;

    public static TickManager Create()
    {
        GD.Print($"creating tick manager!");
        Instance = new TickManager();

        if(NetworkServer.Instance != null)
        {
            Instance.SetServerTickRate(NetworkServer.Instance.ServerInfo.TickRate);
        }
        else
        {
            //Instance.SetServerTickRate(Instance.ServerTickRate);
        }

        return Instance;
    }


    public void SetServerTickRate(int serverTickRate)
    {
        ServerTickInterval = 1.0 / serverTickRate;
    }

    public void Tick(double delta)
    {
        _accumulator += delta;

        while (_accumulator >= ServerTickInterval)
        {
            _accumulator -= ServerTickInterval;            
            MatchState.Instance.Tick(ServerTickInterval);
        }

    }

}