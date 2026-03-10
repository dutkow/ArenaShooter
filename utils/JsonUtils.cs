using Godot;
using System;
using System.Text.Json;

public static class JsonUtils
{
    public static readonly JsonSerializerOptions SnakeCaseOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy(),
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public static T? LoadJson<T>(Json jsonFile)
    {
        if (jsonFile == null)
        {
            GD.PushError("JsonFile is null");
            return default;
        }

        using var file = FileAccess.Open(jsonFile.ResourcePath, FileAccess.ModeFlags.Read);
        string jsonText = file.GetAsText();

        Error err = jsonFile.Parse(jsonText);
        if (err != Error.Ok)
        {
            GD.PrintErr($"JSON Parse Error: {jsonFile.GetErrorMessage()} at line {jsonFile.GetErrorLine()}");
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(jsonText, SnakeCaseOptions);
        }
        catch (Exception e)
        {
            GD.PrintErr($"JSON Deserialization Error: {e.Message}");
            return default;
        }
    }
    public static string ToJson<T>(T obj)
    {
        if (obj == null)
        {
            GD.PushError("Object to serialize is null!");
            return string.Empty;
        }

        try
        {
            return JsonSerializer.Serialize(obj, SnakeCaseOptions);
        }
        catch (Exception e)
        {
            GD.PrintErr($"JSON Serialization Error: {e.Message}");
            return string.Empty;
        }
    }
}
