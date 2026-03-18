using Godot;
using System;

public partial class FpsCounter : Control, ITickable
{
    [Export] Label _fpsCounterLabel;


    private float _timeAccumulator;

    private float _refreshTime = 0.5f;

    public override void _Ready()
    {
        base._Ready();

        ClientGame.Instance.Tickables.Add(this);

        OnShowFPSChanged(Settings.Instance.ShowFPS);
        Settings.Instance.ShowFPSChanged += OnShowFPSChanged;
    }

    public void OnShowFPSChanged(bool value)
    {
        Visible = value;
    }

    public void Tick(double delta)
    {
        if(!Visible)
        {
            return;
        }

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
