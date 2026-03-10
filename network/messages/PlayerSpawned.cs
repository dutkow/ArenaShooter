using Godot;
using System;

/// <summary>
/// Sent from Server → Clients when a new player pawn is spawned.
/// </summary>
public class PlayerSpawned : Message
{
    public byte PlayerID;
    public Vector3 SpawnPosition;
    public float SpawnRotationY; // store as Euler angles for simplicity

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerID);
        Add(SpawnPosition);
        Add(SpawnRotationY);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(PlayerID);
        Write(SpawnPosition);
        Write(SpawnRotationY);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out PlayerID);
        Read(out SpawnPosition);
        Read(out SpawnRotationY);
    }

    public static void Send(byte playerID, Vector3 spawnPosition, float spawnRotationY)
    {
        var msg = new PlayerSpawned
        {
            MessageType = Msg.S2C_PLAYER_SPAWNED,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerID = playerID,
            SpawnPosition = spawnPosition,
            SpawnRotationY = spawnRotationY,
        };
        NetworkSender.Broadcast(msg);
    }
}