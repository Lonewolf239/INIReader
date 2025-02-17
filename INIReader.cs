using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace IniReader
{
    /// <summary>
    /// This is a class for working with INI files
    /// <br></br>
    /// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
    /// <br></br>
    /// <b>Version: 1.4</b>
    /// </summary>
    public class INIReader
    {
        private readonly List<string[]> Data;
        private readonly string Path;

        public INIReader(string path)
        {
            Path = path;
            Data = GetData(path);
        }

        private static List<string[]> GetData(string path)
        {
            if (!File.Exists(path)) File.Create(path).Close();
            var data = new List<string[]>();
            var currentSection = new List<string>();
            var file = File.ReadAllLines(path);
            foreach (var line in file)
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
            return data;
        }

        private void SaveFile()
        {
            ClearFile();
            foreach (var lines in Data)
                File.AppendAllLines(Path, lines);
        }

        /// <summary>
        /// Checks if a section exists in the specified path.
        /// </summary>
        /// <param name="section">The section to search for.</param>
        /// <returns><b>True</b> if the section exists, <b>false</b> otherwise.</returns>
        public bool SectionExist(string section)
        {
            foreach (var sections in Data)
            {
                if (sections[0].Contains(section))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a key exists within a given section in the specified path.
        /// </summary>
        /// <param name="section">The section to search for.</param>
        /// <param name="key">The key to search for within the section.</param>
        /// <returns><b>True</b> if the key exists within the section, <b>false</b> otherwise.</returns>
        public bool KeyExist(string section, string key)
        {
            foreach (var sections in Data)
            {
                if (sections[0].Contains(section))
                {
                    foreach (var keys in sections)
                    {
                        if (keys.Contains(key))
                            return true;
                    }
                }
            }
            return false;
        }

        /// <summary>This is a method to clear the INI file.</summary>
        public void ClearFile() => File.WriteAllText(Path, null);

        /// <summary>This is a method for adding a new section to the end of the file</summary>
        /// <param name="section">The section to write to.</param>
        public void AddSection(string section)
        {
            Data.Add(new string[] { $"[{section}]" });
            SaveFile();
        }

        /// <summary>This is a method of adding a new key to the end of a section</summary>
        /// <param name="section">The section to which the recording will be made.</param>
        /// <param name="key">The key that will be created.</param>
        /// <param name="value">The value that will be written to the key.</param>
        public void AddKeyInSection<T>(string section, string key, T value)
        {
            var string_value = value.ToString();
            var list = new List<string>();
            int i = -1;
            foreach (var name_section in Data)
            {
                i++;
                if (name_section[0].Contains(section))
                {
                    list.AddRange(name_section);
                    list.Add(key + " = " + string_value);
                    break;
                }
            }
            Data[i] = list.ToArray();
            SaveFile();
        }

        /// <summary>This is a method for reading a keys value from an INI file</summary>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>Keys value</returns>
        public Keys GetKeys(string section, string key, Keys default_value = Keys.None)
        {
            Keys result = default_value;
            try
            {
                bool key_exist = false;
                for (int i = 0; i < Data.Count; i++)
                {
                    if (Data[i][0].Contains(section))
                    {
                        var sections = Data[i];
                        foreach (var keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                key_exist = true;
                                var parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                if (Keys.TryParse(parts[1], out Keys res))
                                    result = res;
                                break;
                            }
                        }
                        if (!key_exist)
                            AddKeyInSection(section, key, default_value);
                    }
                }
                return result;
            }
            catch { return default_value; }
        }

        /// <summary>This is a method for reading a boolean value from an INI file</summary>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>Boolean value</returns>
        public bool GetBool(string section, string key, bool default_value = false)
        {
            bool result = default_value;
            try
            {
                bool key_exist = false;
                for (int i = 0; i < Data.Count; i++)
                {
                    if (Data[i][0].Contains(section))
                    {
                        var sections = Data[i];
                        foreach (var keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                key_exist = true;
                                var parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                if (bool.TryParse(parts[1], out bool res))
                                    result = res;
                                break;
                            }
                        }
                        if (!key_exist)
                            AddKeyInSection(section, key, default_value);
                    }
                }
                return result;
            }
            catch { return default_value; }
        }

        /// <summary>This is a method for reading an integer value from an INI file</summary>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>Integer value</returns>
        public int GetInt(string section, string key, int default_value = 0)
        {
            int result = default_value;
            try
            {
                bool key_exist = false;
                for (int i = 0; i < Data.Count; i++)
                {
                    if (Data[i][0].Contains(section))
                    {
                        var sections = Data[i];
                        foreach (var keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                key_exist = true;
                                var parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                result = Convert.ToInt32(parts[1]);
                                break;
                            }
                        }
                        if (!key_exist)
                            AddKeyInSection(section, key, default_value);
                    }
                }
                return result;
            }
            catch { return default_value; }
        }

        /// <summary>This is a method for reading a numeric floating point value from an INI file</summary>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>Floating point numeric value</returns>
        public float GetSingle(string section, string key, float default_value = 0)
        {
            float result = default_value;
            try
            {
                bool key_exist = false;
                for (int i = 0; i < Data.Count; i++)
                {
                    if (Data[i][0].Contains(section))
                    {
                        var sections = Data[i];
                        foreach (var keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                key_exist = true;
                                var parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                result = Convert.ToSingle(parts[1]);
                                break;
                            }
                        }
                        if (!key_exist)
                            AddKeyInSection(section, key, default_value);
                    }
                }
                return result;
            }
            catch { return default_value; }
        }

        /// <summary>This is a method for reading a high precision floating point numeric value from an INI file</summary>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>High precision floating point numeric value</returns>
        public double GetDouble(string section, string key, double default_value = 0)
        {
            double result = default_value;
            try
            {
                bool key_exist = false;
                for (int i = 0; i < Data.Count; i++)
                {
                    if (Data[i][0].Contains(section))
                    {
                        var sections = Data[i];
                        foreach (var keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                key_exist = true;
                                var parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                result = Convert.ToDouble(parts[1]);
                                break;
                            }
                        }
                        if (!key_exist)
                            AddKeyInSection(section, key, default_value);
                    }
                }
                return result;
            }
            catch { return result; }
        }

        /// <summary>This is a method for reading a line from an INI file</summary>
        /// <param name="section">The section from which reading will be performed.</param>
        /// <param name="key">The key by which the reading will be performed.</param>
        /// <param name="default_value">The default value that will be returned in case of a read error.</param>
        /// <returns>
        /// Line
        /// </returns>
        public string GetString(string section, string key, string default_value = "")
        {
            string result = default_value;
            try
            {
                bool key_exist = false;
                for (int i = 0; i < Data.Count; i++)
                {
                    if (Data[i][0].Contains(section))
                    {
                        var sections = Data[i];
                        foreach (var keys in sections)
                        {
                            if (keys.Contains(key))
                            {
                                key_exist = true;
                                var parts = keys.Split('=');
                                parts[0] = parts[0].Trim();
                                parts[1] = parts[1].Trim();
                                result = parts[1];
                                break;
                            }
                        }
                        if (!key_exist)
                            AddKeyInSection(section, key, default_value);
                    }
                }
                return result;
            }
            catch { return default_value; }
        }

        /// <summary>
        /// Writes a string to the specified secret in the INI file.
        /// </summary>
        /// <param name="section">The section to which the recording will be made.</param>
        /// <param name="key">The key by which the recording will be made.</param>
        /// <param name="value">The value that should be written to the file.</param>
        public void SetKey<T>(string section, string key, T value)
        {
            if (!SectionExist(section)) AddSection(section);
            if (!KeyExist(section, key))
            {
                AddKeyInSection(section, key, value);
                return;
            }
            var string_value = value.ToString();
            bool key_exist = false;
            var parts = new string[2];
            for (int i = 0; i < Data.Count; i++)
            {
                if (Data[i][0].Contains(section))
                {
                    var sections = Data[i];
                    for (int j = 0; j < sections.Length; j++)
                    {
                        if (sections[j].Contains(key))
                        {
                            key_exist = true;
                            parts = sections[j].Split('=');
                            parts[0] = parts[0].Trim();
                            parts[1] = string_value;
                            sections[j] = parts[0] + " = " + parts[1];
                            break;
                        }
                    }
                    if (!key_exist)
                        sections.Append(parts[0] + " = " + parts[1]);
                    Data[i] = sections;
                }
            }
            SaveFile();
        }

        /// <summary>
        /// Writes a string to the specified secret in the INI file.
        /// </summary>
        /// <param name="section">The section where the deletion will be performed.</param>
        /// <param name="key">The key that will be deleted.</param>
        public void RemoveKey(string section, string key)
        {
            if (!SectionExist(section)) return;
            if (!KeyExist(section, key)) return;
            bool removed = false;
            for (int i = 0; i < Data.Count; i++)
            {
                if (Data[i][0].Contains(section))
                {
                    var sections = new List<string>(Data[i]);
                    for (int j = 0; j < sections.Count; j++)
                    {
                        if (sections[j].Contains(key))
                        {
                            removed = true;
                            sections.RemoveAt(j);
                            break;
                        }
                    }
                    if (removed)
                    {
                        Data[i] = sections.ToArray();
                        break;
                    }
                }
            }
            SaveFile();
        }

        public void RemoveSection(string section)
        {
            if (!SectionExist(section)) return;
            for (int i = 0; i < Data.Count; i++)
            {
                if (Data[i][0].Contains(section))
                {
                    Data.RemoveAt(i);
                    break;
                }
            }
            SaveFile();
        }
    }
}