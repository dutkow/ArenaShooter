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

public class MatchState : ITickable
{
    public static MatchState Instance { get; private set; }

    public ServerInfo ServerInfo { get; private set; }

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

    public Dictionary<byte, Player> Players = new();

    public event Action<Player>? PlayerJoined;

    public event Action<int, Player>? PlayerLeft;

    // base tick
    public ushort CurrentTick { get; private set; } = 0;


    public static void Initialize()
    {
        if(Instance != null)
        {
            GD.Print($"Match state already exists");
            return;
        }

        Instance = new MatchState();

    }

    public static void Shutdown()
    {
        Instance = null;
    }

 
    public void Tick(double delta)
    {
        ServerGame.Instance?.Tick(delta);
        ClientGame.Instance?.Tick(delta);

        CurrentTick++;


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

    public void OnReceivedInitialMatchState(InitialMatchState initialMatchState)
    {
        if(initialMatchState.PlayerSnapshots == null)
        {
            GD.Print($"initial match state player states are null");
            return;

        }
        for(int i = 0; i < initialMatchState.PlayerSnapshots.Length; ++i)
        {
            GD.Print($"add existing player on client joined ran");
            AddExistingPlayerOnClientJoined(initialMatchState.PlayerSnapshots[i]);
        }

        ClientGame.Instance?.LocalPlayerController.Initialize(); // TODO. rethink this execution flow
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
    public void AddPlayer(PlayerInfo playerInfo)
    {
        GD.Print($"add player running on {NetworkManager.Instance.NetworkMode}. player id: {playerInfo.PlayerID}");
        if (Players.ContainsKey(playerInfo.PlayerID))
        {
            return; // Already added
        }

        var player = Player.Create(playerInfo);

        Players[playerInfo.PlayerID] = player;

        PlayerJoined?.Invoke(player);


        if (ClientGame.Instance != null && playerInfo.PlayerID == ClientGame.Instance.LocalPlayerID)
        {
            ClientGame.Instance.AssignPlayer(player);
        }
    }

    public void AddExistingPlayerOnClientJoined(PlayerSnapshot playerSnapshot)
    {
        Player player = new();
        player.SetID(playerSnapshot.PlayerState.ID);
            
        Players[playerSnapshot.PlayerState.ID] = player;

        if (playerSnapshot.PlayerState.IsSpawned)
        {
            SpawnManager.Instance.LocalSpawnPlayer(playerSnapshot.PlayerState.ID, playerSnapshot.CharacterState.MoveState.Position, playerSnapshot.CharacterState.MoveState.Yaw);
        }
    }

    public void HandlePlayerLeft(byte playerID)
    {
        if(Players.TryGetValue(playerID, out var player))
        {
            player.Left?.Invoke();
            Players.Remove(playerID);

            if (player.Character != null)
            {
                player.Character.HandleDeath();
            }
        }
    }
}