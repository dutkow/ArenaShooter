using Godot;

public struct ClientInfo(string playerName)
{
    public string PlayerName = playerName;
}
/// <summary>
/// Sent from Client → Server after the client finishes loading the level/scene.
/// </summary>
public class ClientLoaded : Message
{
    public ClientInfo ClientInfo;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(ClientInfo.PlayerName);

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(ClientInfo.PlayerName);

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out ClientInfo.PlayerName);
    }

    public static void Send()
    {
        var msg = new ClientLoaded
        {
            MessageType = Msg.C2S_CLIENT_LOADED,
            ENetFlags = ENetPacketFlags.Reliable,
            ClientInfo = new ClientInfo(Settings.Instance.PlayerName)
        };
        NetworkSender.ToServer(msg);
    }
}