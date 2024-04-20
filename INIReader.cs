using System;
using System.Collections.Generic;
using System.IO;

namespace IniReader
{
    /// <summary>
    /// This is a class for working with INI files
    /// <br></br>
    /// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
    /// <br></br>
    /// <b>Version: 1.2</b>
    /// </summary>
    internal class INIReader
    {

        private static string[][] GetData(string path)
        {
            List<string[]> data = new List<string[]>();
            List<string> currentSection = new List<string>();
            string[] file = File.ReadAllLines(path);
            foreach (string line in file)
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (currentSection.Count > 0)
                    {
                        data.Add(currentSection.ToArray());
                        currentSection.Clear();
                    }
                }
                if (line.Length != 0)
                    currentSection.Add(line);
            }
            if (currentSection.Count > 0)
                data.Add(currentSection.ToArray());
            string[][] result = data.ToArray();
            currentSection.Clear();
            data.Clear();
            return result;
        }

        private static bool SaveFile(string path, string[][] data)
        {
            try
            {
                File.Delete(path);
                foreach (string[] lines in data)
                    File.AppendAllLines(path, lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a section exists in the specified path.
        /// </summary>
        /// <param name="path">The path to the data source.</param>
        /// <param name="section">The section to search for.</param>
        /// <returns><b>True</b> if the section exists, <b>false</b> otherwise.</returns>
        public static bool SectionExist(string path, string section)
        {
            string[][] data = GetData(path);
            bool result = false;
            foreach (string[] sections in data)
            {
                if (sections[0].Contains(section))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if a key exists within a given section in the specified path.
        /// </summary>
        /// <param name="path">The path to the data source.</param>
        /// <param name="section">The section to search for.</param>
        /// <param name="key">The key to search for within the section.</param>
        /// <returns><b>True</b> if the key exists within the section, <b>false</b> otherwise.</returns>
        public static bool KeyExist(string path, string section, string key)
        {
            string[][] data = GetData(path);
            bool result = false;
            foreach (string[] sections in data)
            {
                if (sections[0].Contains(section))
                {
                    foreach (string keys in sections)
                    {
                        if (keys.Contains(key))
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>This is a method for creating an INI file if it is missing</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="data">An array of strings representing the data to be written to the file.</param>
        /// <returns>
        /// If an error occurred during file creation: <b>-1</b><br></br>
        /// If the file exists: <b>0</b><br></br>
        /// If the file is created successfully: <b>1</b>
        /// </returns>
        public static int CreateIniFileIfNotExist(string path, string[] data)
        {
            if (File.Exists(path))
                return 0;
            else
            {
                try
                {
                    File.WriteAllLines(path, data);
                    return 1;
                }
                catch
                {
                    return -1;
                }
            }
        }

        /// <summary>This is a method for creating an INI file if it is missing</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="data">A string representing the data to be written to the file.</param>
        /// <returns>
        /// If an error occurred during file creation: <b>-1</b><br></br>
        /// If the file exists: <b>0</b><br></br>
        /// If the file is created successfully: <b>1</b>
        /// </returns>
        public static int CreateIniFileIfNotExist(string path, string data)
        {
            if (File.Exists(path))
                return 0;
            else
            {
                try
                {
                    File.WriteAllText(path, data);
                    return 1;
                }
                catch
                {
                    return -1;
                }
            }
        }

        /// <summary>This is a method for creating an INI file if it is exist</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="data">An array of strings representing the data to be written to the file.</param>
        /// <returns>
        /// <b>False</b> - if an error occurred during file creation<br></br>
        /// <b>True</b> - if the file is created successfully
        /// </returns>
        public static bool CreateIniFile(string path, string[] data)
        {
            try
            {
                File.WriteAllLines(path, data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>This is a method for creating an INI file if it is exist</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="data">A string representing the data to be written to the file.</param>
        /// <returns>
        /// <b>False</b> - if an error occurred during file creation<br></br>
        /// <b>True</b> - if the file is created successfully
        /// </returns>
        public static bool CreateIniFile(string path, string data)
        {
            try
            {
                File.WriteAllText(path, data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>This is a method of adding a new section to the end of the file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <returns>
        /// <b>True</b> if the operation was successful.
        /// <br></br><b>False</b> if an error occurred during execution.
        /// </returns>
        public static bool AddSection(string path, string section)
        {
            try
            {
                File.AppendAllText(path, $"[{section}]");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>This is a method of adding a new key to the end of a section</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section to which the recording will be made.</param>
        /// <param name="key">The key that will be created.</param>
        /// <param name="value">The value that will be written to the key.</param>
        /// <returns>
        /// <b>True</b> if the operation was successful.
        /// <br></br><b>False</b> if an error occurred during execution.
        /// </returns>
        public static bool AddKeyInSection(string path, string section, string key, string value)
        {
            try
            {
                List<string> list = new List<string>();
                string[][] data = GetData(path);
                int i = -1;
                foreach (string[] name_section in data)
                {
                    i++;
                    if (name_section[0].Contains(section))
                    {
                        list.AddRange(name_section);
                        list.Add(key + " = " + value);
                        break;
                    }
                }
                data[i] = list.ToArray();
                File.Delete(path);
                SaveFile(path, data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>This is a method for reading a boolean value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <returns>Boolean value</returns>
        public static bool GetBool(string path, string section, string key)
        {
            return GetBool(path, section, key, false);
        }

        /// <summary>This is a method for reading a boolean value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>Boolean value</returns>
        public static bool GetBool(string path, string section, string key, bool default_value)
        {
            bool result = default_value;
            try
            {
                string[][] data = GetData(path);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i][0].Contains(section))
                    {
                        string[] sections = data[i];
                        foreach (string keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                string[] parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                if (bool.TryParse(parts[1], out bool res))
                                    result = res;
                                break;
                            }
                        }
                    }
                }
                return result;
            }
            catch
            {
                return default_value;
            }
        }

        /// <summary>This is a method for reading an integer value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <returns>Integer value</returns>
        public static int GetInt(string path, string section, string key)
        {
            return GetInt(path, section, key, 0);
        }

        /// <summary>This is a method for reading an integer value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>Integer value</returns>
        public static int GetInt(string path, string section, string key, int default_value)
        {
            int result = default_value;
            try
            {
                string[][] data = GetData(path);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i][0].Contains(section))
                    {
                        string[] sections = data[i];
                        foreach (string keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                string[] parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                result = Convert.ToInt32(parts[1]);
                                break;
                            }
                        }
                    }
                }
                return result;
            }
            catch
            {
                return default_value;
            }
        }

        /// <summary>This is a method for reading a numeric floating point value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <returns>Floating point numeric value</returns>
        public static float GetSingle(string path, string section, string key)
        {
            return GetSingle(path, section, key, 0);
        }

        /// <summary>This is a method for reading a numeric floating point value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>Floating point numeric value</returns>
        public static float GetSingle(string path, string section, string key, float default_value)
        {
            float result = default_value;
            try
            {
                string[][] data = GetData(path);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i][0].Contains(section))
                    {
                        string[] sections = data[i];
                        foreach (string keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                string[] parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                result = Convert.ToSingle(parts[1]);
                                break;
                            }
                        }
                    }
                }
                return result;
            }
            catch
            {
                return default_value;
            }
        }

        /// <summary>This is a method for reading a high precision floating point numeric value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <returns>High precision floating point numeric value</returns>
        public static double GetDouble(string path, string section, string key)
        {
            return GetDouble(path, section, key, 0);
        }

        /// <summary>This is a method for reading a high precision floating point numeric value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>High precision floating point numeric value</returns>
        public static double GetDouble(string path, string section, string key, double default_value)
        {
            double result = default_value;
            try
            {
                string[][] data = GetData(path);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i][0].Contains(section))
                    {
                        string[] sections = data[i];
                        foreach (string keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                string[] parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                result = Convert.ToDouble(parts[1]);
                                break;
                            }
                        }
                    }
                }
                return default_value;
            }
            catch
            {
                return result;
            }
        }

        /// <summary>This is a method for reading a line from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <returns>
        /// Line
        /// </returns>
        public static string GetString(string path, string section, string key)
        {
            return GetString(path, section, key, "");
        }

        /// <summary>This is a method for reading a line from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>
        /// Line
        /// </returns>
        public static string GetString(string path, string section, string key, string default_value)
        {
            string result = default_value;
            try
            {
                string[][] data = GetData(path);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i][0].Contains(section))
                    {
                        string[] sections = data[i];
                        foreach (string keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                string[] parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                result = parts[1];
                                break;
                            }
                        }
                    }
                }
                return result;
            }
            catch
            {
                return default_value;
            }
        }

        /// <summary>
        /// Writes a string to the specified secret in the INI file.
        /// </summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section to which the recording will be made.</param>
        /// <param name="key">The key by which the recording will be made.</param>
        /// <param name="value">The value that should be written to the file.</param>
        /// <returns>
        /// <b>True</b> if the operation was successful.
        /// <br></br><b>False</b> if an error occurred during execution.
        /// </returns>
        public static bool SetKey(string path, string section, string key, string value)
        {
            try
            {
                string[][] data = GetData(path);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i][0].Contains(section))
                    {
                        string[] sections = data[i];
                        for (int j = 0;j < sections.Length;j++)
                        {
                            if (sections[j].Contains(key))
                            {
                                string[] parts = sections[j].Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = value;
                                sections[j] = parts[0] + " = " + parts[1];
                                break;
                            }
                        }
                        data[i] = sections;
                    }
                }
                if (SaveFile(path, data))
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>This is a method for writing a boolean value to an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section to which the recording will be made.</param>
        /// <param name="key">The key by which the recording will be made.</param>
        /// <param name="value">The value that should be written to the file.</param>
        /// <returns>
        /// <b>True</b> if the operation was successful.
        /// <br></br><b>False</b> if an error occurred during execution.
        /// </returns>
        public static bool SetKey(string path, string section, string key, bool value)
        {
            return SetKey(path, section, key, Convert.ToString(value));
        }

        /// <summary>This is a method for writing a floating point numeric value to an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section to which the recording will be made.</param>
        /// <param name="key">The key by which the recording will be made.</param>
        /// <param name="value">The value that should be written to the file.</param>
        /// <returns>
        /// <b>True</b> if the operation was successful.
        /// <br></br><b>False</b> if an error occurred during execution.
        /// </returns>
        public static bool SetKey(string path, string section, string key, float value)
        {
            return SetKey(path, section, key, Convert.ToString(value));
        }

        /// <summary>This is a method for writing a high-precision floating point numeric value to an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section to which the recording will be made.</param>
        /// <param name="key">The key by which the recording will be made.</param>
        /// <param name="value">The value that should be written to the file.</param>
        /// <returns>
        /// <b>True</b> if the operation was successful.
        /// <br></br><b>False</b> if an error occurred during execution.
        /// </returns>
        public static bool SetKey(string path, string section, string key, double value)
        {
            return SetKey(path, section, key, Convert.ToString(value));
        }

        /// <summary>This is a method for writing an integer value to an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section to which the recording will be made.</param>
        /// <param name="key">The key by which the recording will be made.</param>
        /// <param name="value">The value that should be written to the file.</param>
        /// <returns>
        /// <b>True</b> if the operation was successful.
        /// <br></br><b>False</b> if an error occurred during execution.
        /// </returns>
        public static bool SetKey(string path, string section, string key, int value)
        {
            return SetKey(path, section, key, Convert.ToString(value));
        }
    }
}