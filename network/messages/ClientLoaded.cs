using Godot;


/// <summary>
/// Sent from Client → Server after the client finishes loading the level/scene.
/// </summary>
public class ClientLoaded : Message
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

    public static void Send()
    {
        var msg = new ClientLoaded
        {
            MessageType = Msg.C2S_CLIENT_LOADED,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerName = Settings.Instance.PlayerName
        };
        NetworkSender.ToServer(msg);
    }
}