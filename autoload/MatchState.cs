using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum MatchPhase
{
    WARMUP,
    PRE_MATCH,
    MATCH,
    POST_MATCH,
}

public partial class MatchState : Node
{
    public static MatchState Instance { get; private set; }

    // ----------------------
    // Match phase
    // ----------------------
    private MatchPhase _matchPhase;
    public MatchPhase MatchPhase
    {
        get => _matchPhase;
        private set
        {
            if (_matchPhase != value)
            {
                _matchPhase = value;
                MatchPhaseChanged?.Invoke(_matchPhase);
            }
        }
    }

    private int _timeRemaining;
    public int TimeRemaining
    {
        get => _timeRemaining;
        private set
        {
            if (_timeRemaining != value)
            {
                _timeRemaining = value;
                TimeRemainingChanged?.Invoke(_timeRemaining);
            }
        }
    }

    // Events
    public event Action<MatchPhase>? MatchPhaseChanged;
    public event Action<int>? TimeRemainingChanged;

    [Export] public int WarmupDuration { get; set; } = 3;
    [Export] public int PreMatchDuration { get; set; } = 3;
    [Export] public int MatchDuration { get; set; } = 600;
    [Export] public int PostMatchDuration { get; set; } = 5;

    private double _secondAccumulator = 0.0;

    // ----------------------
    // Player management
    // ----------------------
    public Dictionary<byte, PlayerState> ConnectedPlayers = new();

    public event Action<PlayerState>? PlayerJoined;
    public event Action<int, PlayerState>? PlayerLeft;

    ServerTickManager _serverTickManager;
    
    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;
    }
    public void Initialize()
    {
        StartPhase(MatchPhase.WARMUP);

        // Hook network events
        //NetworkHandler.Instance.OnPeerConnected += HandlePlayerJoined;
        //NetworkHandler.Instance.OnPeerDisconnected += HandlePeerDisconnected;

        if (NetworkSession.Instance.IsServer)
        {
            _serverTickManager = new();

            if (NetworkSession.Instance.IsListenServer)
            {
                AddPlayer(NetworkSession.Instance.LocalPlayerID, Settings.Instance.PlayerName);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if(_serverTickManager != null)
        {
            _serverTickManager.PhysicsTick(delta);
        }
    }

    public void OnReceivedInitialMatchState(InitialMatchState initialMatchState)
    {
        for(int i = 0; i < initialMatchState.PlayerIDs.Length; ++i)
        {
            GD.Print("spawning local player after receiving initial match state");

            byte playerID = initialMatchState.PlayerIDs[i];
            ConnectedPlayers[playerID] = new PlayerState(playerID);

            SpawnManager.Instance.LocalSpawnPlayer(initialMatchState.PlayerIDs[i], initialMatchState.Positions[i], initialMatchState.Rotations[i].Y);

            AddPlayer(initialMatchState.PlayerIDs[i], initialMatchState.PlayerNames[i]);
        }
    }

    public override void _Process(double delta)
    {
        if (!IsPhaseTimed(MatchPhase))
        {
            return;
        }

        _secondAccumulator += delta;

        while (_secondAccumulator >= 1.0)
        {
            _secondAccumulator -= 1.0;
            TickOneSecond();
        }
    }

    private void TickOneSecond()
    {
        if (TimeRemaining > 0)
        {
            TimeRemaining--;
        }

        if (TimeRemaining == 0)
        {
            AdvanceToNextMatchPhase();
        }
    }

    private bool IsPhaseTimed(MatchPhase phase)
    {
        return phase == MatchPhase.WARMUP
            || phase == MatchPhase.PRE_MATCH
            || phase == MatchPhase.MATCH
            || phase == MatchPhase.POST_MATCH;
    }

    public void StartPhase(MatchPhase phase)
    {
        MatchPhase = phase;
        _secondAccumulator = 0.0;

        switch (MatchPhase)
        {
            case MatchPhase.WARMUP: StartWarmup(); return;
            case MatchPhase.PRE_MATCH: StartPreMatch(); return;
        }

        TimeRemaining = phase switch
        {
            MatchPhase.PRE_MATCH => PreMatchDuration,
            MatchPhase.MATCH => MatchDuration,
            MatchPhase.POST_MATCH => PostMatchDuration,
            _ => 0
        };
    }

    public void StartWarmup() => TimeRemaining = WarmupDuration;
    public void StartPreMatch() => TimeRemaining = PreMatchDuration;

    public void AdvanceToNextMatchPhase()
    {
        MatchPhase nextPhase = MatchPhase switch
        {
            MatchPhase.WARMUP => MatchPhase.PRE_MATCH,
            MatchPhase.PRE_MATCH => MatchPhase.MATCH,
            MatchPhase.MATCH => MatchPhase.POST_MATCH,
            MatchPhase.POST_MATCH => MatchPhase.WARMUP,
            _ => MatchPhase.WARMUP
        };

        StartPhase(nextPhase);
    }

    // ----------------------
    // Player handling
    // ----------------------
    
    public void AddPlayer(byte playerID, string playerName)
    {
        if (ConnectedPlayers.ContainsKey(playerID))
        {
            return; // Already added
        }

        var player = new PlayerState(playerID)
        {
            PlayerName = playerName,
            Score = 0,
            Health = 100,
            Shields = 100,
            Ammo = 0,
            TeamId = -1,
            Character = null
        };

        ConnectedPlayers[playerID] = player;

        GD.Print($"Player added. Player ID: {playerID}. PlayerName: ({player.PlayerName})");

        try
        {
            PlayerJoined?.Invoke(player);
        }
        catch (Exception e)
        {
            GD.PrintErr("PlayerJoined event crashed: ", e);
        }

        
        if(NetworkSession.Instance.IsServer)
        {
            PlayerController playerController = GameMode.Instance.PlayerControllers[playerID];
            Pawn spawnedPawn = SpawnManager.Instance.ServerSpawnPlayer(playerID);
        }
    }

    private void HandlePeerDisconnected(byte peerId)
    {
        if (ConnectedPlayers.TryGetValue(peerId, out var player))
        {
            ConnectedPlayers.Remove(peerId);

            GD.Print($"Player left: {peerId} ({player.PlayerName})");

            PlayerLeft?.Invoke(peerId, player);

            // TODO: Broadcast to other clients that a player left
            // NetworkHandler.Instance.BroadcastPlayerLeft(peerId);
        }
    }

    // ----------------------
    // Optional helpers
    // ----------------------
    public IReadOnlyList<PlayerState> GetAllPlayers() => new List<PlayerState>(ConnectedPlayers.Values);

    public IReadOnlyList<PlayerState> GetActivePlayers() =>
        new List<PlayerState>(ConnectedPlayers.Values).FindAll(p => p.Character != null);
}