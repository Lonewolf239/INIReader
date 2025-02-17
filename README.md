# INIReader

INIReader is a C# class for working with INI files. It provides methods for creating, reading, and modifying INI files.

## Features

- Read data from an INI file.
- Save data to an INI file.

## Usage

### Creating an INI File

```cs
INIReader reader = new INIReader(PATH);
```

- `path`: The path to the INI file.

### Reading Data from an INI File

To read data from an INI file, use one of the following methods:

```cs
reader.GetString(SECTION, KEY, DEFAULT_VALUE); // Return string
reader.GetBool(SECTION, KEY, DEFAULT_VALUE); // Return bool
reader.GetInt(SECTION, KEY, DEFAULT_VALUE); // Return int
reader.GetSingle(SECTION, KEY, DEFAULT_VALUE); // Return float
reader.GetDouble(SECTION, KEY, DEFAULT_VALUE); // Return double
reader.GetKeys(SECTION, KEY, DEFAULT_VALUE); // Return System.Windows.Forms.Keys
```

- `section`: The section from which reading will be performed.
- `key`: The key by which the reading will be performed.
- `default_value`: default value will be used in case of error, `it does not have to be specified`

The method returns the corresponding data type from the specified key.

### Saving Data to an INI File

To save data to an INI file, use one of the following method:

```cs
reader.SetKey(SECTION, KEY, VALUE);
```

- `section`: The section where the data will be written.
- `key`: The key under which the data will be stored.
- `value`: the value that will be written to the file, `accepts any types`

The method returns `true` if the data is saved successfully, otherwise `false`.

## Example

```cs
INIReader reader = new INIReader("config.ini");

// Writing data to INI file
reader.SetKey("[Section1]", "Key1", "Value1");
reader.SetKey("[Section1]", "Key2", "Value2");

// Read data from INI file
string key1 = reader.GetString("Section1", "Key1");

// Modify data
reader.SetKey("Section1", "Key2", key1);

// Read data from the INI file with default value, default value will be used in case of error
string key2 = reader.GetString("Section1", "Key2", "default");
```

## Developer

This class is developed by [Lonewolf239](https://github.com/Lonewolf239).

## Version 1.4

`Feel free to customize and enhance the code according to your specific requirements.`
