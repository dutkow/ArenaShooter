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

public struct ClientInputCommand
{
    public ushort ClientTick;

    public InputCommand Input;
    public float Yaw;
    public float Pitch;

    public Vector3 LaunchVelocity;
}

public class ClientCommand : Message
{
    // Array of tick commands we want to send in one packet
    public ClientInputCommand[] Commands;

    // The last server tick the client has received & applied
    public ushort ClientTick;
    public ushort LastReceivedServerTick;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(ClientTick);
        Add(LastReceivedServerTick);
        Add((byte)Commands.Length);
        foreach (var cmd in Commands)
        {
            Add(cmd.ClientTick);
            Add((byte)cmd.Input);
            Add(cmd.Yaw);
            Add(cmd.Pitch);
        }
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(ClientTick);
        Write(LastReceivedServerTick);
        Write((byte)Commands.Length);
        foreach (var cmd in Commands)
        {
            Write(cmd.ClientTick);
            Write((byte)cmd.Input);
            Write(cmd.Yaw);
            Write(cmd.Pitch);
        }
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out ClientTick);
        Read(out LastReceivedServerTick);
        byte count;
        Read(out count);
        Commands = new ClientInputCommand[count];
        for (int i = 0; i < count; i++)
        {
            ClientInputCommand cmd = new ClientInputCommand();
            Read(out cmd.ClientTick);

            byte buttons;
            Read(out buttons);
            cmd.Input = (InputCommand)buttons;

            Read(out cmd.Yaw);
            Read(out cmd.Pitch);

            Commands[i] = cmd;
        }
    }

    public static void Send(ClientInputCommand[] commands)
    {
        var msg = new ClientCommand
        {
            MessageType = Msg.C2S_CLIENT_COMMAND,
            ENetFlags = ENetPacketFlags.Unsequenced,
            Commands = commands,
            ClientTick = MatchState.Instance.CurrentTick,
            LastReceivedServerTick = ClientGame.Instance.LastServerTickProcessedByClient
        };
        NetworkSender.ToServer(msg);
    }
}