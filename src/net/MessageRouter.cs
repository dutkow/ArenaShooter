using Godot;
using System;
using System.Collections.Generic;

public class MessageRouter
{
    private readonly FromServerHandler[] _fromServerHandlers;
    private readonly FromClientHandler[] _fromClientHandlers;

    public delegate void FromServerHandler(byte[] data);
    public delegate void FromClientHandler(ENetPacketPeer sender, byte[] data);

    private readonly List<MessageHandler> _handlers = new();

    public MessageRouter()
    {
        int enumSize = Enum.GetValues(typeof(Msg)).Length;
        _fromServerHandlers = new FromServerHandler[enumSize];
        _fromClientHandlers = new FromClientHandler[enumSize];
    }

    public void RegisterModule(MessageHandler handler)
    {
        _handlers.Add(handler);
        handler.Initialize();
    }

    // ---------------- Dispatch ----------------
    public void ReadMessageFromServer(byte[] data)
    {
        var packetType = Message.GetType(data);
        GD.Print($"Client received packet of type: {packetType}");
        _fromServerHandlers[(int)packetType]?.Invoke(data);
    }

    public void ReadMessageFromClient(ENetPacketPeer sender, byte[] data)
    {
        var packetType = Message.GetType(data);
        GD.Print($"Server received packet of type: {packetType}");
        _fromClientHandlers[(int)packetType]?.Invoke(sender, data);
    }

    // ---------------- Registration ----------------
    public void RegisterFromServer(Msg type, FromServerHandler handler)
    {
        _fromServerHandlers[(int)type] = handler;
    }

    public void RegisterFromClient(Msg type, FromClientHandler handler)
    {
        _fromClientHandlers[(int)type] = handler;
    }
}
