using Godot;

public struct PlayerInfo
{
    public byte PlayerID;
    public string PlayerName;

    public PlayerInfo(byte playerID, string playerName)
    {
        PlayerID = playerID;
        PlayerName = playerName;
    }
}
/// <summary>
/// Sent from Server → Clients when a new player joins the game.
/// </summary>
public class PlayerJoined : Message
{
    public PlayerInfo PlayerInfo;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerInfo.PlayerID);
        Add(PlayerInfo.PlayerName);
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();
        Write(PlayerInfo.PlayerID);
        Write(PlayerInfo.PlayerName);
        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);
        Read(out PlayerInfo.PlayerID);
        Read(out PlayerInfo.PlayerName);
    }

    public static void Send(byte playerID, string playerName)
    {
        var playerInfo = new PlayerInfo(playerID, playerName);

        var msg = new PlayerJoined
        {
            MessageType = Msg.S2C_PLAYER_JOINED,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerInfo = playerInfo,
        };
        NetworkSender.BroadcastExcept(playerID, msg);
    }
}