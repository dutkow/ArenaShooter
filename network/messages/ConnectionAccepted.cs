using Godot;


/// <summary>
/// Sent from Server → Client when the connection request is accepted.
/// Includes assigned player ID and a list of currently connected player names.
/// </summary>
public class ConnectionAccepted : Message
{
    public byte AssignedPlayerID;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(AssignedPlayerID);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(AssignedPlayerID);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out AssignedPlayerID);
    }

    public static void Send(ENetPacketPeer client, byte assignedID)
    {
        var msg = new ConnectionAccepted
        {
            MessageType = Msg.S2C_CONNECTION_ACCEPTED,
            ENetFlags = ENetPacketFlags.Reliable,
            AssignedPlayerID = assignedID,
        };

        GD.Print($"assing player id to client: {assignedID}");
        NetworkSender.ToClient(client, msg);
    }
}