using Godot;
using System;

public partial class MatchDebugPanel : Control
{
    [Export] Label _matchPhaseLabel;

    public override void _Ready()
    {
        base._Ready();


        OnMatchPhaseChanged(MatchState.Instance.MatchPhase);
        MatchState.Instance.MatchPhaseChanged += OnMatchPhaseChanged;
    }

    public void OnMatchPhaseChanged(MatchPhase matchPhase)
    {
        _matchPhaseLabel.Text = MatchState.Instance.MatchPhase.ToString();
    }
}
