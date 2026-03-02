using Godot;


/// <summary>
/// Sent from Server → Client when the connection request is denied.
/// Includes a reason for denial.
/// </summary>
public class ConnectionDenied : Message
{
    public string Reason;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(Reason);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(Reason);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out Reason);
    }

    public static void Send(ENetPacketPeer client, string reason)
    {
        var msg = new ConnectionDenied
        {
            MessageType = Msg.S2C_CONNECTION_DENIED,
            ENetFlags = ENetPacketFlags.Reliable,
            Reason = reason
        };
        NetworkSender.ToClient(client, msg);
    }
}