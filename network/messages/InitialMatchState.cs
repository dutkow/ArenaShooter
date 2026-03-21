using Godot;
using System;
using System.Linq;

/// <summary>
/// Sent from Server → Client after receiving ClientLoaded to sync initial match state.
/// Includes positions, rotation, health, alive status, and other relevant starting state.
/// </summary>
public class InitialMatchState : Message
{
    public int ServerTickRate;

    public PlayerSnapshot[] PlayerSnapshots;

    // ----------------------
    // Serialization
    // ----------------------

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(ServerTickRate);

        byte playerCount = (byte)(PlayerSnapshots?.Length ?? 0);
        Add(playerCount);

        if (playerCount > 0)
        {
            foreach (var playerSnapshot in PlayerSnapshots)
            {
                playerSnapshot.Add(this, 0, true);
            }
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(ServerTickRate);

        // Player States
        byte playerCount = (byte)(PlayerSnapshots?.Length ?? 0);
        Write(playerCount);

        if (playerCount > 0)
        {
            foreach (var playerSnapshot in PlayerSnapshots)
            {
                playerSnapshot.Write(this, 0, true);
            }
        }

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out ServerTickRate);
        // Read the number of players

        byte playerCount;
        Read(out playerCount);

        GD.Print($"num received players: {playerCount}");

        if (playerCount > 0)
        {
            PlayerSnapshots = new PlayerSnapshot[playerCount];

            for (int i = 0; i < playerCount; i++)
            {
                PlayerSnapshots[i] = new PlayerSnapshot();
                PlayerSnapshots[i].Read(this, 0, true);
            }
        }
    }

    // ----------------------
    // Sending
    // ----------------------
    public static void Send(ENetPacketPeer client)
    {
        int playerCount = MatchState.Instance.Players.Count;
        var playerSnapshots = new PlayerSnapshot[playerCount];

        for(byte i = 0; i < playerCount; ++i)
        {
            playerSnapshots[i] = MatchState.Instance.Players[i].GetPlayerSnapshot();
        }

        var msg = new InitialMatchState
        {
            MessageType = Msg.S2C_INITIAL_MATCH_STATE,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerSnapshots = playerSnapshots,
            ServerTickRate = NetworkServer.Instance.ServerInfo.TickRate
        };

        NetworkSender.ToClient(client, msg);
    }
}