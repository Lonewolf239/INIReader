# NeoIni

NeoIni is a C# class for working with INI files. It provides secure, thread-safe methods for creating, reading, and modifying INI files with built-in checksum validation and optional AES encryption.

## Installation

```bash
dotnet add package NeoIni
```

**Package:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)  
**Version:** `1.5.5` | **.NET 6+**

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

## Target Framework

.NET 6+

## Usage

### Creating a NeoIni Instance

```cs
NeoIniReader reader = new("config.ini");                   // Standard usage
NeoIniReader encryptedReader = new("config.ini", true);    // With auto-encryption
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

## Configuration Options

- `AutoSave = true`: Automatically saves changes to disk after modifications.
- `UseAutoSaveInterval = true`: When AutoSave is enabled, determines if saves occur at regular intervals instead of after every single modification.
- `AutoSaveInterval = 5`: Number of operations between automatic saves when both AutoSave and UseAutoSaveInterval are enabled. Set to 0 to disable interval saving.
- `AutoBackup = true`: Creates backup files (.backup) during save operations for safety.
- `AutoAdd = true`: Automatically creates missing sections/keys with default values when reading via `GetValue<T>`.
- `UseChecksum = true`: Calculates and verifies checksums during load/save operations to detect corruption or tampering.


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

## Security Features

- **Checksum**: SHA256 validation detects tampering or corruption
- **AES-256**: User-environment derived encryption key with IV
- **Thread-safe**: All operations protected by `lock`
- Password generated from `Environment.UserName`, `MachineName`, `UserDomainName`

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
|--------|-------------|---------------|
| `GetValue<T>` | Read typed value with default fallback | `GetValueAsync<T>` |
| `SetKey<T>` | Set/create key-value | `SetKeyAsync<T>` |
| `AddSection` | Create section if missing | `AddSectionAsync` |
| `AddKeyInSection<T>` | Add unique key-value | `AddKeyInSectionAsync<T>` |
| `RemoveKey` | Delete specific key | `RemoveKeyAsync` |
| `RemoveSection` | Delete entire section | `RemoveSectionAsync` |
| `GetAllSections` | List all sections | `GetAllSectionsAsync` |
| `GetAllKeys` | List section keys | `GetAllKeysAsync` |

## Philosophy

**Black Box Design**: Interact only through public API - internals abstracted

## Developer

[Lonewolf239](https://github.com/Lonewolf239)
