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

    public PlayerState[] PlayerStates;
    byte ReceivingPlayerID;

    public ProjectileSpawnData[] UnacknowledgedRemoteProjectiles;
    public ProjectileState[] UnacknowledgedProjectileStates;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(ServerTick);
        Add(LastProcessedClientTick);
        Add(PickupMask);

        // Player States
        byte playerStatesCount = (byte)PlayerStates.Length;
        if(playerStatesCount > 0)
        {
            Add(playerStatesCount);
            foreach(var playerState in PlayerStates)
            {
                playerState.Add(this, ReceivingPlayerID);
            }
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

        // Player States
        byte playerStatesCount = (byte)PlayerStates.Length;
        if (playerStatesCount > 0)
        {
            Write(playerStatesCount);
            foreach (var playerState in PlayerStates)
            {
                playerState.Write(this, ReceivingPlayerID);
            }
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

        // Player States
        byte playerStatesCount;
        Read(out playerStatesCount);
        if (playerStatesCount > 0)
        {
            foreach (var playerState in PlayerStates)
            {
                playerState.Read(this, ReceivingPlayerID);
            }
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
        NetworkSender.Broadcast(snapshot);
    }

 

    // REFACTOR

    public static WorldSnapshot BuildNew()
    {
        WorldSnapshot newSnapshot = new();

        newSnapshot.ServerTick = MatchState.Instance.CurrentTick;
        newSnapshot.LastProcessedClientTick = 0;
        newSnapshot.PickupMask = PickupManager.Instance.PickupMask;

        // Characters (existing code)
        var playerStates = MatchState.Instance.NewConnectedPlayers.Values;

        return newSnapshot;
    }



    // Build a delta snapshot compared to a previous snapshot
    public WorldSnapshot BuildDelta(WorldSnapshot previous)
    {
        if (previous == null)
            return this;

        var deltaList = new List<PlayerState>();

        var prevDict = previous.PlayerStates.ToDictionary(p => p.PlayerID);

        foreach (var current in PlayerStates)
        {
            if (prevDict.TryGetValue(current.PlayerID, out var old))
            {
                PlayerStateFlags flags = 0;

                if (current.Kills != old.Kills)
                    flags |= PlayerStateFlags.KILLS_CHANGED;

                if (current.Deaths != old.Deaths)
                    flags |= PlayerStateFlags.DEATHS_CHANGED;

                if (current.Ping != old.Ping)
                    flags |= PlayerStateFlags.PING_CHANGED;

                if (current.IsAlive != old.IsAlive)
                    flags |= PlayerStateFlags.IS_ALIVE_CHANGED;

                if (flags != 0)
                {
                    var delta = new PlayerState()
                    {
                        PlayerID = current.PlayerID,
                        Flags = flags,
                        Kills = current.Kills,
                        Deaths = current.Deaths,
                        Ping = current.Ping,
                        IsAlive = current.IsAlive
                    };

                    deltaList.Add(delta);
                }
            }
            else
            {
                // new player → send full state
                var delta = new PlayerState()
                {
                    Flags =
                        PlayerStateFlags.KILLS_CHANGED |
                        PlayerStateFlags.DEATHS_CHANGED |
                        PlayerStateFlags.PING_CHANGED |
                        PlayerStateFlags.IS_ALIVE_CHANGED,

                    PlayerID = current.PlayerID,
                    Kills = current.Kills,
                    Deaths = current.Deaths,
                    Ping = current.Ping,
                    IsAlive = current.IsAlive
                };

                deltaList.Add(delta);
            }
        }

        return new WorldSnapshot
        {
            ENetFlags = ENetPacketFlags.UnreliableFragment,
            ServerTick = ServerTick,
            LastProcessedClientTick = LastProcessedClientTick,
            PickupMask = PickupMask,
            PlayerStates = deltaList.ToArray(),
            MessageType = Msg.S2C_WORLD_SNAPSHOT
        };
    }

    public void AddPrivatePlayerInfo(byte playerID)
    {
        ServerProjectileManager.Instance.AddInfoToWorldSnapshot(this, playerID);
    }
}