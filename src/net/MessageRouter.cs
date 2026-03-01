using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Routes messages between server and client. Handlers register themselves based on role.
/// </summary>
public class MessageRouter
{
    public delegate void FromServerHandler(byte[] data);
    public delegate void FromClientHandler(ENetPacketPeer sender, byte[] data);


    private readonly List<MessageHandler> _handlers = new List<MessageHandler>();

    /// <summary>
    /// Add a handler to the router. Initialize later based on role.
    /// </summary>
    public void AddHandler(MessageHandler handler)
    {
        _handlers.Add(handler);
    }

    /// <summary>
    /// Initialize all handlers for the given role.
    /// </summary>
    public void Initialize(NetRole role)
    {

    }

    // ---------------- Dispatch ----------------
    public void ReadMessageFromServer(byte[] data)
    {
        var type = Message.GetType(data);
        _fromServerHandlers[(int)type]?.Invoke(data);
    }

    public void ReadMessageFromClient(ENetPacketPeer sender, byte[] data)
    {
        var type = Message.GetType(data);
        _fromClientHandlers[(int)type]?.Invoke(sender, data);
    }

    // ---------------- Registration ----------------
    public void RegisterFromServer(ServerMsg type, FromServerHandler handler)
    {
        _fromServerHandlers[(int)type] = handler;
    }

    public void RegisterFromClient(ClientMsg type, FromClientHandler handler)
    {
        _fromClientHandlers[(int)type] = handler;
    }
}