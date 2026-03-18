using Godot;
using System;

public partial class Settings : Node
{
    public static Settings Instance { get; private set; }

    private const string SettingsDir = "user://settings";
    private const string ConfigPath = "user://settings/settings.cfg";

    private readonly ConfigFile _config = new();

    public string PlayerName { get; private set; } = "Unnamed Player";

    public Action<bool> ShowFPSChanged;

    public bool ShowFPS = false;

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        EnsureSettingsDirExists();
        LoadSettings();
    }

    private void EnsureSettingsDirExists()
    {
        var dir = DirAccess.Open("user://");
        if (!dir.DirExists("settings"))
        {
            dir.MakeDir("settings");
        }
    }

    private void LoadSettings()
    {
        if (_config.Load(ConfigPath) != Error.Ok)
        {
            SaveSettings();
            return;
        }

        PlayerName = _config.GetValue("", "player_name", PlayerName).AsString();
    }

    private void SaveSettings()
    {
        _config.SetValue("", "player_name", PlayerName);
        _config.Save(ConfigPath);
    }

    public void SetPlayerName(string newName)
    {
        if(PlayerName == newName)
        {
            return;
        }

        PlayerName = newName;
        SaveSettings();
    }

    public void SetShowFPS(bool showFPS)
    {
        if(ShowFPS == showFPS)
        {
            return;
        }

        ShowFPS = showFPS;
        ShowFPSChanged?.Invoke(ShowFPS);
    }
}