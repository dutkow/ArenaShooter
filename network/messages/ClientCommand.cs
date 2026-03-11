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

    // Eventually we will reconstruct these from the snapshot but fire transform system should be cleaned up first
    public Vector3 PredictedProjectileSpawnPosition;
    public Vector3 PredictedProjectileSpawnRotation;
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

            if (cmd.Mask.HasFlag(ClientCommandMask.YAW))
                Add(cmd.Yaw);
            if (cmd.Mask.HasFlag(ClientCommandMask.PITCH))
                Add(cmd.Pitch);
            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Add(cmd.LaunchVelocity.X);
                Add(cmd.LaunchVelocity.Y);
                Add(cmd.LaunchVelocity.Z);
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.FIRED_PREDICTED_PROJECTILE))
            {
                Add(cmd.PredictedProjectileClientID);

                // Add 6 floats for spawn position + rotation
                Add(cmd.PredictedProjectileSpawnPosition.X);
                Add(cmd.PredictedProjectileSpawnPosition.Y);
                Add(cmd.PredictedProjectileSpawnPosition.Z);

                Add(cmd.PredictedProjectileSpawnRotation.X);
                Add(cmd.PredictedProjectileSpawnRotation.Y);
                Add(cmd.PredictedProjectileSpawnRotation.Z);
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
            WriteEnum(cmd.Mask);

            if (cmd.Mask.HasFlag(ClientCommandMask.YAW))
                Write(cmd.Yaw); // raw float
            if (cmd.Mask.HasFlag(ClientCommandMask.PITCH))
                Write(cmd.Pitch); // raw float
            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Write(cmd.LaunchVelocity.X); // raw float
                Write(cmd.LaunchVelocity.Y);
                Write(cmd.LaunchVelocity.Z);
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.FIRED_PREDICTED_PROJECTILE))
            {
                // Always send the client-side predicted projectile ID
                Write(cmd.PredictedProjectileClientID);

                // Now also send spawn transform
                Write(cmd.PredictedProjectileSpawnPosition.X);
                Write(cmd.PredictedProjectileSpawnPosition.Y);
                Write(cmd.PredictedProjectileSpawnPosition.Z);

                Write(cmd.PredictedProjectileSpawnRotation.X);
                Write(cmd.PredictedProjectileSpawnRotation.Y);
                Write(cmd.PredictedProjectileSpawnRotation.Z);
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
            cmd.Mask = (ClientCommandMask)mask;

            if (cmd.Mask.HasFlag(ClientCommandMask.YAW))
                Read(out cmd.Yaw); // raw float
            if (cmd.Mask.HasFlag(ClientCommandMask.PITCH))
                Read(out cmd.Pitch); // raw float
            if (cmd.Mask.HasFlag(ClientCommandMask.WAS_LAUNCHED))
            {
                Read(out cmd.LaunchVelocity.X); // raw float
                Read(out cmd.LaunchVelocity.Y);
                Read(out cmd.LaunchVelocity.Z);
            }
            if (cmd.Mask.HasFlag(ClientCommandMask.FIRED_PREDICTED_PROJECTILE))
            {
                Read(out cmd.PredictedProjectileClientID);

                Read(out cmd.PredictedProjectileSpawnPosition.X);
                Read(out cmd.PredictedProjectileSpawnPosition.Y);
                Read(out cmd.PredictedProjectileSpawnPosition.Z);

                Read(out cmd.PredictedProjectileSpawnRotation.X);
                Read(out cmd.PredictedProjectileSpawnRotation.Y);
                Read(out cmd.PredictedProjectileSpawnRotation.Z);
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
}