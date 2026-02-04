using System.Collections.Generic;
using System.Linq;
using Data = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

namespace NeoIni;

internal sealed class NeoIniReaderCore
{
    internal static bool SectionExists(Data data, string section) => data.ContainsKey(section);

    internal static bool KeyExists(Data data, string section, string key)
    {
        if (!SectionExists(data, section)) return false;
        return data[section].ContainsKey(key);
    }

    internal static void AddSection(Data data, string section)
    {
        if (SectionExists(data, section)) return;
        data[section] = new Dictionary<string, string>();
    }

    internal static void AddKeyInSection(Data data, string section, string key, string value)
    {
        AddSection(data, section);
        if (KeyExists(data, section, key)) return;
        data[section][key] = value;
    }

    internal static void SetKey(Data data, string section, string key, string value)
    {
        if (!SectionExists(data, section)) AddSection(data, section);
        data[section][key] = value;
    }

    internal static void RemoveKey(Data data, string section, string key)
    {
        if (!KeyExists(data, section, key)) return;
        data[section].Remove(key);
    }

    internal static void RemoveSection(Data data, string section)
    {
        if (!SectionExists(data, section)) return;
        data.Remove(section);
    }

    internal static string[] GetAllKeys(Data data, string section)
    {
        if (!SectionExists(data, section)) return new string[1] { "" };
        return data[section].Keys.ToArray();
    }

    internal static Dictionary<string, string> GetSection(Data data, string section)
    {
        if (!SectionExists(data, section)) return new Dictionary<string, string>();
        return new Dictionary<string, string>(data[section]);
    }

    internal static void ClearSection(Data data, string section)
    {
        if (!SectionExists(data, section)) return;
        data[section].Clear();
    }

    internal static void RenameKey(Data data, string section, string oldKey, string newKey)
    {
        if (!KeyExists(data, section, oldKey)) return;
        data[section][newKey] = data[section][oldKey];
        data[section].Remove(oldKey);
    }

    internal static void RenameSection(Data data, string oldSection, string newSection)
    {
        if (!SectionExists(data, oldSection) || SectionExists(data, newSection)) return;
        data[newSection] = data[oldSection];
        data.Remove(oldSection);
    }
}
