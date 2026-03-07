using Godot;
using System;

public static class NetworkConstants
{
    // Match settings
    public const int MAX_PLAYERS = 16;

    // Server tick
    public const int SERVER_TICK_RATE = 100;
    public const float SERVER_TICK_INTERVAL = 1.0f / SERVER_TICK_RATE;

    // Server discovery
    public const int DEFAULT_PORT = 7777;
    public const float LAN_BROADCAST_INTERVAL = 1.0f;

    // Gameplay

}
