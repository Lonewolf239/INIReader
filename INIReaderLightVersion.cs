using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IniReader;

///<summary>
/// This is a class for working with INI files
/// LightVersion without encryption
/// <br></br>
/// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
/// <br></br>
/// <b>Target Framework: .NET 9+</b>
/// <br></br>
/// <b>Version: 1.5</b>
/// <br></br>
/// <b>Black Box Philosophy:</b> This class follows a strict "black box" design principle - users interact only through the public API without needing to understand internal implementation details. Input goes in, processed output comes out, internals remain hidden and abstracted.
/// </summary>
public class INIReader
{
    private readonly Dictionary<string, Dictionary<string, string>> Data;
    private readonly string FilePath;
    public bool AutoSave = true;
    public bool AutoAdd = true;
    private Lock Lock = new();

    public INIReader(string path, bool autoEncryption = false)
    {
        FilePath = path;
        Data = GetData();
    }

    #region File System

    private Dictionary<string, Dictionary<string, string>> GetData()
    {
        var data = new Dictionary<string, Dictionary<string, string>>();
        string directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (!File.Exists(FilePath))
        {
            using var stream = File.Create(FilePath);
            return data;
        }
        string currentSection = null;
        var lines = ReadFile();
        if (lines == null) return data;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            if (trimmed.StartsWith(';')) continue;
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                currentSection = trimmed.Trim('[', ']');
                if (!data.ContainsKey(currentSection)) data[currentSection] = new Dictionary<string, string>();
            }
            else if (currentSection != null && TryMatchKey(trimmed, out string key, out string value))
                data[currentSection][key] = value;
        }
        return data;
    }

    private string[] ReadFile()
    {
        if (!File.Exists(FilePath)) return null;
        try
        {
            byte[] fileBytes = File.ReadAllBytes(FilePath);
            if (fileBytes.Length < 24) return null;
            if (!ValidateChecksum(fileBytes)) return null;
            string content = Encoding.UTF8.GetString(fileBytes, 0, fileBytes.Length - 8);
            return content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        }
        catch { return null; }
    }

    public void SaveFile()
    {
        var content = new List<string>();
        lock (Lock)
        {
            foreach (var section in Data)
            {
                content.Add($"[{section.Key}]");
                foreach (var kvp in section.Value)
                    content.Add($"{kvp.Key} = {kvp.Value}");
                content.Add("");
            }
        }
        string fullText = string.Join(Environment.NewLine, content);
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(fullText);
        ClearFile();
        var dataWithChecksum = AddChecksum(plaintextBytes);
        File.WriteAllBytes(FilePath, dataWithChecksum);
        return;
    }

    private static bool ValidateChecksum(byte[] data)
    {
        if (data.Length < 8) return false;
        byte[] dataWithoutChecksum = data[..^8];
        byte[] checksumWithPlaceholder = new byte[data.Length];
        Array.Copy(dataWithoutChecksum, 0, checksumWithPlaceholder, 0, dataWithoutChecksum.Length);
        Array.Copy(new byte[8], 0, checksumWithPlaceholder, dataWithoutChecksum.Length, 8);
        byte[] calculatedChecksum = SHA256.HashData(checksumWithPlaceholder)[..8];
        byte[] storedChecksum = data[^8..];
        return storedChecksum.SequenceEqual(calculatedChecksum);
    }

    private static byte[] AddChecksum(byte[] data)
    {
        byte[] checksumWithPlaceholder = new byte[data.Length + 8];
        Array.Copy(data, 0, checksumWithPlaceholder, 0, data.Length);
        Array.Copy(new byte[8], 0, checksumWithPlaceholder, data.Length, 8);
        byte[] checksum = SHA256.HashData(checksumWithPlaceholder)[..8];
        byte[] result = new byte[data.Length + 8];
        Array.Copy(data, 0, result, 0, data.Length);
        Array.Copy(checksum, 0, result, data.Length, 8);
        return result;
    }

    #endregion

    #region Raw Data

    private string GetStringRaw(string section, string keyName)
    {
        lock (Lock)
            return Data.TryGetValue(section, out var sec) && sec.TryGetValue(keyName, out var val) ? val : null;
    }

    private static T TryParseValue<T>(string value, T defaultValue)
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

    private static bool TryMatchKey(string line) => TryMatchKey(line, out _, out _);

    private static bool TryMatchKey(string line, out string key, out string value)
    {
        key = null;
        value = null;
        int eqIndex = line.IndexOf('=');
        if (eqIndex == -1) return false;
        key = line.Substring(0, eqIndex).Trim();
        value = line.Substring(eqIndex + 1).Trim();
        return true;
    }

    #endregion

    #region User Methods

    /// <summary>
    /// Checks if a section exists in the specified path.
    /// </summary>
    /// <param name="section">The section to search for.</param>
    /// <returns><b>True</b> if the section exists, <b>false</b> otherwise.</returns>
    public bool SectionExists(string section) { lock (Lock) return Data.ContainsKey(section); }

    /// <summary>
    /// Checks if a key exists within a given section in the specified path.
    /// </summary>
    /// <param name="section">The section to search for.</param>
    /// <param name="key">The key to search for within the section.</param>
    /// <returns><b>True</b> if the key exists within the section, <b>false</b> otherwise.</returns>
    public bool KeyExists(string sectionName, string keyName)
    {
        if (!SectionExists(sectionName)) return false;
        lock (Lock) return Data[sectionName].ContainsKey(keyName);
    }

    /// <summary>This is a method to clear the INI file.</summary>
    public void ClearFile() => File.WriteAllText(FilePath, string.Empty);

    /// <summary>This is a method for adding a new section to the end of the file</summary>
    /// <param name="section">The section to write to.</param>
    public void AddSection(string section)
    {
        if (SectionExists(section)) return;
        lock (Lock) Data[section] = new Dictionary<string, string>();
        if (AutoSave) SaveFile();
    }

    /// <summary>This is a method of adding a new key to the end of a section</summary>
    /// <param name="section">The section to which the recording will be made.</param>
    /// <param name="key">The key that will be created.</param>
    /// <param name="value">The value that will be written to the key.</param>
    public void AddKeyInSection<T>(string section, string key, T value)
    {
        if (!SectionExists(section)) AddSection(section);
        if (KeyExists(section, key)) return;
        lock (Lock) Data[section][key] = value.ToString().Trim();
        if (AutoSave) SaveFile();
    }

    /// <summary>
    /// Reads a value of any type from an INI file. Automatically detects the type and parses the value.
    /// </summary>
    /// <typeparam name="T">Value type (bool, int, float, double, string, etc.)</typeparam>
    /// <param name="section">Reading section</param>
    /// <param name="key">Key for reading</param>
    /// <param name="defaultValue">Default value on read error</param>
    /// <returns>Value of the required type or defaultValue on error</returns>
    public T GetValue<T>(string section, string key, T defaultValue = default(T))
    {
        try
        {
            string stringValue = GetStringRaw(section, key);
            if (stringValue == null)
            {
                if (AutoAdd) AddKeyInSection(section, key, defaultValue?.ToString() ?? "");
                return defaultValue;
            }
            return TryParseValue<T>(stringValue, defaultValue);
        }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Writes a string to the specified secret in the INI file.
    /// </summary>
    /// <param name="section">The section to which the recording will be made.</param>
    /// <param name="key">The key by which the recording will be made.</param>
    /// <param name="value">The value that should be written to the file.</param>
    public void SetKey<T>(string section, string key, T value)
    {
        if (!SectionExists(section)) AddSection(section);
        if (!KeyExists(section, key))
        {
            AddKeyInSection(section, key, value);
            return;
        }
        lock (Lock) Data[section][key] = value.ToString().Trim();
        if (AutoSave) SaveFile();
    }

    /// <summary>
    /// Removes the specified key from the INI file section.
    /// </summary>
    /// <param name="section">The section in which the deletion is performed.</param>
    /// <param name="key">The key to be deleted.</param>
    public void RemoveKey(string section, string key)
    {
        if (!KeyExists(section, key)) return;
        lock (Lock) Data[section].Remove(key);
        if (AutoSave) SaveFile();
    }

    /// <summary>
    /// Removes the specified section from the INI file.
    /// </summary>
    /// <param name="section">The section to be deleted.</param>
    public void RemoveSection(string section)
    {
        if (!SectionExists(section)) return;
        lock (Lock) Data.Remove(section);
        if (AutoSave) SaveFile();
    }

    #endregion
}
