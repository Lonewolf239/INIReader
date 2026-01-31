using System;
using System.Collections.Generic;
using System.Text;

namespace NeoIni;

internal sealed class NeoIniParser
{
    internal static string GetStringRaw(object sync, Dictionary<string, Dictionary<string, string>> data, string section, string keyName)
    {
        lock (sync)
            return data.TryGetValue(section, out var sec) && sec.TryGetValue(keyName, out var val) ? val : null;
    }

    internal static string GetContent(object sync, Dictionary<string, Dictionary<string, string>> data)
    {
        var content = new StringBuilder();
        lock (sync)
        {
            foreach (var section in data)
            {
                content.Append($"[{section.Key}]\n");
                foreach (var kvp in section.Value)
                    content.Append($"{kvp.Key} = {kvp.Value}\n");
                content.Append("\n");
            }
        }
        return content.ToString();
    }

    internal static T TryParseValue<T>(string value, T defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (typeof(T) == typeof(bool))
            return bool.TryParse(value, out bool parsed) ? (T)(object)parsed : defaultValue;
        try
        {
            if (value == null || string.IsNullOrWhiteSpace(value))
                return defaultValue;
            object parsed = Convert.ChangeType(value.Trim(), typeof(T));
            return (T)parsed;
        }
        catch { return defaultValue; }
    }

    internal static bool TryMatchKey(ReadOnlySpan<char> line, out string key, out string value)
    {
        key = value = null;
        int eqIndex = line.IndexOf('=');
        if (eqIndex == -1) return false;
        ReadOnlySpan<char> keySpan = line[..eqIndex].Trim();
        ReadOnlySpan<char> valueSpan = line[(eqIndex + 1)..].Trim();
        if (keySpan.IsEmpty || valueSpan.IsEmpty) return false;
        key = keySpan.ToString();
        value = valueSpan.ToString();
        return true;
    }
}
