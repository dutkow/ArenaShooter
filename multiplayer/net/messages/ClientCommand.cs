using Godot;
using System;

[Flags]
public enum InputCommand : byte
{
    NONE = 0,
    MOVE_FORWARD = 1 << 0,
    MOVE_BACK = 1 << 1,
    MOVE_LEFT = 1 << 2,
    MOVE_RIGHT = 1 << 3,
    JUMP = 1 << 4,
    FIRE_PRIMARY = 1 << 5
}

public class ClientCommand : Message
{
    public byte PlayerID;
    public uint TickNumber;
    public InputCommand Buttons;

    public float Yaw;
    public float Pitch;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerID);
        Add(TickNumber);
        Add(Buttons);
        Add(Yaw);
        Add(Pitch);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(PlayerID);
        Write(TickNumber);
        Write((byte)Buttons);
        Write(Yaw);
        Write(Pitch);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out PlayerID);
        Read(out TickNumber);

        byte buttonByte;
        Read(out buttonByte);
        Buttons = (InputCommand)buttonByte;

        Read(out Yaw);
        Read(out Pitch);
    }

    public static void Send(ClientCommand cmd)
    {
        cmd.MessageType = Msg.C2S_CLIENT_COMMAND;
        cmd.ENetFlags = ENetPacketFlags.UnreliableFragment;
        NetworkSender.ToServer(cmd);
    }
}