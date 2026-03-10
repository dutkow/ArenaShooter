using Godot;
using System;
using System.Collections.Generic;

public class MessageRouter
{
    public delegate void FromServerHandler(byte[] data);
    public delegate void FromClientHandler(ENetPacketPeer sender, byte[] data);

    private readonly Dictionary<Msg, FromServerHandler> _serverHandlers = new();
    private readonly Dictionary<Msg, FromClientHandler> _clientHandlers = new();



    public void Initialize(NetworkMode role)
    {
        _serverHandlers.Clear();
        _clientHandlers.Clear();

        if(role == NetworkMode.LISTEN_SERVER)
        {
            // Connection state
            RegisterFromClient(Msg.C2S_CONNECTION_REQUEST, ServerConnectionService.HandleConnectionRequest);
            RegisterFromClient(Msg.C2S_CLIENT_LOADED, ServerConnectionService.HandleClientLoaded);

            // Gameplay
            RegisterFromClient(Msg.C2S_CLIENT_COMMAND, ServerGameplayService.HandleClientCommand);

            // Chat
            RegisterFromClient(Msg.C2S_CHAT_MESSAGE_REQUEST, ChatManager.Instance.HandleChatMessageRequest);

        }
        else if (role == NetworkMode.CLIENT)
        {
            // Connection state
            RegisterFromServer(Msg.S2C_CONNECTION_ACCEPTED, ClientConnectionService.HandleConnectionAccepted);
            RegisterFromServer(Msg.S2C_CONNECTION_DENIED, ClientConnectionService.HandleConnectionDenied);
            RegisterFromServer(Msg.S2C_INITIAL_MATCH_STATE, ClientConnectionService.HandleInitialMatchState);
            RegisterFromServer(Msg.S2C_PLAYER_JOINED, ClientConnectionService.HandlePlayerJoined);

            // Gameplay
            RegisterFromServer(Msg.S2C_WORLD_SNAPSHOT, ClientGameplayService.HandleWorldSnapshot);

            RegisterFromServer(Msg.S2C_PLAYER_SPAWNED, ClientGameplayService.HandlePlayerSpawned);
            RegisterFromServer(Msg.S2C_PROJECTILE_SPAWNED, ClientGameplayService.HandleProjectileSpawned);
            RegisterFromServer(Msg.S2C_HEALTH_CHANGED, ClientGameplayService.HandleHealthChanged);
            RegisterFromServer(Msg.S2C_PLAYER_DIED, ClientGameplayService.HandlePlayerDied);

            // Chat
            RegisterFromServer(Msg.S2C_CHAT_MESSAGE, ChatManager.Instance.HandleChatMessage);
        }
    }

    public void RegisterFromServer(Msg type, FromServerHandler handler)
    {
        _serverHandlers[type] = handler;
    }

    public void RegisterFromClient(Msg type, FromClientHandler handler)
    {
        _clientHandlers[type] = handler;
    }

    public void RouteServerMessage(byte[] data)
    {
        var type = Message.GetType(data);
        if (_serverHandlers.TryGetValue(type, out var handler))
        {
            handler?.Invoke(data);
        }
        else
        {
            GD.Print($"No server handler for message {type}");
        }
    }

    public void RouteClientMessage(ENetPacketPeer sender, byte[] data)
    {
        var type = Message.GetType(data);
        if (_clientHandlers.TryGetValue(type, out var handler))
        {
            handler?.Invoke(sender, data);
        }
        else
        {
            GD.Print($"No client handler for message {type}");
        }
    }
}