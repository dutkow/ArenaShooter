using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sent from Server → Client to sync the current tick’s player states
/// </summary>
public class WorldSnapshot : Message
{
    public WorldSnapshot()
    {
        MessageType = Msg.S2C_WORLD_SNAPSHOT;
    }

    public ushort ServerTick;
    public ushort LastProcessedClientTick;
    public ulong PickupMask;

    public CharacterSnapshot[] Characters;
    public ProjectileSpawnData[] UnacknowledgedProjectiles;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(ServerTick);
        Add(LastProcessedClientTick);
        Add(PickupMask);

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

        Write(ServerTick);
        Write(LastProcessedClientTick);
        Write(PickupMask);

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

        Read(out ServerTick);
        Read(out LastProcessedClientTick);
        Read(out PickupMask);

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

            Vector3 pos = Vector3.Zero;
            Vector3 vel = Vector3.Zero;
            float yaw = 0f;
            float pitch = 0f;
            CharacterMoveMode moveMode = CharacterMoveMode.GROUNDED;
            Vector3 launchVelocity = Vector3.Zero;
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

        newSnapshot.ServerTick = MatchState.Instance.CurrentTick;
        newSnapshot.LastProcessedClientTick = 0;
        newSnapshot.PickupMask = PickupManager.Instance.PickupMask;

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
            CharacterMoveMode moveMode = CharacterMoveMode.GROUNDED;
            Vector3 launchVelocity = Vector3.Zero;

            byte health = 0;
            byte shield = 0;

            if (player.Pawn != null && player.Pawn is Character character)
            {
                pos = character.MovementComp.State.Position;
                vel = character.MovementComp.State.Velocity;
                yaw = character.MovementComp.State.Yaw;
                pitch = character.MovementComp.State.Pitch;
                moveMode = character.MovementComp.State.MoveMode;
                health = (byte)character.HealthComp.Health;
                shield = (byte)character.HealthComp.Shield;
            }

            CharacterSnapshotFlags allFlags = CharacterSnapshotFlags.POSITION |
                                                CharacterSnapshotFlags.VELOCITY |
                                                CharacterSnapshotFlags.YAW |
                                                CharacterSnapshotFlags.PITCH |
                                                CharacterSnapshotFlags.MOVE_MODE |
                                                CharacterSnapshotFlags.HEALTH |
                                                CharacterSnapshotFlags.SHIELD;

            characters[i++] = new CharacterSnapshot(kvp.Key, pos, vel, yaw, pitch, moveMode, health, shield, allFlags);
        }

        newSnapshot.Characters = characters;

        return newSnapshot;
    }


    // Build a delta snapshot compared to a previous snapshot
    public WorldSnapshot BuildDelta(WorldSnapshot previous)
    {
        if (previous == null)
        {
            return this;
        }

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
            ServerTick = MatchState.Instance.CurrentTick,
            LastProcessedClientTick = LastProcessedClientTick,
            PickupMask = PickupManager.Instance.PickupMask,
            Characters = deltaList.ToArray(),
            MessageType = Msg.S2C_WORLD_SNAPSHOT,
        };
    }

    public CharacterSnapshot[] GetCharacterSnapshots() => Characters;
}