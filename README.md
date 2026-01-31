# NeoIni

NeoIni is a C# class for working with INI files. It provides secure, thread-safe methods for creating, reading, and modifying INI files with built-in checksum validation and optional AES encryption.

## Installation

```bash
dotnet add package NeoIni
```

**Package:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)  
**Version:** `1.5.2` | **.NET 9+**

## Features

- Read and parse typed values (bool, int, float, double, string, etc.) from INI files
- Automatic type detection and parsing with fallback to default values
- Thread-safe operations using locks
- Automatic file saving and section/key creation (configurable)
- Built-in checksum validation for data integrity
- Optional AES-256 encryption with user-specific password generation
- Support for standard INI format with sections and key-value pairs

## Target Framework

.NET 9+

## Usage

### Creating an NeoIni Instance

```cs
NeoIni reader = new("config.ini");                   // Standard usage
NeoIni encryptedReader = new("config.ini", true);    // With auto-encryption
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
NeoIni reader = new("config.ini");

reader.SetKey("Database", "Host", "localhost");
reader.SetKey("Database", "Port", 5432);
reader.SetKey("Settings", "AutoSave", true);

string host = reader.GetValue<string>("Database", "Host", "127.0.0.1");
int port = reader.GetValue<int>("Database", "Port", 3306);

Console.WriteLine($"DB: {host}:{port}");
```

## Security Features

- **Checksum**: SHA256 validation на каждый файл
- **AES-256**: Опциональное шифрование с IV
- **Thread-safe**: Все операции под `lock`

## Section/Key Management

```cs
reader.AddSection("NewSection");
reader.RemoveKey("Section1", "Key1");
bool exists = reader.SectionExists("Section1");
```

## Developer

[Lonewolf239](https://github.com/Lonewolf239)
