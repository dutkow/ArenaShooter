using System;

[Serializable]
public struct ServerInfo
{
    public string Name;       // server name
    public string HostIP;     // IP address
    public int Port;          // ENet game port
    public int Players;       // optional: current player count
    public int MaxPlayers;    // optional: max players

    public ServerInfo(string name, string ip, int port, int players = 0, int maxPlayers = 8)
    {
        Name = name;
        HostIP = ip;
        Port = port;
        Players = players;
        MaxPlayers = maxPlayers;
    }

    // Serialize to a simple string for UDP broadcast
    public override string ToString()
    {
        return $"{Name}|{HostIP}|{Port}|{Players}|{MaxPlayers}";
    }

    // Deserialize from string received over LAN
    public static ServerInfo FromString(string data)
    {
        string[] parts = data.Split('|');
        return new ServerInfo(
            parts[0],
            parts[1],
            int.Parse(parts[2]),
            int.Parse(parts[3]),
            int.Parse(parts[4])
        );
    }
}