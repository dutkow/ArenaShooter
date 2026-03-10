using Godot;
using System;
using System.Net.Security;
using System.Text.RegularExpressions;

public static class AssetUtils
{
    private static string ToSnakeCase(string str)
    {
        // Replace spaces and non-alphanumeric characters with underscores, lowercase
        str = Regex.Replace(str, @"\W+", "_");
        return str.ToLower();
    }

    public static string ToEnumStyle(string input)
    {
        string result = input.Replace(" ", "_");

        result = result.ToUpper();

        return result;
    }

    public static T[] GetEnumValues<T>() where T : struct, Enum
    {
        return (T[])Enum.GetValues(typeof(T));
    }
}
