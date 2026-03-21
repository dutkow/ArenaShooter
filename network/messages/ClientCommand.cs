using Godot;
using System;
using System.Collections.Generic;

[Flags]
public enum InputFlags : ushort
{
    NONE = 0,

    // INPUT
    FORWARD = 1 << 0,
    BACKWARD = 1 << 1,
    STRAFE_LEFT = 1 << 2,
    STRAFE_RIGHT = 1 << 3,
    JUMP = 1 << 4,
    FIRE_PRIMARY = 1 << 5,
    FIRE_SECONDARY = 1 << 6,

    // CLIENT AUTHORITATIVE ROTATION
    LOOK = 1 << 7,
}

public struct ClientInputCommand
{
    public ushort ClientTick;
    public InputFlags Flags;
    public Vector2 Look;

}

public class ClientCommand : Message
{
    public ushort ClientTick;
    public ushort LastServerTickProcessedByClient;

    public ClientInputCommand[] Commands;

    protected override int BufferSize()
    {
        base.BufferSize();

        var cmds = Commands ?? Array.Empty<ClientInputCommand>();

        Add(ClientTick);
        Add(LastServerTickProcessedByClient);
        Add((byte)cmds.Length);

        foreach (var cmd in cmds)
        {
            Add(cmd.ClientTick);
            AddEnum(cmd.Flags);

            if (cmd.Flags.HasFlag(InputFlags.LOOK))
            {
                Add(cmd.Look);
            }
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        var cmds = Commands ?? Array.Empty<ClientInputCommand>();

        Write(ClientTick);
        Write(LastServerTickProcessedByClient);
        Write((byte)cmds.Length);

        foreach (var cmd in cmds)
        {
            Write(cmd.ClientTick);
            WriteEnum(cmd.Flags);

            if (cmd.Flags.HasFlag(InputFlags.LOOK))
            {
                Write(cmd.Look);
            }
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

            Read(out ushort mask);
            cmd.Flags = (InputFlags)mask;

            if (cmd.Flags.HasFlag(InputFlags.LOOK))
            {
                Read(out cmd.Look);
            }

            Commands[i] = cmd;
        }
    }

    public static void Send(ClientInputCommand[] commands)
    {
        var msg = new ClientCommand
        {
            MessageType = Msg.C2S_CLIENT_COMMAND,
            ENetFlags = ENetPacketFlags.Unsequenced,
            Commands = commands ?? Array.Empty<ClientInputCommand>(),
            ClientTick = MatchState.Instance.CurrentTick,
            LastServerTickProcessedByClient = ClientGame.Instance.LastServerTickProcessedByClient
        };
        NetworkSender.ToServer(msg);
    }
    public static ClientCommand FromData(byte[] data)
    {
        var cmd = new ClientCommand();
        cmd.ReadMessage(data);
        return cmd;
    }
}