using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;

namespace HostsLauncher.Models
{
    public class CustomProviderConfig
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string SiteUrl { get; set; }
        public bool Enabled { get; set; }
        public string LastHash { get; set; }
        public string LastUpdated { get; set; }

        public CustomProviderConfig()
        {
            Name = "";
            Url = "";
            SiteUrl = "";
            Enabled = true;
            LastHash = "";
            LastUpdated = "";
        }
    }

    public class AppConfig
    {
        public string PreferredEditor { get; set; }
        public string CustomEditorPath { get; set; }
        public bool AlwaysAdmin { get; set; }
        public bool GeoHideEnabled { get; set; }
        public string GeoHideRegion { get; set; }
        public string GeoHideLastUpdated { get; set; }
        public string GeoHideLastHash { get; set; }
        public List<CustomProviderConfig> CustomProviders { get; set; }
        public int TaskScheduleIndex { get; set; }
        public bool TaskOnlyIfIdle { get; set; }
        public string Language { get; set; }

        public AppConfig()
        {
            PreferredEditor = "openwith";
            CustomEditorPath = "";
            AlwaysAdmin = false;
            GeoHideEnabled = true;
            GeoHideRegion = "ru";
            GeoHideLastUpdated = "";
            GeoHideLastHash = "";
            CustomProviders = ConfigManager.GetDefaultPresets();
            TaskScheduleIndex = 0;
            TaskOnlyIfIdle = true;
            Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "ru" ? "ru" : "en";
        }
    }

    public static class ConfigManager
    {
        public static string GetConfigPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "config.json");
        }

        public static List<CustomProviderConfig> GetDefaultPresets()
        {
            return new List<CustomProviderConfig>
            {
                new CustomProviderConfig
                {
                    Name = "GitHub520 (Ускорение доступа к GitHub)",
                    Url = "https://raw.hellogithub.com/hosts",
                    SiteUrl = "https://github.com/521xueweihan/GitHub520",
                    Enabled = false
                },
                new CustomProviderConfig
                {
                    Name = "StevenBlack Unified (Реклама + Вредоносные сайты)",
                    Url = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts",
                    SiteUrl = "https://github.com/StevenBlack/hosts",
                    Enabled = false
                },
                new CustomProviderConfig
                {
                    Name = "WindowsSpyBlocker (Блокировка телеметрии Windows)",
                    Url = "https://raw.githubusercontent.com/crazy-max/WindowsSpyBlocker/master/data/hosts/spy.txt",
                    SiteUrl = "https://github.com/crazy-max/WindowsSpyBlocker",
                    Enabled = false
                },
                new CustomProviderConfig
                {
                    Name = "AdAway (Популярный блокировщик рекламы)",
                    Url = "https://adaway.org/hosts.txt",
                    SiteUrl = "https://adaway.org/",
                    Enabled = false
                },
                new CustomProviderConfig
                {
                    Name = "Dan Pollock (someonewhocares — спам и трекинг)",
                    Url = "https://someonewhocares.org/hosts/zero/hosts",
                    SiteUrl = "https://someonewhocares.org/hosts/",
                    Enabled = false
                }
            };
        }

        public static AppConfig LoadConfig(string customPath = null)
        {
            try
            {
                string path = customPath ?? GetConfigPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    AppConfig cfg = serializer.Deserialize<AppConfig>(json);
                    if (cfg != null)
                    {
                        if (cfg.CustomProviders == null) cfg.CustomProviders = GetDefaultPresets();
                        if (string.IsNullOrEmpty(cfg.Language))
                        {
                            cfg.Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "ru" ? "ru" : "en";
                        }
                        return cfg;
                    }
                }
            }
            catch { }
            return new AppConfig();
        }

        public static void SaveConfig(AppConfig config, string customPath = null)
        {
            try
            {
                string path = customPath ?? GetConfigPath();
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(config);
                File.WriteAllText(path, FormatJson(json));
            }
            catch { }
        }

        public static string FormatJson(string json)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                var obj = serializer.Deserialize<object>(json);
                return FormatObject(obj, 0);
            }
            catch
            {
                return json;
            }
        }

        private static string FormatObject(object obj, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 2);
            string subIndent = new string(' ', (indentLevel + 1) * 2);

            Dictionary<string, object> dict = obj as Dictionary<string, object>;
            if (dict != null)
            {
                List<string> lines = new List<string>();
                foreach (var kvp in dict)
                {
                    lines.Add(string.Format("{0}\"{1}\": {2}", subIndent, kvp.Key, FormatObject(kvp.Value, indentLevel + 1)));
                }
                return "{\n" + string.Join(",\n", lines.ToArray()) + "\n" + indent + "}";
            }

            object[] arr = obj as object[];
            if (arr != null)
            {
                List<string> items = new List<string>();
                foreach (var item in arr)
                {
                    items.Add(subIndent + FormatObject(item, indentLevel + 1));
                }
                return "[\n" + string.Join(",\n", items.ToArray()) + "\n" + indent + "]";
            }

            string s = obj as string;
            if (s != null)
            {
                return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
            }

            if (obj is bool)
            {
                return ((bool)obj) ? "true" : "false";
            }

            if (obj == null)
            {
                return "null";
            }
            return obj.ToString();
        }
    }
}
