using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using static NetworkConstants;

public class ServerAdvertisement
{
    public ServerAdvertisement() { }

    public ServerAdvertisement(ServerParams serverParams)
    {
        Port = serverParams.Port;
        ServerName = serverParams.Name;
        Map = serverParams.Map;
        GameMode = serverParams.GameMode;
        MaxPlayers = serverParams.MaxPlayers;
        IsPasswordProtected = serverParams.Password != null && serverParams.Password.Length > 0;

        // Add local player by default
        ConnectedPlayers.Add(Settings.Instance.PlayerName);
    }

    // -----------------------
    // Serialized fields
    // -----------------------
    [JsonPropertyName(SERVER_KEY_IP_ADDRESS)]
    public string? IP { get; set; }

    [JsonPropertyName(SERVER_KEY_PORT)]
    public int Port { get; set; }

    [JsonPropertyName(SERVER_KEY_NAME)]
    public string ServerName { get; set; }

    [JsonPropertyName(SERVER_KEY_MAP)]
    public string Map { get; set; }

    [JsonPropertyName(SERVER_KEY_GAME_MODE)]
    public GameMode GameMode { get; set; }

    [JsonPropertyName(SERVER_KEY_MAX_PLAYERS)]
    public int MaxPlayers { get; set; }

    [JsonPropertyName(SERVER_KEY_IS_PASSWORD_PROTECTED)]
    public bool IsPasswordProtected { get; set; }

    /// <summary>
    /// Server-reported count. Used when deserializing from the server.
    /// When sending, ConnectedPlayers.Count is used instead.
    /// </summary>
    [JsonPropertyName(SERVER_KEY_NUM_CONNECTED_PLAYERS)]
    public int ConnectedPlayersCount
    {
        get => ConnectedPlayers.Count;                // when sending
        set => _serverConnectedPlayersCount = value;  // when receiving
    }

    [JsonIgnore]
    private int _serverConnectedPlayersCount;

    [JsonPropertyName(SERVER_KEY_CONNECTED_PLAYER_NAMES)]
    public List<string> ConnectedPlayers { get; set; } = new();

    // -----------------------
    // Methods to manage players
    // -----------------------
    public void OnPlayerJoined(string playerName)
    {
        if (!ConnectedPlayers.Contains(playerName))
            ConnectedPlayers.Add(playerName);
    }

    public void OnPlayerChangedName(string oldName, string newName)
    {
        ConnectedPlayers.Remove(oldName);
        ConnectedPlayers.Add(newName);
    }

    public void OnPlayerLeft(string playerName)
    {
        ConnectedPlayers.Remove(playerName);
    }
}
