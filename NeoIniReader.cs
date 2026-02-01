using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoIni;

/// <summary>
/// A class for working with INI files.
/// <br/>
/// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
/// <br/>
/// <b>Target Framework: .NET 6+</b>
/// <br/>
/// <b>Version: 1.5.6.2</b>
/// <br/>
/// <b>Black Box Philosophy:</b> This class follows a strict "black box" design principle - users interact only through the public API without needing to understand internal implementation details. Input goes in, processed output comes out, internals remain hidden and abstracted.
/// </summary>
public class NeoIniReader : IDisposable
{
    private Dictionary<string, Dictionary<string, string>> Data;
    private readonly string FilePath;

    /// <summary>
    /// Determines whether changes are automatically written to the disk after every modification.
	/// Default is <c>true</c>.
    /// </summary>
    public bool AutoSave = true;

    /// <summary>
    /// Determines whether automatic saves occur at regular intervals when <see cref="AutoSave"/> is enabled.
    /// When <c>false</c>, saves happen after every modification instead of waiting for the interval.
    /// Default value is <c>false</c>.
    /// </summary>
    public bool UseAutoSaveInterval = false;

    /// <summary>
    /// Interval (in operations) between automatic saves when <see cref="AutoSave"/> is enabled.
    /// Default value is 3.
    /// </summary>
    public int AutoSaveInterval
    {
        get => _AutoSaveInterval;
        set
        {
            if (value < 0) throw new ArgumentException("Interval cannot be negative.");
            UseAutoSaveInterval = value != 0;
            _AutoSaveInterval = value;
        }
    }
    private int _AutoSaveInterval = 3;
    private int SaveIterationCounter = 0;

    /// <summary>
    /// Determines whether backup files (.backup) are created during save operations.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool AutoBackup = true;

    /// <summary>
    /// Determines whether missing keys are automatically added to the file with a default value when requested via <see cref="GetValue{T}"/>. 
	/// Default is <c>true</c>.
    /// </summary>
    public bool AutoAdd = true;

    /// <summary>
    /// Determines whether a checksum is calculated and verified during file load and save operations to ensure data integrity.
    /// When enabled, the configuration file includes a checksum that detects corruption or tampering.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool UseChecksum = true;

    /// <summary>
    /// Called before saving a file to disk.
    /// </summary>
    public Action OnSave;

    /// <summary>
    /// Called after successfully loading data from a file or reloading.
    /// </summary>
    public Action OnLoad;

    /// <summary>
    /// Called when the value of an existing key in a section changes.
    /// </summary>
    /// <param>Name of the section where the change occurred.</param>
    /// <param>Name of the changed key.</param>
    /// <param>New value of the key.</param>
    public Action<string, string, string> OnKeyChanged;

    /// <summary>
    /// Called when a new key is added to a section.
    /// </summary>
    /// <param>Name of the section to which the key was added.</param>
    /// <param>Name of the added key.</param>
    /// <param>Value of the added key.</param>
    public Action<string, string, string> OnKeyAdded;

    /// <summary>
    /// Called when a key is removed from a section.
    /// </summary>
    /// <param>The name of the section from which the key was removed.</param>
    /// <param>The name of the removed key.</param>
    public Action<string, string> OnKeyRemoved;

    /// <summary>
    /// Called whenever a section changes (keys are changed/added/removed).
    /// </summary>
    /// <param>Name of the modified section.</param>
    public Action<string> OnSectionChanged;

    /// <summary>
    /// Called when a new section is added.
    /// </summary>
    /// <param>The name of the added section.</param>
    public Action<string> OnSectionAdded;

    /// <summary>
    /// Called when a section is deleted.
    /// </summary>
    /// <param>Name of the deleted section.</param>
    public Action<string> OnSectionRemoved;

    /// <summary>
    /// Called when the data is completely cleared.
    /// </summary>
    public Action OnDataCleared;

    /// <summary>
    /// Called before automatic saving.
    /// </summary>
    public Action OnAutoSave;

    /// <summary>
    /// Called when errors occur (parsing, saving, reading a file, etc.).
    /// </summary>
    /// <param>An exception containing information about the error.</param>
    public Action<Exception> OnError;

    private readonly bool AutoEncryption = false;
    private readonly bool CustomEncryptionPassword = false;
    private readonly byte[] EncryptionKey;
    private readonly object Lock = new();

    private readonly NeoIniFileProvider FileProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoIni"/> class.
    /// </summary>
    /// <param name="path">The absolute or relative path to the INI file.</param>
    /// <param name="autoEncryption">
    /// If set to <c>true</c>, enables automatic encryption of the file content based on the user's environment.
    /// <para><b>Warning:</b> Enabling encryption ties the file to the specific machine/user 
    /// environment. The file will be unreadable on other computers due to machine-specific key generation!</para>
    /// </param>
    public NeoIniReader(string path, bool autoEncryption = false)
    {
        FilePath = path;
        if (autoEncryption)
        {
            EncryptionKey = NeoIniEncryptionProvider.GetEncryptionKey();
            AutoEncryption = true;
            FileProvider = new(FilePath, EncryptionKey, OnError);
        }
        else FileProvider = new(FilePath, OnError);
        Data = FileProvider.GetData(UseChecksum);
        OnLoad?.Invoke();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoIni"/> class with custom encryption.
    /// </summary>
    /// <param name="path">The absolute or relative path to the INI file.</param>
    /// <param name="encryptionPassword">The password used to derive the encryption key.</param>
    public NeoIniReader(string path, string encryptionPassword)
    {
        FilePath = path;
        if (string.IsNullOrEmpty(encryptionPassword))
            throw new ArgumentException("Encryption password cannot be null or empty.", nameof(encryptionPassword));
        EncryptionKey = NeoIniEncryptionProvider.GetEncryptionKey(encryptionPassword);
        AutoEncryption = CustomEncryptionPassword = true;
        FileProvider = new(FilePath, EncryptionKey, OnError);
        Data = FileProvider.GetData(UseChecksum);
        OnLoad?.Invoke();
    }

    /// <summary>
    /// Releases managed resources and saves changes to the file.
    /// </summary>
    public void Dispose()
    {
        SaveFile();
        Data?.Clear();
        OnDataCleared?.Invoke();
    }

    /// <summary>
    /// Saves the current data to an INI file with checksums and encryption applied, if enabled.
    /// </summary>
    public void SaveFile()
    {
        OnSave?.Invoke();
        FileProvider.SaveFile(NeoIniParser.GetContent(Lock, Data), UseChecksum, AutoBackup);
    }

    /// <summary>
    /// Asynchronously saves the current data to the INI file.
    /// </summary>
    public async Task SaveFileAsync()
    {
        OnSave?.Invoke();
        await FileProvider.SaveFileAsync(NeoIniParser.GetContent(Lock, Data), UseChecksum, AutoBackup);
    }

    /// <summary>
    /// Determines whether a specific section exists in the loaded data.
    /// </summary>
    /// <param name="section">The name of the section to search for.</param>
    /// <returns><c>true</c> if the section exists; otherwise, <c>false</c>.</returns>
    public bool SectionExists(string section) { lock (Lock) return Data.ContainsKey(section); }

    /// <summary>
    /// Determines whether a specific key exists within a given section.
    /// </summary>
    /// <param name="section">The name of the section to search in.</param>
    /// <param name="key">The name of the key to search for.</param>
    /// <returns><c>true</c> if the key exists within the section; otherwise, <c>false</c>.</returns>
    public bool KeyExists(string section, string key)
    {
        if (!SectionExists(section)) return false;
        lock (Lock) return Data[section].ContainsKey(key);
    }

    /// <summary>
    /// Adds a new section to the file if it does not already exist.
    /// </summary>
    /// <param name="section">The name of the new section.</param>
    public void AddSection(string section)
    {
        if (SectionExists(section)) return;
        lock (Lock) Data[section] = new Dictionary<string, string>();
        OnSectionAdded?.Invoke(section);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            SaveFile();
        }
    }

    /// <summary>
    /// Asynchronously adds a new section to the file if it does not already exist.
    /// </summary>
    /// <param name="section">The name of the new section.</param>
    public async Task AddSectionAsync(string section)
    {
        if (SectionExists(section)) return;
        lock (Lock) Data[section] = new Dictionary<string, string>();
        OnSectionAdded?.Invoke(section);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            await SaveFileAsync();
        }
    }

    /// <summary>
    /// Adds a new key-value pair to a specified section.
    /// </summary>
    /// <typeparam name="T">The type of the value being added.</typeparam>
    /// <param name="section">The name of the target section.</param>
    /// <param name="key">The name of the key to create.</param>
    /// <param name="value">The value to assign to the key.</param>
    public void AddKeyInSection<T>(string section, string key, T value)
    {
        if (!SectionExists(section)) AddSection(section);
        if (KeyExists(section, key)) return;
        string valueString = value.ToString().Trim();
        lock (Lock) Data[section][key] = valueString;
        OnKeyAdded?.Invoke(section, key, valueString);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            SaveFile();
        }
    }

    /// <summary>
    /// Asynchronously adds a new key-value pair to a specified section.
    /// </summary>
    /// <typeparam name="T">The type of the value being added.</typeparam>
    /// <param name="section">The name of the target section.</param>
    /// <param name="key">The name of the key to create.</param>
    /// <param name="value">The value to assign to the key.</param>
    public async Task AddKeyInSectionAsync<T>(string section, string key, T value)
    {
        if (!SectionExists(section)) await AddSectionAsync(section);
        if (KeyExists(section, key)) return;
        string valueString = value.ToString().Trim();
        lock (Lock) Data[section][key] = valueString;
        OnKeyAdded?.Invoke(section, key, valueString);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            await SaveFileAsync();
        }
    }

    /// <summary>
    /// Retrieves a value of a specified type from the INI file.
	/// Automatically parses the string value to the target type.
    /// </summary>
    /// <typeparam name="T">The expected type of the value (e.g., bool, int, double, string, etc.).</typeparam>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The name of the key to retrieve.</param>
    /// <param name="defaultValue">The value to return if the key or section does not exist, or if parsing fails.</param>
    /// <returns>The parsed value, or <paramref name="defaultValue"/> if retrieval fails.</returns>
    public T GetValue<T>(string section, string key, T defaultValue = default(T))
    {
        try
        {
            string stringValue = NeoIniParser.GetStringRaw(Lock, Data, section, key);
            if (stringValue == null)
            {
                if (AutoAdd) AddKeyInSection(section, key, defaultValue?.ToString() ?? "");
                return defaultValue;
            }
            return NeoIniParser.TryParseValue<T>(stringValue, defaultValue, OnError);
        }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Asynchronously retrieves a value of a specified type from the INI file.
    /// Automatically parses the string value to the target type.
    /// </summary>
    /// <typeparam name="T">The expected type of the value (e.g., bool, int, double, string, etc.).</typeparam>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The name of the key to retrieve.</param>
    /// <param name="defaultValue">The value to return if the key or section does not exist, or if parsing fails.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the parsed value, or <paramref name="defaultValue"/> if retrieval fails.</returns>
    public async Task<T> GetValueAsync<T>(string section, string key, T defaultValue = default(T))
    {
        try
        {
            string stringValue = NeoIniParser.GetStringRaw(Lock, Data, section, key);
            if (stringValue == null)
            {
                if (AutoAdd) await AddKeyInSectionAsync(section, key, defaultValue?.ToString() ?? "");
                return defaultValue;
            }
            return NeoIniParser.TryParseValue<T>(stringValue, defaultValue, OnError);
        }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Updates or creates a key-value pair in the specified section.
    /// </summary>
    /// <typeparam name="T">The type of the value to be stored.</typeparam>
    /// <param name="section">The name of the section where the value will be written.</param>
    /// <param name="key">The key to update or create.</param>
    /// <param name="value">The value to write to the file.</param>
    public void SetKey<T>(string section, string key, T value)
    {
        if (!SectionExists(section)) AddSection(section);
        bool keyExists = KeyExists(section, key);
        string valueString = value.ToString().Trim();
        lock (Lock) Data[section][key] = valueString;
        if (keyExists) OnKeyChanged?.Invoke(section, key, valueString);
        else OnKeyAdded?.Invoke(section, key, valueString);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            SaveFile();
        }
    }

    /// <summary>
    /// Asynchronously updates or creates a key-value pair in the specified section.
    /// </summary>
    /// <typeparam name="T">The type of the value to be stored.</typeparam>
    /// <param name="section">The name of the section where the value will be written.</param>
    /// <param name="key">The key to update or create.</param>
    /// <param name="value">The value to write to the file.</param>
    public async Task SetKeyAsync<T>(string section, string key, T value)
    {
        if (!SectionExists(section)) await AddSectionAsync(section);
        bool keyExists = KeyExists(section, key);
        string valueString = value.ToString().Trim();
        lock (Lock) Data[section][key] = valueString;
        if (keyExists) OnKeyChanged?.Invoke(section, key, valueString);
        else OnKeyAdded?.Invoke(section, key, valueString);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            await SaveFileAsync();
        }
    }

    /// <summary>
    /// Removes a specific key from a section in the INI file.
    /// </summary>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The key to remove.</param>
    public void RemoveKey(string section, string key)
    {
        if (!KeyExists(section, key)) return;
        lock (Lock) Data[section].Remove(key);
        OnKeyRemoved?.Invoke(section, key);
        OnSectionChanged?.Invoke(section);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            SaveFile();
        }
    }

    /// <summary>
    /// Asynchronously removes a specific key from a section in the INI file.
    /// </summary>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The key to remove.</param>
    public async Task RemoveKeyAsync(string section, string key)
    {
        if (!KeyExists(section, key)) return;
        lock (Lock) Data[section].Remove(key);
        OnKeyRemoved?.Invoke(section, key);
        OnSectionChanged?.Invoke(section);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            await SaveFileAsync();
        }
    }

    /// <summary>
    /// Removes an entire section and all its keys from the INI file.
    /// </summary>
    /// <param name="section">The name of the section to remove.</param>
    public void RemoveSection(string section)
    {
        if (!SectionExists(section)) return;
        lock (Lock) Data.Remove(section);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            SaveFile();
        }
    }

    /// <summary>
    /// Asynchronously removes an entire section and all its keys from the INI file.
    /// </summary>
    /// <param name="section">The name of the section to remove.</param>
    public async Task RemoveSectionAsync(string section)
    {
        if (!SectionExists(section)) return;
        lock (Lock) Data.Remove(section);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            await SaveFileAsync();
        }
    }

    /// <summary>
    /// Returns an array of all sections contained in the INI file.
    /// </summary>
    /// <returns>An array of strings containing the names of all sections.</returns>
    public string[] GetAllSections() { lock (Lock) return Data.Keys.ToArray(); }

    /// <summary>
    /// Returns an array of all keys in the specified INI file section.
    /// </summary>
    /// <param name="section">Name of the section to receive keys from.</param>
    /// <returns>An array of strings containing the names of all keys in the section, or an empty array if the section does not exist.</returns>
    public string[] GetAllKeys(string section)
    {
        if (!SectionExists(section)) return new string[1] { "" };
        lock (Lock) return Data[section].Keys.ToArray();
    }

    /// <summary>
    /// Returns a dictionary containing all key-value pairs from the specified section.
    /// </summary>
    /// <param name="section">The name of the section to retrieve.</param>
    /// <returns>A read-only copy of the section's key-value pairs, or an empty dictionary if the section does not exist.</returns>
    public Dictionary<string, string> GetSection(string section)
    {
        if (!SectionExists(section)) return new Dictionary<string, string>();
        lock (Lock) return new Dictionary<string, string>(Data[section]);
    }

    /// <summary>
    /// Searches for a specific key across all sections and returns a dictionary mapping section names to their corresponding values.
    /// </summary>
    /// <param name="key">The key name to search for across all sections.</param>
    /// <returns>A dictionary where keys are section names and values are the corresponding key values found, or an empty dictionary if no matches are found.</returns>
    public Dictionary<string, string> FindKeyInAllSections(string key)
    {
        var results = new Dictionary<string, string>();
        lock (Lock)
        {
            foreach (var section in Data)
            {
                if (section.Value.TryGetValue(key, out var value))
                    results[section.Key] = value;
            }
        }
        return results;
    }

    /// <summary>
    /// Clears all keys from the specified section while keeping the section itself intact.
    /// </summary>
    /// <param name="section">The name of the section to clear.</param>
    public void ClearSection(string section)
    {
        if (!SectionExists(section)) return;
        lock (Lock) Data[section].Clear();
        OnSectionChanged?.Invoke(section);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            SaveFile();
        }
    }

    /// <summary>
    /// Asynchronously clears all keys from the specified section.
    /// </summary>
    /// <param name="section">The name of the section to clear.</param>
    public async void ClearSectionAsync(string section)
    {
        if (!SectionExists(section)) return;
        lock (Lock) Data[section].Clear();
        OnSectionChanged?.Invoke(section);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            await SaveFileAsync();
        }
    }

    /// <summary>
    /// Renames a key within a specific section by copying its value to a new key name and removing the old one.
    /// </summary>
    /// <param name="section">The section containing the key to rename.</param>
    /// <param name="oldKey">The current name of the key.</param>
    /// <param name="newKey">The new name for the key.</param>
    public void RenameKey(string section, string oldKey, string newKey)
    {
        if (!KeyExists(section, oldKey)) return;
        lock (Lock)
        {
            Data[section][newKey] = Data[section][oldKey];
            Data[section].Remove(oldKey);
            OnKeyChanged?.Invoke(section, oldKey, Data[section][newKey]);
        }
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            SaveFile();
        }
    }

    /// <summary>
    /// Asynchronously renames a key within a specific section.
    /// </summary>
    /// <param name="section">The section containing the key to rename.</param>
    /// <param name="oldKey">The current name of the key.</param>
    /// <param name="newKey">The new name for the key.</param>
    public async void RenameKeyAsync(string section, string oldKey, string newKey)
    {
        if (!KeyExists(section, oldKey)) return;
        lock (Lock)
        {
            Data[section][newKey] = Data[section][oldKey];
            Data[section].Remove(oldKey);
            OnKeyChanged?.Invoke(section, oldKey, Data[section][newKey]);
        }
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            await SaveFileAsync();
        }
    }

    /// <summary>
    /// Renames an entire section by moving all its contents to a new section name and removing the old one.
    /// </summary>
    /// <param name="oldSection">The current name of the section.</param>
    /// <param name="newSection">The new name for the section.</param>
    public void RenameSection(string oldSection, string newSection)
    {
        if (!SectionExists(oldSection) || SectionExists(newSection)) return;
        lock (Lock)
        {
            Data[newSection] = Data[oldSection];
            Data.Remove(oldSection);
        }
        OnSectionChanged?.Invoke(oldSection);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            SaveFile();
        }
    }

    /// <summary>
    /// Asynchronously renames an entire section.
    /// </summary>
    /// <param name="oldSection">The current name of the section.</param>
    /// <param name="newSection">The new name for the section.</param>
    public async void RenameSectionAsync(string oldSection, string newSection)
    {
        if (!SectionExists(oldSection) || SectionExists(newSection)) return;
        lock (Lock)
        {
            Data[newSection] = Data[oldSection];
            Data.Remove(oldSection);
        }
        OnSectionChanged?.Invoke(oldSection);
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
        {
            OnAutoSave?.Invoke();
            await SaveFileAsync();
        }
    }

    /// <summary>
    /// Searches for keys or values matching a pattern across all sections and returns matching entries.
    /// </summary>
    /// <param name="pattern">The search pattern to match against keys and values (case-insensitive).</param>
    /// <returns>A list of tuples containing (section, key, value) for all matches found.</returns>
    public List<(string section, string key, string value)> Search(string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return new List<(string, string, string)>();
        var results = new List<(string, string, string)>();
        var searchPattern = pattern.ToLowerInvariant();
        lock (Lock)
        {
            foreach (var section in Data)
            {
                foreach (var kvp in section.Value)
                {
                    if ((kvp.Key?.ToLowerInvariant().Contains(searchPattern) == true) ||
                            (kvp.Value?.ToLowerInvariant().Contains(searchPattern) == true))
                        results.Add((section.Key, kvp.Key, kvp.Value));
                }
            }
        }
        return results;
    }

    /// <summary>
    /// Reloads data from the INI file, updating the internal data structure.
    /// </summary>
    public void ReloadFromFile()
    {
        lock (Lock) Data = FileProvider.GetData(UseChecksum);
        OnLoad?.Invoke();
    }

    /// <summary>
    /// Removes the INI file from disk.
    /// </summary>
    public void DeleteFile() => FileProvider.DeleteFile();

    /// <summary>
    /// Deletes the INI file from disk and clears the internal data structure.
    /// </summary>
    public void DeleteFileWithData()
    {
        DeleteFile();
        lock (Lock) Data.Clear();
        OnDataCleared?.Invoke();
    }

    /// <summary>
    /// Clears the internal data structure.
    /// </summary>
    public void Clear() { lock (Lock) Data.Clear(); }

    /// <summary>
    /// Returns the current encryption password if encryption is enabled, or a status message if disabled.
    /// </summary>
    /// <returns>The generated encryption password or status message.</returns>
    public string GetEncryptionPassword()
    {
        if (!AutoEncryption) return "AutoEncryption is disabled";
        if (CustomEncryptionPassword) return "CustomEncryptionPassword is used. For security reasons, the password is not saved.";
        return NeoIniEncryptionProvider.GetEncryptionPassword();
    }
}
