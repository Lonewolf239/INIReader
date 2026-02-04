using System;
using System.Globalization;
using System.Text;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni;

internal sealed class NeoIniParser
{
    internal static string GetStringRaw(Data data, string section, string keyName) =>
        data.TryGetValue(section, out var sec) && sec.TryGetValue(keyName, out var val) ? val.Trim() : null;

    internal static string GetContent(Data data)
    {
        var content = new StringBuilder();
        content.Append("; Do not modify this file! This will result in data loss!\n");
        content.Append("; (Data will be downloaded from the backup)\n");
        foreach (var section in data)
        {
            content.Append($"[{section.Key}]\n");
            foreach (var kvp in section.Value)
                content.Append($"{kvp.Key} = {kvp.Value}\n");
            content.Append("\n");
        }
        return content.ToString();
    }

    internal static T TryParseValue<T>(string value, T defaultValue, Action<Exception> onError)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        try
        {
            if (targetType.IsEnum)
                return Enum.TryParse(targetType, value, true, out object enumResult) ? (T)enumResult : defaultValue;
            if (targetType == typeof(bool))
                return bool.TryParse(value, out bool boolResult) ? (T)(object)boolResult : defaultValue;
            if (targetType == typeof(DateTime))
                return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime dtResult) ?
                     (T)(object)dtResult : defaultValue;
            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
            return defaultValue;
        }
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
