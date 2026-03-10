using Godot;
using Godot.Collections;
using System;

public partial class GameUI : Control
{
    [ExportCategory("Scenes")]
    [Export] PackedScene _hudScene;
    [Export] PackedScene _pauseMenuScene;


    public Hud Hud;
    public PauseMenu PauseMenu;

    public bool _isMenuStateDirty;

   
    public override void _Ready()
    {
        base._Ready();

        OpenHud();

        var inputMgr = InputManager.Instance;
        inputMgr.OpenPauseMenuPressed += OpenPauseMenu;
        inputMgr.ClosePauseMenuPressed += ClosePauseMenu;
        inputMgr.NavigateBackPressed += NavigateBack;
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        var inputMgr = InputManager.Instance;
        inputMgr.OpenPauseMenuPressed -= OpenPauseMenu;
        inputMgr.ClosePauseMenuPressed -= ClosePauseMenu;
        inputMgr.NavigateBackPressed -= NavigateBack;
    }

    public void OpenHud()
    {
        if(Hud == null)
        {
            Hud = (Hud)_hudScene.Instantiate();
            AddChild(Hud);
        }
        Hud.Show();
    }

    public void CloseHud()
    {
        Hud?.Hide();
    }

    public void OpenPauseMenu()
    {
        if(PauseMenu == null)
        {
            PauseMenu = (PauseMenu)_pauseMenuScene.Instantiate();
            AddChild(PauseMenu);

            PauseMenu.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            PauseMenu.SizeFlagsVertical = SizeFlags.ExpandFill;

            PauseMenu.SetAnchorsPreset(LayoutPreset.FullRect);

            MarkMenuStateDirty();
            TimeManager.Instance.PauseGame();

            InputManager.Instance.SetInputMode(InputMode.UI);
        }
        else if (!PauseMenu.Visible && !_isMenuStateDirty)
        {
            MarkMenuStateDirty();
            TimeManager.Instance.PauseGame();
            PauseMenu.Show();

            InputManager.Instance.SetInputMode(InputMode.UI);
        }
    }

    public void ClosePauseMenu()
    {
        if(PauseMenu.Visible && !_isMenuStateDirty)
        {
            MarkMenuStateDirty();
            PauseMenu?.Hide();

            InputManager.Instance.SetInputMode(InputMode.GAME);
        }
    }

    public void MarkMenuStateDirty()
    {
        _isMenuStateDirty = true;
        CallDeferred(nameof(SetMenuStateIsNotDirty));
    }

    public void SetMenuStateIsNotDirty()
    {
        _isMenuStateDirty = false;
    }

    public void NavigateBack()
    {
        //
    }
}
