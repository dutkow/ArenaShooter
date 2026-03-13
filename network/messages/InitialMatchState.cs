using Godot;
using System;
using System.Linq;

/// <summary>
/// Sent from Server → Client after receiving ClientLoaded to sync initial match state.
/// Includes positions, rotation, health, alive status, and other relevant starting state.
/// </summary>
public class InitialMatchState : Message
{
    public byte[] PlayerIDs;
    public string[] PlayerNames;

    // ----------------------
    // Serialization
    // ----------------------

    protected override int BufferSize()
    {
        base.BufferSize();

        int count = PlayerIDs.Length;

        Add(count);

        for (int i = 0; i < count; i++)
        {
            Add(PlayerIDs[i]);
            Add(PlayerNames[i]);
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        int count = PlayerIDs.Length;

        Write(count);

        for (int i = 0; i < count; i++)
        {
            Write(PlayerIDs[i]);
            Write(PlayerNames[i]);
        }

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out int count);

        PlayerIDs = new byte[count];
        PlayerNames = new string[count];

        for (int i = 0; i < count; i++)
        {
            Read(out PlayerIDs[i]);
            Read(out PlayerNames[i]);
        }
    }

    // ----------------------
    // Sending
    // ----------------------



    public static void Send(ENetPacketPeer client)
    {
        var playerStates = MatchState.Instance.ConnectedPlayers.Values.ToArray();
        int numPlayers = playerStates.Length;

        byte[] playerIDs = new byte[numPlayers];
        string[] playerNames = new string[numPlayers];

        for(int i = 0; i < numPlayers; ++i)
        {
            var playerState = playerStates[i];
            playerIDs[i] = playerState.PlayerID;
            playerNames[i] = playerState.PlayerName;
        }

        var msg = new InitialMatchState
        {
            MessageType = Msg.S2C_INITIAL_MATCH_STATE,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerIDs = playerIDs,
            PlayerNames = playerNames,
        };

        GD.Print($"sending initial match state to peer: {client}");
        NetworkSender.ToClient(client, msg);
    }
}