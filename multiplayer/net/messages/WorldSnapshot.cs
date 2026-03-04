using Godot;
using System;
using System.Linq;




/// <summary>
/// Sent from Server → Client to sync the current tick’s player positions, rotations, and velocity.
/// </summary>
public class WorldSnapshot : Message
{
    public byte[] PlayerIDs;
    public string[] PlayerNames;
    public Vector3[] Positions;
    public Vector3[] Velocities; // NEW
    public float[] CharacterYaws;
    public float[] AimPitches;

    protected override int BufferSize()
    {
        base.BufferSize();

        // Player IDs
        Add(PlayerIDs.Length);
        for (int i = 0; i < PlayerIDs.Length; i++) Add(PlayerIDs[i]);

        // Player Names
        Add(PlayerNames.Length);
        for (int i = 0; i < PlayerNames.Length; i++) Add(PlayerNames[i]);

        // Positions
        for (int i = 0; i < Positions.Length; i++) Add(Positions[i]);

        // Velocities
        for (int i = 0; i < Velocities.Length; i++) Add(Velocities[i]); // NEW

        // Yaws
        for (int i = 0; i < CharacterYaws.Length; i++) Add(CharacterYaws[i]);

        // Aim pitches
        for (int i = 0; i < AimPitches.Length; i++) Add(AimPitches[i]);

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        // Player IDs
        Write(PlayerIDs.Length);
        for (int i = 0; i < PlayerIDs.Length; i++) Write(PlayerIDs[i]);

        // Player Names
        Write(PlayerNames.Length);
        for (int i = 0; i < PlayerNames.Length; i++) Write(PlayerNames[i]);

        // Positions
        for (int i = 0; i < Positions.Length; i++) Write(Positions[i]);

        // Velocities
        for (int i = 0; i < Velocities.Length; i++) Write(Velocities[i]); // NEW

        // Yaws
        for (int i = 0; i < CharacterYaws.Length; i++) Write(CharacterYaws[i]);

        // Aim pitches
        for (int i = 0; i < AimPitches.Length; i++) Write(AimPitches[i]);

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        int count = 0;

        // Player IDs
        Read(out count);
        PlayerIDs = new byte[count];
        for (int i = 0; i < count; i++) Read(out PlayerIDs[i]);

        // Player Names
        Read(out count);
        PlayerNames = new string[count];
        for (int i = 0; i < count; i++) Read(out PlayerNames[i]);

        // Positions
        Positions = new Vector3[count];
        for (int i = 0; i < count; i++) Read(out Positions[i]);

        // Velocities
        Velocities = new Vector3[count]; // NEW
        for (int i = 0; i < count; i++) Read(out Velocities[i]);

        // Yaws
        CharacterYaws = new float[count];
        for (int i = 0; i < count; i++) Read(out CharacterYaws[i]);

        // Aim pitches
        AimPitches = new float[count];
        for (int i = 0; i < count; i++) Read(out AimPitches[i]);
    }

    public static void Send()
    {
        var players = MatchState.Instance.ConnectedPlayers;
        int count = players.Count;

        byte[] playerIDs = new byte[count];
        string[] playerNames = new string[count];
        Vector3[] positions = new Vector3[count];
        Vector3[] velocities = new Vector3[count]; // NEW
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
                positions[i] = player.Character.CharacterBody.GlobalPosition;
                velocities[i] = player.Character.CharacterBody.Velocity; // NEW
                yaws[i] = player.Character.Yaw;
                pitches[i] = player.Character.AimPitch;
            }
            else
            {
                positions[i] = Vector3.Zero;
                velocities[i] = Vector3.Zero; // NEW
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
            Velocities = velocities, // NEW
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