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

    public PlayerState[] PlayerStates;

    // ----------------------
    // Serialization
    // ----------------------

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(ServerTickRate);

        byte playerStatesCount = (byte)(PlayerStates?.Length ?? 0);
        Add(playerStatesCount);

        if (playerStatesCount > 0)
        {
            foreach (var playerState in PlayerStates)
            {
                playerState.Add(this, 0, true);
            }
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(ServerTickRate);

        // Player States
        byte playerStatesCount = (byte)(PlayerStates?.Length ?? 0);
        Write(playerStatesCount);

        if (playerStatesCount > 0)
        {
            foreach (var playerState in PlayerStates)
            {
                playerState.Write(this, 0, true);
            }
        }

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out ServerTickRate);
        // Read the number of players

        byte playerStatesCount;
        Read(out playerStatesCount);

        GD.Print($"num received players: {playerStatesCount}");

        if (playerStatesCount > 0)
        {
            PlayerStates = new PlayerState[playerStatesCount];

            for (int i = 0; i < playerStatesCount; i++)
            {
                PlayerStates[i] = new PlayerState(); // ✅ important!
                PlayerStates[i].Read(this, 0, true);
            }
        }
    }

    // ----------------------
    // Sending
    // ----------------------
    public static void Send(ENetPacketPeer client)
    {
        var msg = new InitialMatchState
        {
            MessageType = Msg.S2C_INITIAL_MATCH_STATE,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerStates = MatchState.Instance.ConnectedPlayers.Values.ToArray(),
            ServerTickRate = NetworkServer.Instance.ServerInfo.TickRate
        };

        NetworkSender.ToClient(client, msg);
    }
}