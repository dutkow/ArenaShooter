using Godot;
using System;
using static NetworkConstants;

public class ServerParams
{
    public int Port { get; set; } = DEFAULT_PORT;
    public string Name { get; set; }
    public string MapID { get; set; }
    public string GameModeID { get; set; }
    public int MaxPlayers { get; set; }

    public string Password { get; set; }

    public int TickRate { get; set; }
}
