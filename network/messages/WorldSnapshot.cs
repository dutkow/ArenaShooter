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

    public PlayerStateOld[] PlayerStates;
    byte ReceivingPlayerID;

    public ProjectileSpawnData[] UnacknowledgedSpawnedProjectiles;
    public ProjectileState[] UnacknowledgedProjectileStates;

    protected override int BufferSize()
    {
        base.BufferSize();

        Add(ServerTick);
        Add(LastProcessedClientTick);
        Add(PickupMask);

        // Player States
        byte playerStatesCount = (byte)(PlayerStates?.Length ?? 0);
        Add(playerStatesCount);
        if (playerStatesCount > 0)
        {
            foreach(var playerState in PlayerStates)
            {
                playerState.Add(this, ReceivingPlayerID, true);
            }
        }    

        /*
        // Unacknowledged spawned projectiles
        ushort unackedCount = (ushort)(UnacknowledgedSpawnedProjectiles?.Length ?? 0);
        Add(unackedCount);
        for (int i = 0; i < unackedCount; i++)
        {
            var proj = UnacknowledgedSpawnedProjectiles[i];
            Add(proj.ProjectileID);
            Add(proj.ownerPlayerID);
            Add((byte)proj.Type);
            Add(proj.ServerTickOnSpawn);
            Add(proj.SpawnLocation);
            Add(proj.SpawnDirection);
        }

        // Unacknowledged projectile state changes
        ushort stateChangesCount = (ushort)(UnacknowledgedProjectileStates?.Length ?? 0);
        Add(stateChangesCount);
        for (int i = 0; i < stateChangesCount; i++)
        {
            var change = UnacknowledgedProjectileStates[i];
            Add(change.ProjectileID);
        }*/

        return _dataSize;
    }

    public override byte[] WriteMessage()
    {
        base.WriteMessage();

        Write(ServerTick);
        Write(LastProcessedClientTick);
        Write(PickupMask);

        // Player States
        byte playerStatesCount = (byte)(PlayerStates?.Length ?? 0);
        Write(playerStatesCount);

        if (playerStatesCount > 0)
        {
            foreach (var playerState in PlayerStates)
            {
                playerState.Write(this, ReceivingPlayerID);
            }
        }

        /*
        // Unacknowledged spawned projectiles
        ushort unackedCount = (ushort)(UnacknowledgedSpawnedProjectiles?.Length ?? 0);
        Write(unackedCount);
        for (int i = 0; i < unackedCount; i++)
        {
            var proj = UnacknowledgedSpawnedProjectiles[i];
            Write(proj.ProjectileID);
            Write(proj.ownerPlayerID);
            Write((byte)proj.Type);
            Write(proj.ServerTickOnSpawn);
            Write(proj.SpawnLocation);
            Write(proj.SpawnDirection);
        }

        // Unacknowledged projectile state changes
        ushort stateChangesCount = (ushort)(UnacknowledgedProjectileStates?.Length ?? 0);
        Write(stateChangesCount);
        for (int i = 0; i < stateChangesCount; i++)
        {
            var change = UnacknowledgedProjectileStates[i];
            Write(change.ProjectileID);
        }*/


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
            PlayerStates = new PlayerStateOld[playerStatesCount];

            for(int i = 0; i < playerStatesCount; ++i)
            {
                PlayerStateOld playerState = new();
                PlayerStates[i] = playerState;
                playerState.Read(this, ReceivingPlayerID);
            }
        }

        // Unacknowledged spawned projectiles
        /*
        Read(out ushort unackedCount);
        UnacknowledgedSpawnedProjectiles = new ProjectileSpawnData[unackedCount];
        for (int i = 0; i < unackedCount; ++i)
        {
            var proj = new ProjectileSpawnData();
            Read(out proj.ProjectileID);
            Read(out proj.ownerPlayerID);
            Read(out byte type);
            proj.Type = (ProjectileType)type;
            Read(out proj.ServerTickOnSpawn);
            Read(out proj.SpawnLocation);
            Read(out proj.SpawnDirection);

            UnacknowledgedSpawnedProjectiles[i] = proj;
        }

        // Unacknowledged projectile state changes
        Read(out ushort stateChangesCount);
        UnacknowledgedProjectileStates = new ProjectileState[stateChangesCount];
        for (int i = 0; i < stateChangesCount; ++i)
        {
            var change = new ProjectileState();
            Read(out change.ProjectileID);
            UnacknowledgedProjectileStates[i] = change;
        }*/
    }

    public static void Send(ENetPacketPeer peer, WorldSnapshot snapshot)
    {
        //var msg = snapshot.Read();
        NetworkSender.Broadcast(snapshot);
    }

 
    public static WorldSnapshot Build()
    {
        WorldSnapshot newSnapshot = new();

        newSnapshot.ServerTick = MatchState.Instance.CurrentTick;
        newSnapshot.LastProcessedClientTick = 0;
        newSnapshot.PickupMask = PickupManager.Instance.PickupMask;

        newSnapshot.PlayerStates = MatchState.Instance.ConnectedPlayers.Values.ToArray();


        return newSnapshot;
    }

    // Build a delta snapshot compared to a previous snapshot
    public WorldSnapshot BuildDelta(WorldSnapshot previous)
    {
        return previous;
        /*
        if (previous == null)
            return this;

        var deltaList = new List<PlayerStateOld>();

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
                    var delta = new PlayerStateOld()
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
                var delta = new PlayerStateOld()
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
        };*/
    }

    public void AddPrivatePlayerInfo(byte playerID)
    {
        ServerProjectileManager.Instance.AddInfoToWorldSnapshot(this, playerID);
    }
}