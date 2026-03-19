using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;


public class ServerGame()
{
    const int MAX_PLAYERS = 16;
    private bool _isServerFull => MatchState.Instance.ConnectedPlayers.Count >= MAX_PLAYERS;
    public static ServerGame Instance { get; private set; }

    public bool IsListenServer => NetworkManager.Instance.NetworkMode == NetworkMode.LISTEN_SERVER;

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


    public static void Initialize()
    {
        if(Instance != null)
        {
            GD.Print($"Server game already exists!");
        }

        Instance = new ServerGame();

        Instance.InitMessageHandlers();
    }

    public void PostLoad()
    {
        CommandConsole.Instance.AddConsoleLogEntry($"=== Initializing Server Game ===");
        ServerProjectileManager.Initialize();
    }

    public void Tick()
    {
        ProcessNextClientInputCommands();

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

    public void ProcessNextClientInputCommands()
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

            ClientInputCommand nextInputCommand = new();
            

            if (queue.Count > 0)
            {
                ushort clientTickOfNextInputCommand = queue.Keys.Min();
                nextInputCommand = queue[clientTickOfNextInputCommand];
                queue.Remove(clientTickOfNextInputCommand);

                LastProcessedClientTicksByPlayerID[playerID] = clientTickOfNextInputCommand;
                LastProcessedClientCommandsByPlayerID[playerID] = nextInputCommand;
            }
            else if (LastProcessedClientCommandsByPlayerID.TryGetValue(playerID, out var lastCommand))
            {
                nextInputCommand = lastCommand;
            }

            character.ServerProcessNextClientInput(nextInputCommand);
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

            if(IsListenServer && ClientGame.Instance.LocalPlayerID == playerID)
            {
                continue;
            }

            if (!NetworkServer.Instance.PeersByPlayerID.TryGetValue(playerID, out var peer))
            {
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

    public void ApplyClientCommand(byte playerID, ClientCommand cmd)
    {
        if (!_unprocessedClientInputs.ContainsKey(playerID))
        {
            _unprocessedClientInputs[playerID] = new SortedDictionary<ushort, ClientInputCommand>();
        }

        var queue = _unprocessedClientInputs[playerID];

        foreach (var clientCmd in cmd.Commands)
        {
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

    public void HandleClientMessage(ENetPacketPeer peer, Msg type, byte[] payload)
    {
        if (_messageHandlers.TryGetValue(type, out var handler))
        {
            handler(peer, payload);
        }
        else
        {
            GD.Print($"Unknown client message type: {type}");
        }
    }

    public void HandleConnectionRequest(ENetPacketPeer peer, ConnectionRequest connectionRequest)
    {
        GD.Print($"handle connection request ran.");

        if (_isServerFull)
        {
            GD.Print($"server is full denied");

            ConnectionDenied.Send(peer, "Server full");
            return;
        }

        byte playerID = GetNextAvailablePlayerID();
        NetUtils.SetPeerPlayerID(peer, playerID);

        NetworkServer.Instance.PeersByPlayerID[playerID] = peer;

        ConnectionAccepted.Send(peer, playerID);
    }

    public void HandleClientLoaded(ENetPacketPeer peer, ClientLoaded clientLoaded)
    {
        byte playerID = NetUtils.GetPeerPlayerID(peer);
        NetworkPeer.Instance.ReadyPeers.Add(peer);

        GD.Print($"handle client loaded ran. adding last processed tick 0 for player id: {playerID}");

        ServerGame.Instance.LastProcessedServerTicksByPlayerID[playerID] = 0;
        ServerGame.Instance.LastProcessedClientTicksByPlayerID[playerID] = 0;

        ApplyClientLoaded(new PlayerInfo(playerID, clientLoaded.ClientInfo.PlayerName));

        SpawnManager.Instance.ServerSpawnPlayer(playerID);

        InitialMatchState.Send(peer);
    }

    public void ApplyClientLoaded(PlayerInfo playerInfo)
    {
        MatchState.Instance.AddPlayer(playerInfo);
    }

    public void HandleClientCommand(ENetPacketPeer peer, ClientCommand clientCommand)
    {
        ApplyClientCommand(NetUtils.GetPeerPlayerID(peer), clientCommand);
    }

    private readonly Dictionary<Msg, Action<ENetPacketPeer, byte[]>> _messageHandlers = new();

    public virtual void InitMessageHandlers()
    {
        _messageHandlers[Msg.C2S_CONNECTION_REQUEST] = (peer, payload) => Dispatch<ConnectionRequest>(peer, payload, HandleConnectionRequest);
        _messageHandlers[Msg.C2S_CLIENT_LOADED] = (peer, payload) => Dispatch<ClientLoaded>(peer, payload, HandleClientLoaded);
        _messageHandlers[Msg.C2S_CLIENT_COMMAND] = (peer, payload) => Dispatch<ClientCommand>(peer, payload, HandleClientCommand);
        _messageHandlers[Msg.C2S_CHANGE_PLAYER_NAME] = (peer, payload) => Dispatch<PlayerNameChangeRequest>(peer, payload, HandlePlayerNameChangeRequest);

    }

    private void Dispatch<T>(ENetPacketPeer peer, byte[] payload, Action<ENetPacketPeer, T> handler) where T : Message, new()
    {
        var msg = Message.FromData<T>(payload);
        handler(peer, msg);
    }

    public byte GetNextAvailablePlayerID()
    {
        var connectedPlayerIDs = MatchState.Instance.ConnectedPlayers.Keys.ToHashSet();

        GD.Print($"get next available player id ran");
        for (byte b = 0; b < byte.MaxValue; ++b)
        {
            GD.Print($"get next available player id iterated");

            if (!connectedPlayerIDs.Contains(b))
            {
                GD.Print($"next available player id = {b}");
                return b;
            }
            GD.Print($"connected player ids already contains {b}");

        }
        return byte.MaxValue;
    }

    public void StartMatch()
    {
        GD.Print($"start match ran on {NetworkManager.Instance.NetworkMode}");

        if (NetworkManager.Instance.IsServer)
        {
            foreach (var player in MatchState.Instance.ConnectedPlayers.Values)
            {
                var spawnedPlayer = SpawnManager.Instance.ServerSpawnPlayer(player.PlayerInfo.PlayerID);
            }
        }

    }

    public void HandlePlayerNameChangeRequest(ENetPacketPeer peer, PlayerNameChangeRequest request)
    {
        byte playerID = NetUtils.GetPeerPlayerID(peer);

        ApplyPlayerNameChange(playerID, request.Name);
    }

    public void ApplyPlayerNameChange(byte playerID, string name)
    {
        if (MatchState.Instance.ConnectedPlayers.TryGetValue(playerID, out var playerState))
        {
            playerState.SetPlayerName(name);
        }

        PlayerNameChange.Send(playerID, name);
    }
}
