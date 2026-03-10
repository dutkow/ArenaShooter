using Godot;
using System;

public class ChatMessageRequest : Message
{
    public ChatChannel Channel;
    public string Text;
    public byte TargetPlayerID; // used only for private messages

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(Channel);
        Add(Text);
        Add(TargetPlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(Channel);
        Write(Text);
        Write(TargetPlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out Channel);
        Read(out Text);
        Read(out TargetPlayerID);
    }

    public static void Send(ENetPacketPeer client, ChatChannel channel, string text, byte targetPlayerID = 0)
    {
        var msg = new ChatMessage
        {
            MessageType = Msg.S2C_CHAT_MESSAGE,
            ENetFlags = ENetPacketFlags.Reliable,
            Channel = channel,
            Text = text,
            PlayerID = targetPlayerID,

        };
        NetworkSender.ToServer(msg);
    }
}