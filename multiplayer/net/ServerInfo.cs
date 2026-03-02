using Godot;
using System;
using System.Linq;
using System.Reflection;

[Serializable]
public class ServerInfo
{
    public string ServerID;
    public string Name;
    public string HostIP;
    public int Port;
    public int Players;
    public int MaxPlayers;
    public string MapID;
    public int PingMs;
    public bool RequiresPassword;

    [NonSerialized]
    private string _password = "";

    // Constructor
    public ServerInfo(
        string name = "New Server",
        string ip = "127.0.0.1",
        int port = 42069,
        int players = 0,
        int maxPlayers = 8,
        string? serverID = null,
        string mapID = "test_map_1",
        string password = ""
    )
    {
        ServerID = serverID ?? Guid.NewGuid().ToString();
        Name = name;
        HostIP = ip;
        Port = port;
        Players = players;
        MaxPlayers = maxPlayers;
        MapID = mapID;

        _password = password;
        RequiresPassword = !string.IsNullOrEmpty(password);
    }

    public string GetPassword()
    {
        return _password;
    }

    public void SetPassword(string password)
    {
        _password = password;
        RequiresPassword = !string.IsNullOrEmpty(password);
    }

    public override string ToString()
    {
        var fields = this.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance);
        return string.Join("|", fields.Select(f => f.GetValue(this)?.ToString() ?? ""));
    }

    public static ServerInfo FromString(string data)
    {
        string[] parts = data.Split('|');
        var fields = typeof(ServerInfo).GetFields(BindingFlags.Public | BindingFlags.Instance);

        var server = new ServerInfo();

        for (int i = 0; i < fields.Length && i < parts.Length; i++)
        {
            var field = fields[i];
            object? value = null;

            if (field.FieldType == typeof(int))
            {
                value = int.Parse(parts[i]);
            }
            else if (field.FieldType == typeof(bool))
            {
                value = bool.Parse(parts[i]);
            }
            else
            {
                value = parts[i];
            }

            field.SetValue(server, value);
        }

        return server;
    }

    public override bool Equals(object? obj)
    {
        return obj is ServerInfo s && s.ServerID == ServerID;
    }

    public override int GetHashCode() => ServerID.GetHashCode();

    public void PrintInfo()
    {
        GD.Print($"ServerID: {ServerID}, Name: {Name}, HostIP: {HostIP}, Port: {Port}, Players: {Players}/{MaxPlayers}, MapID: {MapID}, RequiresPassword: {RequiresPassword}");
    }
}