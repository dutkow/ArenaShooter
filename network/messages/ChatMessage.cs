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
    public byte PlayerID; // disregarded for system messages

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(Channel);
        Add(Text);
        Add(PlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(Channel);
        Write(Text);
        Write(PlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out Channel);
        Read(out Text);
        Read(out PlayerID);
    }

    public static void Send(ENetPacketPeer client, ChatChannel channel, string text, byte playerID = 0)
    {
        var msg = new ChatMessage
        {
            MessageType = Msg.S2C_CHAT_MESSAGE,
            ENetFlags = ENetPacketFlags.Reliable,
            Channel = channel,
            Text = text,
            PlayerID = playerID,

        };
        NetworkSender.ToClient(client, msg);
    }
}