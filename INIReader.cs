using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NeoIni;

///<summary>
/// This is a class for working with INI files
/// <br></br>
/// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
/// <br></br>
/// <b>Target Framework: .NET 9+</b>
/// <br></br>
/// <b>Version: 1.5.2</b>
/// <br></br>
/// <b>Black Box Philosophy:</b> This class follows a strict "black box" design principle - users interact only through the public API without needing to understand internal implementation details. Input goes in, processed output comes out, internals remain hidden and abstracted.
/// </summary>
public class NeoIni
{
    private readonly Dictionary<string, Dictionary<string, string>> Data;
    private readonly string FilePath;
    public bool AutoSave = true;
    public bool AutoAdd = true;
    private bool AutoEncryption = false;
    private string EncryptionPassword;
    private readonly byte[] EncryptionKey;
    private readonly object Lock = new();

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
    public async Task<bool> SectionExistsAsync(string section) =>
        await Task.Run(() => SectionExists(section));

    /// <summary>
    /// Checks if a key exists within a given section in the specified path.
    /// </summary>
    /// <param name="section">The section to search for.</param>
    /// <param name="key">The key to search for within the section.</param>
    /// <returns><b>True</b> if the key exists within the section, <b>false</b> otherwise.</returns>
    public bool KeyExists(string section, string key)
    {
        if (!SectionExists(section)) return false;
        lock (Lock) return Data[section].ContainsKey(key);
    }
    public async Task<bool> KeyExistsAsync(string section, string key) =>
        await Task.Run(() => KeyExists(section, key));

    /// <summary>This is a method for adding a new section to the end of the file</summary>
    /// <param name="section">The section to write to.</param>
    public void AddSection(string section)
    {
        if (SectionExists(section)) return;
        lock (Lock) Data[section] = new Dictionary<string, string>();
        if (AutoSave) SaveFile();
    }
    public async Task AddSectionAsync(string section) =>
        await Task.Run(() => AddSection(section));

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
    public async Task AddKeyInSectionAsync<T>(string section, string key, T value) =>
        await Task.Run(() => AddKeyInSection<T>(section, key, value));

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
    public async Task<T> GetValueAsync<T>(string section, string key, T defaultValue = default(T)) =>
        await Task.Run(() => GetValue<T>(section, key, defaultValue));

    /// <summary>
    /// Writes a string to the specified secret in the INI file.
    /// </summary>
    /// <param name="section">The section to which the recording will be made.</param>
    /// <param name="key">The key by which the recording will be made.</param>
    /// <param name="value">The value that should be written to the file.</param>
    public void SetKey<T>(string section, string key, T value)
    {
        if (!SectionExists(section)) AddSection(section);
        lock (Lock) Data[section][key] = value.ToString().Trim();
        if (AutoSave) SaveFile();
    }
    public async Task SetKeyAsync<T>(string section, string key, T value) =>
        await Task.Run(() => SetKey<T>(section, key, value));

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
    public async Task RemoveKeyAsync(string section, string key) =>
        await Task.Run(() => RemoveKey(section, key));

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
    public async Task RemoveSectionAsync(string section) =>
        await Task.Run(() => RemoveSection(section));

    #endregion
}
