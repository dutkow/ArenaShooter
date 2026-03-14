using Godot;
using System.Collections.Generic;
using System;
using System.Linq;


public class ServerGame
{
    public static ServerGame Instance { get; private set; }

    public bool IsListenServer => Game.Instance.NetworkMode == NetworkMode.LISTEN_SERVER;

    // Client-server synchronization
    public ushort LastClientTickProcessedByServer;
    public Dictionary<byte, ushort> LastProcessedServerTicksByPlayerID = new();
    public Dictionary<byte, ushort> LastProcessedClientTicksByPlayerID = new();
    private readonly Dictionary<byte, SortedDictionary<ushort, ClientInputCommand>> _unprocessedClientInputs = new();

    private Dictionary<byte, ClientInputCommand> LastProcessedClientCommandsByPlayerID = new();

    // Snapshots
    private int _maxSnapshotHistory = 250;

    private readonly Dictionary<ushort, WorldSnapshot> _snapshotHistory = new();
    private readonly Queue<ushort> _snapshotQueue = new();

    // Bandwidth tracking
    private int _bytesSentThisPeriod = 0;


    public void Initialize()
    {
        Instance = this;

        ServerProjectileManager.Create();
    }

    public void Tick()
    {
        ProcessNextClientInputs();

        var newSnapshot = WorldSnapshot.Build();
        AddSnapshotToHistory(MatchState.Instance.CurrentTick, newSnapshot);

        foreach (var playerState in newSnapshot.PlayerStates)
        {
            // Instead of clearing flags, mark everything as changed
            playerState.CharacterPublicState.Flags =
                CharacterPublicFlags.POSITION_CHANGED |
                CharacterPublicFlags.VELOCITY_CHANGED |
                CharacterPublicFlags.ROTATION_CHANGED |
                CharacterPublicFlags.MOVEMENT_MODE_CHANGED |
                CharacterPublicFlags.EQUIPPED_WEAPON_CHANGED;

            playerState.CharacterPrivateState.Flags =
                CharacterPrivateFlags.HEALTH_CHANGED |
                CharacterPrivateFlags.MAX_HEALTH_CHANGED |
                CharacterPrivateFlags.ARMOR_CHANGED |
                CharacterPrivateFlags.MAX_ARMOR_CHANGED |
                CharacterPrivateFlags.WEAPONS_CHANGED |
                CharacterPrivateFlags.AMMO_CHANGED;
        }

        SendWorldSnapshotDeltas(newSnapshot); // in this we send the snapshot prior to updating the next client input. we could alternatively, process client inputs, then update?

    }

    public void ProcessNextClientInputs()
    {
        foreach (var kvp in MatchState.Instance.ConnectedPlayers)
        {
            byte playerID = kvp.Key;
            var playerState = kvp.Value;
            var character = playerState.Character;

            if (character == null)
            {
                continue;
            }

            if (!_unprocessedClientInputs.TryGetValue(playerID, out var queue))
            {
                queue = new SortedDictionary<ushort, ClientInputCommand>();
            }

            ClientInputCommand nextCommand = new();
            

            if (queue.Count > 0)
            {
                ushort clientTickOfNextCommand = queue.Keys.Min();
                nextCommand = queue[clientTickOfNextCommand];
                queue.Remove(clientTickOfNextCommand);

                LastProcessedClientTicksByPlayerID[playerID] = clientTickOfNextCommand;
                LastProcessedClientCommandsByPlayerID[playerID] = nextCommand;
            }
            else if (LastProcessedClientCommandsByPlayerID.TryGetValue(playerID, out var lastCommand))
            {
                nextCommand = lastCommand;
            }

            character.ProcessClientInput(nextCommand);
            _unprocessedClientInputs[playerID] = queue;
        }
    }

    private void SendWorldSnapshotDeltas(WorldSnapshot newSnapshot)
    {
        var snapshotDeltas = new Dictionary<uint, WorldSnapshot>();

        foreach (var kvp in LastProcessedServerTicksByPlayerID)
        {
            byte playerID = kvp.Key;
            ushort lastProcessedServerTick = kvp.Value;
            ushort lastProcessedClientTick = LastProcessedClientTicksByPlayerID[playerID];


            if (!NetworkSession.Instance.PlayerIDsToPeers.TryGetValue(playerID, out var peer))
            {
                GD.Print($"Peer not found. Peer ID: {playerID}. Peer: {peer}");
                continue;
            }

            if (snapshotDeltas.TryGetValue(lastProcessedServerTick, out var deltaSnapshot))
            {
                newSnapshot = deltaSnapshot;
            }
            else if (_snapshotHistory.TryGetValue((ushort)lastProcessedServerTick, out var previousSnapshot))
            {
                newSnapshot = newSnapshot.BuildDelta(previousSnapshot);
                snapshotDeltas[lastProcessedServerTick] = newSnapshot;
            }

            newSnapshot.LastProcessedClientTick = lastProcessedClientTick;

            var bytes = newSnapshot.WriteMessage();
            _bytesSentThisPeriod += bytes.Length;

            newSnapshot.AddPrivatePlayerInfo(playerID);

            NetworkSender.ToClient(peer, newSnapshot);
        }
    }

    private void AddSnapshotToHistory(ushort tick, WorldSnapshot snapshot)
    {
        _snapshotHistory[tick] = snapshot;
        _snapshotQueue.Enqueue(tick);

        while (_snapshotQueue.Count > _maxSnapshotHistory)
        {
            var oldTick = _snapshotQueue.Dequeue();
            _snapshotHistory.Remove(oldTick);
        }
    }

    public void ReceiveClientCommand(ClientCommand cmd, byte playerID)
    {
        if (!_unprocessedClientInputs.ContainsKey(playerID))
        {
            // Create a new sorted dictionary for this player
            _unprocessedClientInputs[playerID] = new SortedDictionary<ushort, ClientInputCommand>();
        }

        var queue = _unprocessedClientInputs[playerID];

        foreach (var clientCmd in cmd.Commands)
        {
            // Only add if we don’t already have this tick
            if (!queue.ContainsKey(clientCmd.ClientTick))
            {
                queue.Add(clientCmd.ClientTick, clientCmd);
            }
        }

        ServerProjectileManager.Instance.ReceiveClientCommand(cmd, playerID);
    }
    
    public WorldSnapshot GetWorldSnapshotByTick(ushort tick)
    {
        return _snapshotHistory[tick];
    }
}
