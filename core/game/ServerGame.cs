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

    }

    public void Tick()
    {
        ProcessNextClientInputs();

        var newSnapshot = WorldSnapshot.Build();
        SendWorldSnapshotDeltas(newSnapshot); // in this we send the snapshot prior to updating the next client input. we could alternatively, process client inputs, then update?
        AddSnapshotToHistory(MatchState.Instance.CurrentTick, newSnapshot);

    }

    public void ProcessNextClientInputs()
    {
        foreach (var kvp in MatchState.Instance.ConnectedPlayers)
        {
            byte playerID = kvp.Key;
            var playerState = kvp.Value;
            var pawn = playerState.Pawn;

            if (pawn == null)
            {
                continue;
            }

            // Skip local player on listen server
            if (ClientGame.Instance != null && ClientGame.Instance.LocalPlayerID == playerID)
            {
                continue;
            }

            if (!_unprocessedClientInputs.TryGetValue(playerID, out var queue))
            {
                queue = new SortedDictionary<ushort, ClientInputCommand>();
            }

            ClientInputCommand cmd = new();

            if (queue.Count > 0)
            {
                ushort tickToProcess = queue.Keys.Min();
                cmd = queue[tickToProcess];
                queue.Remove(tickToProcess);

                LastProcessedClientTicksByPlayerID[playerID] = tickToProcess;
                LastProcessedClientCommandsByPlayerID[playerID] = cmd;
            }
            else if (LastProcessedClientCommandsByPlayerID.TryGetValue(playerID, out var lastCommand))
            {
                cmd = lastCommand;
            }

            pawn.ProcessClientInput(cmd);
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
            _unprocessedClientInputs[playerID] = new SortedDictionary<ushort, ClientInputCommand>();
        }

        var queue = _unprocessedClientInputs[playerID];

        foreach(var inputCommand in cmd.Commands)
        {
            if(!queue.ContainsKey(cmd.ClientTick))
            {
                queue.Add(cmd.ClientTick, inputCommand);
            }
        }
    }
}
