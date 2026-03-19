using Godot;
using System;


public class PlayerNameChanged: Message
{
    public byte PlayerID;
    public string Name;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(PlayerID);
        Add(Name);

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(PlayerID);
        Write(Name);

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out PlayerID);
        Read(out Name);
    }

    public static void Send(byte playerID, string name)
    {
        var msg = new PlayerNameChanged
        {
            MessageType = Msg.S2C_PLAYER_NAME_CHANGED,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerID = playerID,
            Name = name
        };

        NetworkSender.Broadcast(msg);
    }
}