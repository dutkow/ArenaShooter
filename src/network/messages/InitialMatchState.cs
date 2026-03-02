using Godot;


/// <summary>
/// Sent from Server → Client after receiving ClientLoaded to sync initial match state.
/// Includes positions, health, and any other relevant starting state.
/// </summary>
public class InitialMatchState : Message
{
    public byte[] PlayerIDs;
    public Vector3[] Positions;
    public int[] Health;

    protected override int BufferSize()
    {
        base.BufferSize();
        Add(PlayerIDs.Length);
        foreach (var id in PlayerIDs)
        {
            Add(id);
        }

        Add(Positions.Length);
        foreach (var pos in Positions)
        {
            Add(pos);
        }

        Add(Health.Length);
        foreach (var hp in Health)
        {
            Add(hp);
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(PlayerIDs.Length);
        foreach (var id in PlayerIDs)
        {
            Write(id);
        }

        Write(Positions.Length);
        foreach (var pos in Positions)
        {
            Write(pos);
        }

        Write(Health.Length);
        foreach (var hp in Health)
        {
            Write(hp);
        }

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        int count;
        Read(out count);
        PlayerIDs = new byte[count];
        for (int i = 0; i < count; i++)
        {
            Read(out PlayerIDs[i]);
        }

        Read(out count);
        Positions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Read(out Positions[i]);
        }

        Read(out count);
        Health = new int[count];
        for (int i = 0; i < count; i++)
        {
            Read(out Health[i]);
        }
    }

    public static void Send(ENetPacketPeer client, byte[] playerIDs, Vector3[] positions, int[] health)
    {
        var msg = new InitialMatchState
        {
            MessageType = Msg.S2C_INITIAL_MATCH_STATE,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerIDs = playerIDs,
            Positions = positions,
            Health = health
        };
        NetworkSender.ToClient(client, msg);
    }
}