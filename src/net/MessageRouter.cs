using Godot;
using System;
using System.Collections.Generic;

public class MessageRouter
{
    public List<MessageHandler> _messageHandlers = new();

    public delegate void FromServerHandler(byte[] data);
    public delegate void FromClientHandler(ENetPacketPeer sender, byte[] data);

    private readonly Dictionary<Msg, FromServerHandler> _serverHandlers = new();
    private readonly Dictionary<Msg, FromClientHandler> _clientHandlers = new();

    public MessageRouter()
    {
        ConnectionMessageHandler connectionMessageHandler = new();
        _messageHandlers.Add(connectionMessageHandler);


    }

    public void OnRoleChanged(NetRole role)
    {
        _serverHandlers.Clear();
        _clientHandlers.Clear();

        if(role == NetRole.LOCAL)
        {
            return;
        }

        foreach(var handler in _messageHandlers)
        {
            handler.Initialize(this, role);
        }
    }

    public void RegisterFromServer(Msg type, FromServerHandler handler)
    {
        _serverHandlers[type] = handler;
        GD.Print($"Registered server handler for {type}");
    }

    public void RegisterFromClient(Msg type, FromClientHandler handler)
    {
        _clientHandlers[type] = handler;
        GD.Print($"Registered client handler for {type}");
    }

    public void ReadMessageFromServer(byte[] data)
    {
        var type = Message.GetType(data);
        if (_serverHandlers.TryGetValue(type, out var handler))
        {
            GD.Print($"Dispatching server message {type}");
            handler?.Invoke(data);
        }
        else
        {
            GD.Print($"No server handler for message {type}");
        }
    }

    public void ReadMessageFromClient(ENetPacketPeer sender, byte[] data)
    {
        var type = Message.GetType(data);
        if (_clientHandlers.TryGetValue(type, out var handler))
        {
            GD.Print($"Dispatching client message {type}");
            handler?.Invoke(sender, data);
        }
        else
        {
            GD.Print($"No client handler for message {type}");
        }
    }
}