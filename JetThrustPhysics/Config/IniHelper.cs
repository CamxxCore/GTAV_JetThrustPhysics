using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace JetBlast.Config
{
    public static class IniHelper
    {
        private static readonly CultureInfo Culture = new CultureInfo("en-US");

        public static readonly string FilePath = $@"scripts\{GetAssemblyName()}.ini";

        private static readonly IniFile File = new IniFile(FilePath);

        /// <summary>
        /// Write a string value to the config file at the specified section and key
        /// </summary>
        /// <param name="section">The section in the config file</param>
        /// <param name="key">The key of the config string</param>
        /// <param name="value">The value of the config string</param>
        public static void WriteValue(string section, string key, string value)
        {
            File.IniWriteValue(section, key, value);
        }

        /// <summary>
        /// Gets a config setting
        /// </summary>
        /// <param name="section">The section of the config file</param>
        /// <param name="key">The config setting</param>
        /// <param name="defaultKey"></param>
        /// <returns></returns>
        public static T GetConfigSetting<T>(string section, string key, T defaultKey = default(T))
        {
            var type = typeof(T);

            if (!type.IsValueType)
            {
                throw new ArgumentException("GetConfigSetting: Not a supported type.");
            }

            var keyStr = File.IniReadValue(section, key);

            var converter = TypeDescriptor.GetConverter(type);

            if (keyStr.Length > 0 && converter.CanConvertFrom(typeof(string)))
            {
                return (T) converter.ConvertFromString(null, Culture, keyStr);
            }

            return defaultKey;
        }

        /// <summary>
        /// Gets the current assembly name.
        /// </summary>
        /// <returns></returns>
        public static string GetAssemblyName()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            return assemblyName.Name;
        }
    }
}


