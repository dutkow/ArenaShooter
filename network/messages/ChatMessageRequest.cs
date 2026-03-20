using Godot;
using System;
using static System.Net.Mime.MediaTypeNames;

public class ChatMessageRequest : Message
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
        ChatMessage chatMessage = new();
        chatMessage.Info = info;
        NetworkSender.ToServer(chatMessage);
    }
}