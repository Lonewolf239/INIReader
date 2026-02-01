# NeoIni

NeoIni is a full-featured C# library for INI file management. It provides secure, thread-safe operations for creating, reading, and modifying files with built-in checksum validation and optional AES encryption.

## Installation

```bash
dotnet add package NeoIni
```

**Package:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)  
**Version:** `1.5.6.3` | **.NET 6+**
**Developer:** [Lonewolf239](https://github.com/Lonewolf239)

## Features

- Read and parse typed values (bool, int, float, double, string, etc.) from INI files
- Automatic type detection and parsing with fallback to default values
- Thread-safe operations using locks
- Automatic file saving and section/key creation (configurable via `AutoSave`, `AutoAdd`, `AutoBackup`, etc.)
- Built-in checksum validation (SHA256) for data integrity
- Optional AES-256 encryption with user-specific password generation
- Support for standard INI format with sections and key-value pairs
- Full async method support for all operations
- Automatic directory creation and file initialization

## Security Features

- **Checksum**: SHA256 validation detects tampering or corruption
- **AES-256**: User-environment derived encryption key with IV
- **Thread-safe**: All operations protected by `lock`
- Password generated from `Environment.UserName`, `MachineName`, `UserDomainName`

## Target Framework

.NET 6+

## Usage

### Creating a NeoIni Instance

```cs
NeoIniReader reader = new("config.ini");                                          // Standard usage
NeoIniReader encryptedReader = new("config.ini", true);                           // With auto-encryption
NeoIniReader encryptedCustomReader = new("config.ini", "MySecretPas123");         // With user password
```

### Reading Values

```cs
string text = reader.GetValue<string>("Section1", "Key1", "default");
int number = reader.GetValue<int>("Section1", "Number", 0);
bool flag = reader.GetValue<bool>("Section1", "Enabled", false);
double value = reader.GetValue<double>("Section1", "Value", 0.0);
```

### Writing Values

```cs
reader.SetKey("Section1", "Key1", "Value1");
reader.SetKey("Section1", "Number", 42);
reader.SetKey("Section1", "Enabled", true);
```


## Example

```cs
using NeoIniReader reader = new("config.ini");

reader.SetKey("Database", "Host", "localhost");
reader.SetKey("Database", "Port", 5432);
reader.SetKey("Settings", "AutoSave", true);

string host = reader.GetValue<string>("Database", "Host", "127.0.0.1");
int port = reader.GetValue<int>("Database", "Port", 3306);

Console.WriteLine($"DB: {host}:{port}");
```

## Section/Key Management

```cs
bool exists = reader.SectionExists("Section1");
bool keyExists = reader.KeyExists("Section1", "Key1");

reader.AddSection("NewSection");
reader.RemoveKey("Section1", "Key1");
reader.RemoveSection("Section1");

string[] sections = reader.GetAllSections();
string[] keys = reader.GetAllKeys("Section1");
```

## File Operations

```cs
reader.SaveFile();                    // Manual save
await reader.SaveFileAsync();         // Async save

reader.ReloadFromFile();              // Reload from disk
await reader.ReloadFromFileAsync();   // Async reload from disk

reader.DeleteFile();                  // Delete file only
reader.DeleteFileWithData();          // Delete file + clear data
```

## API Reference

| Method | Description | Async Version |
|--------|-----------  |---------------|
| `GetValue<T>` | Read typed value with default fallback | `GetValueAsync<T>` |
| `SetKey<T>` | Set/create key-value | `SetKeyAsync<T>` |
| `AddSection` | Create section if missing | `AddSectionAsync` |
| `AddKeyInSection<T>` | Add unique key-value | `AddKeyInSectionAsync<T>` |
| `RemoveKey` | Delete specific key | `RemoveKeyAsync` |
| `RemoveSection` | Delete entire section | `RemoveSectionAsync` |
| `GetAllSections` | List all sections | - |
| `GetAllKeys` | List section keys | - |
| `GetSection` | Get all key-value pairs in section | - |
| `FindKeyInAllSections` | Search key across all sections | - |
| `ClearSection` | Remove all keys from section | `ClearSectionAsync` |
| `RenameKey` | Rename key in section | `RenameKeyAsync` |
| `RenameSection` | Rename entire section | `RenameSectionAsync` |
| `Search` | Search keys/values by pattern | - |
| `SectionExists` | Check if section exists | - |
| `KeyExists` | Check if key exists in section | - |
| `SaveFile` | Saving data to a file | `SaveFileAsync` |
| `ReloadFromFile` | Reloading data from a file | - |
| `DeleteFile` | Deleting a file from disk | - |
| `DeleteFileWithData` | Deleting a file and clearing data | - |
| `GetEncryptionPassword` | Getting the encryption password (or status) | - |

| Option | Description | Default |
|--------|-------------|---------|
| `AutoSave` | Automatically saves changes to disk after modifications | `true` |
| `UseAutoSaveInterval` | When AutoSave is enabled, determines if saves occur at regular intervals instead of after every single modification | `false` |
| `AutoSaveInterval` | Number of operations between automatic saves when both AutoSave and UseAutoSaveInterval are enabled. | `3` |
| `AutoBackup` | Creates backup files (.backup) during save operations for safety | `true` |
| `AutoAdd` | Automatically creates missing sections/keys with default values when reading via `GetValue<T>` | `true` |
| `UseChecksum` | Calculates and verifies checksums during load/save operations to detect corruption or tampering | `true` |

| Action | Description |
|--------|-------------|
| `OnSave` | Called before saving a file to disk |
| `OnLoad` | Called after successfully loading data from a file or reloading |
| `OnKeyChanged` | Called when the value of an existing key in a section changes |
| `OnKeyAdded` | Called when a new key is added to a section |
| `OnKeyRemoved` | Called when a key is removed from a section |
| `OnSectionChanged` | Called whenever a section changes (keys are changed/added/removed) |
| `OnSectionAdded` | Called when a new section is added |
| `OnSectionRemoved` | Called when a section is deleted |
| `OnDataCleared` | Called when the data is completely cleared |
| `OnAutoSave` | Called before automatic saving |
| `OnError` | Called when errors occur (parsing, saving, reading a file, etc.) |

## Philosophy

**Black Box Design**: Interact only through public API - internals abstracted
