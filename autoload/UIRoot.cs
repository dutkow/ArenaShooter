using Godot;
using System;

public partial class UIRoot : Control
{
    public static UIRoot Instance { get; private set; }

    // Scenes
    [Export] PackedScene _loadingScreenScene;
    private LoadingScreen _loadingScreenInstance;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        if (_loadingScreenScene != null)
        {
            _loadingScreenInstance = (LoadingScreen)_loadingScreenScene.Instantiate();
            AddChild(_loadingScreenInstance);
            _loadingScreenInstance.Visible = false;
        }

        GD.Print($"UIRoot is in tree? {IsInsideTree()} at path {GetPath()}");

    }

    public void ShowLoadingScreen()
    {
        if (_loadingScreenInstance != null)
        {
            _loadingScreenInstance.Visible = true;
            _loadingScreenInstance.ShowLoading();
        }
    }

    public void HideLoadingScreen()
    {
        if (_loadingScreenInstance != null)
        {
            _loadingScreenInstance.Visible = false;
            _loadingScreenInstance.HideLoading();
        }
    }

    public void SetProgress(float value)
    {
        _loadingScreenInstance?.SetProgress(value);
    }
}
