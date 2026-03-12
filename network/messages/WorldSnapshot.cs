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
    public PublicPlayerState[] PublicPlayerStates; // refactor
    public ProjectileSpawnData[] UnacknowledgedRemoteProjectiles;
    public ProjectileState[] UnacknowledgedProjectileStates;
    public ProjectileSpawnData[] UnacknowledgedPredictedProjectiles;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(ServerTick);
        Add(LastProcessedClientTick);
        Add(PickupMask);

        byte charactersLength = (byte)Characters.Length;
        Add(charactersLength);
        for (int i = 0; i < Characters.Length; i++)
        {
            var c = Characters[i];
            Add(c.PlayerID);

            Add((ushort)c.DirtyFlags);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.POSITION)) Add(c.Position);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.VELOCITY)) Add(c.Velocity);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.YAW)) Add(c.Yaw);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.PITCH)) Add(c.Pitch);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.MOVE_MODE)) AddEnum(c.MoveMode);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.HEALTH)) Add(c.Health);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.SHIELD)) Add(c.Shield);
        }

        ushort unackedCount = (ushort)(UnacknowledgedRemoteProjectiles?.Length ?? 0);
        Add(unackedCount);
        for (int i = 0; i < unackedCount; i++)
        {
            var proj = UnacknowledgedRemoteProjectiles[i];
            Add(proj.ProjectileID);
            Add(proj.ownerPlayerID);
            Add((byte)proj.Type);
            Add(proj.ServerTickOnSpawn);
            Add(proj.SpawnLocation);
            Add(proj.SpawnDirection);
        }

        // --- Unacknowledged Projectile State Changes ---
        ushort stateChangesCount = (ushort)(UnacknowledgedProjectileStates?.Length ?? 0);
        Add(stateChangesCount);
        for (int i = 0; i < stateChangesCount; i++)
        {
            var change = UnacknowledgedProjectileStates[i];
            Add(change.ProjectileID);
        }

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(ServerTick);
        Write(LastProcessedClientTick);
        Write(PickupMask);

        byte charactersLength = (byte)Characters.Length;
        Write(charactersLength);
        for (int i = 0; i < Characters.Length; i++)
        {
            var c = Characters[i];
            Write(c.PlayerID);

            Write((ushort)c.DirtyFlags);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.POSITION)) Write(c.Position);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.VELOCITY)) Write(c.Velocity);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.YAW)) Write(c.Yaw);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.PITCH)) Write(c.Pitch);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.MOVE_MODE)) WriteEnum(c.MoveMode);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.HEALTH)) Write(c.Health);
            if (c.DirtyFlags.HasFlag(CharacterSnapshotFlags.SHIELD)) Write(c.Shield);
        }

        // **Write unacked projectiles**
        ushort unackedCount = (ushort)(UnacknowledgedRemoteProjectiles?.Length ?? 0);
        Write(unackedCount);
        for (int i = 0; i < unackedCount; i++)
        {
            var proj = UnacknowledgedRemoteProjectiles[i];
            Write(proj.ProjectileID);
            Write(proj.ownerPlayerID);
            Write((byte)proj.Type);
            Write(proj.ServerTickOnSpawn);
            Write(proj.SpawnLocation);
            Write(proj.SpawnDirection);
        }

        // --- Unacknowledged Projectile State Changes ---
        ushort stateChangesCount = (ushort)(UnacknowledgedProjectileStates?.Length ?? 0);
        Write(stateChangesCount);
        for (int i = 0; i < stateChangesCount; i++)
        {
            var change = UnacknowledgedProjectileStates[i];
            Write(change.ProjectileID);
        }


        return _data;
    }

    public override void ReadMessage(byte[] data)
    {
        base.ReadMessage(data);

        Read(out ServerTick);
        Read(out LastProcessedClientTick);
        Read(out PickupMask);

        byte count;
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
            if (flags.HasFlag(CharacterSnapshotFlags.MOVE_MODE)) ReadEnum(out moveMode);
            if (flags.HasFlag(CharacterSnapshotFlags.HEALTH)) Read(out health);
            if (flags.HasFlag(CharacterSnapshotFlags.SHIELD)) Read(out shield);

            Characters[i] = new CharacterSnapshot(id, pos, vel, yaw, pitch, moveMode, health, shield, flags);
        }

        // **Read unacked projectiles**
        Read(out ushort unackedCount);
        UnacknowledgedRemoteProjectiles = new ProjectileSpawnData[unackedCount];
        for (int i = 0; i < unackedCount; i++)
        {
            var proj = new ProjectileSpawnData();
            Read(out proj.ProjectileID);
            Read(out proj.ownerPlayerID);
            Read(out byte type);
            proj.Type = (ProjectileType)type;
            Read(out proj.ServerTickOnSpawn);
            Read(out proj.SpawnLocation);
            Read(out proj.SpawnDirection);

            UnacknowledgedRemoteProjectiles[i] = proj;
        }

        // --- Unacknowledged Projectile State Changes ---
        Read(out ushort stateChangesCount);
        UnacknowledgedProjectileStates = new ProjectileState[stateChangesCount];
        for (int i = 0; i < stateChangesCount; i++)
        {
            var change = new ProjectileState();
            Read(out change.ProjectileID);
            UnacknowledgedProjectileStates[i] = change;
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

        // Characters (existing code)
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

    // REFACTOR

    public static WorldSnapshot BuildNew()
    {
        WorldSnapshot newSnapshot = new();

        newSnapshot.ServerTick = MatchState.Instance.CurrentTick;
        newSnapshot.LastProcessedClientTick = 0;
        newSnapshot.PickupMask = PickupManager.Instance.PickupMask;

        // Characters (existing code)
        var playerStates = MatchState.Instance.NewConnectedPlayers;
        var publicPlayerStates = new PublicPlayerState[playerStates.Count];

        int i = 0;
        foreach (var kvp in playerStates)
        {
            var newPlayerState = kvp.Value;
            Vector3 pos = Vector3.Zero;
            Vector3 vel = Vector3.Zero;
            float yaw = 0f;
            float pitch = 0f;
            CharacterMoveMode moveMode = CharacterMoveMode.GROUNDED;
            byte health = 0;
            byte shield = 0;

            Character character = newPlayerState.PublicState.Character;
            if (character != null)
            {
                pos = character.MovementComp.State.Position;
                vel = character.MovementComp.State.Velocity;
                yaw = character.MovementComp.State.Yaw;
                pitch = character.MovementComp.State.Pitch;
                moveMode = character.MovementComp.State.MoveMode;
                health = (byte)character.HealthComp.Health;
                shield = (byte)character.HealthComp.Shield;
            }

            PublicPlayerFlags allFlags = PublicPlayerFlags.POSITION |
                                              PublicPlayerFlags.VELOCITY |
                                              PublicPlayerFlags.YAW |
                                              PublicPlayerFlags.PITCH |
                                              PublicPlayerFlags.MOVE_MODE;


            newPlayerState.PublicState.Flags = allFlags;
            publicPlayerStates[i] = newPlayerState.PublicState;

            i++;
        }

        newSnapshot.PublicPlayerStates = publicPlayerStates;

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


    public WorldSnapshot BuildDeltaNew(WorldSnapshot previous)
    {
        if (previous == null)
        {
            return this;
        }

        var deltaList = new List<PublicPlayerState>();

        // Convert previous snapshot to dictionary for fast lookup
        var prevDict = previous.PublicPlayerStates.ToDictionary(p => p.PlayerID);

        foreach (var current in PublicPlayerStates)
        {
            PublicPlayerFlags flags = PublicPlayerFlags.NONE;

            if (prevDict.TryGetValue(current.PlayerID, out var old))
            {
                // compute which fields changed
                flags = PublicPlayerState.ComputeDirtyFlags(current, old);

                // Only add if something actually changed
                if (flags != PublicPlayerFlags.NONE)
                {
                    var delta = new PublicPlayerState
                    {
                        PlayerID = current.PlayerID,
                        Flags = flags,

                        Kills = current.Kills,
                        Deaths = current.Deaths,
                        IsAlive = current.IsAlive,

                        Position = current.Position,
                        Velocity = current.Velocity,
                        Yaw = current.Yaw,
                        Pitch = current.Pitch,
                        MoveMode = current.MoveMode,

                        EquippedWeapon = current.EquippedWeapon
                    };

                    deltaList.Add(delta);
                }
            }
            else
            {
                // New player not in previous snapshot — include all fields
                flags = PublicPlayerState.ComputeDirtyFlags(current, null);

                var delta = new PublicPlayerState
                {
                    PlayerID = current.PlayerID,
                    Flags = flags,

                    Kills = current.Kills,
                    Deaths = current.Deaths,
                    IsAlive = current.IsAlive,

                    Position = current.Position,
                    Velocity = current.Velocity,
                    Yaw = current.Yaw,
                    Pitch = current.Pitch,
                    MoveMode = current.MoveMode,

                    EquippedWeapon = current.EquippedWeapon
                };

                deltaList.Add(delta);
            }
        }

        return new WorldSnapshot
        {
            ENetFlags = ENetPacketFlags.UnreliableFragment,
            ServerTick = MatchState.Instance.CurrentTick,
            LastProcessedClientTick = LastProcessedClientTick,
            PickupMask = PickupManager.Instance.PickupMask,
            PublicPlayerStates = deltaList.ToArray(),
            MessageType = Msg.S2C_WORLD_SNAPSHOT,
        };
    }


    public CharacterSnapshot[] GetCharacterSnapshots() => Characters;

    public void AddPrivatePlayerInfo(byte playerID)
    {
        ServerProjectileManager.Instance.AddInfoToWorldSnapshot(this, playerID);
    }
}