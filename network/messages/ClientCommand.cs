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
    YAW = 1 << 7,
    PITCH = 1 << 8,

    // EVENTS
    WAS_LAUNCHED = 1 << 9,
    FIRED_PREDICTED_PROJECTILE = 1 << 10,
}

public struct ClientInputCommand
{
    public ushort ClientTick;
    public ClientCommandMask Mask;

    public float Yaw;
    public float Pitch;
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

        Add(ClientTick);
        Add(LastServerTickProcessedByClient);
        Add((byte)Commands.Length);

        foreach (var cmd in Commands)
        {
            Write(cmd.ClientTick);
            WriteEnum(cmd.Mask);

            if (cmd.Mask.HasFlag(ClientCommandMask.YAW))
                Write(cmd.Yaw);
            if (cmd.Mask.HasFlag(ClientCommandMask.PITCH))
                Write(cmd.Pitch);
            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Write(cmd.LaunchVelocity.X);
                Write(cmd.LaunchVelocity.Y);
                Write(cmd.LaunchVelocity.Z);
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.FIRED_PREDICTED_PROJECTILE))
                Write(cmd.PredictedProjectileClientID);
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
            WriteEnum(cmd.Mask);

            // Masked & Quantized
            if (cmd.Mask.HasFlag(ClientCommandMask.YAW))
                Write(Quantize.Angle(cmd.Yaw));
            if (cmd.Mask.HasFlag(ClientCommandMask.PITCH))
                Write(Quantize.Angle(cmd.Pitch));
            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Write(Quantize.Velocity(cmd.LaunchVelocity.X));
                Write(Quantize.Velocity(cmd.LaunchVelocity.Y));
                Write(Quantize.Velocity(cmd.LaunchVelocity.Z));
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

            if (cmd.Mask.HasFlag(ClientCommandMask.YAW))
            {
                Read(out cmd.Yaw);
            }

            if (cmd.Mask.HasFlag(ClientCommandMask.PITCH))
            {
                Read(out cmd.Pitch);
            }

            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Read(out cmd.LaunchVelocity.X);
                Read(out cmd.LaunchVelocity.Y);
                Read(out cmd.LaunchVelocity.Z);
            }

            if (cmd.Mask.HasFlag(ClientCommandMask.FIRED_PREDICTED_PROJECTILE))
            {
                Read(out cmd.PredictedProjectileClientID);
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
            Commands = commands,
            ClientTick = MatchState.Instance.CurrentTick,
            LastServerTickProcessedByClient = ClientGame.Instance.LastServerTickProcessedByClient
        };
        NetworkSender.ToServer(msg);
    }
}