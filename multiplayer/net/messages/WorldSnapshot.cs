using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


[Flags]
public enum CharacterSnapshotFlags : ushort
{
    NONE = 0,
    POSITION = 1 << 0,
    VELOCITY = 1 << 1,
    YAW = 1 << 2,
    PITCH = 1 << 3,
    MOVE_MODE = 1 << 4,
    HEALTH = 1 << 5,
    SHIELD = 1 << 6,
}

public struct CharacterSnapshot
{
    public byte PlayerID;
    public CharacterSnapshotFlags DirtyFlags;

    public Vector3 Position;
    public Vector3 Velocity;
    public float Yaw;
    public float Pitch;
    public CharacterMoveMode MoveMode;
    public byte Health;
    public byte Shield;

    public CharacterMoveState GetMoveState()
    {
        CharacterMoveState state = new();
        state.Position = Position;
        state.Velocity = Velocity;
        state.Yaw = Yaw;
        state.Pitch = Pitch;
        state.MoveMode = MoveMode; // populate
        return state;
    }

    public CharacterSnapshot(byte playerID, Vector3 position, Vector3 velocity,
                             float yaw, float pitch, CharacterMoveMode moveMode,
                             byte health, byte shield,
                             CharacterSnapshotFlags dirtyFlags)
    {
        PlayerID = playerID;
        Position = position;
        Velocity = velocity;
        Yaw = yaw;
        Pitch = pitch;
        MoveMode = moveMode;
        Health = health;
        Shield = shield;
        DirtyFlags = dirtyFlags;
    }

    public static CharacterSnapshotFlags ComputeDirtyFlags(CharacterSnapshot current, CharacterSnapshot? previous)
    {
        if (previous == null)
            return CharacterSnapshotFlags.POSITION |
                   CharacterSnapshotFlags.VELOCITY |
                   CharacterSnapshotFlags.YAW |
                   CharacterSnapshotFlags.PITCH |
                   CharacterSnapshotFlags.MOVE_MODE |
                   CharacterSnapshotFlags.HEALTH |
                   CharacterSnapshotFlags.SHIELD;

        CharacterSnapshotFlags flags = CharacterSnapshotFlags.NONE;

        if (current.Position != previous.Value.Position) flags |= CharacterSnapshotFlags.POSITION;
        if (current.Velocity != previous.Value.Velocity) flags |= CharacterSnapshotFlags.VELOCITY;
        if (current.Yaw != previous.Value.Yaw) flags |= CharacterSnapshotFlags.YAW;
        if (current.Pitch != previous.Value.Pitch) flags |= CharacterSnapshotFlags.PITCH;
        if (current.MoveMode != previous.Value.MoveMode) flags |= CharacterSnapshotFlags.MOVE_MODE;
        if (current.Health != previous.Value.Health) flags |= CharacterSnapshotFlags.HEALTH;
        if (current.Shield != previous.Value.Shield) flags |= CharacterSnapshotFlags.SHIELD;

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

    public ushort LastProcessedClientTick;

    // Use an array of character snapshots instead of parallel arrays
    public CharacterSnapshot[] Characters;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(LastProcessedClientTick);

        Add(Characters.Length);
        for (int i = 0; i < Characters.Length; i++)
        {
            var c = Characters[i];
            Add(c.PlayerID);

            Add((ushort)c.DirtyFlags);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.POSITION)) Add(c.Position);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.VELOCITY)) Add(c.Velocity);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.YAW)) Add(c.Yaw);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.PITCH)) Add(c.Pitch);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.MOVE_MODE)) Add(c.MoveMode);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.HEALTH)) Add(c.Health);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.SHIELD)) Add(c.Shield);
        }
        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(LastProcessedClientTick);

        Write(Characters.Length);
        for (int i = 0; i < Characters.Length; i++)
        {
            var c = Characters[i];
            Write(c.PlayerID);

            Write((ushort)c.DirtyFlags);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.POSITION)) Write(c.Position);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.VELOCITY)) Write(c.Velocity);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.YAW)) Write(c.Yaw);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.PITCH)) Write(c.Pitch);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.MOVE_MODE)) Write(c.MoveMode);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.HEALTH)) Write(c.Health);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.SHIELD)) Write(c.Shield);
        }

        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out LastProcessedClientTick);

        int count;
        Read(out count);
        Characters = new CharacterSnapshot[count];

        for (int i = 0; i < count; i++)
        {
            byte id;
            Read(out id);

            ushort rawFlags;
            Read(out rawFlags);
            var flags = (CharacterSnapshotFlags)rawFlags;

            Vector3 pos = default;
            Vector3 vel = default;
            float yaw = 0f;
            float pitch = 0f;
            CharacterMoveMode moveMode = default;
            byte health = 0;
            byte shield = 0;

            if (flags.HasFlag(CharacterSnapshotFlags.POSITION)) Read(out pos);
            if (flags.HasFlag(CharacterSnapshotFlags.VELOCITY)) Read(out vel);
            if (flags.HasFlag(CharacterSnapshotFlags.YAW)) Read(out yaw);
            if (flags.HasFlag(CharacterSnapshotFlags.PITCH)) Read(out pitch);
            if (flags.HasFlag(CharacterSnapshotFlags.MOVE_MODE)) Read(out moveMode);
            if (flags.HasFlag(CharacterSnapshotFlags.HEALTH)) Read(out health);
            if (flags.HasFlag(CharacterSnapshotFlags.SHIELD)) Read(out shield);

            Characters[i] = new CharacterSnapshot(id, pos, vel, yaw, pitch, moveMode, health, shield, flags);
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
        var characters = new CharacterSnapshot[players.Count];
        int i = 0;

        foreach (var kvp in players)
        {
            var player = kvp.Value;


            Vector3 pos = Vector3.Zero;
            Vector3 vel = Vector3.Zero;
            float yaw = 0f;
            float pitch = 0f;
            CharacterMoveMode moveMode = default;

            byte health = 0;
            byte shield = 0;

            if (player.Pawn != null && player.Pawn is Character character)
            {
                pos = character.MovementComp.State.Position;
                vel = character.MovementComp.State.Velocity;
                yaw = character.MovementComp.State.Yaw;
                pitch = character.MovementComp.State.Pitch;
                health = (byte)character.HealthComp.Health;
                shield = (byte)character.HealthComp.Shield;
            }

            CharacterSnapshotFlags allFlags = CharacterSnapshotFlags.POSITION |
                                                CharacterSnapshotFlags.VELOCITY |
                                                CharacterSnapshotFlags.YAW |
                                                CharacterSnapshotFlags.PITCH |
                                                CharacterSnapshotFlags.HEALTH |
                                                CharacterSnapshotFlags.SHIELD;

            characters[i++] = new CharacterSnapshot(kvp.Key, pos, vel, yaw, pitch, moveMode, health, shield, allFlags);
        }

        newSnapshot.LastProcessedClientTick = MatchState.Instance.ServerTickManager.ServerTick;
        newSnapshot.Characters = characters;

        return newSnapshot;
    }


    // Build a delta snapshot compared to a previous snapshot
    public WorldSnapshot BuildDelta(WorldSnapshot previous)
    {
        if (previous == null)
            return this; // no previous snapshot, send full snapshot

        var deltaList = new List<CharacterSnapshot>();

        // Convert previous snapshot to dictionary for fast lookup
        var prevDict = previous.Characters.ToDictionary(c => c.PlayerID);

        foreach (var current in Characters)
        {
            CharacterSnapshotFlags flags = CharacterSnapshotFlags.NONE;

            if (prevDict.TryGetValue(current.PlayerID, out var old))
            {
                // compute which fields changed
                flags = CharacterSnapshot.ComputeDirtyFlags(current, old);

                // Only add if something actually changed
                if (flags != CharacterSnapshotFlags.NONE)
                {
                    var delta = new CharacterSnapshot(
                        current.PlayerID,
                        current.Position,
                        current.Velocity,
                        current.Yaw,
                        current.Pitch,
                        current.MoveMode,
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
                flags = CharacterSnapshot.ComputeDirtyFlags(current, null);
                var delta = new CharacterSnapshot(
                    current.PlayerID,
                    current.Position,
                    current.Velocity,
                    current.Yaw,
                    current.Pitch,
                    current.MoveMode,
                    current.Health,
                    current.Shield,
                    flags
                );
                deltaList.Add(delta);
            }
        }

        return new WorldSnapshot
        {
            ENetFlags = ENetPacketFlags.UnreliableFragment,
            LastProcessedClientTick = LastProcessedClientTick,
            Characters = deltaList.ToArray(),
            MessageType = Msg.S2C_WORLD_SNAPSHOT,
        };
    }

    public CharacterSnapshot[] GetCharacterSnapshots() => Characters;
}