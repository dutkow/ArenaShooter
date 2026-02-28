using Godot;
using System;

public partial class MatchTimer : Control
{
    [Export] Label _warmupLabel;
    [Export] Label _matchTimerLabel;

    public override void _Ready()
    {
        base._Ready();

        // fetch game state and time and init warmup label, timer, etc.
    }
    public void OnWarmupStarted()
    {
        _warmupLabel.Show();
    }

    public void OnWarmupFinished()
    {
        _warmupLabel.Hide();
    }

    public void OnMatchTimeRemainingChanged(int seconds)
    {
        seconds = Math.Max(seconds, 0);

        int minutes = seconds / 60;
        int secs = seconds % 60;

        _matchTimerLabel.Text = $"{minutes:D2}:{secs:D2}";
    }
}
