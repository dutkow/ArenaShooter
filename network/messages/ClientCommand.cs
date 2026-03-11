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
    public ClientInputCommand[] Commands;

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
            AddEnum(cmd.Mask);

            // Masked & Quantized
            if (cmd.Mask.HasFlag(ClientCommandMask.YAW))
                Add(Quantize.Angle(cmd.Yaw));
            if (cmd.Mask.HasFlag(ClientCommandMask.PITCH))
                Add(Quantize.Angle(cmd.Pitch));
            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Add(Quantize.Velocity(cmd.LaunchVelocity.X));
                Add(Quantize.Velocity(cmd.LaunchVelocity.Y));
                Add(Quantize.Velocity(cmd.LaunchVelocity.Z));
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.FIRED_PREDICTED_PROJECTILE))
                Add(cmd.PredictedProjectileClientID);
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

            // Masked & Dequantized
            if (cmd.Mask.HasFlag(ClientCommandMask.YAW))
            {
                short qYaw;
                Read(out qYaw);
                cmd.Yaw = Quantize.Angle(qYaw);
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.PITCH))
            {
                short qPitch;
                Read(out qPitch);
                cmd.Pitch = Quantize.Angle(qPitch);
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                short vx, vy, vz;
                Read(out vx);
                Read(out vy);
                Read(out vz);
                cmd.LaunchVelocity = new Vector3(
                    Quantize.Velocity(vx),
                    Quantize.Velocity(vy),
                    Quantize.Velocity(vz)
                );
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