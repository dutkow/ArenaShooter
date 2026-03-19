using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CommandConsole : Control
{
    public static CommandConsole Instance { get; private set; }

    [Export] LineEdit _lineEdit;
    [Export] VBoxContainer _consoleLog;
    [Export] PackedScene _controlLogEntryScene;

    private Dictionary<string, Action<string[]>> _commands = new();

    public override void _Ready()
    {
        base._Ready();

        Instance = this;

        _commands["maxfps"] = HandleMaxFps;
        _commands["showfps"] = HandleShowFPS;
        _commands["vsync"] = HandleVSync;
        _commands["sv_tick"] = HandleServerTickRate;
        _commands["name"] = HandleChangePlayerName;
        _commands["playerlist"] = PrintPlayerList;

        _lineEdit.TextSubmitted += TryCommand;

        _lineEdit.KeepEditingOnTextSubmit = true;
    }

    public bool Toggle()
    {
        GD.Print($"toggle cmd console ran");
        if(Visible)
        {
            Visible = false;
        }
        else
        {
            Visible = true;
            CallDeferred(nameof(GrabLineEditFocus));
        }

        return Visible;
    }

    public void GrabLineEditFocus()
    {
        _lineEdit.GrabFocus();
    }

    public void TryCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string command = parts[0].ToLower();

        if (_commands.TryGetValue(command, out var handler))
        {
            handler(parts);
        }
        else
        {
            AddConsoleLogEntry($"Unknown command: {command}");
        }

        _lineEdit.Clear();

    }


    private void HandleMaxFps(string[] args)
    {
        if (args.Length > 1 && int.TryParse(args[1], out int fps))
        {
            Engine.MaxFps = fps;

            if(fps == 0)
            {
                AddConsoleLogEntry($"Max FPS set to unlimited");
            }
            else
            {
                AddConsoleLogEntry($"Max FPS set to {fps}");
            }
        }
        else
        {
            AddConsoleLogEntry("Unknown maxfps value");
        }
    }

    private void HandleVSync(string[] args)
    {
        if (args.Length > 1 && int.TryParse(args[1], out int value))
        {
            if(value == 0)
            {
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
                AddConsoleLogEntry("VSync disabled");

            }
            else if (value == 1)
            {
                DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
                AddConsoleLogEntry("VSync enabled");

            }
        }
        else
        {
            AddConsoleLogEntry("Unknown vsync value");
        }
    }

    private void HandleShowFPS(string[] args)
    {
        if (args.Length > 1 && int.TryParse(args[1], out int value))
        {
            if (value == 0)
            {
                UserSettings.Instance.SetShowFPS(false);
                AddConsoleLogEntry("Show FPS disabled");

            }
            else if (value == 1)
            {
                UserSettings.Instance.SetShowFPS(true);
                AddConsoleLogEntry("Show FPS enabled");
            }
        }
        else
        {
            AddConsoleLogEntry("Unknown vsync value");
        }
    }

    private void HandleServerTickRate(string[] args)
    {
        if (args.Length > 1 && int.TryParse(args[1], out int value))
        {
            if (value > 0)
            {
                value = Math.Min(value, NetworkConstants.MAX_SERVER_TICK_RATE);

                if(TickManager.Instance != null && NetworkManager.Instance.NetworkMode != NetworkMode.CLIENT)
                {
                    TickManager.Instance.SetServerTickRate(value);
                    TickRateChanged.Send((ushort)value);
                }
                AddConsoleLogEntry($"Server tick rate set to {value}");
            }
        }
        else
        {
            AddConsoleLogEntry($"Attempted command: {args[0]} had invalid value: {args[1]}");
        }
    }

    private void HandleChangePlayerName(string[] args)
    {
        if (args.Length > 1)
        {
            string value = string.Join(" ", args[1..]);

            UserSettings.Instance.SetPlayerName(value);

            if (NetworkManager.Instance.NetworkMode == NetworkMode.CLIENT)
            {
                PlayerNameChangeRequest.Send(value);
            }
            else if (NetworkManager.Instance.NetworkMode != NetworkMode.OFFLINE)
            {
                ServerGame.Instance?.ApplyPlayerNameChange(ClientGame.Instance.LocalPlayerID, value);
            }

            AddConsoleLogEntry($"Changed player name to: {value}");
        }
        else
        {
            AddConsoleLogEntry($"Attempted command: {args[0]} had invalid value: NULL");
        }
    }

    public void PrintPlayerList(string[] args)
    {
        AddConsoleLogEntry($"--- Connected players ---");

        foreach (var player in MatchState.Instance.ConnectedPlayers.Values.OrderBy(p => p.PlayerInfo.PlayerName, StringComparer.OrdinalIgnoreCase))
        {
            AddConsoleLogEntry($"-{player.PlayerInfo.PlayerName}, Player ID: {player.PlayerInfo.PlayerID}");
        }
    }

    public void AddConsoleLogEntry(string text)
    {
        var consoleLogEntry = (ConsoleLogEntry)_controlLogEntryScene.Instantiate();
        consoleLogEntry.Text = text;
        _consoleLog.AddChild(consoleLogEntry);
    }
}