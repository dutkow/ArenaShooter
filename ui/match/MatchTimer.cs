using Godot;
using System;

public partial class MatchTimer : Control
{
    [Export] Label _warmupLabel;
    [Export] Label _matchTimerLabel;

    public override void _Ready()
    {
        base._Ready();

        OnMatchPhaseChanged(MatchState.Instance.MatchPhase);
        OnMatchTimeRemainingChanged(MatchState.Instance.TimeRemaining);

        MatchState.Instance.MatchPhaseChanged += OnMatchPhaseChanged;
        MatchState.Instance.TimeRemainingChanged += OnMatchTimeRemainingChanged;
    }

    public void OnMatchPhaseChanged(MatchPhase matchPhase)
    {
        if(matchPhase == MatchPhase.WARMUP)
        {
            _warmupLabel.Show();
        }
        else
        {
            _warmupLabel.Hide();
        }
    }

    public void OnWarmupFinished()
    {
    }

    public void OnMatchTimeRemainingChanged(int seconds)
    {
        seconds = Math.Max(seconds, 0);

        int minutes = seconds / 60;
        int secs = seconds % 60;

        _matchTimerLabel.Text = $"{minutes:D2}:{secs:D2}";
    }
}
