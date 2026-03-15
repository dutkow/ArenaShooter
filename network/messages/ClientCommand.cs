using Godot;
using System;

[Flags]
public enum ClientCommandMask : ushort
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

    // EVENTS
    WAS_LAUNCHED = 1 << 8,
    FIRED_PREDICTED_PROJECTILE = 1 << 9,
}

public struct ClientInputCommand
{
    public ushort ClientTick;
    public ClientCommandMask Mask;

    public Vector2 Look;

    public Vector3 LaunchVelocity;
    public ushort PredictedProjectileClientID;
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
            AddEnum(cmd.Mask);

            if (cmd.Mask.HasFlag(ClientCommandMask.LOOK))
                Add(cmd.Look);
            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Add(cmd.LaunchVelocity.X);
                Add(cmd.LaunchVelocity.Y);
                Add(cmd.LaunchVelocity.Z);
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.FIRED_PREDICTED_PROJECTILE))
                Add(cmd.PredictedProjectileClientID);
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
            WriteEnum(cmd.Mask);

            if (cmd.Mask.HasFlag(ClientCommandMask.LOOK))
                Write(cmd.Look);

            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Write(cmd.LaunchVelocity.X); // raw float
                Write(cmd.LaunchVelocity.Y);
                Write(cmd.LaunchVelocity.Z);
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.FIRED_PREDICTED_PROJECTILE))
                Write(cmd.PredictedProjectileClientID);
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
            cmd.Mask = (ClientCommandMask)mask;

            if (cmd.Mask.HasFlag(ClientCommandMask.LOOK))
                Read(out cmd.Look);

            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Read(out cmd.LaunchVelocity.X); // raw float
                Read(out cmd.LaunchVelocity.Y);
                Read(out cmd.LaunchVelocity.Z);
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.FIRED_PREDICTED_PROJECTILE))
                Read(out cmd.PredictedProjectileClientID);

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