using Godot;
using System;

/// <summary>
/// Sent from Server → Client after receiving ClientLoaded to sync initial match state.
/// Includes positions, rotation, health, alive status, and other relevant starting state.
/// </summary>
public class InitialMatchState : Message
{
    public byte[] PlayerIDs;
    public string[] PlayerNames;
    public Vector3[] Positions;
    public Vector3[] Rotations;
    public bool[] IsAlive;

    // ----------------------
    // Serialization
    // ----------------------

    protected override int BufferSize()
    {
        base.BufferSize();

        int count = PlayerIDs.Length;

#if DEBUG
        if (
            PlayerNames.Length != count ||
            Positions.Length != count ||
            Rotations.Length != count ||
            IsAlive.Length != count
        )
        {
            throw new InvalidOperationException("InitialMatchState arrays are not the same length");
        }
#endif

        Add(count);

        for (int i = 0; i < count; i++)
        {
            Add(PlayerIDs[i]);
            Add(PlayerNames[i]);
            Add(Positions[i]);
            Add(Rotations[i]);
            Add(IsAlive[i]);
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        int count = PlayerIDs.Length;

#if DEBUG
        if (
            PlayerNames.Length != count ||
            Positions.Length != count ||
            Rotations.Length != count ||
            IsAlive.Length != count
        )
        {
            throw new InvalidOperationException("InitialMatchState arrays are not the same length");
        }
#endif

        Write(count);

        for (int i = 0; i < count; i++)
        {
            Write(PlayerIDs[i]);
            Write(PlayerNames[i]);
            Write(Positions[i]);
            Write(Rotations[i]);
            Write(IsAlive[i]);
        }

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out int count);

        PlayerIDs = new byte[count];
        PlayerNames = new string[count];
        Positions = new Vector3[count];
        Rotations = new Vector3[count];
        IsAlive = new bool[count];

        for (int i = 0; i < count; i++)
        {
            Read(out PlayerIDs[i]);
            Read(out PlayerNames[i]);
            Read(out Positions[i]);
            Read(out Rotations[i]);
            Read(out IsAlive[i]);
        }
    }

    // ----------------------
    // Sending
    // ----------------------

    public static void Send(ENetPacketPeer client)
    {
        var players = MatchState.Instance.ConnectedPlayers;
        int count = players.Count;

        byte[] playerIDs = new byte[count];
        string[] playerNames = new string[count];
        Vector3[] positions = new Vector3[count];
        Vector3[] rotations = new Vector3[count];
        bool[] isAlive = new bool[count];

        int i = 0;
        foreach (var kvp in players)
        {
            var player = kvp.Value;

            playerIDs[i] = kvp.Key;
            playerNames[i] = player.PlayerName;

            if (player.Pawn != null)
            {
                positions[i] = player.Pawn.GlobalPosition;
                rotations[i] = player.Pawn.GlobalRotation;
                isAlive[i] = player.IsAlive;
                GD.Print("is alive is true in initial match state");
            }
            else
            {
                positions[i] = Vector3.Zero;
                rotations[i] = Vector3.Zero;
                isAlive[i] = false;
                GD.Print("is alive is false in initial match state");
            }

            i++;
        }

        var msg = new InitialMatchState
        {
            MessageType = Msg.S2C_INITIAL_MATCH_STATE,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerIDs = playerIDs,
            PlayerNames = playerNames,
            Positions = positions,
            Rotations = rotations,
            IsAlive = isAlive
        };

#if DEBUG
        GD.Print("=== SENDING InitialMatchState ===");
        GD.Print($"Player count: {count}");

        for (int j = 0; j < count; j++)
        {
            GD.Print(
                $"[{j}] ID={playerIDs[j]} " +
                $"Name={playerNames[j]} " +
                $"Pos={positions[j]} " +
                $"RotY={rotations[j].Y} " +
                $"Alive={isAlive[j]}"
            );
        }
        GD.Print("=== END InitialMatchState ===");
#endif

        NetworkSender.ToClient(client, msg);
    }
}