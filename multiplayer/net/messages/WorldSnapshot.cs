using Godot;
using System;

/// <summary>
/// Snapshot of a single character in the world
/// </summary>
public struct ArenaCharacterSnapshot
{
    public byte PlayerID;
    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float AimPitch;
    public byte Health;
    public byte Shield;

    public ArenaCharacterSnapshot(byte playerID, Vector3 position, Vector3 velocity,
                                  float yaw, float aimPitch, byte health, byte shield)
    {
        PlayerID = playerID;
        Position = position;
        Velocity = velocity;
        Yaw = yaw;
        AimPitch = aimPitch;
        Health = health;
        Shield = shield;
    }
}

/// <summary>
/// Sent from Server → Client to sync the current tick’s player states
/// </summary>
public class WorldSnapshot : Message
{
    public ushort Tick;

    // Use an array of character snapshots instead of parallel arrays
    public ArenaCharacterSnapshot[] Characters;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(Tick);

        Add(Characters.Length);
        for (int i = 0; i < Characters.Length; i++)
        {
            var c = Characters[i];
            Add(c.PlayerID);
            Add(c.Position);
            Add(c.Velocity);
            Add(c.Yaw);
            Add(c.AimPitch);
            Add(c.Health);
            Add(c.Shield);
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(Tick);

        Write(Characters.Length);
        for (int i = 0; i < Characters.Length; i++)
        {
            var c = Characters[i];
            Write(c.PlayerID);
            Write(c.Position);
            Write(c.Velocity);
            Write(c.Yaw);
            Write(c.AimPitch);
            Write(c.Health);
            Write(c.Shield);
        }

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out Tick);

        int count;
        Read(out count);
        Characters = new ArenaCharacterSnapshot[count];

        for (int i = 0; i < count; i++)
        {
            byte id;
            Vector3 pos, vel;
            float yaw, pitch;
            byte health, shield;

            Read(out id);
            Read(out pos);
            Read(out vel);
            Read(out yaw);
            Read(out pitch);
            Read(out health);
            Read(out shield);

            Characters[i] = new ArenaCharacterSnapshot(id, pos, vel, yaw, pitch, health, shield);
        }
    }

    public static void Send()
    {
        var players = MatchState.Instance.ConnectedPlayers;
        int count = players.Count;

        var characters = new ArenaCharacterSnapshot[count];
        int i = 0;

        foreach (var kvp in players)
        {
            var player = kvp.Value;
            byte health = 0, shield = 0;
            Vector3 pos = Vector3.Zero, vel = Vector3.Zero;
            float yaw = 0f, pitch = 0f;

            if (player.Character != null)
            {
                pos = player.Character.GlobalPosition;
                vel = player.Character.Velocity;
                yaw = player.Character.Yaw;
                pitch = player.Character.AimPitch;
                health = (byte)player.Character.HealthComponent.Health;
                shield = (byte)player.Character.HealthComponent.Shield;
            }

            characters[i++] = new ArenaCharacterSnapshot(kvp.Key, pos, vel, yaw, pitch, health, shield);
        }

        var msg = new WorldSnapshot()
        {
            MessageType = Msg.S2C_WORLD_SNAPSHOT,
            ENetFlags = ENetPacketFlags.UnreliableFragment,
            Tick = MatchState.Instance.CurrentTick,
            Characters = characters
        };

        NetworkSender.Broadcast(msg);
    }

    public ArenaCharacterSnapshot[] GetCharacterSnapshots() => Characters;
}