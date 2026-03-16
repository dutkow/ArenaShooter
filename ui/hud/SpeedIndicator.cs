using Godot;
using System;
using static Godot.WebSocketPeer;

public partial class SpeedIndicator : Control
{
    Character _character;

    [Export] Label HorizontalSpeedLabel;
    [Export] Label VerticalSpeedLabel;


    public override void _Ready()
    {
        base._Ready();

        _character = ClientGame.Instance.LocalPlayerPawn as Character; // should make on possession or something
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        HorizontalSpeedLabel.Text = UnitConversion.ToQuake(Mathf.RoundToInt(_character.MovementComp.HorizontalVelocity)).ToString();
        VerticalSpeedLabel.Text = UnitConversion.ToQuake(Mathf.RoundToInt(_character.MovementComp.VerticalVelocity)).ToString();

    }
}
