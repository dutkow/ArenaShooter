using Godot;
using System;
using System.Reflection;
using System.Text.Json.Serialization;

public static class SettingsLoader
{
    public static GameSettingsConfig LoadGameConfig(Json json)
    {
        var config = JsonUtils.LoadJson<GameSettingsConfig>(json);
        AssignLocalizationKeys(config);
        return config;
    } 

    public static UserSettingsConfig LoadOrCreateUserConfig(GameSettingsConfig defaults)
    {
        if (FileAccess.FileExists(FilePaths.USER_GAME_SETTINGS))
        {
            var json = ResourceLoader.Load<Json>(FilePaths.USER_GAME_SETTINGS);
            return JsonUtils.LoadJson<UserSettingsConfig>(json);
        }

        var newConfig = UserSettingsConfig.FromGameSettingsConfig(defaults);
        newConfig.SaveToDisk();
        return newConfig;
    }

    public static void AssignLocalizationKeys(object obj)
    {
        if (obj == null) return;

        var type = obj.GetType();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(obj);
            if (value == null) continue;

            if (value is SettingsConfig setting)
            {
                string jsonKey = prop.Name;

                var attr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (attr != null)
                {
                    jsonKey = attr.Name.ToUpper();
                }
                else
                {
                    jsonKey = TextUtils.PascalToUpperSnake(jsonKey);
                }

                setting.LocalizationKey = jsonKey;
            }
            else if (!prop.PropertyType.IsPrimitive && !prop.PropertyType.IsEnum && prop.PropertyType != typeof(string))
            {
                AssignLocalizationKeys(value);
            }
        }
    }
    public static RuntimeUserSettings CreateRuntime(UserSettingsConfig user) => RuntimeUserSettings.FromConfig(user);

}