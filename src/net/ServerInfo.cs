using System;

[Serializable]
public struct ServerInfo
{
    public string ServerID;
    public string Name;
    public string HostIP;
    public int Port;
    public int Players;
    public int MaxPlayers;

    public ServerInfo(string name, string ip, int port, int players = 0, int maxPlayers = 8, string? serverID = null)
    {
        ServerID = serverID ?? Guid.NewGuid().ToString();
        Name = name;
        HostIP = ip;
        Port = port;
        Players = players;
        MaxPlayers = maxPlayers;
    }

    public override string ToString()
    {
        return $"{ServerID}|{Name}|{HostIP}|{Port}|{Players}|{MaxPlayers}";
    }

    public static ServerInfo FromString(string data)
    {
        string[] parts = data.Split('|');
        return new ServerInfo(
            parts[1],
            parts[2],
            int.Parse(parts[3]),
            int.Parse(parts[4]),
            int.Parse(parts[5]),
            parts[0]
        );
    }

    public override bool Equals(object? obj)
    {
        return obj is ServerInfo s && s.ServerID == ServerID;
    }

    public override int GetHashCode() => ServerID.GetHashCode();
}