using Godot;
using System;


public class TickRateChanged : Message
{
    public ushort TickRate;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(TickRate);

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(TickRate);

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out TickRate);
    }

    public static void Send(ushort tickRate)
    {
        var msg = new TickRateChanged
        {
            MessageType = Msg.S2C_PLAYER_NAME_CHANGED,
            ENetFlags = ENetPacketFlags.Reliable,
            TickRate = tickRate
        };

        NetworkSender.Broadcast(msg);
    }
}