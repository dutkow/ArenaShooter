using Godot;
using System.Linq;


/// <summary>
/// Sent from Server → Client after receiving ClientLoaded to sync initial match state.
/// Includes positions, health, and any other relevant starting state.
/// </summary>
// <summary>
/// Server → Client: Initial full match state
/// Includes player ID, name, position, rotation (Euler angles)
/// Reliable because it must always arrive
/// </summary>
public class InitialMatchState : Message
{
    public byte[] PlayerIDs;
    public string[] PlayerNames;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(PlayerIDs.Length);
        foreach (var id in PlayerIDs)
        {
            Add(id);
        }

        Add(PlayerNames.Length);
        foreach (var name in PlayerNames)
        {
            Add(name);
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

        Write(PlayerNames.Length);
        foreach (var name in PlayerNames)
        {
            Write(name);
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
        PlayerNames = new string[count];
        for (int i = 0; i < count; i++)
        {
            Read(out PlayerNames[i]);
        }
    }

    public static void Send(ENetPacketPeer client)
    {
        var players = MatchState.Instance.ConnectedPlayers;

        int count = players.Count;

        byte[] playerIDs = new byte[count];
        string[] playerNames = new string[count];
        Vector3[] positions = new Vector3[count];
        Vector3[] rotations = new Vector3[count];

        int i = 0;
        foreach(var kvp in players)
        {
            playerIDs[i] = kvp.Key;
            playerNames[i] = kvp.Value.PlayerName;
            i++;
        }

        var msg = new InitialMatchState
        {
            MessageType = Msg.S2C_INITIAL_MATCH_STATE,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerIDs = playerIDs,
            PlayerNames = playerNames,
        };


        NetworkSender.ToClient(client, msg);

        return;
    }
}