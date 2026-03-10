using Godot;
using System;

public static class FilePaths
{
    public const string RESOURCE_PREFIX = "res://";
    public const string GEOGRAPHY_FOLDER = RESOURCE_PREFIX + "assets/data/geography/";
    public const string GEOGRAPHY_FILE_EXT = ".json";

    public const string USER_PREFIX = "user://";
    public const string USER_SETTINGS = USER_PREFIX + "user_settings/";
    public const string USER_INPUT_MAPPINGS = USER_SETTINGS + "input_mappings.json";
    public const string USER_GAME_SETTINGS = USER_SETTINGS + "game_settings.json";

}
