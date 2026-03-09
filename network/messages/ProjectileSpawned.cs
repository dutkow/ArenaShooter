using Godot;
using System;

public enum ProjectileType
{
    DEFAULT,
}

public class ProjectileSpawned : Message
{
    public ushort ID;      // unique ID for this projectile
    public ProjectileType Type;      // enum for the projectile type
    public Vector3 SpawnPosition;    // world position
    public Vector3 SpawnRotation;    // Euler angles for simplicity

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(ID);
        Add((byte)Type); // store enum as byte
        Add(SpawnPosition);
        Add(SpawnRotation);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(ID);
        Write((byte)Type);
        Write(SpawnPosition);
        Write(SpawnRotation);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out ID);
        byte typeByte;
        Read(out typeByte);
        Type = (ProjectileType)typeByte;
        Read(out SpawnPosition);
        Read(out SpawnRotation);
    }

    public static void Send(ushort id, ProjectileType type, Vector3 spawnPosition, Vector3 spawnRotation)
    {
        var msg = new ProjectileSpawned
        {
            MessageType = Msg.S2C_PROJECTILE_SPAWNED,
            ENetFlags = ENetPacketFlags.Reliable,
            ID = id,
            Type = type,
            SpawnPosition = spawnPosition,
            SpawnRotation = spawnRotation
        };

        NetworkSender.Broadcast(msg);
    }

    public static void Execute(ushort projectileID, ProjectileType type, Vector3 spawnPosition, Vector3 spawnRotation)
    {

    }
}