using Godot;
using System;
using System.Linq;




/// <summary>
/// Sent from Server → Client to sync the current tick’s player positions, rotations, and velocity.
/// </summary>
using Godot;
using System;
using System.Linq;

public class WorldSnapshot : Message
{
    // Arena character snapshot
    public byte[] PlayerIDs;
    public string[] PlayerNames;
    public Vector3[] Positions;
    public Vector3[] Velocities;
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

        for (int i = 0; i < Velocities.Length; i++)
        {
            Add(Velocities[i]);
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

        for (int i = 0; i < Velocities.Length; i++)
        {
            Write(Velocities[i]);
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

        Velocities = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Read(out Velocities[i]);
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
        Vector3[] velocities = new Vector3[count];
        float[] yaws = new float[count];
        float[] pitches = new float[count];
        int[] health = new int[count];
        int[] shield = new int[count];

        int i = 0;

        foreach (var kvp in players)
        {
            var player = kvp.Value;

            playerIDs[i] = kvp.Key;
            playerNames[i] = player.PlayerName;

            if (player.Character != null)
            {
                positions[i] = player.Character.GlobalPosition;
                velocities[i] = player.Character.Velocity;
                yaws[i] = player.Character.Yaw;
                pitches[i] = player.Character.AimPitch;
                health[i] = player.Character.HealthComponent.Health;
                shield[i] = player.Character.HealthComponent.Shield;
            }
            else
            {
                positions[i] = Vector3.Zero;
                velocities[i] = Vector3.Zero;
                yaws[i] = 0f;
                pitches[i] = 0f;
                health[i] = 0;
                shield[i] = 0;
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
            Velocities = velocities,
            CharacterYaws = yaws,
            AimPitches = pitches
        };

        NetworkSender.Broadcast(msg);
    }

    public ArenaCharacterSnapshot[] GetCharacterSnapshots()
    {
        int count = PlayerIDs.Length;
        var snapshots = new ArenaCharacterSnapshot[count];

        for (int i = 0; i < count; i++)
        {
            snapshots[i] = new ArenaCharacterSnapshot(
                PlayerIDs[i],
                Positions[i],
                Velocities[i],
                CharacterYaws[i],
                AimPitches[i]
            );
        }
        return snapshots;
    }
}