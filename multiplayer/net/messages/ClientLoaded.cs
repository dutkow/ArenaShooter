using Godot;


/// <summary>
/// Sent from Client → Server after the client finishes loading the level/scene.
/// </summary>
public class ClientLoaded : Message
{
    protected override int BufferSize()
    {
        base.BufferSize();
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
    }

    public static void Send()
    {
        var msg = new ClientLoaded
        {
            MessageType = Msg.C2S_CLIENT_LOADED,
            ENetFlags = ENetPacketFlags.Reliable,
        };
        NetworkSender.ToServer(msg);
    }
}