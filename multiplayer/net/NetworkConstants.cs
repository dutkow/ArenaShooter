using Godot;
using System;

public static class NetworkConstants
{
    // Match settings
    public const int MAX_PLAYERS = 16;

    // Server tick
    public const int SERVER_TICK_RATE = 20;
    public const double SERVER_TICK_INTERVAL = 1.0 / SERVER_TICK_RATE;

    // Server discovery
    public const int DEFAULT_PORT = 7777;
    public const float LAN_BROADCAST_INTERVAL = 1.0f;

    // Gameplay

}
