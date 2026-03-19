using Godot;
using System;
using System.Net;
using System.Net.Sockets;

public static class NetworkConstants
{
    // Match settings
    public const int MAX_PLAYERS = 16;


    // Server discovery


    // -------------------
    // Server Browser
    // -------------------

    public const int DEFAULT_PORT = 7777;

    // Lobby keys expected by the server browser
    public const string SERVER_KEY_IP_ADDRESS = "ip";
    public const string SERVER_KEY_PORT = "port";
    public const string SERVER_KEY_NAME = "name";
    public const string SERVER_KEY_GAME_MODE_ID = "game_mode";
    public const string SERVER_KEY_MAP_ID = "map";
    public const string SERVER_KEY_MAX_PLAYERS = "max_players";
    public const string SERVER_KEY_NUM_CONNECTED_PLAYERS = "num_connected_players";
    public const string SERVER_KEY_CONNECTED_PLAYER_NAMES = "connected_player_names";
    public const string SERVER_KEY_IS_PASSWORD_PROTECTED = "is_password_protected";
    public const string SERVER_KEY_TICK_RATE = "tick_rate";


    // Server browser addresses
    public const string REGISTER_SERVER_URL_ADDRESS = "https://vincegamedev.pythonanywhere.com/register_lobby";
    public const string UNREGISTER_SERVER_URL_ADDRESS = "https://vincegamedev.pythonanywhere.com/unregister_lobby";
    public const string SERVER_BROWSER_ADDRESS = "https://vincegamedev.pythonanywhere.com/lobbies";

    public const float INTERNET_SERVER_BROADCAST_INTERVAL = 2.0f;
    public const float LAN_SERVER_BROADCAST_INTERVAL = 1.0f;

    public const int MAX_SERVER_TICK_RATE = 256;

    public static string GetLocalIP()
    {
        string localIP = "127.0.0.1"; // fallback
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530); // Google DNS, won't actually send
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
        }
        catch { }
        return localIP;
    }

    /// </summary>
    public static string GetBroadcastIP()
    {
        try
        {
            var parts = GetLocalIP().Split('.');
            if (parts.Length != 4) return "255.255.255.255"; // fallback

            return $"{parts[0]}.{parts[1]}.{parts[2]}.255";
        }
        catch
        {
            return "255.255.255.255";
        }
    }
}
