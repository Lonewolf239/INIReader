# INIReader

INIReader is a C# class for working with INI files. It provides secure, thread-safe methods for creating, reading, and modifying INI files with built-in checksum validation and optional AES encryption.

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

### Creating an INIReader Instance

```cs
INIReader reader = new INIReader("config.ini");                    // Standard usage
INIReader encryptedReader = new INIReader("config.ini", true);     // With auto-encryption
```

- `path`: Path to the INI file (directory auto-created if needed)
- `autoEncryption`: Enable automatic AES encryption (uses machine/user-specific password)

### Reading Values

Use the generic `GetValue<T>` method for automatic type parsing:

```cs
string text = reader.GetValue<string>("Section1", "Key1", "default");
int number = reader.GetValue<int>("Section1", "Number", 0);
bool flag = reader.GetValue<bool>("Section1", "Enabled", false);
double value = reader.GetValue<double>("Section1", "Value", 0.0);
```

- Auto-creates missing sections/keys if `AutoAdd = true` (default)
- Returns `defaultValue` on parse errors or missing keys

### Writing Values

```cs
reader.SetKey("Section1", "Key1", "Value1");
reader.SetKey("Section1", "Number", 42);
reader.SetKey("Section1", "Enabled", true);
```

- Auto-creates missing sections if needed
- Supports any type (converted to string automatically)
- Auto-saves if `AutoSave = true` (default)

### Configuration Options

```cs
reader.AutoSave = false;  // Disable auto-save after changes
reader.AutoAdd = false;   // Disable auto-creation of missing keys/sections
reader.SaveFile();        // Manual save
```

### Section and Key Management

```cs
reader.AddSection("NewSection");           // Add empty section
reader.RemoveSection("Section1");          // Remove section
reader.RemoveKey("Section1", "Key1");      // Remove specific key
bool exists = reader.SectionExists("Section1");
bool keyExists = reader.KeyExists("Section1", "Key1");
```

## Security Features

- **Checksum Validation**: Every file includes SHA256 checksum for integrity verification
- **AES-256 Encryption**: Optional encryption with IV and key derivation from user/machine info
- **Thread Safety**: All operations protected by internal lock

## Example

```cs
INIReader reader = new INIReader("config.ini");

// Write data
reader.SetKey("Database", "Host", "localhost");
reader.SetKey("Database", "Port", 5432);
reader.SetKey("Settings", "AutoSave", true);

// Read with default values
string host = reader.GetValue<string>("Database", "Host", "127.0.0.1");
int port = reader.GetValue<int>("Database", "Port", 3306);

// Check existence
if (reader.SectionExists("User"))
{
    string name = reader.GetValue<string>("User", "Name", "Anonymous");
    Console.WriteLine($"User: {name}");
}

// Manual save (optional if AutoSave is true)
reader.SaveFile();
```

## File Format

INIReader uses standard INI format with enhanced security:

```
[Section1]
Key1 = Value1
Key2 = 123

[Section2]
Enabled = true
```

Files are stored with binary header (checksum + optional encryption).

## Developer

Developed by [Lonewolf239](https://github.com/Lonewolf239).

## Version 1.5
