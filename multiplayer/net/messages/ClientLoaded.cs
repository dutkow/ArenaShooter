using Godot;


/// <summary>
/// Sent from Client → Server after the client finishes loading the level/scene.
/// </summary>
public class ClientLoaded : Message
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

    public static void Send(ENetPacketPeer server, byte playerID)
    {
        var msg = new ClientLoaded
        {
            MessageType = Msg.C2S_CLIENT_LOADED,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerID = playerID
        };
        NetworkSender.ToServer(msg);
    }
}