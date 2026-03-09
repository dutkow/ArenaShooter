using Godot;
using System;

/// <summary>
/// Sent from Server → Clients when a player dies.
/// </summary>
public class PlayerDied : Message
{
    public byte PlayerID;
    public byte KillerPlayerID;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerID);
        Add(KillerPlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(PlayerID);
        Write(KillerPlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out PlayerID);
        Read(out KillerPlayerID);
    }

    /// <summary>
    /// Sends the death message to all clients.
    /// </summary>
    public static void Send(byte playerID, byte killerPlayerID = 0xFF)
    {
        var msg = new PlayerDied
        {
            MessageType = Msg.S2C_PLAYER_DIED,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerID = playerID,
            KillerPlayerID = killerPlayerID
        };
        NetworkSender.Broadcast(msg);
    }
}