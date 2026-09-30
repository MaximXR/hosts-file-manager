using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Script.Serialization;

namespace HostsLauncher.Models
{
    public class CustomProviderConfig
    {
        public string Id { get; set; }
        public bool IsPreset { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string SiteUrl { get; set; }
        public bool Enabled { get; set; }
        public string LastHash { get; set; }
        public string LastUpdated { get; set; }

        public CustomProviderConfig()
        {
            Id = "";
            IsPreset = false;
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
        public List<string> RemovedPresetIds { get; set; }
        public int TaskScheduleIndex { get; set; }
        public bool TaskOnlyIfIdle { get; set; }
        public string Language { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }

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
            RemovedPresetIds = new List<string>();
            TaskScheduleIndex = 0;
            TaskOnlyIfIdle = true;
            Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "ru" ? "ru" : "en";
            WindowWidth = 690;
            WindowHeight = 780;
        }
    }

    public static class ConfigManager
    {
        public static string GetConfigPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "config.json");
        }

        public static string GetPresetsPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "presets.json");
        }

        public static List<CustomProviderConfig> GetDefaultPresets()
        {
            return new List<CustomProviderConfig>
            {
                new CustomProviderConfig
                {
                    Id = "github520",
                    IsPreset = true,
                    Name = "GitHub520 (Ускорение доступа к GitHub)",
                    Url = "https://raw.hellogithub.com/hosts",
                    SiteUrl = "https://github.com/521xueweihan/GitHub520",
                    Enabled = false
                },
                new CustomProviderConfig
                {
                    Id = "stevenblack",
                    IsPreset = true,
                    Name = "StevenBlack Unified (Реклама + Вредоносные сайты)",
                    Url = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts",
                    SiteUrl = "https://github.com/StevenBlack/hosts",
                    Enabled = false
                },
                new CustomProviderConfig
                {
                    Id = "windowsspyblocker",
                    IsPreset = true,
                    Name = "WindowsSpyBlocker (Блокировка телеметрии Windows)",
                    Url = "https://raw.githubusercontent.com/crazy-max/WindowsSpyBlocker/master/data/hosts/spy.txt",
                    SiteUrl = "https://github.com/crazy-max/WindowsSpyBlocker",
                    Enabled = false
                },
                new CustomProviderConfig
                {
                    Id = "adaway",
                    IsPreset = true,
                    Name = "AdAway (Популярный блокировщик рекламы)",
                    Url = "https://adaway.org/hosts.txt",
                    SiteUrl = "https://adaway.org/",
                    Enabled = false
                },
                new CustomProviderConfig
                {
                    Id = "danpollock",
                    IsPreset = true,
                    Name = "Dan Pollock (someonewhocares — спам и трекинг)",
                    Url = "https://someonewhocares.org/hosts/zero/hosts",
                    SiteUrl = "https://someonewhocares.org/hosts/",
                    Enabled = false
                }
            };
        }

        public static List<CustomProviderConfig> LoadPresets(string customPath = null)
        {
            try
            {
                string path = customPath ?? GetPresetsPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    List<CustomProviderConfig> list = serializer.Deserialize<List<CustomProviderConfig>>(json);
                    if (list != null && list.Count > 0)
                    {
                        foreach (var p in list)
                        {
                            p.IsPreset = true;
                            if (string.IsNullOrEmpty(p.Id))
                            {
                                p.Id = GeneratePresetId(p.Name, p.Url);
                            }
                        }
                        return list;
                    }
                }
            }
            catch { }

            var defaults = GetDefaultPresets();
            try
            {
                string path = customPath ?? GetPresetsPath();
                if (!File.Exists(path))
                {
                    SavePresets(defaults, path);
                }
            }
            catch { }
            return defaults;
        }

        public static void SavePresets(List<CustomProviderConfig> presets, string customPath = null)
        {
            try
            {
                string path = customPath ?? GetPresetsPath();
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(presets);
                File.WriteAllText(path, FormatJson(json), System.Text.Encoding.UTF8);
            }
            catch { }
        }

        private static string GeneratePresetId(string name, string url)
        {
            if (string.IsNullOrEmpty(name)) return "preset_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            string id = name.Split(' ')[0].ToLowerInvariant();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in id)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.Length > 0 ? sb.ToString() : "preset_" + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        public static void NormalizeAndMergePresets(AppConfig cfg, string presetsPath = null)
        {
            if (cfg == null) return;
            if (cfg.CustomProviders == null) cfg.CustomProviders = new List<CustomProviderConfig>();
            if (cfg.RemovedPresetIds == null) cfg.RemovedPresetIds = new List<string>();

            List<CustomProviderConfig> officialPresets = LoadPresets(presetsPath);

            // 1. Tag existing items: check if any item matches a known preset
            foreach (var item in cfg.CustomProviders)
            {
                if (item.IsPreset && !string.IsNullOrEmpty(item.Id)) continue;

                foreach (var off in officialPresets)
                {
                    if ((!string.IsNullOrEmpty(item.Id) && item.Id.Equals(off.Id, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(item.Url) && item.Url.Trim().Equals(off.Url.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(item.Name) && item.Name.Trim().Equals(off.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        item.Id = off.Id;
                        item.IsPreset = true;
                        break;
                    }
                }
            }

            // 2. Auto-merge new presets from officialPresets that are neither in CustomProviders nor in RemovedPresetIds
            foreach (var off in officialPresets)
            {
                if (cfg.RemovedPresetIds.Contains(off.Id))
                {
                    // User explicitly deleted this preset
                    continue;
                }

                bool exists = false;
                foreach (var existing in cfg.CustomProviders)
                {
                    if (existing.IsPreset && existing.Id == off.Id)
                    {
                        exists = true;
                        if (!string.IsNullOrEmpty(off.SiteUrl)) existing.SiteUrl = off.SiteUrl;
                        break;
                    }
                }

                if (!exists)
                {
                    // Newly introduced preset in an updated presets.json!
                    cfg.CustomProviders.Add(new CustomProviderConfig
                    {
                        Id = off.Id,
                        IsPreset = true,
                        Name = off.Name,
                        Url = off.Url,
                        SiteUrl = off.SiteUrl,
                        Enabled = off.Enabled,
                        LastHash = "",
                        LastUpdated = ""
                    });
                }
            }
        }

        public static void RestoreStandardPresets(AppConfig cfg, string presetsPath = null)
        {
            if (cfg == null) return;
            if (cfg.CustomProviders == null) cfg.CustomProviders = new List<CustomProviderConfig>();
            if (cfg.RemovedPresetIds != null) cfg.RemovedPresetIds.Clear();

            List<CustomProviderConfig> officialPresets = LoadPresets(presetsPath);

            // Add back any official preset that is currently missing
            for (int i = 0; i < officialPresets.Count; i++)
            {
                var off = officialPresets[i];
                CustomProviderConfig existing = null;
                foreach (var item in cfg.CustomProviders)
                {
                    if (item.IsPreset && item.Id == off.Id)
                    {
                        existing = item;
                        break;
                    }
                    if (string.IsNullOrEmpty(item.Id) && item.Url == off.Url)
                    {
                        existing = item;
                        existing.Id = off.Id;
                        existing.IsPreset = true;
                        break;
                    }
                }

                if (existing == null)
                {
                    cfg.CustomProviders.Insert(Math.Min(i, cfg.CustomProviders.Count), new CustomProviderConfig
                    {
                        Id = off.Id,
                        IsPreset = true,
                        Name = off.Name,
                        Url = off.Url,
                        SiteUrl = off.SiteUrl,
                        Enabled = off.Enabled,
                        LastHash = "",
                        LastUpdated = ""
                    });
                }
                else
                {
                    existing.Name = off.Name;
                    existing.Url = off.Url;
                    existing.SiteUrl = off.SiteUrl;
                }
            }
        }

        public static int RemoveCustomSources(AppConfig cfg)
        {
            if (cfg == null || cfg.CustomProviders == null) return 0;
            int countBefore = cfg.CustomProviders.Count;
            cfg.CustomProviders.RemoveAll(p => !p.IsPreset);
            return countBefore - cfg.CustomProviders.Count;
        }

        public static void FullResetPresets(AppConfig cfg, string presetsPath = null)
        {
            if (cfg == null) return;
            if (cfg.RemovedPresetIds != null) cfg.RemovedPresetIds.Clear();
            cfg.CustomProviders = LoadPresets(presetsPath);
        }

        public static AppConfig LoadConfig(string customPath = null, string presetsPath = null)
        {
            try
            {
                string path = customPath ?? GetConfigPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    AppConfig cfg = serializer.Deserialize<AppConfig>(json);
                    if (cfg != null)
                    {
                        if (cfg.CustomProviders == null) cfg.CustomProviders = GetDefaultPresets();
                        if (cfg.RemovedPresetIds == null) cfg.RemovedPresetIds = new List<string>();
                        if (string.IsNullOrEmpty(cfg.Language))
                        {
                            cfg.Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower() == "ru" ? "ru" : "en";
                        }
                        if (cfg.WindowWidth < 600) cfg.WindowWidth = 690;
                        if (cfg.WindowHeight < 560) cfg.WindowHeight = 780;

                        NormalizeAndMergePresets(cfg, presetsPath);
                        return cfg;
                    }
                }
            }
            catch { }
            AppConfig freshCfg = new AppConfig();
            NormalizeAndMergePresets(freshCfg, presetsPath);
            return freshCfg;
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
