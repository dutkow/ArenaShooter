using Godot;
using System;

/// <summary>
/// Reliable packet for sending health and shield updates to the owning client.
/// </summary>
public class HealthUpdate : Message
{
    public byte Health;
    public byte Shield;

    public HealthUpdate() { }

    public HealthUpdate(byte health, byte shield)
    {
        Health = health;
        Shield = shield;
    }

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(Health);
        Add(Shield);

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(Health);
        Write(Shield);

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out Health);
        Read(out Shield);
    }

    public static void Send(byte playerID, int health, int shield)
    {
        ENetPacketPeer peer = NetworkManager.Instance.PlayerIDsToPeers[playerID];
        if(peer == null)
        {
            GD.PushError($"Peer with player ID [{playerID}] is null.");
            return;
        }

        var msg = new HealthUpdate
        {
            MessageType = Msg.S2C_HEALTH_CHANGED,
            ENetFlags = ENetPacketFlags.Reliable,
            Health = (byte)health,
            Shield = (byte)shield
        };
        NetworkSender.ToClient(peer, msg);
    }
}