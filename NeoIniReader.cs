using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoIni;

/// <summary>
/// A class for working with INI files.
///
/// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
///
/// <b>Target Framework: .NET 8+</b>
///
/// <b>Version: 1.5.4.4</b>
///
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

    private readonly bool AutoEncryption = false;
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
            FileProvider = new(FilePath, EncryptionKey);
        }
        else FileProvider = new(FilePath);
        Data = FileProvider.GetData(UseChecksum);
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
        AutoEncryption = true;
        FileProvider = new(FilePath, EncryptionKey);
        Data = FileProvider.GetData(UseChecksum);
    }

    /// <summary>
    /// Releases managed resources and saves changes to the file.
    /// </summary>
    public void Dispose()
    {
        SaveFile();
        Data?.Clear();
    }

    /// <summary>
    /// Saves the current data to an INI file with checksums and encryption applied, if enabled.
    /// </summary>
    public void SaveFile() => FileProvider.SaveFile(NeoIniParser.GetContent(Lock, Data), UseChecksum, AutoBackup);

    /// <summary>
    /// Asynchronously saves the current data to the INI file.
    /// </summary>
    public async Task SaveFileAsync() => await FileProvider.SaveFileAsync(NeoIniParser.GetContent(Lock, Data), UseChecksum, AutoBackup);

    /// <summary>
    /// Determines whether a specific section exists in the loaded data.
    /// </summary>
    /// <param name="section">The name of the section to search for.</param>
    /// <returns><c>true</c> if the section exists; otherwise, <c>false</c>.</returns>
    public bool SectionExists(string section) { lock (Lock) return Data.ContainsKey(section); }

    /// <summary>
    /// Asynchronously determines whether a specific section exists in the loaded data.
    /// </summary>
    /// <param name="section">The name of the section to search for.</param>
    /// <returns>A task that represents the asynchronous operation.
	/// The task result contains <c>true</c> if the section exists; otherwise, <c>false</c>.</returns>
    public async Task<bool> SectionExistsAsync(string section) => await Task.Run(() => SectionExists(section));

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
    /// Asynchronously determines whether a specific key exists within a given section.
    /// </summary>
    /// <param name="section">The name of the section to search in.</param>
    /// <param name="key">The name of the key to search for.</param>
    /// <returns>A task that represents the asynchronous operation.
	/// The task result contains <c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    public async Task<bool> KeyExistsAsync(string section, string key) => await Task.Run(() => KeyExists(section, key));

    /// <summary>
    /// Adds a new section to the file if it does not already exist.
    /// </summary>
    /// <param name="section">The name of the new section.</param>
    public void AddSection(string section)
    {
        if (SectionExists(section)) return;
        lock (Lock) Data[section] = new Dictionary<string, string>();
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
            SaveFile();
    }

    /// <summary>
    /// Asynchronously adds a new section to the file if it does not already exist.
    /// </summary>
    /// <param name="section">The name of the new section.</param>
    public async Task AddSectionAsync(string section)
    {
        if (SectionExists(section)) return;
        lock (Lock) Data[section] = new Dictionary<string, string>();
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
            await SaveFileAsync();
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
        lock (Lock) Data[section][key] = value.ToString().Trim();
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
            SaveFile();
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
        if (!SectionExists(section)) AddSection(section);
        if (KeyExists(section, key)) return;
        lock (Lock) Data[section][key] = value.ToString().Trim();
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
            await SaveFileAsync();
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
            return NeoIniParser.TryParseValue<T>(stringValue, defaultValue);
        }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Asynchronously retrieves a value of a specified type from the INI file.
    /// </summary>
    /// <typeparam name="T">The expected type of the value (e.g., bool, int, double, string, etc.).</typeparam>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The name of the key to retrieve.</param>
    /// <param name="defaultValue">The value to return if the key or section does not exist, or if parsing fails.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the parsed value.</returns>
    public async Task<T> GetValueAsync<T>(string section, string key, T defaultValue = default(T)) =>
        await Task.Run(() => GetValue<T>(section, key, defaultValue));

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
        lock (Lock) Data[section][key] = value.ToString().Trim();
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
            SaveFile();
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
        if (!SectionExists(section)) AddSection(section);
        lock (Lock) Data[section][key] = value.ToString().Trim();
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
            await SaveFileAsync();
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
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
            SaveFile();
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
        if (AutoSave && (!UseAutoSaveInterval || ++SaveIterationCounter % AutoSaveInterval == 0))
            await SaveFileAsync();
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
            SaveFile();
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
            await SaveFileAsync();
    }

    /// <summary>
    /// Returns an array of all sections contained in the INI file.
    /// </summary>
    /// <returns>An array of strings containing the names of all sections.</returns>
    public string[] GetAllSections() { lock (Lock) return Data.Keys.ToArray(); }

    /// <summary>
    /// Asynchronously returns an array of all sections contained in the INI file.
    /// </summary>
    /// <returns>A task whose result contains an array of strings with the names of all sections.</returns>
    public async Task<string[]> GetAllSectionsAsync() => await Task.Run(GetAllSections);

    /// <summary>
    /// Returns an array of all keys in the specified INI file section.
    /// </summary>
    /// <param name="section">Name of the section to receive keys from.</param>
    /// <returns>An array of strings containing the names of all keys in the section, or an empty array if the section does not exist.</returns>
    public string[] GetAllKeys(string section)
    {
        if (!SectionExists(section)) return [];
        lock (Lock) return Data[section].Keys.ToArray();
    }

    /// <summary>
    /// Asynchronously returns an array of all keys in the specified INI file section.
    /// </summary>
    /// <param name="section">Name of the section to receive keys from.</param>
    /// <returns>A task whose result contains an array of strings containing the names of all keys in the section.</returns>
    public async Task<string[]> GetAllKeysAsync(string section) => await Task.Run(() => GetAllKeys(section));

    /// <summary>
    /// Reloads data from the INI file, updating the internal data structure.
    /// </summary>
    public void ReloadFromFile() { lock (Lock) Data = FileProvider.GetData(UseChecksum); }

    /// <summary>
    /// Asynchronously reloads data from an INI file.
    /// </summary>
    /// <returns>A task representing an asynchronous reboot operation.</returns>
    public async Task ReloadFromFileAsync() => await Task.Run(ReloadFromFile);

    /// <summary>
    /// Removes the INI file from disk.
    /// </summary>
    public void DeleteFile() => FileProvider.DeleteFile();

    /// <summary>
    /// Asynchronously deletes an INI file from disk.
    /// </summary>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    public async Task DeleteFileAsync() => await Task.Run(DeleteFile);

    /// <summary>
    /// Deletes the INI file from disk and clears the internal data structure.
    /// </summary>
    public void DeleteFileWithData()
    {
        DeleteFile();
        Data.Clear();
    }

    /// <summary>
    /// Asynchronously deletes an INI file from disk and cleans up its internal data structure.
    /// </summary>
    /// <returns>A task representing the asynchronous delete and cleanup operation.</returns>
    public async Task DeleteFileWithDataAsync() => await Task.Run(DeleteFileWithData);

    /// <summary>
    /// Returns the current encryption password if encryption is enabled, or a status message if disabled.
    /// </summary>
    /// <returns>The generated encryption password or status message.</returns>
    public string GetEncryptionPassword()
    {
        if (!AutoEncryption) return "AutoEncryption is disabled";
        return NeoIniEncryptionProvider.GetEncryptionPassword();
    }
}
