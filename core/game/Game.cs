using Godot;
using System;

public class Game
{
    public static Game Instance { get; private set; }

    public ServerGame _serverGame;
    public ClientGame _clientGame;

    public NetworkMode NetworkMode { get; private set; }

    public void Initialize(NetworkMode mode, byte localPlayerID = 0)
    {
        NetworkMode = mode;

        _serverGame = null;
        _clientGame = null;

        switch(mode)
        {
            case NetworkMode.DEDICATED_SERVER:

                break;

            case NetworkMode.LISTEN_SERVER:
                InitializeServer();
                InitializeClient(localPlayerID);
                break;

            case NetworkMode.CLIENT:
                InitializeClient(localPlayerID);
                break;
            case NetworkMode.OFFLINE:

                break;
        }
    }

    public void InitializeServer()
    {
        _serverGame = new();
        _serverGame.Initialize();
    }

    public void InitializeClient(byte localPlayerID)
    {
        _clientGame = new();
        _clientGame.Initialize(localPlayerID);

    }


}
