using Godot;
using System;

public partial class Game : Node
{
    public static Game Instance { get; private set; }

    public ServerGame _serverGame;
    public ClientGame _clientGame;

    public float ServerTickInterval = 1.0f / 100.0f;
    public NetworkMode NetworkMode { get; private set; }

    public bool IsAuthority => NetworkMode != NetworkMode.CLIENT;
    public bool IsClient => NetworkMode != NetworkMode.DEDICATED_SERVER;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        ChatManager.Create();

    }


    public virtual void InitMessageHandlers()
    {

    }
}
