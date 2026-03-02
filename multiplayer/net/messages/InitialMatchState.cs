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
    public Vector3[] Positions;
    public Vector3[] Rotations;
    public bool[] IsAlive;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(PlayerIDs.Length);

        for (int i = 0; i < PlayerIDs.Length; i++)
        {
            Add(PlayerIDs[i]);
        }

        Add(PlayerNames.Length);

        for (int i = 0; i < PlayerNames.Length; i++)
        {
            Add(PlayerNames[i]);
        }

        for (int i = 0; i < Positions.Length; i++)
        {
            Add(Positions[i]);
        }

        for (int i = 0; i < Rotations.Length; i++)
        {
            Add(Rotations[i]);
        }

        for (int i = 0; i < IsAlive.Length; i++)
        {
            Add(IsAlive[i]);
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(PlayerIDs.Length);

        for (int i = 0; i < PlayerIDs.Length; i++)
        {
            Write(PlayerIDs[i]);
        }

        Write(PlayerNames.Length);

        for (int i = 0; i < PlayerNames.Length; i++)
        {
            Write(PlayerNames[i]);
        }

        for (int i = 0; i < Positions.Length; i++)
        {
            Write(Positions[i]);
        }

        for (int i = 0; i < Rotations.Length; i++)
        {
            Write(Rotations[i]);
        }

        for (int i = 0; i < IsAlive.Length; i++)
        {
            Write(IsAlive[i]);
        }

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        int count = 0;

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

        Positions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Read(out Positions[i]);
        }

        Rotations = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Read(out Rotations[i]);
        }

        IsAlive = new bool[count];
        for (int i = 0; i < count; i++)
        {
            Read(out IsAlive[i]);
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
        bool[] isAlive = new bool[count];

        int i = 0;

        foreach (var kvp in players)
        {
            var player = kvp.Value;

            playerIDs[i] = kvp.Key;
            playerNames[i] = player.PlayerName;

            if (player.Character != null)
            {
                positions[i] = player.Character.GlobalPosition;
                rotations[i] = player.Character.GlobalRotation;
                isAlive[i] = player.IsAlive;
            }
            else
            {
                positions[i] = Vector3.Zero;
                rotations[i] = Vector3.Zero;
                isAlive[i] = false;
            }

            i++;
        }

        var msg = new InitialMatchState()
        {
            MessageType = Msg.S2C_INITIAL_MATCH_STATE,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerIDs = playerIDs,
            PlayerNames = playerNames,
            Positions = positions,
            Rotations = rotations,
            IsAlive = isAlive
        };

        NetworkSender.ToClient(client, msg);
        return;
    }
}