using Godot;
using System;

public partial class FpsCounter : Control, ITickable
{
    [Export] Label _fpsCounterLabel;


    private float _timeAccumulator;

    private float _refreshTime = 1.0f;

    public override void _Ready()
    {
        base._Ready();

        ClientGame.Instance.Tickables.Add(this);
    }

    public void Tick(double delta)
    {
        _timeAccumulator += (float)delta;

        if(_timeAccumulator > _refreshTime)
        {
            _timeAccumulator -= _refreshTime;

            UpdateFPS();
        }
    }

    public void UpdateFPS()
    {
        GD.Print("Update fps ran");

        _fpsCounterLabel.Text = Mathf.RoundToInt(Engine.GetFramesPerSecond()).ToString();
    }
}
