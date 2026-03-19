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
    public Dictionary<byte, PlayerState> ConnectedPlayers = new();

    public event Action<PlayerState>? PlayerJoined;

    public event Action<int, PlayerState>? PlayerLeft;

    // base tick
    public ushort CurrentTick { get; private set; } = 0;



    public override void _EnterTree()
    {
        base._EnterTree();

        Instance = this;

    }

    public static void Initialize()
    {
        if(Instance != null)
        {
            GD.Print($"Match state already exists");
            return;
        }

        Instance = new MatchState();

    }

 
    public void Tick(double delta)
    {
        ServerGame.Instance?.Tick(delta);
        ClientGame.Instance?.Tick(delta);

        CurrentTick++;
    }

    public void OnReceivedInitialMatchState(InitialMatchState initialMatchState)
    {
        if(initialMatchState.PlayerStates == null)
        {
            GD.Print($"initial match state player states are null");
            return;

        }
        for(int i = 0; i < initialMatchState.PlayerStates.Length; ++i)
        {
            GD.Print($"add existing player on client joined ran");
            AddExistingPlayerOnClientJoined(initialMatchState.PlayerStates[i]);
        }

        ClientGame.Instance?.LocalPlayerController.Initialize(); // TODO. rethink this execution flow
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
    public void AddPlayer(PlayerInfo playerInfo)
    {
        GD.Print($"add player running on {NetworkManager.Instance.NetworkMode}. player id: {playerInfo.PlayerID}");
        if (ConnectedPlayers.ContainsKey(playerInfo.PlayerID))
        {
            return; // Already added
        }

        var playerState = new PlayerState();
        playerState.PlayerInfo = playerInfo;
  
        ConnectedPlayers[playerInfo.PlayerID] = playerState;

        PlayerJoined?.Invoke(playerState);


        if (ClientGame.Instance != null && playerInfo.PlayerID == ClientGame.Instance.LocalPlayerID)
        {
            ClientGame.Instance.AssignPlayerState(playerState);
        }
    }

    public void AddExistingPlayerOnClientJoined(PlayerState playerState)
    {
        ConnectedPlayers[playerState.PlayerInfo.PlayerID] = playerState;

        if(playerState.IsSpawned)
        {

            SpawnManager.Instance.LocalSpawnPlayer(playerState.PlayerInfo.PlayerID, playerState.CharacterPublicState.Position, playerState.CharacterPublicState.Yaw);

            GD.Print($"client is spawning {playerState.PlayerInfo.PlayerID} at position {playerState.CharacterPublicState.Position}");

        }
    }

    public void HandlePlayerLeft(byte playerID)
    {
        if(ConnectedPlayers.TryGetValue(playerID, out var playerState))
        {
            playerState.PlayerLeft?.Invoke();
            ConnectedPlayers.Remove(playerID);

            if (playerState.Character != null)
            {
                playerState.Character.HandleDeath();
            }
        }
    }
}