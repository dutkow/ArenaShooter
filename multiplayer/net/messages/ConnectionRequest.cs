using Godot;


/// <summary>
/// Sent from Client → Server to request joining the game.
/// </summary>
public class ConnectionRequest : Message
{
    public string PlayerName;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerName);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(PlayerName);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out PlayerName);
    }

    public static void Send(string playerName)
    {
        var msg = new ConnectionRequest
        {
            MessageType = Msg.C2S_CONNECTION_REQUEST,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerName = playerName
        };
        NetworkSender.ToServer(msg);
    }
}