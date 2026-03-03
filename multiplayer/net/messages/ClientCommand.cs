using Godot;
using System;

/// <summary>
/// Sent from Client → Server each tick to tell the server what inputs were pressed.
/// Includes movement buttons and mouse look changes.
/// </summary>
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
    public float YawDelta;
    public float PitchDelta;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerID);
        Add(TickNumber);
        Add(Buttons);
        Add(YawDelta);
        Add(PitchDelta);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(PlayerID);
        Write(TickNumber);
        Write((byte)Buttons);
        Write(YawDelta);
        Write(PitchDelta);
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
        Read(out YawDelta);
        Read(out PitchDelta);
    }

    public static void Send(ClientCommand cmd, ENetPacketPeer serverPeer)
    {
        cmd.MessageType = Msg.C2S_CLIENT_COMMAND;
        cmd.ENetFlags = ENetPacketFlags.Reliable;
        NetworkSender.ToServer(cmd);
    }
}