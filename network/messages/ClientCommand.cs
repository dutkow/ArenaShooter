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
    public ushort LastServerTickProcessedByClient;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(ClientTick);
        Add(LastServerTickProcessedByClient);
        Add((byte)Commands.Length);
        foreach (var cmd in Commands)
        {
            Add(cmd.ClientTick);
            Add((byte)cmd.Input);
            Add(cmd.Yaw);
            Add(cmd.Pitch);
            Add(cmd.LaunchVelocity);
        }
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(ClientTick);
        Write(LastServerTickProcessedByClient);
        Write((byte)Commands.Length);
        foreach (var cmd in Commands)
        {
            Write(cmd.ClientTick);
            Write((byte)cmd.Input);
            Write(cmd.Yaw);
            Write(cmd.Pitch);
            Write(cmd.LaunchVelocity);
        }
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out ClientTick);
        Read(out LastServerTickProcessedByClient);
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
            Read(out cmd.LaunchVelocity);
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
            LastServerTickProcessedByClient = ClientGame.Instance.LastServerTickProcessedByClient
        };
        NetworkSender.ToServer(msg);
    }
}