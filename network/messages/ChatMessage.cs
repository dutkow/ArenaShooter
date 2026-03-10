using Godot;
using System;
using System.Security.Cryptography.X509Certificates;


/// <summary>
/// Sent from Server → Client when a chat message is sent
/// </summary>
/// 

public class ChatMessage : Message
{
    public ChatChannel Channel;
    public string Text;
    public byte SenderPlayerID; // disregarded for system messages

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(Channel);
        Add(Text);
        Add(SenderPlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(Channel);
        Write(Text);
        Write(SenderPlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out Channel);
        Read(out Text);
        Read(out SenderPlayerID);
    }

    public ChatMessageInfo ToInfo()
    {
        return new ChatMessageInfo(Channel, Text, SenderPlayerID);
    }

    public static void Send(ChatMessageInfo info)
    {
        var msg = new ChatMessage
        {
            MessageType = Msg.S2C_CHAT_MESSAGE,
            ENetFlags = ENetPacketFlags.Reliable,
            Channel = info.Channel,
            Text = info.Text,
            SenderPlayerID = info.PlayerID,

        };

        // TODO: when team logic exists, make this not a broadcast and filter it accordingly by channel
        NetworkSender.Broadcast(msg);
    }


}