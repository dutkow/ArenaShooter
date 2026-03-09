using Godot;
using System;

public class ServerGame
{
    public static ServerGame Instance { get; private set; }

    public bool IsListenServer => Game.Instance.NetworkMode == NetworkMode.LISTEN_SERVER;

    public void Initialize()
    {
        Instance = this;
    }


}
