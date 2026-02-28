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

    // Phase durations (seconds) — adjustable per project
    [Export] public int WarmupDuration { get; set; } = 5;
    [Export] public int PreMatchDuration { get; set; } = 3;
    [Export] public int MatchDuration { get; set; } = 600; // 10 min
    [Export] public int PostMatchDuration { get; set; } = 5;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        StartPhase(MatchPhase.WARMUP);
    }

    public override void _Process(double delta)
    {
        // Only decrement timer for active phases
        if (MatchPhase == MatchPhase.WARMUP || MatchPhase == MatchPhase.PRE_MATCH || MatchPhase == MatchPhase.MATCH)
        {
            // Subtract time each second
            if (TimeRemaining > 0)
            {
                TimeRemaining--;
            }
            else
            {
                AdvanceToNextMatchPhase();
            }
        }
    }

    public void StartPhase(MatchPhase phase)
    {
        MatchPhase = phase;
        switch (phase)
        {
            case MatchPhase.WARMUP:
                TimeRemaining = WarmupDuration;
                break;
            case MatchPhase.PRE_MATCH:
                TimeRemaining = PreMatchDuration;
                break;
            case MatchPhase.MATCH:
                TimeRemaining = MatchDuration;
                break;
            case MatchPhase.POST_MATCH:
                TimeRemaining = PostMatchDuration;
                break;
        }
    }

    public void SetTimeRemaining(int secondsRemaining)
    {
        if (secondsRemaining <= 0)
        {
            AdvanceToNextMatchPhase();
        }
        else
        {
            TimeRemaining = secondsRemaining;
        }
    }

    public void AdvanceToNextMatchPhase()
    {
        MatchPhase nextPhase = MatchPhase switch
        {
            MatchPhase.WARMUP => MatchPhase.PRE_MATCH,
            MatchPhase.PRE_MATCH => MatchPhase.MATCH,
            MatchPhase.MATCH => MatchPhase.POST_MATCH,
            MatchPhase.POST_MATCH => MatchPhase.WARMUP,
            _ => MatchPhase.POST_MATCH
        };

        StartPhase(nextPhase);
    }
}