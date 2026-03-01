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
    public void OnRoleChanged(NetRole role)
    {

    }

    public void ReadMessageFromServer(byte[] data)
    {
    }

    public void ReadMessageFromClient(ENetPacketPeer sender, byte[] data)
    {
    }

    public void RegisterFromServer(Msg type, FromServerHandler handler)
    {
    }

    public void RegisterFromClient(Msg type, FromClientHandler handler)
    {
    }
}