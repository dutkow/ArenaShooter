using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

[Serializable]
public class InputBinding
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [JsonPropertyName("modifiers")]
    public List<String> Modifiers { get; set; } = new();


    public static InputBinding FromInputEvent(InputEventWithModifiers ev)
    {
        var binding = new InputBinding();

        // Handle keyboard keys
        if (ev is InputEventKey keyEv)
        {
            if (InputMappings.KeyMappingsReverse.TryGetValue(keyEv.Keycode, out var keyName))
            {
                binding.Key = keyName;
            }
            else
            {
                binding.Key = keyEv.Keycode.ToString();
            }

            if (keyEv.ShiftPressed)
            {
                binding.Modifiers.Add("shift");
            }

            if (keyEv.CtrlPressed)
            {
                binding.Modifiers.Add("ctrl");
            }

            if (keyEv.AltPressed)
            {
                binding.Modifiers.Add("alt");
            }

            return binding;
        }

        // Handle mouse buttons
        if (ev is InputEventMouseButton mouseEv)
        {
            if (InputMappings.MouseButtonMappingsReverse.TryGetValue(mouseEv.ButtonIndex, out var btnName))
            {
                binding.Key = btnName;
            }
            else
            {
                binding.Key = mouseEv.ButtonIndex.ToString();
            }

            if (mouseEv.ShiftPressed)
            {
                binding.Modifiers.Add("shift");
            }

            if (mouseEv.CtrlPressed)
            {
                binding.Modifiers.Add("ctrl");
            }

            if (mouseEv.AltPressed)
            {
                binding.Modifiers.Add("alt");
            }

            return binding;
        }

        GD.PrintErr("Unsupported InputEvent type in InputBinding.FromInputEvent()");
        return null;
    }


    public InputEventWithModifiers GetInputEvent()
    {
        if (InputMappings.KeyMappings.TryGetValue(Key, out var key))
        {
            var inputEvent = new InputEventKey();
            inputEvent.Keycode = key;
            inputEvent.Pressed = true;

            if (Modifiers != null)
            {
                inputEvent.AltPressed = Modifiers.Contains("alt", StringComparer.OrdinalIgnoreCase);
                inputEvent.ShiftPressed = Modifiers.Contains("shift", StringComparer.OrdinalIgnoreCase);
                inputEvent.CtrlPressed = Modifiers.Contains("ctrl", StringComparer.OrdinalIgnoreCase);
            }

            return inputEvent;
        }

        if (InputMappings.MouseButtonMappings.TryGetValue(Key, out var mouseButton))
        {
            var inputEvent = new InputEventMouseButton();
            inputEvent.ButtonIndex = mouseButton;
            inputEvent.Pressed = true;

            if (Modifiers != null)
            {
                inputEvent.AltPressed = Modifiers.Contains("alt", StringComparer.OrdinalIgnoreCase);
                inputEvent.ShiftPressed = Modifiers.Contains("shift", StringComparer.OrdinalIgnoreCase);
                inputEvent.CtrlPressed = Modifiers.Contains("ctrl", StringComparer.OrdinalIgnoreCase);
            }

            return inputEvent;
        }

        GD.PrintErr($"Unknown key or mouse button: {Key}");
        return null;
    }
}

[Serializable]
public class InputAction
{
    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("input_bindings")]
    public List<InputBinding> InputBindings { get; set; } = new();

    [JsonIgnore]
    public InputBinding PrimaryInputBinding => InputBindings.ElementAtOrDefault(0);

    [JsonIgnore]
    public InputBinding SecondaryInputBinding => InputBindings.ElementAtOrDefault(1);
}

[Serializable]
public class InputCategory
{
    [JsonPropertyName("localization_key")]
    public string LocalizationKey { get; set; }

    [JsonPropertyName("input_actions")]
    public List<InputAction> InputActions { get; set; } = new();
}

[Serializable]
public class InputMappingProfile
{
    [JsonPropertyName("categories")]
    public List<InputCategory> Categories { get; set; } = new();
}

public static class InputMappings
{
    public static Dictionary<string, Key> KeyMappings = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase)
    {
        { "a", Key.A }, { "b", Key.B }, { "c", Key.C }, { "d", Key.D },
        { "e", Key.E }, { "f", Key.F }, { "g", Key.G }, { "h", Key.H },
        { "i", Key.I }, { "j", Key.J }, { "k", Key.K }, { "l", Key.L },
        { "m", Key.M }, { "n", Key.N }, { "o", Key.O }, { "p", Key.P },
        { "q", Key.Q }, { "r", Key.R }, { "s", Key.S }, { "t", Key.T },
        { "u", Key.U }, { "v", Key.V }, { "w", Key.W }, { "x", Key.X },
        { "y", Key.Y }, { "z", Key.Z },

        { "0", Key.Key0 }, { "1", Key.Key1 }, { "2", Key.Key2 },
        { "3", Key.Key3 }, { "4", Key.Key4 }, { "5", Key.Key5 },
        { "6", Key.Key6 }, { "7", Key.Key7 }, { "8", Key.Key8 }, 
        { "9", Key.Key9 },

        { "up", Key.Up },
        { "down", Key.Down },
        { "left", Key.Left },
        { "right", Key.Right },

        { "space", Key.Space },
        { "shift", Key.Shift },
        { "ctrl", Key.Ctrl },
        { "alt", Key.Alt },
        { "enter", Key.Enter },
        { "escape", Key.Escape },
        { "tab", Key.Tab },
        { "backspace", Key.Backspace },
        { "caps_lock", Key.Capslock },

        { "f1", Key.F1 }, { "f2", Key.F2 }, { "f3", Key.F3 }, { "f4", Key.F4 },
        { "f5", Key.F5 }, { "f6", Key.F6 }, { "f7", Key.F7 }, { "f8", Key.F8 },
        { "f9", Key.F9 }, { "f10", Key.F10 }, { "f11", Key.F11 }, { "f12", Key.F12 },

        { "[", Key.Bracketleft }, { "]", Key.Bracketright },
        { "-", Key.Minus }, { "=", Key.Equal },
        { ";", Key.Semicolon }, { "'", Key.Apostrophe },
        { ",", Key.Comma }, { ".", Key.Period }, { "/", Key.Slash },
        { "\\", Key.Backslash },

        { "numpad_0", Key.Kp0 },
        { "numpad_1", Key.Kp1 },
        { "numpad_2", Key.Kp2 },
        { "numpad_3", Key.Kp3 },
        { "numpad_4", Key.Kp4 },
        { "numpad_5", Key.Kp5 },
        { "numpad_6", Key.Kp6 },
        { "numpad_7", Key.Kp7 },
        { "numpad_8", Key.Kp8 },
        { "numpad_9", Key.Kp9 },
        
        { "numpad_add", Key.KpAdd },
        { "numpad_subtract", Key.KpSubtract },
        { "numpad_multiply", Key.KpMultiply },
        { "numpad_divide", Key.KpDivide },
        { "numpad_period", Key.KpPeriod },
        { "numpad_enter", Key.KpEnter },
        { "num_lock", Key.Numlock }
    };

    public static Dictionary<string, Key> ModifierKeys = new Dictionary<string, Key>(StringComparer.OrdinalIgnoreCase)
    {
        {"ctrl", Key.Ctrl },
        {"alt", Key.Alt },
        {"shift", Key.Shift },
    };

    public static Dictionary<string, MouseButton> MouseButtonMappings = new Dictionary<string, MouseButton>(StringComparer.OrdinalIgnoreCase)
    {
        { "left_mouse_button", MouseButton.Left },
        { "middle_mouse_button", MouseButton.Middle },
        { "right_mouse_button", MouseButton.Right },
        { "mouse_wheel_up", MouseButton.WheelUp },
        { "mouse_wheel_down", MouseButton.WheelDown },
        { "mouse_x1", MouseButton.Xbutton1 },
        { "mouse_x2", MouseButton.Xbutton2 }
    };

    public static Dictionary<Key, string> KeyMappingsReverse = KeyMappings.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static Dictionary<MouseButton, string> MouseButtonMappingsReverse = MouseButtonMappings.ToDictionary(kv => kv.Value, kv => kv.Key);
}
