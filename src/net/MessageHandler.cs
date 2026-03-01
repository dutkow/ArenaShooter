using Godot;
using System;

public abstract class MessageHandler
{
    protected MessageRouter Router;

    public MessageHandler(MessageRouter router)
    {
        Router = router;
    }

    public abstract void Initialize();
}