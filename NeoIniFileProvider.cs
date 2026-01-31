using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NeoIni;

internal sealed class NeoIniFileProvider
{
    private readonly string FilePath;
    private string TempFilePath => FilePath + ".tmp";
    private readonly byte[] EncryptionKey;
    private readonly bool AutoEncryption = false;

    internal NeoIniFileProvider(string filePath) => FilePath = filePath;

    internal NeoIniFileProvider(string filePath, byte[] encryptionKey)
    {
        FilePath = filePath;
        EncryptionKey = encryptionKey;
        AutoEncryption = true;
    }

    internal Dictionary<string, Dictionary<string, string>> GetData(bool useChecksum)
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
        var lines = ReadFile(useChecksum);
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
            else if (currentSection != null && NeoIniParser.TryMatchKey(trimmed.AsSpan(), out string key, out string value))
                data[currentSection][key] = value;
        }
        return data;
    }

    internal void DeleteFile() { if (File.Exists(FilePath)) File.Delete(FilePath); }

    private string[] ReadFile(bool useChecksum)
    {
        if (!File.Exists(FilePath)) return null;
        try
        {
            byte[] fileBytes = File.ReadAllBytes(FilePath);
            if (fileBytes.Length < 24) return null;
            string content;
            if (!ValidateChecksum(fileBytes, useChecksum)) return null;
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
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Failed to decrypt configuration file. Invalid encryption key or corrupted data.", ex);
        }
        catch { return null; }
    }

    internal void SaveFile(string content, bool useChecksum, bool useBackup)
    {
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(content);
        byte[] dataWithChecksum;
        if (!AutoEncryption)
        {
            dataWithChecksum = AddChecksum(plaintextBytes, useChecksum);
            File.WriteAllBytes(TempFilePath, dataWithChecksum);
        }
        else
        {
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
            dataWithChecksum = AddChecksum(encryptedData, useChecksum);
            File.WriteAllBytes(TempFilePath, dataWithChecksum);
        }
        if (useBackup) File.Replace(TempFilePath, FilePath, FilePath + ".backup");
        else File.Replace(TempFilePath, FilePath, null);
    }

    internal async Task SaveFileAsync(string content, bool useChecksum, bool useBackup)
    {
        byte[] plaintextBytes = Encoding.UTF8.GetBytes(content);
        byte[] dataWithChecksum;
        if (!AutoEncryption)
        {
            dataWithChecksum = AddChecksum(plaintextBytes, useChecksum);
            await File.WriteAllBytesAsync(TempFilePath, dataWithChecksum).ConfigureAwait(false);
        }
        else
        {
            using var aes = Aes.Create();
            aes.Key = EncryptionKey;
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor();
            await using var ms = new MemoryStream();
            await ms.WriteAsync(aes.IV, 0, aes.IV.Length).ConfigureAwait(false);
            await using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            await cs.WriteAsync(plaintextBytes, 0, plaintextBytes.Length).ConfigureAwait(false);
            await cs.FlushFinalBlockAsync().ConfigureAwait(false);
            byte[] encryptedData = ms.ToArray();
            dataWithChecksum = AddChecksum(encryptedData, useChecksum);
            await File.WriteAllBytesAsync(TempFilePath, dataWithChecksum).ConfigureAwait(false);
        }
        if (useBackup) File.Replace(TempFilePath, FilePath, FilePath + ".backup");
        else File.Replace(TempFilePath, FilePath, null);
    }

    private bool ValidateChecksum(byte[] data, bool useChecksum)
    {
        if (!useChecksum) return true;
        if (data.Length < 8) return false;
        byte[] checksumWithPlaceholder = new byte[data.Length];
        Array.Copy(data, checksumWithPlaceholder, data.Length - 8);
        Array.Copy(new byte[8], 0, checksumWithPlaceholder, data.Length - 8, 8);
        byte[] calculatedChecksum = SHA256.HashData(checksumWithPlaceholder)[..8];
        return data[^8..].SequenceEqual(calculatedChecksum);
    }

    private byte[] AddChecksum(byte[] data, bool useChecksum)
    {
        if (!useChecksum) return data;
        byte[] checksumWithPlaceholder = new byte[data.Length + 8];
        Array.Copy(data, checksumWithPlaceholder, data.Length);
        Array.Copy(new byte[8], 0, checksumWithPlaceholder, data.Length, 8);
        byte[] checksum = SHA256.HashData(checksumWithPlaceholder)[..8];
        Array.Copy(checksum, 0, checksumWithPlaceholder, data.Length, 8);
        return checksumWithPlaceholder;
    }
}
