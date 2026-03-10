using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public enum ControlSetting
{
    CAMERA_PAN_SPEED,
}

public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; }

    [Export] Json _gameSettingsConfigJson;

    public GameSettingsConfig SettingsConfig;
    private UserSettingsConfig _userSettingsConfig;

    public RuntimeUserSettings Settings;

    [ExportCategory("Input")]
    public bool InvertDragScroll = false;

    public event Action<bool> InvertDragScrollChanged;

    public event Action<DisplayServer.WindowMode> WindowModeChanged;

    public Dictionary<ControlSetting, Variant> ControlSettingsPendingChange = new();


    // Setting changed events

    public event Action<bool> SettingsDirtyChanged;

    private bool _wasAnyDirty;


    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        SettingsConfig = SettingsLoader.LoadGameConfig(_gameSettingsConfigJson);
        _userSettingsConfig = SettingsLoader.LoadOrCreateUserConfig(SettingsConfig);
        Settings = SettingsLoader.CreateRuntime(_userSettingsConfig);

        InputManager.Instance.DirtyStateChanged += OnInputBindingsDirtyChanged;
    }

    public void ToggleInvertDragScroll()
    {
        InvertDragScroll = !InvertDragScroll;
        InvertDragScrollChanged?.Invoke(InvertDragScroll);
    }

    public void LoadGameSettings(Json settingsJson)
    {
        SettingsConfig = JsonUtils.LoadJson<GameSettingsConfig>(_gameSettingsConfigJson);
    }

    public void SetWindowMode(DisplayServer.WindowMode windowMode)
    {
        if(DisplayServer.WindowGetMode() == windowMode)
        {
            return;
        }

        DisplayServer.WindowSetMode(windowMode);
        WindowModeChanged?.Invoke(windowMode);
    }

    public void AcceptAllPendingChanges()
    {
        if(IsAnyDirty)
        {
            Settings.ApplyAll();
            SettingsDirtyChanged?.Invoke(false);
        }
    }

    public void RevertAllPendingChanges()
    {
        if(IsAnyDirty)
        {
            Settings.RevertAll();
            SettingsDirtyChanged?.Invoke(false);
        }
    }

    private readonly HashSet<ISetting> _pendingSettings = new();

    public bool IsAnyDirty => IsSettingsDirty || InputManager.Instance.InputBindingsDirty;

    public bool IsSettingsDirty => _pendingSettings.Count > 0;

    public void RegisterPendingChange(ISetting setting)
    {
        if (!_pendingSettings.Contains(setting))
        {
            _pendingSettings.Add(setting);
            EvaluateDirtyState();
        }
    }

    public void UnregisterPendingChange(ISetting setting)
    {
        if (_pendingSettings.Contains(setting))
        {
            _pendingSettings.Remove(setting);
            EvaluateDirtyState();
        }
    }

    public void OnInputBindingsDirtyChanged(bool dirty)
    {
        EvaluateDirtyState();
    }

    private void EvaluateDirtyState()
    {
        bool nowDirty = IsAnyDirty;
        if (nowDirty != _wasAnyDirty)
        {
            _wasAnyDirty = nowDirty;
            SettingsDirtyChanged?.Invoke(nowDirty);
        }
    }
}
