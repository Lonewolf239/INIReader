# INIReader

INIReader is a C# class for working with INI files. It provides methods for creating, reading, and modifying INI files.

## Features

- Create an INI file if it doesn't exist.
- Read data from an INI file.
- Save data to an INI file.

## Usage

### Creating an INI File

To create an INI file if it doesn't exist, use one of the following methods:

```csharp
CreateIniFileIfNotExist(string path, string[] data)
CreateIniFileIfNotExist(string path, string data)
```

- `path`: The path to the INI file.
- `data`: The data to be written to the file, either as an array of strings or as a single string.

The method returns:
- `-1` if an error occurred during file creation.
- `0` if the file already exists.
- `1` if the file is created successfully.

To recreate an existing file, use one of the following methods:

```csharp
CreateIniFile(string path, string[] data)
CreateIniFile(string path, string data)
```

- `path`: The path to the INI file.
- `data`: The data to be written to the file, either as an array of strings or as a single string.

The method returns:
- `false` if an error occurred during file creation.
- `true` if the file is created successfully.

### Reading Data from an INI File

To read data from an INI file, use one of the following methods:

```csharp
GetBool(string path, string section, string key)
GetInt(string path, string section, string key)
GetSingle(string path, string section, string key)
GetDouble(string path, string section, string key)
GetString(string path, string section, string key)
```

- `path`: The path to the INI file.
- `section`: The section from which reading will be performed.
- `key`: The key by which the reading will be performed.

The method returns the corresponding data type from the specified key.

### Saving Data to an INI File

To save data to an INI file, use one of the following methods:

```csharp
SetKey(string path, string section, string key, string value)
SetKey(string path, string section, string key, bool value)
SetKey(string path, string section, string key, int value)
SetKey(string path, string section, string key, float value)
SetKey(string path, string section, string key, double value)
```

- `path`: The path to the INI file.
- `section`: The section where the data will be written.
- `key`: The key under which the data will be stored.
- `value`: The value to be written to the file.

The method returns `true` if the data is saved successfully, otherwise `false`.

## Example

```csharp
string iniFilePath = "config.ini";

// Create an INI file if it doesn't exist
string[] initialData = { "[Section1]", "Key1=Value1", "Key2=Value2" };
INIReader.CreateIniFileIfNotExist(iniFilePath, initialData);

// Read data from the INI file
string key1 = INIReader.GetString(iniFilePath, "Section1", "Key1");

// Modify data
INIReader.SetKey(iniFilePath, "Section1", "Key2", key1);
```

## Developer

This class is developed by Lonewolf239 (https://github.com/Lonewolf239).

## Version

1.0

`Feel free to customize and enhance the code according to your specific requirements.`