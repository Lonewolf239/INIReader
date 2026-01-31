using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NeoIni;

/// <summary>
/// A class for working with INI files.
///
/// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
///
/// <b>Target Framework: .NET 9+</b>
///
/// <b>Version: 1.5.3</b>
///
/// <b>Black Box Philosophy:</b> This class follows a strict "black box" design principle - users interact only through the public API without needing to understand internal implementation details. Input goes in, processed output comes out, internals remain hidden and abstracted.
/// </summary>
public class NeoIni
{
    private Dictionary<string, Dictionary<string, string>> Data;
    private readonly string FilePath;

    /// <summary>
    /// Determines whether changes are automatically written to the disk after every modification.
	/// Default is <c>true</c>.
    /// </summary>
    public bool AutoSave = true;

    /// <summary>
    /// Determines whether missing keys are automatically added to the file with a default value when requested via <see cref="GetValue{T}"/>. 
	/// Default is <c>true</c>.
    /// </summary>
    public bool AutoAdd = true;
    private bool AutoEncryption = false;
    private string EncryptionPassword;
    private readonly byte[] EncryptionKey;
    private readonly object Lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NeoIni"/> class.
    /// </summary>
    /// <param name="path">The absolute or relative path to the INI file.</param>
    /// <param name="autoEncryption">If set to <c>true</c>, enables automatic encryption of the file content based on the user's environment.</param>
    public NeoIni(string path, bool autoEncryption = false)
    {
        FilePath = path;
        if (autoEncryption)
        {
            EncryptionPassword = GeneratePasswordFromUserId();
            EncryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(EncryptionPassword))[..32];
            AutoEncryption = true;
        }
        Data = GetData();
    }

    #region File System

    private static string GeneratePasswordFromUserId(int length = 16)
    {
        string userId = Environment.UserName ?? Environment.GetEnvironmentVariable("USER") ?? "unknown";
        string fullSeed = $"{userId}:{Environment.MachineName}:{Environment.UserDomainName ?? "local"}";
        using (var sha256 = SHA256.Create())
        {
            byte[] seedBytes = Encoding.UTF8.GetBytes(fullSeed);
            byte[] hash = sha256.ComputeHash(seedBytes);
            StringBuilder password = new StringBuilder(length);
            var rnd = new Random(BitConverter.ToInt32(hash, 0) ^ BitConverter.ToInt32(hash, 4));
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            for (int i = 0; i < length; i++)
                password.Append(chars[rnd.Next(chars.Length)]);
            return password.ToString();
        }
    }

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
            else if (currentSection != null && TryMatchKey(trimmed.AsSpan(), out string key, out string value))
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
            string content;
            if (!ValidateChecksum(fileBytes)) return null;
            if (!AutoEncryption)
            {
                content = Encoding.UTF8.GetString(fileBytes, 0, fileBytes.Length - 8);
                return content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            }
            byte[] iv = new byte[16];
            Array.Copy(fileBytes, 0, iv, 0, 16);
            int encryptedLength = fileBytes.Length - 16 - 8;
            byte[] encryptedContent = new byte[encryptedLength];
            Array.Copy(fileBytes, 16, encryptedContent, 0, encryptedLength);
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(encryptedContent);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            content = sr.ReadToEnd();
            return content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        }
        catch { return null; }
    }

    /// <summary>
    /// Saves the current data to an INI file with checksums and encryption applied, if enabled.
    /// </summary>
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
        byte[] dataWithChecksum;
        if (!AutoEncryption)
        {
            dataWithChecksum = AddChecksum(plaintextBytes);
            File.WriteAllBytes(FilePath, dataWithChecksum);
            return;
        }
        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(plaintextBytes, 0, plaintextBytes.Length);
        cs.FlushFinalBlock();
        byte[] encryptedData = ms.ToArray();
        dataWithChecksum = AddChecksum(encryptedData);
        File.WriteAllBytes(FilePath, dataWithChecksum);
    }

    /// <summary>
    /// Asynchronously saves the current data to the INI file.
    /// </summary>
    public async Task SaveFileAsync() => await Task.Run(SaveFile);

    private static bool ValidateChecksum(byte[] data)
    {
        if (data.Length < 8) return false;
        byte[] checksumWithPlaceholder = new byte[data.Length];
        Array.Copy(data, checksumWithPlaceholder, data.Length - 8);
        Array.Copy(new byte[8], 0, checksumWithPlaceholder, data.Length - 8, 8);
        byte[] calculatedChecksum = SHA256.HashData(checksumWithPlaceholder)[..8];
        return data[^8..].SequenceEqual(calculatedChecksum);
    }

    private static byte[] AddChecksum(byte[] data)
    {
        byte[] checksumWithPlaceholder = new byte[data.Length + 8];
        Array.Copy(data, checksumWithPlaceholder, data.Length);
        Array.Copy(new byte[8], 0, checksumWithPlaceholder, data.Length, 8);
        byte[] checksum = SHA256.HashData(checksumWithPlaceholder)[..8];
        Array.Copy(checksum, 0, checksumWithPlaceholder, data.Length, 8);
        return checksumWithPlaceholder;
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

    private static bool TryMatchKey(ReadOnlySpan<char> line, out string key, out string value)
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

    #endregion

    #region User Methods

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
        if (AutoSave) SaveFile();
    }

    /// <summary>
    /// Asynchronously adds a new section to the file if it does not already exist.
    /// </summary>
    /// <param name="section">The name of the new section.</param>
    public async Task AddSectionAsync(string section) => await Task.Run(() => AddSection(section));

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
        if (AutoSave) SaveFile();
    }

    /// <summary>
    /// Asynchronously adds a new key-value pair to a specified section.
    /// </summary>
    /// <typeparam name="T">The type of the value being added.</typeparam>
    /// <param name="section">The name of the target section.</param>
    /// <param name="key">The name of the key to create.</param>
    /// <param name="value">The value to assign to the key.</param>
    public async Task AddKeyInSectionAsync<T>(string section, string key, T value) =>
        await Task.Run(() => AddKeyInSection<T>(section, key, value));

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
        if (AutoSave) SaveFile();
    }

    /// <summary>
    /// Asynchronously updates or creates a key-value pair in the specified section.
    /// </summary>
    /// <typeparam name="T">The type of the value to be stored.</typeparam>
    /// <param name="section">The name of the section where the value will be written.</param>
    /// <param name="key">The key to update or create.</param>
    /// <param name="value">The value to write to the file.</param>
    public async Task SetKeyAsync<T>(string section, string key, T value) => await Task.Run(() => SetKey<T>(section, key, value));

    /// <summary>
    /// Removes a specific key from a section in the INI file.
    /// </summary>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The key to remove.</param>
    public void RemoveKey(string section, string key)
    {
        if (!KeyExists(section, key)) return;
        lock (Lock) Data[section].Remove(key);
        if (AutoSave) SaveFile();
    }

    /// <summary>
    /// Asynchronously removes a specific key from a section in the INI file.
    /// </summary>
    /// <param name="section">The section containing the key.</param>
    /// <param name="key">The key to remove.</param>
    public async Task RemoveKeyAsync(string section, string key) => await Task.Run(() => RemoveKey(section, key));

    /// <summary>
    /// Removes an entire section and all its keys from the INI file.
    /// </summary>
    /// <param name="section">The name of the section to remove.</param>
    public void RemoveSection(string section)
    {
        if (!SectionExists(section)) return;
        lock (Lock) Data.Remove(section);
        if (AutoSave) SaveFile();
    }

    /// <summary>
    /// Asynchronously removes an entire section and all its keys from the INI file.
    /// </summary>
    /// <param name="section">The name of the section to remove.</param>
    public async Task RemoveSectionAsync(string section) => await Task.Run(() => RemoveSection(section));

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
    public void ReloadFromFile() { lock (Lock) Data = GetData(); }

    /// <summary>
    /// Asynchronously reloads data from an INI file.
    /// </summary>
    /// <returns>A task representing an asynchronous reboot operation.</returns>
    public async Task ReloadFromFileAsync() => await Task.Run(ReloadFromFile);

    /// <summary>
    /// Removes the INI file from disk.
    /// </summary>
    public void DeleteFile() { if (File.Exists(FilePath)) File.Delete(FilePath); }

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

    #endregion
}
