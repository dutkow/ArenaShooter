using Godot;

/// <summary>
/// Sent from Server → Clients when a new player joins the game.
/// </summary>
public class PlayerJoined : Message
{
    public byte PlayerID;
    public string PlayerName;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerID);
        Add(PlayerName);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(PlayerID);
        Write(PlayerName);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out PlayerID);
        Read(out PlayerName);
    }

    public static void Send(byte playerID, string playerName)
    {
        var msg = new PlayerJoined
        {
            MessageType = Msg.S2C_PLAYER_JOINED,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerID = playerID,
            PlayerName = playerName
        };
        NetworkSender.Broadcast(msg);
    }

}