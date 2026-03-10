using Godot;
using System;
using System.Net;
using System.Net.Sockets;

public static class NetworkConstants
{
    // Match settings
    public const int MAX_PLAYERS = 16;

    // Server tick
    public const int SERVER_TICK_RATE = 100;
    public const float SERVER_TICK_INTERVAL = 1.0f / SERVER_TICK_RATE;

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

    // Server browser addresses
    public const string REGISTER_SERVER_URL_ADDRESS = "https://vincegamedev.pythonanywhere.com/register_lobby";
    public const string UNREGISTER_SERVER_URL_ADDRESS = "https://vincegamedev.pythonanywhere.com/unregister_lobby";
    public const string SERVER_BROWSER_ADDRESS = "https://vincegamedev.pythonanywhere.com/lobbies";

    public const float INTERNET_SERVER_BROADCAST_INTERVAL = 2.0f;
    public const float LAN_SERVER_BROADCAST_INTERVAL = 1.0f;


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
}
