using Godot;
using System;


/// <summary>
/// Sent from Server → Client when a chat message is sent
/// </summary>
/// 

public class PlayerNameChangeRequest : Message
{
    public string Name;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(Name);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(Name);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out Name);
    }

    public static void Send(string name)
    {
        var msg = new PlayerNameChangeRequest
        {
            MessageType = Msg.C2S_CHANGE_PLAYER_NAME,
            ENetFlags = ENetPacketFlags.Reliable,
            Name = name
        };

        NetworkSender.ToServer(msg);
    }
}