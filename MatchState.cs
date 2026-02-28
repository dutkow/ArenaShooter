using Godot;
using System;

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

    // Current match phase
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

    // Time remaining in seconds
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

    // Phase durations (seconds)
    [Export] public int WarmupDuration { get; set; } = 6;
    [Export] public int PreMatchDuration { get; set; } = 3;
    [Export] public int MatchDuration { get; set; } = 600;
    [Export] public int PostMatchDuration { get; set; } = 5;

    // Accumulates delta until we hit 1 second
    private double _secondAccumulator = 0.0;

    public override void _Ready()
    {
        Instance = this;

        StartPhase(MatchPhase.WARMUP);
    }

    public override void _Process(double delta)
    {
        if (!IsPhaseTimed(MatchPhase))
            return;

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

        switch(MatchPhase)
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

    public void StartWarmup()
    {
        TimeRemaining = WarmupDuration;

    }

    public void StartPreMatch()
    {
        TimeRemaining = PreMatchDuration;
    }

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
}