using Godot;
using System;
using System.Linq;

public static class TextUtils
{
    public static string SnakeToPascal(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string[] parts = input.Split('_', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];

            if (part.Length > 0)
            {
                char first = char.ToUpper(part[0]);
                string rest = string.Empty;

                if (part.Length > 1)
                {
                    rest = part.Substring(1).ToLower();
                }

                parts[i] = first + rest;
            }
        }

        string result = string.Empty;

        for (int i = 0; i < parts.Length; i++)
        {
            result += parts[i];
        }

        return result;
    }

    public static string PascalToSnake(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (char.IsUpper(c))
            {
                bool isNotFirst = i > 0;
                bool prevLowerOrDigit = isNotFirst && (char.IsLower(input[i - 1]) || char.IsDigit(input[i - 1]));
                bool nextLower = (i + 1 < input.Length) && char.IsLower(input[i + 1]);

                if (isNotFirst && (prevLowerOrDigit || nextLower))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLower(c));
            }
            else
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }

    public static string PascalToUpperSnake(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (char.IsUpper(c))
            {
                bool isNotFirst = i > 0;
                bool prevLowerOrDigit = isNotFirst && (char.IsLower(input[i - 1]) || char.IsDigit(input[i - 1]));
                bool nextLower = (i + 1 < input.Length) && char.IsLower(input[i + 1]);

                if (isNotFirst && (prevLowerOrDigit || nextLower))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToUpper(c));
            }
            else
            {
                builder.Append(char.ToUpper(c));
            }
        }

        return builder.ToString();
    }

    public static string GetInputEventDisplayString(InputEventWithModifiers inputEvent)
    {
        string displayString = string.Empty;

        if(inputEvent.CtrlPressed)
        {
            displayString += TranslationServer.Translate("CTRL") + " + ";
        }
        if (inputEvent.AltPressed)
        {
            displayString += TranslationServer.Translate("ALT") + " + ";
        }
        if (inputEvent.ShiftPressed)
        {
            displayString += TranslationServer.Translate("SHIFT") + " + ";
        }

        if(inputEvent is InputEventKey eventKey)
        {
            if(InputMappings.KeyMappingsReverse.TryGetValue(eventKey.Keycode, out var displayName))
            {
                displayString += TranslationServer.Translate(displayName.ToUpper());
            }
        }
        else if(inputEvent is InputEventMouseButton mouseButton)
        {
            if (InputMappings.MouseButtonMappingsReverse.TryGetValue(mouseButton.ButtonIndex, out var displayName))
            {
                displayString += TranslationServer.Translate(displayName.ToUpper());
            }
        }

        return displayString;
    }

    public static T EnumFromString<T>(string value) where T : struct, Enum
    {
        string cleaned = value.Replace(" ", "");
        if (Enum.TryParse<T>(cleaned, true, out var result))
        {
            return result;
        }

        return Enum.GetValues(typeof(T)).Cast<T>().First();
    }
}
