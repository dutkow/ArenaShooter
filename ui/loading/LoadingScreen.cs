using Godot;
using System;

public partial class LoadingScreen : Control
{
    [Export] ProgressBar _loadingProgressBar;
    public void ShowLoading()
    {
        Visible = true;
        SetProcess(true);
    }

    public void HideLoading()
    {
        Visible = false;
        SetProcess(false);
    }

    // Optional: call to update a progress bar
    public void SetProgress(float value)
    {
        if(_loadingProgressBar == null)
        {
            GD.PushError("Loading progress bar not set on loading screen");
            return;
        }

        _loadingProgressBar.Value = Mathf.Clamp(value * 100f, 0, 100);
    }
}