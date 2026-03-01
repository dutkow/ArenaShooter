using Godot;
using System;

public class MessageHandler
{
    public Msg Type;
    public MessageRouter.FromServerHandler? ServerHandler;
    public MessageRouter.FromClientHandler? ClientHandler;


    /// <summary>
    /// Called when the role changes, could perform registration logic if needed.
    /// </summary>
    public virtual void Initialize(MessageRouter router, NetRole role)
    {
    }
}