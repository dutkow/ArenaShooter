using Godot;
using System;
using static Godot.WebSocketPeer;

public partial class SpeedIndicator : Control
{
    Character _character;

    [Export] Label SpeedLabel;

    public override void _Ready()
    {
        base._Ready();

        _character = ClientGame.Instance.LocalPlayerPawn as Character; // should make on possession or something
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        Vector3 characterVelocity = _character.MovementComp.State.Velocity;
        float horizontalVelocity = Mathf.RoundToInt(new Vector2(characterVelocity.X, characterVelocity.Z).Length() * 32.0f);
        SpeedLabel.Text = horizontalVelocity.ToString();
    }
}
