using Godot;


/// <summary>
/// Sent from Server → Clients when a new player joins the game.
/// </summary>
public class PlayerLeft : Message
{
    public byte PlayerID;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(PlayerID);

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(PlayerID);

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out PlayerID);
    }

    public static void Send(byte playerID)
    {
        var msg = new PlayerLeft
        {
            MessageType = Msg.S2C_PLAYER_LEFT,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerID = playerID,
        };
        NetworkSender.Broadcast(msg);
    }
}