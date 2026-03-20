using Godot;
using System;
using System.Security.Cryptography.X509Certificates;


/// <summary>
/// Sent from Server → Client when a chat message is sent
/// </summary>
/// 

public class ChatMessage : Message
{    
    public ChatMessageInfo Info;

    protected override int BufferSize()
    {
        base.BufferSize();
        AddEnum(Info.Channel);
        Add(Info.Text);
        Add(Info.PlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        WriteEnum(Info.Channel);
        Write(Info.Text);
        Write(Info.PlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        ReadEnum(out Info.Channel);
        Read(out Info.Text);
        Read(out Info.PlayerID);
    }

    public static void Send(ChatMessageInfo info)
    {
        var msg = new ChatMessage
        {
            MessageType = Msg.S2C_CHAT_MESSAGE,
            ENetFlags = ENetPacketFlags.Reliable,
            Info = info

        };

        // TODO: when team logic exists, make this not a broadcast and filter it accordingly by channel
        NetworkSender.Broadcast(msg);
    }
}