using Godot;
using System;
using System.Collections.Generic;


public class UIManager
{
    [Export] PackedScene _pauseMenuScene;
    [Export] PackedScene _settingsMenuScene;


    public static UIManager Instance { get; private set; }


    public void Initialize()
    {
        Instance = this;
    }

    public void OpenPauseMenu()
    {

    }

    public void NavigateBack()
    {
        //Stack. do something idk
    }

    public void SettingsMenu()
    {
        //var settingsMenu = (SettingsMenu)_settingsMenuScene.Instantiate();
        //Stack.Push(settingsMenu);
    }

}
