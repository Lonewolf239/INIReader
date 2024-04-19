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
    /// Version: 1.0
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

        /// <summary>This is a method for creating an INI file if it is missing</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="data">An array of strings representing the data to be written to the file.</param>
        /// <returns>
        /// If an error occurred during file creation: -1<br></br>
        /// If the file exists: 0<br></br>
        /// If the file is created successfully: 1
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
        /// If an error occurred during file creation: -1<br></br>
        /// If the file exists: 0<br></br>
        /// If the file is created successfully: 1
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

        /// <summary>This is a method for creating an INI file if it is exist/summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="data">An array of strings representing the data to be written to the file.</param>
        /// <returns>
        /// False - if an error occurred during file creation<br></br>
        /// True - if the file is created successfully
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
        /// False - if an error occurred during file creation<br></br>
        /// True - if the file is created successfully
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
        /// True if the operation was successful.
        /// <br></br>False if an error occurred during execution.
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
        /// True if the operation was successful.
        /// <br></br>False if an error occurred during execution.
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
            bool result = false;
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
            }
            catch
            {
                result = false;
            }
            return result;
        }

        /// <summary>This is a method for reading an integer value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <returns>Integer value</returns>
        public static int GetInt(string path, string section, string key)
        {
            int result = 0;
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
            }
            catch
            {
                result = 0;
            }
            return result;
        }

        /// <summary>This is a method for reading a numeric floating point value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <returns>Floating point numeric value</returns>
        public static float GetSingle(string path, string section, string key)
        {
            float result = 0;
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
            }
            catch
            {
                result = 0;
            }
            return result;
        }

        /// <summary>This is a method for reading a high precision floating point numeric value from an INI file</summary>
        /// <param name="path">Path to the INI file.</param>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <returns>High precision floating point numeric value</returns>
        public static double GetDouble(string path, string section, string key)
        {
            double result = 0;
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
            }
            catch
            {
                result = 0;
            }
            return result;
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
            string result = null;
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
                return null;
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
        /// True if the operation was successful.
        /// <br></br>False if an error occurred during execution.
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
        /// True if the operation was successful.
        /// <br></br>False if an error occurred during execution.
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
        /// True if the operation was successful.
        /// <br></br>False if an error occurred during execution.
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
        /// True if the operation was successful.
        /// <br></br>False if an error occurred during execution.
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
        /// True if the operation was successful.
        /// <br></br>False if an error occurred during execution.
        /// </returns>
        public static bool SetKey(string path, string section, string key, int value)
        {
            return SetKey(path, section, key, Convert.ToString(value));
        }
    }
}