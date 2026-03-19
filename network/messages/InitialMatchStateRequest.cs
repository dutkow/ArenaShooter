using Godot;

public struct ClientInfo(string playerName)
{
    public string PlayerName = playerName;
}
/// <summary>
/// Sent from Client → Server after the client finishes loading the level/scene.
/// </summary>
public class InitialMatchStateRequest : Message
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
        var msg = new InitialMatchStateRequest
        {
            MessageType = Msg.C2S_INITIAL_MATCH_STATE_REQUEST,
            ENetFlags = ENetPacketFlags.Reliable,
            ClientInfo = new ClientInfo(UserSettings.Instance.PlayerName)
        };
        NetworkSender.ToServer(msg);
    }
}