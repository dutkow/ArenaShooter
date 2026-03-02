using System;
using System.Text;
using System.Text.Json;

public class PascalToSnake : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLower(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public class SnakeToPascal : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    sb.Append(char.ToUpper(part[0]));
                }
                if (part.Length > 1)
                {
                    sb.Append(part.Substring(1));
                }
            }

            return sb.ToString();
        }
    }
}