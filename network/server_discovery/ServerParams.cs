using Godot;
using System;
using static NetworkConstants;

public class ServerParams
{
    public int Port { get; set; } = DEFAULT_PORT;
    public string Name { get; set; }
    public string Map { get; set; }
    public GameMode GameMode { get; set; }
    public int MaxPlayers { get; set; }

    public string Password { get; set; }
}
