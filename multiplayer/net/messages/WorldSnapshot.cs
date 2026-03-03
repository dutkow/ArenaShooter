using Godot;
using System;
using System.Linq;

/// <summary>
/// Sent from Server → Client to sync the current tick’s player positions and rotations.
/// Purely about movement/rotation for now.
/// </summary>
public class WorldSnapshot : Message
{
    public byte[] PlayerIDs;
    public string[] PlayerNames;
    public Vector3[] Positions;
    public float[] CharacterYaws;
    public float[] AimPitches;

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

        for (int i = 0; i < CharacterYaws.Length; i++)
        {
            Add(CharacterYaws[i]);
        }

        for (int i = 0; i < AimPitches.Length; i++)
        {
            Add(AimPitches[i]);
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

        for (int i = 0; i < CharacterYaws.Length; i++)
        {
            Write(CharacterYaws[i]);
        }

        for (int i = 0; i < AimPitches.Length; i++)
        {
            Write(AimPitches[i]);
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

        CharacterYaws = new float[count];

        for (int i = 0; i < count; i++)
        {
            Read(out CharacterYaws[i]);
        }

        AimPitches = new float[count];

        for (int i = 0; i < count; i++)
        {
            Read(out AimPitches[i]);
        }
    }

    public static void Send()
    {
        var players = MatchState.Instance.ConnectedPlayers;
        int count = players.Count;

        byte[] playerIDs = new byte[count];
        string[] playerNames = new string[count];
        Vector3[] positions = new Vector3[count];
        float[] yaws = new float[count];
        float[] pitches = new float[count];

        int i = 0;

        foreach (var kvp in players)
        {
            var player = kvp.Value;

            playerIDs[i] = kvp.Key;
            playerNames[i] = player.PlayerName;

            if (player.Character != null)
            {
                positions[i] = player.Character.Body.GlobalPosition;
                yaws[i] = player.Character.Yaw;
                pitches[i] = player.Character.AimPitch;
            }
            else
            {
                positions[i] = Vector3.Zero;
                yaws[i] = 0f;
                pitches[i] = 0f;
            }

            i++;
        }

        var msg = new WorldSnapshot()
        {
            MessageType = Msg.S2C_WORLD_SNAPSHOT,
            ENetFlags = ENetPacketFlags.Reliable,
            PlayerIDs = playerIDs,
            PlayerNames = playerNames,
            Positions = positions,
            CharacterYaws = yaws,
            AimPitches = pitches
        };

        NetworkSender.Broadcast(msg);
    }
}