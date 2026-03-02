using Godot;
using System;
using System.Collections.Generic;

public class MessageRouter
{
    public delegate void FromServerHandler(byte[] data);
    public delegate void FromClientHandler(ENetPacketPeer sender, byte[] data);

    private readonly Dictionary<Msg, FromServerHandler> _serverHandlers = new();
    private readonly Dictionary<Msg, FromClientHandler> _clientHandlers = new();



    public void Initialize(NetRole role)
    {
        _serverHandlers.Clear();
        _clientHandlers.Clear();

        if(role == NetRole.SERVER)
        {
            RegisterFromClient(Msg.C2S_CONNECTION_REQUEST, ServerConnectionService.HandleConnectionRequest);
            RegisterFromClient(Msg.C2S_CLIENT_LOADED, ServerConnectionService.HandleClientLoaded);
        }
        else if (role == NetRole.CLIENT)
        {
            RegisterFromServer(Msg.S2C_CONNECTION_ACCEPTED, ClientConnectionService.HandleConnectionAccepted);
            RegisterFromServer(Msg.S2C_CONNECTION_DENIED, ClientConnectionService.HandleConnectionDenied);
            RegisterFromServer(Msg.S2C_INITIAL_MATCH_STATE, ClientConnectionService.HandleInitialMatchState);
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
            GD.Print($"Routing server message: {type}");
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
            GD.Print($"Routing client message: {type}");
            handler?.Invoke(sender, data);
        }
        else
        {
            GD.Print($"No client handler for message {type}");
        }
    }
}