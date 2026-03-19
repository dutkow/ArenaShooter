using Godot;
using System;

public enum ServerNotificationType : byte
{
    DISCONNECTION_SERVER_SHUTDOWN,
}

public class ServerNotification : Message
{
    public ServerNotificationType Type;

    protected override int BufferSize()
    {
        base.BufferSize();
        AddEnum(Type);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        WriteEnum(Type);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        ReadEnum(out Type);
    }

    public static void Send(ENetPacketPeer peer, ServerNotificationType type)
    {
        ServerNotification serverNotification = new();
        serverNotification.MessageType = Msg.S2C_SERVER_NOTIFICATION;
        serverNotification.Type = type;
        NetworkSender.ToClient(peer, serverNotification);
    }

    public static void Broadcast(ServerNotificationType type)
    {
        ServerNotification serverNotification = new();
        serverNotification.MessageType = Msg.S2C_SERVER_NOTIFICATION;
        serverNotification.Type = type;
        NetworkSender.Broadcast(serverNotification);
    }
}