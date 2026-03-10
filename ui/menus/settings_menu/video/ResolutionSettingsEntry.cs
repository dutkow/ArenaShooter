using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// TODO: Refactor to take Vector2I instead of string
public partial class ResolutionSettingsEntry : DropdownSettingsEntry
{
    public override void _Ready()
    {
        base._Ready();

        InitConfig(SettingsManager.Instance.SettingsConfig.Video.Resolution);

        SettingsManager.Instance.WindowModeChanged += OnWindowModeChanged;

        GetTree().Root.SizeChanged += SetTextToWindowSize;
    }

    public override void PopulateItems()
    {
        var currentSetting = SettingsManager.Instance.SettingsConfig.Video.Resolution.Value;
        List<Resolution> options = SettingsManager.Instance.SettingsConfig.Video.Resolution.Options;

        int screenCount = DisplayServer.GetScreenCount();

        for (int s = 0; s < screenCount; s++)
        {
            Vector2I screenSize = DisplayServer.ScreenGetSize(s);
            var screenRes = Resolution.FromVector2I(screenSize);

            if (!options.Any(r => r.Width == screenRes.Width && r.Height == screenRes.Height))
            {
                options.Add(screenRes);
            }
        }

        options = options
            .Where(r =>
            {
                for (int s = 0; s < screenCount; s++)
                {
                    Vector2I screenSize = DisplayServer.ScreenGetSize(s);
                    if (r.Width <= screenSize.X && r.Height <= screenSize.Y)
                        return true;
                }
                return false;
            })
            .ToList();

        options.Sort((a, b) =>
        {
            int cmp = a.Width.CompareTo(b.Width);
            return cmp != 0 ? cmp : a.Height.CompareTo(b.Height);
        });

        Dropdown.Clear();
        for (int i = 0; i < options.Count; ++i)
        {
            var option = options[i];

            Dropdown.AddItem($"{option.Width}x{option.Height}");
            Dropdown.SetItemMetadata(i, new Vector2I(option.Width, option.Height));

            if (option.Width == currentSetting.Width && option.Height == currentSetting.Height)
            {
                Dropdown.Select(i);
            }
        }
    }

    public override void OnItemSelected(long itemIndex)
    {
        base.OnItemSelected(itemIndex);

        if (DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Maximized)
        {
            SettingsManager.Instance.SetWindowMode(DisplayServer.WindowMode.Windowed);
        }

        Vector2I newSize = (Vector2I)Dropdown.GetItemMetadata((int)itemIndex);
        DisplayServer.WindowSetSize(newSize);

        int currentScreen = DisplayServer.WindowGetCurrentScreen();
        Vector2I screenSize = DisplayServer.ScreenGetSize(currentScreen);
        Vector2I screenPosition = DisplayServer.ScreenGetPosition(currentScreen);

        Vector2I newPosition = screenPosition + (screenSize - newSize) / 2;

        DisplayServer.WindowSetPosition(newPosition);
    }

    public void OnWindowModeChanged(DisplayServer.WindowMode windowMode)
    {
        if(windowMode == DisplayServer.WindowMode.ExclusiveFullscreen || windowMode == DisplayServer.WindowMode.Fullscreen)
        {
            Dropdown.Disabled = true;
            SetTextToWindowSize();
        }
        else
        {
            Dropdown.Disabled = false;
        }
    }

    public void SetTextToWindowSize()
    {
        var currentResolution = DisplayServer.WindowGetSize();
        Dropdown.Text = $"{currentResolution.X}x{currentResolution.Y}";
    }
}