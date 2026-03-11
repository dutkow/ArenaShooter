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
        AddEnum(Channel);
        Add(Text);
        Add(SenderPlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        WriteEnum(Channel);
        Write(Text);
        Write(SenderPlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        ReadEnum(out Channel);
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

    protected void ReadEnum<T>(out T value) where T : Enum
    {
        Type underlying = Enum.GetUnderlyingType(typeof(T));

        if (underlying == typeof(byte))
        {
            Read(out byte raw);
            value = (T)(object)raw;
        }
        else if (underlying == typeof(ushort))
        {
            Read(out ushort raw);
            value = (T)(object)raw;
        }
        else if (underlying == typeof(int))
        {
            Read(out int raw);
            value = (T)(object)raw;
        }
        else if (underlying == typeof(uint))
        {
            Read(out uint raw);
            value = (T)(object)raw;
        }
        else if (underlying == typeof(long))
        {
            Read(out long raw);
            value = (T)(object)raw;
        }
        else if (underlying == typeof(ulong))
        {
            Read(out ulong raw);
            value = (T)(object)raw;
        }
        else
        {
            throw new InvalidOperationException($"Unsupported enum underlying type: {underlying}");
        }
    }

    protected void ReadEnumArray<T>(out T[] arr) where T : Enum
    {
        Read(out byte count);
        arr = new T[count];
        for (int i = 0; i < count; i++)
        {
            ReadEnum(out arr[i]);
        }
    }
}