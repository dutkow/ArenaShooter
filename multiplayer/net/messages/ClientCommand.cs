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

public struct TickCommand
{
    public ushort TickNumber;      // client tick number
    public InputCommand InputButtons;
    public float Yaw;
    public float Pitch;
}

public class ClientCommand : Message
{
    // Array of tick commands we want to send in one packet
    public TickCommand[] Commands;

    // The last server tick the client has received & applied
    public ushort LastAppliedServerTick;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(LastAppliedServerTick);        // send the server tick first
        Add((byte)Commands.Length);        // then send command count
        foreach (var cmd in Commands)
        {
            Add(cmd.TickNumber);
            Add((byte)cmd.InputButtons);
            Add(cmd.Yaw);
            Add(cmd.Pitch);
        }
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(LastAppliedServerTick);
        Write((byte)Commands.Length);
        foreach (var cmd in Commands)
        {
            Write(cmd.TickNumber);
            Write((byte)cmd.InputButtons);
            Write(cmd.Yaw);
            Write(cmd.Pitch);
        }
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out LastAppliedServerTick);   // read server tick first

        byte count;
        Read(out count);
        Commands = new TickCommand[count];
        for (int i = 0; i < count; i++)
        {
            TickCommand cmd = new TickCommand();
            Read(out cmd.TickNumber);

            byte buttons;
            Read(out buttons);
            cmd.InputButtons = (InputCommand)buttons;

            Read(out cmd.Yaw);
            Read(out cmd.Pitch);

            Commands[i] = cmd;
        }
    }

    public static void Send(TickCommand[] commands, ushort lastAppliedServerTick)
    {
        var msg = new ClientCommand
        {
            MessageType = Msg.C2S_CLIENT_COMMAND,
            ENetFlags = ENetPacketFlags.UnreliableFragment,
            Commands = commands,
            LastAppliedServerTick = lastAppliedServerTick
        };

        NetworkSender.ToServer(msg);
    }
}