using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


[Flags]
public enum CharacterSnapshotFlags : ushort
{
    None = 0,
    Position = 1 << 0,
    Velocity = 1 << 1,
    Yaw = 1 << 2,
    AimPitch = 1 << 3,
    Health = 1 << 4,
    Shield = 1 << 5,
}

public struct ArenaCharacterSnapshot
{
    public byte PlayerID;
    public CharacterSnapshotFlags DirtyFlags;

    public Vector3 Position;
    public Vector3 Velocity;

    public float Yaw;
    public float AimPitch;

    public byte Health;
    public byte Shield;

    public ArenaCharacterSnapshot(byte playerID, Vector3 position, Vector3 velocity,
                                  float yaw, float aimPitch, byte health, byte shield,
                                  CharacterSnapshotFlags dirtyFlags)
    {
        PlayerID = playerID;
        Position = position;
        Velocity = velocity;
        Yaw = yaw;
        AimPitch = aimPitch;
        Health = health;
        Shield = shield;
        DirtyFlags = dirtyFlags;
    }

    // Compute DirtyFlags compared to a previous snapshot
    public static CharacterSnapshotFlags ComputeDirtyFlags(ArenaCharacterSnapshot current, ArenaCharacterSnapshot? previous)
    {
        if (previous == null)
            return CharacterSnapshotFlags.Position |
                   CharacterSnapshotFlags.Velocity |
                   CharacterSnapshotFlags.Yaw |
                   CharacterSnapshotFlags.AimPitch |
                   CharacterSnapshotFlags.Health |
                   CharacterSnapshotFlags.Shield;

        CharacterSnapshotFlags flags = CharacterSnapshotFlags.None;

        if (current.Position != previous.Value.Position) flags |= CharacterSnapshotFlags.Position;
        if (current.Velocity != previous.Value.Velocity) flags |= CharacterSnapshotFlags.Velocity;
        if (current.Yaw != previous.Value.Yaw) flags |= CharacterSnapshotFlags.Yaw;
        if (current.AimPitch != previous.Value.AimPitch) flags |= CharacterSnapshotFlags.AimPitch;
        if (current.Health != previous.Value.Health) flags |= CharacterSnapshotFlags.Health;
        if (current.Shield != previous.Value.Shield) flags |= CharacterSnapshotFlags.Shield;

        return flags;
    }
}

/// <summary>
/// Sent from Server → Client to sync the current tick’s player states
/// </summary>
public class WorldSnapshot : Message
{
    public WorldSnapshot()
    {
        MessageType = Msg.S2C_WORLD_SNAPSHOT;
    }

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

            Add((ushort)c.DirtyFlags);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Position)) Add(c.Position);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Velocity)) Add(c.Velocity);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Yaw)) Add(c.Yaw);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.AimPitch)) Add(c.AimPitch);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Health)) Add(c.Health);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Shield)) Add(c.Shield);
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

            Write((ushort)c.DirtyFlags);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Position)) Write(c.Position);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Velocity)) Write(c.Velocity);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Yaw)) Write(c.Yaw);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.AimPitch)) Write(c.AimPitch);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Health)) Write(c.Health);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.Shield)) Write(c.Shield);
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
            Read(out id);

            ushort rawFlags;
            Read(out rawFlags);
            var flags = (CharacterSnapshotFlags)rawFlags;

            Vector3 pos = default, vel = default;
            float yaw = 0f, pitch = 0f;
            byte health = 0, shield = 0;

            if (flags.HasFlag(CharacterSnapshotFlags.Position)) Read(out pos);
            if (flags.HasFlag(CharacterSnapshotFlags.Velocity)) Read(out vel);
            if (flags.HasFlag(CharacterSnapshotFlags.Yaw)) Read(out yaw);
            if (flags.HasFlag(CharacterSnapshotFlags.AimPitch)) Read(out pitch);
            if (flags.HasFlag(CharacterSnapshotFlags.Health)) Read(out health);
            if (flags.HasFlag(CharacterSnapshotFlags.Shield)) Read(out shield);

            Characters[i] = new ArenaCharacterSnapshot(id, pos, vel, yaw, pitch, health, shield, flags);
        }
    }

    public static void Send(ENetPacketPeer peer, WorldSnapshot snapshot)
    {
        //var msg = snapshot.Read();
        //NetworkSender.Broadcast(msg);
    }

    public static WorldSnapshot Build()
    {
        WorldSnapshot newSnapshot = new();

        var players = MatchState.Instance.ConnectedPlayers;
        var characters = new ArenaCharacterSnapshot[players.Count];
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

            CharacterSnapshotFlags allFlags = CharacterSnapshotFlags.Position |
                                                CharacterSnapshotFlags.Velocity |
                                                CharacterSnapshotFlags.Yaw |
                                                CharacterSnapshotFlags.AimPitch |
                                                CharacterSnapshotFlags.Health |
                                                CharacterSnapshotFlags.Shield;

            characters[i++] = new ArenaCharacterSnapshot(kvp.Key, pos, vel, yaw, pitch, health, shield, allFlags);
        }

        newSnapshot.Tick = MatchState.Instance.ServerTickManager.ServerTick;
        newSnapshot.Characters = characters;

        return newSnapshot;
    }


    // Build a delta snapshot compared to a previous snapshot
    public WorldSnapshot BuildDelta(WorldSnapshot previous)
    {
        if (previous == null)
            return this; // no previous snapshot, send full snapshot

        var deltaList = new List<ArenaCharacterSnapshot>();

        // Convert previous snapshot to dictionary for fast lookup
        var prevDict = previous.Characters.ToDictionary(c => c.PlayerID);

        foreach (var current in Characters)
        {
            CharacterSnapshotFlags flags = CharacterSnapshotFlags.None;

            if (prevDict.TryGetValue(current.PlayerID, out var old))
            {
                // compute which fields changed
                flags = ArenaCharacterSnapshot.ComputeDirtyFlags(current, old);

                // Only add if something actually changed
                if (flags != CharacterSnapshotFlags.None)
                {
                    var delta = new ArenaCharacterSnapshot(
                        current.PlayerID,
                        current.Position,
                        current.Velocity,
                        current.Yaw,
                        current.AimPitch,
                        current.Health,
                        current.Shield,
                        flags
                    );
                    deltaList.Add(delta);
                }
            }
            else
            {
                // New character not in previous snapshot — include all fields
                flags = ArenaCharacterSnapshot.ComputeDirtyFlags(current, null);
                var delta = new ArenaCharacterSnapshot(
                    current.PlayerID,
                    current.Position,
                    current.Velocity,
                    current.Yaw,
                    current.AimPitch,
                    current.Health,
                    current.Shield,
                    flags
                );
                deltaList.Add(delta);
            }
        }

        return new WorldSnapshot
        {
            Tick = Tick,
            Characters = deltaList.ToArray(),
            MessageType = Msg.S2C_WORLD_SNAPSHOT,
            ENetFlags = ENetFlags
        };
    }

    public ArenaCharacterSnapshot[] GetCharacterSnapshots() => Characters;
}