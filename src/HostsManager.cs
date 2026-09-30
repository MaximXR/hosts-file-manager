using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace HostsManager
{
    public class CustomProviderConfig
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string SiteUrl { get; set; } // Сайт / репозиторий проекта с описанием
        public bool Enabled { get; set; }
        public string LastHash { get; set; }
        public string LastUpdated { get; set; }
    }

    public class AppConfig
    {
        public string PreferredEditor { get; set; }
        public string CustomEditorPath { get; set; }
        public bool AlwaysAdmin { get; set; }
        public bool BackupBeforeUpdate { get; set; }

        // GeoHide
        public bool GeoHideEnabled { get; set; }
        public string GeoHideRegion { get; set; } // "ru", "eu", "us"
        public string GeoHideLastHash { get; set; }
        public string GeoHideLastUpdated { get; set; }

        // Настройки планировщика
        public bool TaskOnlyIfIdle { get; set; }
        public int TaskScheduleIndex { get; set; }

        // Дополнительные источники
        public List<CustomProviderConfig> CustomProviders { get; set; }

        // Язык интерфейса ("ru", "en")
        public string Language { get; set; }

        public AppConfig()
        {
            Language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant() == "ru" ? "ru" : "en";
            PreferredEditor = "openwith";
            CustomEditorPath = "";
            AlwaysAdmin = false;
            BackupBeforeUpdate = true;

            GeoHideEnabled = true;
            GeoHideRegion = "ru";
            GeoHideLastHash = "";
            GeoHideLastUpdated = "";

            TaskOnlyIfIdle = true;
            TaskScheduleIndex = 0;

            CustomProviders = Program.GetDefaultPresets();
        }
    }

    public class FastWebClient : WebClient
    {
        private int timeoutMs;

        public FastWebClient(int timeoutMs = 10000)
        {
            this.timeoutMs = timeoutMs;
            this.Proxy = null; // Отключаем медленный поиск WPAD прокси
            this.Encoding = Encoding.UTF8;
            this.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) HostsManager";
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest req = base.GetWebRequest(address);
            if (req != null)
            {
                req.Timeout = timeoutMs;
            }
            return req;
        }
    }

    static class Program
    {
        public static List<CustomProviderConfig> GetDefaultPresets()
        {
            return new List<CustomProviderConfig>
            {
                new CustomProviderConfig
                {
                    Name = "GitHub520 (Ускорение доступа к GitHub)",
                    Url = "https://raw.hellogithub.com/hosts",
                    SiteUrl = "https://github.com/521xueweihan/GitHub520",
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                },
                new CustomProviderConfig
                {
                    Name = "Windows SpyBlocker (Телеметрия)",
                    Url = "https://raw.githubusercontent.com/crazy-max/WindowsSpyBlocker/master/data/hosts/spy.txt",
                    SiteUrl = "https://github.com/crazy-max/WindowsSpyBlocker",
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                },
                new CustomProviderConfig
                {
                    Name = "AdAway (Блокировка рекламы)",
                    Url = "https://adaway.org/hosts.txt",
                    SiteUrl = "https://adaway.org",
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                },
                new CustomProviderConfig
                {
                    Name = "StevenBlack (Анти-реклама и фишинг)",
                    Url = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts",
                    SiteUrl = "https://github.com/StevenBlack/hosts",
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                },
                new CustomProviderConfig
                {
                    Name = "Dan Pollock (Анти-реклама и трекеры)",
                    Url = "https://someonewhocares.org/hosts/zero/hosts",
                    SiteUrl = "https://someonewhocares.org/hosts/",
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                }
            };
        }

        public static string ConfigPath
        {
            get
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(dir, "config.json");
            }
        }

        public static string HostsPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
            }
        }

        public static AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    AppConfig cfg = serializer.Deserialize<AppConfig>(json);
                    if (cfg != null)
                    {
                        if (cfg.CustomProviders == null || cfg.CustomProviders.Count == 0)
                        {
                            cfg.CustomProviders = GetDefaultPresets();
                            SaveConfig(cfg);
                        }
                        else
                        {
                            // Обогащаем SiteUrl для существующих пресетов
                            var defaults = GetDefaultPresets();
                            bool changed = false;
                            foreach (var p in cfg.CustomProviders)
                            {
                                if (string.IsNullOrEmpty(p.SiteUrl))
                                {
                                    var match = defaults.Find(d => d.Url == p.Url || d.Name == p.Name);
                                    if (match != null) { p.SiteUrl = match.SiteUrl; changed = true; }
                                    else { p.SiteUrl = p.Url; changed = true; }
                                }
                            }
                            if (changed) SaveConfig(cfg);
                        }
                        return cfg;
                    }
                }
            }
            catch { }
            AppConfig def = new AppConfig();
            def.CustomProviders = GetDefaultPresets();
            return def;
        }

        public static void SaveConfig(AppConfig config)
        {
            try
            {
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(config);
                File.WriteAllText(ConfigPath, json, Encoding.UTF8);
            }
            catch { }
        }

        public static bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void FlushDnsSafe()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ipconfig.exe",
                    Arguments = "/flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process p = Process.Start(psi);
                if (p != null) p.WaitForExit(3000);
            }
            catch { }
        }

        public static bool CheckTaskStatus(out string nextRun, out string state, out bool isIdleOnly)
        {
            nextRun = "";
            state = "";
            isIdleOnly = false;
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/query /tn \"HostsManagerAutoUpdate\" /fo CSV /nh",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                Process p = Process.Start(psi);
                if (p != null)
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(3000);
                    if (p.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        string[] parts = output.Trim().Split(new[] { "\",\"" }, StringSplitOptions.None);
                        if (parts.Length >= 3)
                        {
                            nextRun = parts[1].Trim('\"');
                            state = parts[2].Trim('\"', '\r', '\n');
                        }

                        try
                        {
                            ProcessStartInfo psiXml = new ProcessStartInfo
                            {
                                FileName = "schtasks.exe",
                                Arguments = "/query /tn \"HostsManagerAutoUpdate\" /xml",
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardOutput = true
                            };
                            Process pXml = Process.Start(psiXml);
                            if (pXml != null)
                            {
                                string xml = pXml.StandardOutput.ReadToEnd();
                                pXml.WaitForExit(2000);
                                if (xml.IndexOf("<RunOnlyIfIdle>true</RunOnlyIfIdle>", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    xml.IndexOf("<IdleTrigger>", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    isIdleOnly = true;
                                }
                            }
                        }
                        catch { }

                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        public static bool CheckTaskStatus(out string nextRun, out string state)
        {
            bool dummy;
            return CheckTaskStatus(out nextRun, out state, out dummy);
        }

        [STAThread]
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            if (args.Length > 0)
            {
                string cmd = args[0].ToLowerInvariant();
                if (cmd == "/update-silent" || cmd == "/update-now" || cmd == "/apply-hosts-elevated")
                {
                    bool isSilent = (cmd == "/update-silent" || cmd == "/apply-hosts-elevated");
                    bool success = UpdateHostsRoutine(isSilent);
                    if (cmd == "/update-now")
                    {
                        if (success)
                        {
                            MessageBox.Show("Файл hosts успешно обновлен!\nПодписки синхронизированы, DNS-кэш Windows сброшен.", "HostsManager — Обновление завершено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось обновить файл hosts (проверьте интернет или окно подтверждения прав администратора UAC).", "HostsManager — Ошибка обновления", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    Environment.Exit(success ? 0 : 1);
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        public static bool UpdateHostsRoutine(bool silent)
        {
            AppConfig config = LoadConfig();
            if (!IsAdmin())
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Application.ExecutablePath;
                    psi.Arguments = silent ? "/update-silent" : "/apply-hosts-elevated";
                    psi.Verb = "runas";
                    psi.UseShellExecute = true;
                    Process p = Process.Start(psi);
                    if (p != null) p.WaitForExit();
                    return p != null && p.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                string hostsContent = File.Exists(HostsPath) ? File.ReadAllText(HostsPath, Encoding.UTF8) : "";

                if (config.BackupBeforeUpdate && File.Exists(HostsPath))
                {
                    string backupPath = HostsPath + ".bak";
                    File.Copy(HostsPath, backupPath, true);
                }

                bool hasAnyChanges = false;
                using (FastWebClient client = new FastWebClient(10000))
                {
                    // 1. Обработка GeoHide
                    string geoBlockStart = "# === BEGIN HOSTS-MANAGER MANAGED BLOCK [GeoHide] ===";
                    string geoBlockEnd = "# === END HOSTS-MANAGER MANAGED BLOCK [GeoHide] ===";

                    if (!config.GeoHideEnabled)
                    {
                        if (hostsContent.Contains(geoBlockStart))
                        {
                            hostsContent = RemoveBlock(hostsContent, geoBlockStart, geoBlockEnd);
                            hasAnyChanges = true;
                            config.GeoHideLastHash = "";
                            config.GeoHideLastUpdated = "Отключен";
                        }
                    }
                    else
                    {
                        string region = string.IsNullOrEmpty(config.GeoHideRegion) ? "ru" : config.GeoHideRegion.ToLowerInvariant();
                        string geoUrl = region == "ru" ? "https://geohide.ru/hosts" : ("https://geohide.ru/" + region + "/hosts");

                        try
                        {
                            string downloadedData = client.DownloadString(geoUrl);
                            string newHash = ComputeHash(downloadedData);

                            if (config.GeoHideLastHash != newHash || !hostsContent.Contains(geoBlockStart))
                            {
                                StringBuilder newBlock = new StringBuilder();
                                newBlock.AppendLine(geoBlockStart);
                                newBlock.AppendLine(string.Format("# Source: {0} (Region: {1})", geoUrl, region.ToUpperInvariant()));
                                newBlock.AppendLine(string.Format("# Updated: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                                newBlock.AppendLine(downloadedData.Trim());
                                newBlock.AppendLine(geoBlockEnd);

                                if (hostsContent.Contains(geoBlockStart))
                                {
                                    hostsContent = ReplaceBlock(hostsContent, geoBlockStart, geoBlockEnd, newBlock.ToString());
                                }
                                else
                                {
                                    if (hostsContent.Length > 0 && !hostsContent.EndsWith(Environment.NewLine))
                                    {
                                        hostsContent += Environment.NewLine;
                                    }
                                    hostsContent += Environment.NewLine + newBlock.ToString();
                                }

                                config.GeoHideLastHash = newHash;
                                config.GeoHideLastUpdated = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                                hasAnyChanges = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Ошибка скачивания GeoHide: " + ex.Message);
                        }
                    }

                    // 2. Обработка дополнительных независимых провайдеров
                    if (config.CustomProviders != null)
                    {
                        foreach (var provider in config.CustomProviders)
                        {
                            string blockStart = string.Format("# === BEGIN HOSTS-MANAGER MANAGED BLOCK [{0}] ===", provider.Name);
                            string blockEnd = string.Format("# === END HOSTS-MANAGER MANAGED BLOCK [{0}] ===", provider.Name);

                            if (!provider.Enabled)
                            {
                                if (hostsContent.Contains(blockStart))
                                {
                                    hostsContent = RemoveBlock(hostsContent, blockStart, blockEnd);
                                    hasAnyChanges = true;
                                    provider.LastHash = "";
                                    provider.LastUpdated = "Отключен";
                                }
                                continue;
                            }

                            try
                            {
                                string downloadedData = client.DownloadString(provider.Url);
                                string newHash = ComputeHash(downloadedData);

                                if (provider.LastHash != newHash || !hostsContent.Contains(blockStart))
                                {
                                    StringBuilder newBlock = new StringBuilder();
                                    newBlock.AppendLine(blockStart);
                                    newBlock.AppendLine(string.Format("# Source: {0}", provider.Url));
                                    newBlock.AppendLine(string.Format("# Updated: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                                    newBlock.AppendLine(downloadedData.Trim());
                                    newBlock.AppendLine(blockEnd);

                                    if (hostsContent.Contains(blockStart))
                                    {
                                        hostsContent = ReplaceBlock(hostsContent, blockStart, blockEnd, newBlock.ToString());
                                    }
                                    else
                                    {
                                        if (hostsContent.Length > 0 && !hostsContent.EndsWith(Environment.NewLine))
                                        {
                                            hostsContent += Environment.NewLine;
                                        }
                                        hostsContent += Environment.NewLine + newBlock.ToString();
                                    }

                                    provider.LastHash = newHash;
                                    provider.LastUpdated = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                                    hasAnyChanges = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Ошибка скачивания " + provider.Name + ": " + ex.Message);
                            }
                        }
                    }
                }

                if (hasAnyChanges)
                {
                    File.WriteAllText(HostsPath, hostsContent, Encoding.UTF8);
                    FlushDnsSafe();
                }

                SaveConfig(config);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ошибка записи hosts: " + ex.Message);
                return false;
            }
        }

        private static string RemoveBlock(string content, string startMarker, string endMarker)
        {
            int start = content.IndexOf(startMarker);
            if (start < 0) return content;
            int end = content.IndexOf(endMarker, start);
            if (end < 0) return content;
            end += endMarker.Length;

            if (end < content.Length && content[end] == '\r') end++;
            if (end < content.Length && content[end] == '\n') end++;

            return content.Substring(0, start) + content.Substring(end);
        }

        private static string ReplaceBlock(string content, string startMarker, string endMarker, string newBlock)
        {
            int start = content.IndexOf(startMarker);
            if (start < 0) return content;
            int end = content.IndexOf(endMarker, start);
            if (end < 0) return content;
            end += endMarker.Length;

            return content.Substring(0, start) + newBlock + content.Substring(end);
        }

        private static string ComputeHash(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }

    public static class L10n
    {
        public static string CurrentLang = "ru";

        private static readonly Dictionary<string, string> Ru = new Dictionary<string, string>
        {
            { "AppTitle", "Hosts Manager — Панель управления файлом hosts" },
            { "TabShortcuts", "🚀 Создать ярлык" },
            { "TabSubscriptions", "🌐 Подписки и автообновление" },
            { "GbEditorTitle", "1. Выберите редактор для открытия файла hosts:" },
            { "RbOpenWith", "Стандартное окно Windows «Открыть с помощью...» (выбор редактора)" },
            { "RbNpp", "Notepad++ (при наличии в системе)" },
            { "RbCode", "Visual Studio Code" },
            { "RbNotepad", "Системный Блокнот (всегда с правами администратора)" },
            { "RbCustom", "Другой редактор:" },
            { "BtnBrowse", "Обзор..." },
            { "ChkAdmin", "Запускать с правами Администратора (UAC)" },
            { "LblShortcutName", "Имя ярлыка на Рабочем столе:" },
            { "LblIcon", "Иконка ярлыка:" },
            { "IconSystem", "📄 Системный файл (shell32.dll #0)" },
            { "IconNotepad", "📝 Блокнот (notepad.exe)" },
            { "IconNpp", "🟢 Notepad++ (если установлен)" },
            { "BtnCreateHostsShortcut", "🚀 Создать ярлык для файла hosts" },
            { "BtnCreateUpdateShortcut", "🔄 Создать ярлык: «Обновить hosts в 1 клик»" },
            { "BtnCreateManagerShortcut", "⚙️ Создать ярлык: «Панель Hosts Manager»" },
            { "ShortcutHint", "💡 Созданные ярлыки можно закрепить на панели задач: ПКМ по ярлыку -> «Закрепить на панели задач»." },
            { "GbGeoTitle", "1. Сервис GeoHide (Антиблокировка / DNS Прокси)" },
            { "ChkGeoHide", "Включить GeoHide (автоматически обновлять правила обхода)" },
            { "LblRegionTitle", "Серверный регион (выбирается строго один для маршрутизации):" },
            { "RbGeoRU", "Россия (RU) — наименьшая задержка (по умолчанию)" },
            { "RbGeoEU", "Европа (EU)" },
            { "RbGeoUS", "США (US)" },
            { "LnkGeoSite", "🌐 Открыть сайт сервиса GeoHide: https://geohide.ru/" },
            { "GeoStatusPrefix", "Статус: Обновлено: " },
            { "StatusNever", "Никогда" },
            { "StatusDisabled", "Отключен" },
            { "GbCustomTitle", "2. Каталог популярных подписок и свои источники (отметьте нужные галочками):" },
            { "BtnAddCustom", "➕ Добавить свой источник..." },
            { "BtnResetPresets", "🔄 Восстановить пресеты" },
            { "CustomHint", "💡 Отмечайте нужные источники. Нажмите «Сайт проекта», чтобы изучить описание." },
            { "CardSiteLink", "🌐 Сайт проекта" },
            { "CardRawLink", "📄 hosts-файл (raw)" },
            { "CardUpdatedPrefix", "Обновлено: " },
            { "ConfirmDeleteSource", "Удалить источник \"{0}\" из списка?" },
            { "ConfirmResetPresets", "Сбросить список к популярным встроенным пресетам с официальными сайтами?" },
            { "GbSchedulerTitle", "3. Фоновое автообновление (Планировщик задач Windows)" },
            { "TaskChecking", "Статус: Проверка задачи..." },
            { "TaskActive", "🟢 Задача активна в Windows{0}. Следующий запуск: {1} ({2})" },
            { "TaskIdleSuffix", " [режим простоя ПК]" },
            { "TaskNotCreated", "⚪ Задача не создана (фоновое автообновление выключено)" },
            { "LblScheduleFreq", "Расписание:" },
            { "SchedDaily9", "Раз в день (в 09:00)" },
            { "SchedDaily14", "Раз в день (в 14:00)" },
            { "SchedEvery6h", "Каждые 6 часов" },
            { "SchedEvery12h", "Каждые 12 часов" },
            { "SchedLogon", "При каждом входе в Windows (onlogon)" },
            { "SchedIdle", "При простое компьютера (onidle — через 10 мин)" },
            { "ChkOnlyIfIdle", "💤 Обновлять только при простое компьютера" },
            { "BtnCreateTask", "⚡ Создать задачу с выбранным расписанием" },
            { "BtnDeleteTask", "🗑️ Удалить задачу из Планировщика" },
            { "BtnOpenScheduler", "⏱️ Открыть в Планировщике Windows" },
            { "BtnCopyCmd", "📋 Скопировать команду" },
            { "SchedulerPathHint", "💡 Задача находится в Планировщике: Библиотека -> HostsManagerAutoUpdate" },
            { "BtnUpdateNow", "🔄 Синхронизировать hosts сейчас" },
            { "BottomSafeNotice", "Личные ручные записи в hosts изолированы и надежно защищены от перезаписи." },
            { "SyncInProgress", "⏳ Синхронизация правил... Пожалуйста, подождите." },
            { "SyncSuccess", "✅ Hosts успешно синхронизирован! DNS-кэш сброшен ({0})" },
            { "SyncError", "⚠️ Ошибка обновления (отменено в окне UAC или нет сети)." },
            { "SyncSuccessBox", "Файл hosts успешно обновлен!\nDNS-кэш Windows автоматически сброшен." },
            { "Done", "Готово" },
            { "Success", "Успех" },
            { "Error", "Ошибка" },
            { "Confirmation", "Подтверждение" },
            { "Copied", "Скопировано" },
            { "CmdCopied", "Команда для Планировщика скопирована в буфер обмена!" },
            { "TaskDeleted", "Задача успешно удалена из Планировщика Windows." },
            { "TaskCreated", "Задача автообновления успешно создана в Планировщике Windows!" },
            { "TaskCreateError", "Не удалось создать задачу: {0}" },
            { "TaskDeleteError", "Не удалось удалить задачу: {0}" },
            { "SchedulerLocationHint", "Задача находится по пути:\nБиблиотека планировщика заданий -> HostsManagerAutoUpdate\n\nСейчас откроется стандартная системная оснастка." },
            { "ShortcutCreatedSuccess", "Ярлык \"{0}.lnk\" успешно создан на Рабочем столе!\n\nЧтобы закрепить его на панели задач:\nНажмите правой кнопкой мыши по созданному ярлыку -> «Закрепить на панели задач»." },
            { "ShortcutCreateError", "Ошибка создания ярлыка: {0}" },
            { "SelectExeFilter", "Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*" },
            { "SelectExeTitle", "Выберите исполняемый файл редактора" },
            { "SpecifyEditorPath", "Пожалуйста, укажите путь к исполняемому файлу редактора." },
            { "EnterUrlTitle", "URL файла hosts" },
            { "EnterUrlPrompt", "Введите прямую ссылку на файл правил hosts (raw .txt):" },
            { "EnterSiteTitle", "Сайт проекта" },
            { "EnterSitePrompt", "Введите сайт / репозиторий проекта с описанием (необязательно):" },
            { "EnterNameTitle", "Имя источника" },
            { "EnterNamePrompt", "Введите краткое имя источника:" },
            { "SourceDefaultName", "Источник {0}" },
            { "LanguageName", "Язык / Language:" },
            { "Cancel", "Отмена" },
            { "ShortcutDefaultName", "Hosts" },
            { "ShortcutUpdateName", "Обновить hosts" },
            { "ShortcutUpdateDesc", "Обновление подписок и сброс DNS в 1 клик" },
            { "ShortcutManagerName", "Hosts Manager" },
            { "ShortcutManagerDesc", "Панель управления hosts, подписками и планировщиком" },
            { "GbSettingsTitle", "2. Настройки ярлыка:" },
            { "GbExtraShortcutsTitle", "3. Дополнительные полезные ярлыки (в 1 клик):" },
            { "LblExtraHint", "💡 Удобно разместить на Рабочем столе или закрепить в Панели задач Windows." },
            { "Warning", "Внимание" },
            { "ShortcutHostsDesc", "Быстрое открытие файла hosts" },
            { "HumanSchedDaily9", "каждый день в 09:00" },
            { "HumanSchedDaily14", "каждый день в 14:00" },
            { "HumanSchedEvery6h", "каждые 6 часов" },
            { "HumanSchedEvery12h", "каждые 12 часов" },
            { "HumanSchedLogon", "при каждом входе в Windows" },
            { "HumanSchedIdle", "при простое компьютера (от 10 минут)" },
            { "TaskCreatedIdleNotice", "\nУсловие: запуск ТОЛЬКО при простое компьютера (не мешает активной работе)." },
            { "TaskCreatedDetails", "Задача успешно создана в Планировщике Windows!\n\nРасписание: {0}.{1}\nЗапуск производится тихо в фоновом режиме с наивысшими правами." },
            { "ErrorPrefix", "Ошибка: " },
            { "ErrorLaunchScheduler", "Не удалось запустить оснастку Планировщика: {0}" },
            { "ErrorOpenBrowser", "Не удалось открыть браузер: {0}" },
            { "ErrorOpenRaw", "Не удалось открыть hosts-файл: {0}" }
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "AppTitle", "Hosts Manager — Hosts File Control Panel" },
            { "TabShortcuts", "🚀 Create Shortcut" },
            { "TabSubscriptions", "🌐 Subscriptions & Scheduler" },
            { "GbEditorTitle", "1. Choose editor for opening hosts file:" },
            { "RbOpenWith", "Standard Windows system dialog (\"Open with...\" editor choice)" },
            { "RbNpp", "Notepad++ (if installed on system)" },
            { "RbCode", "Visual Studio Code" },
            { "RbNotepad", "System Notepad (always with Administrator rights)" },
            { "RbCustom", "Custom editor executable:" },
            { "BtnBrowse", "Browse..." },
            { "ChkAdmin", "Always run as Administrator (UAC)" },
            { "LblShortcutName", "Desktop shortcut name:" },
            { "LblIcon", "Shortcut icon:" },
            { "IconSystem", "📄 System file icon (shell32.dll #0)" },
            { "IconNotepad", "📝 Notepad (notepad.exe)" },
            { "IconNpp", "🟢 Notepad++ (if installed)" },
            { "BtnCreateHostsShortcut", "🚀 Create hosts file shortcut" },
            { "BtnCreateUpdateShortcut", "🔄 Create shortcut: \"Update hosts in 1 click\"" },
            { "BtnCreateManagerShortcut", "⚙️ Create shortcut: \"Hosts Manager Panel\"" },
            { "ShortcutHint", "💡 Pin shortcuts to taskbar: Right-click shortcut -> \"Pin to taskbar\"." },
            { "GbGeoTitle", "1. GeoHide Service (Anti-censorship / DNS Proxy)" },
            { "ChkGeoHide", "Enable GeoHide (automatically update bypass rules)" },
            { "LblRegionTitle", "Server region (select strictly one for routing):" },
            { "RbGeoRU", "Russia (RU) — lowest latency (default)" },
            { "RbGeoEU", "Europe (EU)" },
            { "RbGeoUS", "USA (US)" },
            { "LnkGeoSite", "🌐 Open GeoHide website: https://geohide.ru/" },
            { "GeoStatusPrefix", "Status: Updated: " },
            { "StatusNever", "Never" },
            { "StatusDisabled", "Disabled" },
            { "GbCustomTitle", "2. Catalog of popular subscriptions & custom sources (check desired):" },
            { "BtnAddCustom", "➕ Add custom source..." },
            { "BtnResetPresets", "🔄 Reset presets" },
            { "CustomHint", "💡 Check desired sources. Click \"Project website\" to inspect details." },
            { "CardSiteLink", "🌐 Project website" },
            { "CardRawLink", "📄 hosts file (raw)" },
            { "CardUpdatedPrefix", "Updated: " },
            { "ConfirmDeleteSource", "Delete source \"{0}\" from list?" },
            { "ConfirmResetPresets", "Reset list to popular built-in presets with official websites?" },
            { "GbSchedulerTitle", "3. Background auto-update (Windows Task Scheduler)" },
            { "TaskChecking", "Status: Checking task..." },
            { "TaskActive", "🟢 Task active in Windows{0}. Next run: {1} ({2})" },
            { "TaskIdleSuffix", " [computer idle mode]" },
            { "TaskNotCreated", "⚪ Task not created (background auto-update is off)" },
            { "LblScheduleFreq", "Schedule:" },
            { "SchedDaily9", "Daily (at 09:00)" },
            { "SchedDaily14", "Daily (at 14:00)" },
            { "SchedEvery6h", "Every 6 hours" },
            { "SchedEvery12h", "Every 12 hours" },
            { "SchedLogon", "At Windows logon (onlogon)" },
            { "SchedIdle", "When computer is idle (onidle — after 10 min)" },
            { "ChkOnlyIfIdle", "💤 Update only when computer is idle" },
            { "BtnCreateTask", "⚡ Create task with selected schedule" },
            { "BtnDeleteTask", "🗑️ Delete task from Task Scheduler" },
            { "BtnOpenScheduler", "⏱️ Open in Windows Task Scheduler" },
            { "BtnCopyCmd", "📋 Copy command" },
            { "SchedulerPathHint", "💡 Task in Scheduler: Library -> HostsManagerAutoUpdate" },
            { "BtnUpdateNow", "🔄 Sync hosts now" },
            { "BottomSafeNotice", "Manual custom entries in hosts are isolated and strictly preserved." },
            { "SyncInProgress", "⏳ Syncing rules... Please wait." },
            { "SyncSuccess", "✅ Hosts successfully synchronized! DNS cache flushed ({0})" },
            { "SyncError", "⚠️ Update error (cancelled in UAC prompt or no network)." },
            { "SyncSuccessBox", "Hosts file successfully updated!\nWindows DNS cache was flushed." },
            { "Done", "Done" },
            { "Success", "Success" },
            { "Error", "Error" },
            { "Confirmation", "Confirmation" },
            { "Copied", "Copied" },
            { "CmdCopied", "Command for Task Scheduler copied to clipboard!" },
            { "TaskDeleted", "Task successfully removed from Windows Task Scheduler." },
            { "TaskCreated", "Auto-update task successfully created in Task Scheduler!" },
            { "TaskCreateError", "Failed to create task: {0}" },
            { "TaskDeleteError", "Failed to delete task: {0}" },
            { "SchedulerLocationHint", "The task is located at:\nTask Scheduler Library -> HostsManagerAutoUpdate\n\nOpening Windows Task Scheduler now." },
            { "ShortcutCreatedSuccess", "Shortcut \"{0}.lnk\" created on Desktop!\n\nTo pin to taskbar:\nRight-click shortcut -> \"Pin to taskbar\"." },
            { "ShortcutCreateError", "Failed to create shortcut: {0}" },
            { "SelectExeFilter", "Executable files (*.exe)|*.exe|All files (*.*)|*.*" },
            { "SelectExeTitle", "Select editor executable" },
            { "SpecifyEditorPath", "Please specify the path to editor executable." },
            { "EnterUrlTitle", "Hosts file URL" },
            { "EnterUrlPrompt", "Enter direct link to hosts rule file (raw .txt):" },
            { "EnterSiteTitle", "Project Website" },
            { "EnterSitePrompt", "Enter project website / repository with description (optional):" },
            { "EnterNameTitle", "Source Name" },
            { "EnterNamePrompt", "Enter short name for this source:" },
            { "SourceDefaultName", "Source {0}" },
            { "LanguageName", "Language / Язык:" },
            { "Cancel", "Cancel" },
            { "ShortcutDefaultName", "Hosts" },
            { "ShortcutUpdateName", "Update hosts" },
            { "ShortcutUpdateDesc", "1-click subscription update and DNS flush" },
            { "ShortcutManagerName", "Hosts Manager" },
            { "ShortcutManagerDesc", "Control panel for hosts, subscriptions and scheduler" },
            { "GbSettingsTitle", "2. Shortcut settings:" },
            { "GbExtraShortcutsTitle", "3. Additional useful shortcuts (1-click):" },
            { "LblExtraHint", "💡 Convenient to place on Desktop or pin to Windows Taskbar." },
            { "Warning", "Warning" },
            { "ShortcutHostsDesc", "Quick open hosts file" },
            { "HumanSchedDaily9", "daily at 09:00" },
            { "HumanSchedDaily14", "daily at 14:00" },
            { "HumanSchedEvery6h", "every 6 hours" },
            { "HumanSchedEvery12h", "every 12 hours" },
            { "HumanSchedLogon", "at each Windows logon" },
            { "HumanSchedIdle", "when computer is idle (after 10 min)" },
            { "TaskCreatedIdleNotice", "\nCondition: run ONLY when computer is idle (does not interrupt active work)." },
            { "TaskCreatedDetails", "Task successfully created in Windows Task Scheduler!\n\nSchedule: {0}.{1}\nRuns silently in background with highest privileges." },
            { "ErrorPrefix", "Error: " },
            { "ErrorLaunchScheduler", "Failed to launch Task Scheduler: {0}" },
            { "ErrorOpenBrowser", "Failed to open browser: {0}" },
            { "ErrorOpenRaw", "Failed to open hosts file: {0}" }
        };

        public static string T(string key, params object[] args)
        {
            var dict = (CurrentLang == "en") ? En : Ru;
            string val;
            if (!dict.TryGetValue(key, out val))
            {
                if (!Ru.TryGetValue(key, out val))
                    val = key;
            }
            if (args != null && args.Length > 0)
            {
                try { return string.Format(val, args); } catch { return val; }
            }
            return val;
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Header and Language
        private Panel pnlHeader;
        private Label lblAppInfo;
        private Label lblLang;
        private ComboBox cboLanguage;

        private TabControl tabs;
        private TabPage tabShortcuts;
        private TabPage tabProviders;

        // Shortcuts tab
        private GroupBox gbEditor;
        private RadioButton rbOpenWith;
        private RadioButton rbNpp;
        private RadioButton rbCode;
        private RadioButton rbNotepad;
        private RadioButton rbCustom;
        private TextBox txtCustomPath;
        private Button btnBrowseCustom;
        private CheckBox chkAdmin;
        private Label lblShortcutName;
        private TextBox txtShortcutName;
        private Label lblIcon;
        private ComboBox cboIcon;
        private Button btnCreateShortcut;
        private Button btnCreateUpdateShortcut;
        private Button btnCreateManagerShortcut;
        private Label lblShortcutHint;
        private GroupBox gbSettings;
        private GroupBox gbExtraShortcuts;
        private Label lblExtraHint;

        // Providers tab (GeoHide + Sources + Task Scheduler)
        private GroupBox gbGeo;
        private CheckBox chkGeoHide;
        private Label lblRegionTitle;
        private RadioButton rbGeoRU;
        private RadioButton rbGeoEU;
        private RadioButton rbGeoUS;
        private LinkLabel lnkGeoSite;
        private Label lblGeoHideInfo;

        private GroupBox gbCustom;
        private Panel pnlCustomProviders;
        private Button btnAddCustom;
        private Button btnResetPresets;
        private Label lblCustomHint;

        private GroupBox gbScheduler;
        private Label lblTaskStatus;
        private Label lblFreq;
        private ComboBox cboSchedule;
        private CheckBox chkOnlyIfIdle;
        private TextBox txtSchedulerCmd;
        private Button btnCopyCmd;
        private Button btnToggleTask;
        private Button btnOpenTaskScheduler;
        private Label lblPathHint;

        private Button btnUpdateNow;
        private Label lblProviderStatus;

        private AppConfig config;

        public MainForm()
        {
            config = Program.LoadConfig();
            L10n.CurrentLang = string.IsNullOrEmpty(config.Language) ? "ru" : config.Language;
            InitUI();
            LoadConfigToUI();
        }

        private void InitUI()
        {
            this.Size = new Size(690, 805);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(246, 248, 250)
            };

            lblAppInfo = new Label
            {
                Text = "Hosts Launcher & Manager",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(14, 8),
                AutoSize = true
            };

            lblLang = new Label
            {
                Text = L10n.T("LanguageName"),
                Location = new Point(415, 9),
                AutoSize = true,
                ForeColor = Color.DimGray
            };

            cboLanguage = new ComboBox
            {
                Location = new Point(545, 6),
                Size = new Size(115, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboLanguage.Items.Add("🇷🇺 Русский");
            cboLanguage.Items.Add("🇬🇧 English");
            cboLanguage.SelectedIndex = (config.Language == "en") ? 1 : 0;
            cboLanguage.SelectedIndexChanged += (s, e) =>
            {
                string newLang = cboLanguage.SelectedIndex == 1 ? "en" : "ru";
                if (config.Language != newLang)
                {
                    config.Language = newLang;
                    L10n.CurrentLang = newLang;
                    Program.SaveConfig(config);
                    ApplyLocalization();
                }
            };

            pnlHeader.Controls.Add(lblAppInfo);
            pnlHeader.Controls.Add(lblLang);
            pnlHeader.Controls.Add(cboLanguage);

            tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F) };

            tabShortcuts = new TabPage();
            InitShortcutsTab();
            tabs.TabPages.Add(tabShortcuts);

            tabProviders = new TabPage();
            InitProvidersTab();
            tabs.TabPages.Add(tabProviders);

            this.Controls.Add(tabs);
            this.Controls.Add(pnlHeader);
        }

        private void InitShortcutsTab()
        {
            gbEditor = new GroupBox
            {
                Location = new Point(15, 12),
                Size = new Size(645, 168)
            };

            rbOpenWith = new RadioButton { Location = new Point(20, 24), AutoSize = true, Checked = true };
            rbNpp = new RadioButton { Location = new Point(20, 48), AutoSize = true };
            rbCode = new RadioButton { Location = new Point(20, 72), AutoSize = true };
            rbNotepad = new RadioButton { Location = new Point(20, 96), AutoSize = true };
            rbCustom = new RadioButton { Location = new Point(20, 120), AutoSize = true };

            txtCustomPath = new TextBox { Location = new Point(160, 119), Size = new Size(380, 23), Enabled = false };
            btnBrowseCustom = new Button { Location = new Point(550, 118), Size = new Size(80, 25), Enabled = false };

            rbCustom.CheckedChanged += (s, e) =>
            {
                txtCustomPath.Enabled = rbCustom.Checked;
                btnBrowseCustom.Enabled = rbCustom.Checked;
            };

            btnBrowseCustom.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = L10n.T("SelectExeFilter");
                    ofd.Title = L10n.T("SelectExeTitle");
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        txtCustomPath.Text = ofd.FileName;
                    }
                }
            };

            gbEditor.Controls.Add(rbOpenWith);
            gbEditor.Controls.Add(rbNpp);
            gbEditor.Controls.Add(rbCode);
            gbEditor.Controls.Add(rbNotepad);
            gbEditor.Controls.Add(rbCustom);
            gbEditor.Controls.Add(txtCustomPath);
            gbEditor.Controls.Add(btnBrowseCustom);

            gbSettings = new GroupBox
            {
                Location = new Point(15, 186),
                Size = new Size(645, 130)
            };

            lblShortcutName = new Label { Location = new Point(20, 26), AutoSize = true };
            txtShortcutName = new TextBox { Text = "Hosts", Location = new Point(140, 23), Size = new Size(200, 23) };

            lblIcon = new Label { Location = new Point(20, 58), AutoSize = true };
            cboIcon = new ComboBox { Location = new Point(140, 55), Size = new Size(480, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cboIcon.Items.Add(L10n.T("IconSystem"));
            cboIcon.Items.Add(L10n.T("IconNotepad"));
            cboIcon.Items.Add(L10n.T("IconNpp"));
            cboIcon.SelectedIndex = 0;

            chkAdmin = new CheckBox { Location = new Point(20, 92), AutoSize = true };

            gbSettings.Controls.Add(lblShortcutName);
            gbSettings.Controls.Add(txtShortcutName);
            gbSettings.Controls.Add(lblIcon);
            gbSettings.Controls.Add(cboIcon);
            gbSettings.Controls.Add(chkAdmin);

            btnCreateShortcut = new Button
            {
                Location = new Point(15, 324),
                Size = new Size(645, 42),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 245, 255)
            };
            btnCreateShortcut.Click += BtnCreateShortcut_Click;

            gbExtraShortcuts = new GroupBox
            {
                Location = new Point(15, 376),
                Size = new Size(645, 120)
            };

            btnCreateUpdateShortcut = new Button
            {
                Location = new Point(20, 28),
                Size = new Size(295, 40),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 255, 240)
            };
            btnCreateUpdateShortcut.Click += BtnCreateUpdateShortcut_Click;

            btnCreateManagerShortcut = new Button
            {
                Location = new Point(330, 28),
                Size = new Size(295, 40),
                Font = new Font("Segoe UI", 9.5F),
                BackColor = Color.FromArgb(245, 245, 250)
            };
            btnCreateManagerShortcut.Click += BtnCreateManagerShortcut_Click;

            lblExtraHint = new Label
            {
                Location = new Point(20, 78),
                Size = new Size(605, 32),
                ForeColor = Color.DarkSlateGray
            };

            gbExtraShortcuts.Controls.Add(btnCreateUpdateShortcut);
            gbExtraShortcuts.Controls.Add(btnCreateManagerShortcut);
            gbExtraShortcuts.Controls.Add(lblExtraHint);

            lblShortcutHint = new Label
            {
                Location = new Point(15, 510),
                Size = new Size(645, 35),
                ForeColor = Color.DimGray
            };

            tabShortcuts.Controls.Add(gbEditor);
            tabShortcuts.Controls.Add(gbSettings);
            tabShortcuts.Controls.Add(btnCreateShortcut);
            tabShortcuts.Controls.Add(gbExtraShortcuts);
            tabShortcuts.Controls.Add(lblShortcutHint);
        }

        private void InitProvidersTab()
        {
            // Блок 1: GeoHide
            gbGeo = new GroupBox
            {
                Location = new Point(15, 8),
                Size = new Size(645, 195)
            };

            chkGeoHide = new CheckBox
            {
                Location = new Point(15, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            lblRegionTitle = new Label
            {
                Location = new Point(15, 45),
                AutoSize = true
            };

            rbGeoRU = new RadioButton { Location = new Point(35, 68), AutoSize = true, Checked = true };
            rbGeoEU = new RadioButton { Location = new Point(35, 92), AutoSize = true };
            rbGeoUS = new RadioButton { Location = new Point(35, 116), AutoSize = true };

            lnkGeoSite = new LinkLabel
            {
                Location = new Point(35, 142),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };
            lnkGeoSite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://geohide.ru/"); }
                catch (Exception ex) { MessageBox.Show("Не удалось открыть браузер: " + ex.Message); }
            };

            lblGeoHideInfo = new Label
            {
                Location = new Point(35, 168),
                AutoSize = true,
                ForeColor = Color.DarkSlateGray
            };

            chkGeoHide.CheckedChanged += (s, e) =>
            {
                rbGeoRU.Enabled = chkGeoHide.Checked;
                rbGeoEU.Enabled = chkGeoHide.Checked;
                rbGeoUS.Enabled = chkGeoHide.Checked;
            };

            gbGeo.Controls.Add(chkGeoHide);
            gbGeo.Controls.Add(lblRegionTitle);
            gbGeo.Controls.Add(rbGeoRU);
            gbGeo.Controls.Add(rbGeoEU);
            gbGeo.Controls.Add(rbGeoUS);
            gbGeo.Controls.Add(lnkGeoSite);
            gbGeo.Controls.Add(lblGeoHideInfo);

            // Блок 2: Дополнительные источники
            gbCustom = new GroupBox
            {
                Location = new Point(15, 210),
                Size = new Size(645, 220)
            };

            pnlCustomProviders = new Panel
            {
                Location = new Point(15, 22),
                Size = new Size(615, 155),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(252, 252, 254)
            };

            btnAddCustom = new Button { Location = new Point(15, 183), Size = new Size(185, 28) };
            btnAddCustom.Click += BtnAddCustom_Click;

            btnResetPresets = new Button { Location = new Point(208, 183), Size = new Size(175, 28) };
            btnResetPresets.Click += (s, e) =>
            {
                if (MessageBox.Show(L10n.T("ConfirmResetPresets"), L10n.T("Confirmation"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    config.CustomProviders = Program.GetDefaultPresets();
                    Program.SaveConfig(config);
                    RefreshCustomProvidersList();
                }
            };

            lblCustomHint = new Label
            {
                Location = new Point(390, 183),
                Size = new Size(240, 32),
                ForeColor = Color.DarkSlateBlue
            };

            gbCustom.Controls.Add(pnlCustomProviders);
            gbCustom.Controls.Add(btnAddCustom);
            gbCustom.Controls.Add(btnResetPresets);
            gbCustom.Controls.Add(lblCustomHint);

            // Блок 3: Планировщик Windows
            gbScheduler = new GroupBox
            {
                Location = new Point(15, 436),
                Size = new Size(645, 185)
            };

            lblTaskStatus = new Label
            {
                Location = new Point(15, 20),
                Size = new Size(615, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            lblFreq = new Label
            {
                Location = new Point(15, 46),
                AutoSize = true
            };

            cboSchedule = new ComboBox
            {
                Location = new Point(100, 43),
                Size = new Size(230, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboSchedule.Items.Add(L10n.T("SchedDaily9"));
            cboSchedule.Items.Add(L10n.T("SchedDaily14"));
            cboSchedule.Items.Add(L10n.T("SchedEvery6h"));
            cboSchedule.Items.Add(L10n.T("SchedEvery12h"));
            cboSchedule.Items.Add(L10n.T("SchedLogon"));
            cboSchedule.Items.Add(L10n.T("SchedIdle"));
            cboSchedule.SelectedIndex = 0;

            string cmdString = "\"" + Application.ExecutablePath + "\" /update-silent";
            txtSchedulerCmd = new TextBox
            {
                Text = cmdString,
                Location = new Point(338, 43),
                Size = new Size(182, 23),
                ReadOnly = true,
                BackColor = Color.WhiteSmoke
            };

            btnCopyCmd = new Button
            {
                Location = new Point(525, 42),
                Size = new Size(105, 25)
            };
            btnCopyCmd.Click += (s, e) =>
            {
                Clipboard.SetText(txtSchedulerCmd.Text);
                MessageBox.Show(L10n.T("CmdCopied") + "\n" + txtSchedulerCmd.Text, L10n.T("Done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            chkOnlyIfIdle = new CheckBox
            {
                Location = new Point(15, 74),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Checked = true
            };

            btnToggleTask = new Button
            {
                Location = new Point(15, 102),
                Size = new Size(310, 36),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnToggleTask.Click += BtnToggleTask_Click;

            btnOpenTaskScheduler = new Button
            {
                Location = new Point(335, 102),
                Size = new Size(295, 36),
                Font = new Font("Segoe UI", 9F)
            };
            btnOpenTaskScheduler.Click += BtnOpenTaskScheduler_Click;

            lblPathHint = new Label
            {
                Location = new Point(15, 146),
                Size = new Size(615, 28),
                ForeColor = Color.DimGray
            };

            gbScheduler.Controls.Add(lblTaskStatus);
            gbScheduler.Controls.Add(lblFreq);
            gbScheduler.Controls.Add(cboSchedule);
            gbScheduler.Controls.Add(txtSchedulerCmd);
            gbScheduler.Controls.Add(btnCopyCmd);
            gbScheduler.Controls.Add(chkOnlyIfIdle);
            gbScheduler.Controls.Add(btnToggleTask);
            gbScheduler.Controls.Add(btnOpenTaskScheduler);
            gbScheduler.Controls.Add(lblPathHint);

            // Кнопка синхронизации
            btnUpdateNow = new Button
            {
                Location = new Point(15, 628),
                Size = new Size(645, 42),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 255, 240)
            };
            btnUpdateNow.Click += BtnUpdateNow_Click;

            lblProviderStatus = new Label
            {
                Location = new Point(15, 674),
                Size = new Size(645, 25),
                ForeColor = Color.Gray
            };

            tabProviders.Controls.Add(gbGeo);
            tabProviders.Controls.Add(gbCustom);
            tabProviders.Controls.Add(gbScheduler);
            tabProviders.Controls.Add(btnUpdateNow);
            tabProviders.Controls.Add(lblProviderStatus);
        }

        private void ApplyLocalization()
        {
            this.Text = L10n.T("AppTitle");
            if (lblLang != null) lblLang.Text = L10n.T("LanguageName");
            if (tabShortcuts != null) tabShortcuts.Text = L10n.T("TabShortcuts");
            if (tabProviders != null) tabProviders.Text = L10n.T("TabSubscriptions");

            // Shortcuts Tab
            if (gbEditor != null) gbEditor.Text = L10n.T("GbEditorTitle");
            if (rbOpenWith != null) rbOpenWith.Text = L10n.T("RbOpenWith");
            if (rbNpp != null) rbNpp.Text = L10n.T("RbNpp");
            if (rbCode != null) rbCode.Text = L10n.T("RbCode");
            if (rbNotepad != null) rbNotepad.Text = L10n.T("RbNotepad");
            if (rbCustom != null) rbCustom.Text = L10n.T("RbCustom");
            if (btnBrowseCustom != null) btnBrowseCustom.Text = L10n.T("BtnBrowse");

            if (gbSettings != null) gbSettings.Text = L10n.T("GbSettingsTitle");
            if (lblShortcutName != null) lblShortcutName.Text = L10n.T("LblShortcutName");
            if (lblIcon != null) lblIcon.Text = L10n.T("LblIcon");
            if (chkAdmin != null) chkAdmin.Text = L10n.T("ChkAdmin");

            if (cboIcon != null)
            {
                int iconIdx = cboIcon.SelectedIndex;
                cboIcon.Items.Clear();
                cboIcon.Items.Add(L10n.T("IconSystem"));
                cboIcon.Items.Add(L10n.T("IconNotepad"));
                cboIcon.Items.Add(L10n.T("IconNpp"));
                cboIcon.SelectedIndex = (iconIdx >= 0 && iconIdx < cboIcon.Items.Count) ? iconIdx : 0;
            }

            if (btnCreateShortcut != null) btnCreateShortcut.Text = L10n.T("BtnCreateHostsShortcut");
            if (gbExtraShortcuts != null) gbExtraShortcuts.Text = L10n.T("GbExtraShortcutsTitle");
            if (btnCreateUpdateShortcut != null) btnCreateUpdateShortcut.Text = L10n.T("BtnCreateUpdateShortcut");
            if (btnCreateManagerShortcut != null) btnCreateManagerShortcut.Text = L10n.T("BtnCreateManagerShortcut");
            if (lblExtraHint != null) lblExtraHint.Text = L10n.T("LblExtraHint");
            if (lblShortcutHint != null) lblShortcutHint.Text = L10n.T("ShortcutHint");

            // Providers Tab - GeoHide
            if (gbGeo != null) gbGeo.Text = L10n.T("GbGeoTitle");
            if (chkGeoHide != null) chkGeoHide.Text = L10n.T("ChkGeoHide");
            if (lblRegionTitle != null) lblRegionTitle.Text = L10n.T("LblRegionTitle");
            if (rbGeoRU != null) rbGeoRU.Text = L10n.T("RbGeoRU");
            if (rbGeoEU != null) rbGeoEU.Text = L10n.T("RbGeoEU");
            if (rbGeoUS != null) rbGeoUS.Text = L10n.T("RbGeoUS");
            if (lnkGeoSite != null) lnkGeoSite.Text = L10n.T("LnkGeoSite");
            if (lblGeoHideInfo != null)
            {
                string statusText = string.IsNullOrEmpty(config.GeoHideLastUpdated) ? L10n.T("StatusNever") : (config.GeoHideLastUpdated == "Отключен" ? L10n.T("StatusDisabled") : config.GeoHideLastUpdated);
                lblGeoHideInfo.Text = L10n.T("GeoStatusPrefix") + statusText;
            }

            // Providers Tab - Custom Providers
            if (gbCustom != null) gbCustom.Text = L10n.T("GbCustomTitle");
            if (btnAddCustom != null) btnAddCustom.Text = L10n.T("BtnAddCustom");
            if (btnResetPresets != null) btnResetPresets.Text = L10n.T("BtnResetPresets");
            if (lblCustomHint != null) lblCustomHint.Text = L10n.T("CustomHint");

            // Providers Tab - Task Scheduler
            if (gbScheduler != null) gbScheduler.Text = L10n.T("GbSchedulerTitle");
            if (lblFreq != null) lblFreq.Text = L10n.T("LblScheduleFreq");
            if (chkOnlyIfIdle != null) chkOnlyIfIdle.Text = L10n.T("ChkOnlyIfIdle");
            if (btnOpenTaskScheduler != null) btnOpenTaskScheduler.Text = L10n.T("BtnOpenScheduler");
            if (btnCopyCmd != null) btnCopyCmd.Text = L10n.T("BtnCopyCmd");
            if (lblPathHint != null) lblPathHint.Text = L10n.T("SchedulerPathHint");

            if (cboSchedule != null)
            {
                int schedIdx = cboSchedule.SelectedIndex;
                cboSchedule.Items.Clear();
                cboSchedule.Items.Add(L10n.T("SchedDaily9"));
                cboSchedule.Items.Add(L10n.T("SchedDaily14"));
                cboSchedule.Items.Add(L10n.T("SchedEvery6h"));
                cboSchedule.Items.Add(L10n.T("SchedEvery12h"));
                cboSchedule.Items.Add(L10n.T("SchedLogon"));
                cboSchedule.Items.Add(L10n.T("SchedIdle"));
                cboSchedule.SelectedIndex = (schedIdx >= 0 && schedIdx < cboSchedule.Items.Count) ? schedIdx : (config.TaskScheduleIndex >= 0 && config.TaskScheduleIndex < cboSchedule.Items.Count ? config.TaskScheduleIndex : 0);
            }

            if (btnUpdateNow != null) btnUpdateNow.Text = L10n.T("BtnUpdateNow");
            if (lblProviderStatus != null) lblProviderStatus.Text = L10n.T("BottomSafeNotice");

            RefreshCustomProvidersList();
            RefreshTaskStatus();
        }

        private void LoadConfigToUI()
        {
            if (config.PreferredEditor == "npp") rbNpp.Checked = true;
            else if (config.PreferredEditor == "code") rbCode.Checked = true;
            else if (config.PreferredEditor == "notepad") rbNotepad.Checked = true;
            else if (config.PreferredEditor == "custom")
            {
                rbCustom.Checked = true;
                txtCustomPath.Text = config.CustomEditorPath;
            }
            else rbOpenWith.Checked = true;

            chkAdmin.Checked = config.AlwaysAdmin;

            // GeoHide
            chkGeoHide.Checked = config.GeoHideEnabled;
            if (config.GeoHideRegion == "eu") rbGeoEU.Checked = true;
            else if (config.GeoHideRegion == "us") rbGeoUS.Checked = true;
            else rbGeoRU.Checked = true;

            rbGeoRU.Enabled = chkGeoHide.Checked;
            rbGeoEU.Enabled = chkGeoHide.Checked;
            rbGeoUS.Enabled = chkGeoHide.Checked;

            lblGeoHideInfo.Text = L10n.T("GeoStatusPrefix") + (string.IsNullOrEmpty(config.GeoHideLastUpdated) ? L10n.T("StatusNever") : (config.GeoHideLastUpdated == "Отключен" ? L10n.T("StatusDisabled") : config.GeoHideLastUpdated));

            if (chkOnlyIfIdle != null) chkOnlyIfIdle.Checked = config.TaskOnlyIfIdle;
            if (cboSchedule != null && config.TaskScheduleIndex >= 0 && config.TaskScheduleIndex < cboSchedule.Items.Count)
            {
                cboSchedule.SelectedIndex = config.TaskScheduleIndex;
            }

            RefreshCustomProvidersList();
            RefreshTaskStatus();
        }

        private void RefreshTaskStatus()
        {
            string nextRun, state;
            bool isIdleOnly;
            bool exists = Program.CheckTaskStatus(out nextRun, out state, out isIdleOnly);

            if (exists)
            {
                string idleSuffix = isIdleOnly ? L10n.T("TaskIdleSuffix") : "";
                lblTaskStatus.Text = L10n.T("TaskActive", idleSuffix, nextRun, state);
                lblTaskStatus.ForeColor = Color.DarkGreen;
                btnToggleTask.Text = L10n.T("BtnDeleteTask");
                btnToggleTask.BackColor = Color.FromArgb(255, 235, 235);
            }
            else
            {
                lblTaskStatus.Text = L10n.T("TaskNotCreated");
                lblTaskStatus.ForeColor = Color.DimGray;
                btnToggleTask.Text = L10n.T("BtnCreateTask");
                btnToggleTask.BackColor = Color.FromArgb(235, 255, 240);
            }
        }

        private void RefreshCustomProvidersList()
        {
            pnlCustomProviders.SuspendLayout();
            pnlCustomProviders.Controls.Clear();
            if (config.CustomProviders == null) config.CustomProviders = Program.GetDefaultPresets();

            int cardWidth = 590;
            int cardHeight = 54;
            int yOffset = 6;

            foreach (var p in config.CustomProviders)
            {
                Panel card = new Panel
                {
                    Location = new Point(6, yOffset),
                    Size = new Size(cardWidth, cardHeight),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Color.White
                };

                CheckBox chk = new CheckBox
                {
                    Text = p.Name,
                    Checked = p.Enabled,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Location = new Point(8, 4),
                    AutoSize = true
                };
                chk.CheckedChanged += (s, e) =>
                {
                    p.Enabled = chk.Checked;
                    Program.SaveConfig(config);
                };

                Button btnDel = new Button
                {
                    Text = "✕",
                    Size = new Size(26, 22),
                    Location = new Point(cardWidth - 32, 3),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.IndianRed,
                    Cursor = Cursors.Hand
                };
                btnDel.FlatAppearance.BorderSize = 0;
                btnDel.Click += (s, e) =>
                {
                    if (MessageBox.Show(L10n.T("ConfirmDeleteSource", p.Name), L10n.T("Confirmation"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        config.CustomProviders.Remove(p);
                        Program.SaveConfig(config);
                        RefreshCustomProvidersList();
                    }
                };

                LinkLabel lnkSite = new LinkLabel
                {
                    Text = L10n.T("CardSiteLink"),
                    Location = new Point(28, 28),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5F)
                };
                string siteUrl = !string.IsNullOrEmpty(p.SiteUrl) ? p.SiteUrl : p.Url;
                lnkSite.LinkClicked += (s, e) =>
                {
                    try { Process.Start(siteUrl); }
                    catch (Exception ex) { MessageBox.Show(L10n.T("ErrorOpenBrowser", ex.Message)); }
                };

                LinkLabel lnkRaw = new LinkLabel
                {
                    Text = L10n.T("CardRawLink"),
                    Location = new Point(140, 28),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5F)
                };
                lnkRaw.LinkClicked += (s, e) =>
                {
                    try { Process.Start(p.Url); }
                    catch (Exception ex) { MessageBox.Show(L10n.T("ErrorOpenRaw", ex.Message)); }
                };

                Label lblUpd = new Label
                {
                    Text = L10n.T("CardUpdatedPrefix") + (string.IsNullOrEmpty(p.LastUpdated) ? L10n.T("StatusNever") : p.LastUpdated),
                    Location = new Point(275, 28),
                    AutoSize = true,
                    ForeColor = Color.DarkSlateGray,
                    Font = new Font("Segoe UI", 8.5F)
                };

                card.Controls.Add(chk);
                card.Controls.Add(btnDel);
                card.Controls.Add(lnkSite);
                card.Controls.Add(lnkRaw);
                card.Controls.Add(lblUpd);

                pnlCustomProviders.Controls.Add(card);
                yOffset += cardHeight + 6;
            }

            pnlCustomProviders.ResumeLayout();
        }

        private void CreateDesktopShortcut(string targetPath, string arguments, string iconLocation, string linkName, string description)
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string shortcutPath = Path.Combine(desktop, linkName + ".lnk");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.Arguments = arguments;
                shortcut.IconLocation = iconLocation;
                shortcut.Description = description;
                shortcut.Save();

                MessageBox.Show(
                    L10n.T("ShortcutCreatedSuccess", linkName),
                    L10n.T("Done"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(L10n.T("ShortcutCreateError", ex.Message), L10n.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCreateShortcut_Click(object sender, EventArgs e)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string launcherPath = Path.Combine(baseDir, "OpenHostsFile.exe");
            if (!File.Exists(launcherPath)) launcherPath = Path.Combine(baseDir, "OpenHosts.exe");
            if (!File.Exists(launcherPath)) launcherPath = Path.Combine(baseDir, "HostsLauncher.exe");
            if (!File.Exists(launcherPath)) launcherPath = Application.ExecutablePath;

            string args = "";
            if (rbNpp.Checked) args = "/npp";
            else if (rbCode.Checked) args = "/code";
            else if (rbNotepad.Checked) args = "/notepad";
            else if (rbCustom.Checked)
            {
                if (string.IsNullOrEmpty(txtCustomPath.Text))
                {
                    MessageBox.Show(L10n.T("SpecifyEditorPath"), L10n.T("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                args = "\"" + txtCustomPath.Text + "\"";
            }

            if (chkAdmin.Checked && !args.Contains("/admin"))
            {
                args = (args + " /admin").Trim();
            }

            string iconLoc = @"C:\Windows\System32\shell32.dll,0";
            if (cboIcon.SelectedIndex == 1) iconLoc = "notepad.exe,0";
            else if (cboIcon.SelectedIndex == 2)
            {
                string npp = @"C:\Program Files\Notepad++\notepad++.exe";
                if (File.Exists(npp)) iconLoc = npp + ",0";
            }

            string linkName = string.IsNullOrEmpty(txtShortcutName.Text) ? L10n.T("ShortcutDefaultName") : txtShortcutName.Text.Trim();

            config.PreferredEditor = rbNpp.Checked ? "npp" : (rbCode.Checked ? "code" : (rbNotepad.Checked ? "notepad" : (rbCustom.Checked ? "custom" : "openwith")));
            config.CustomEditorPath = txtCustomPath.Text;
            config.AlwaysAdmin = chkAdmin.Checked;
            Program.SaveConfig(config);

            CreateDesktopShortcut(launcherPath, args, iconLoc, linkName, L10n.T("ShortcutHostsDesc"));
        }

        private void BtnCreateUpdateShortcut_Click(object sender, EventArgs e)
        {
            string exePath = Application.ExecutablePath;
            string iconLoc = @"C:\Windows\System32\shell32.dll,238";
            CreateDesktopShortcut(exePath, "/update-now", iconLoc, L10n.T("ShortcutUpdateName"), L10n.T("ShortcutUpdateDesc"));
        }

        private void BtnCreateManagerShortcut_Click(object sender, EventArgs e)
        {
            string exePath = Application.ExecutablePath;
            string iconLoc = @"C:\Windows\System32\shell32.dll,21";
            CreateDesktopShortcut(exePath, "", iconLoc, L10n.T("ShortcutManagerName"), L10n.T("ShortcutManagerDesc"));
        }

        private void BtnAddCustom_Click(object sender, EventArgs e)
        {
            string url = PromptDialog(L10n.T("EnterUrlPrompt"), L10n.T("EnterUrlTitle"));
            if (string.IsNullOrEmpty(url)) return;

            string siteUrl = PromptDialog(L10n.T("EnterSitePrompt"), L10n.T("EnterSiteTitle"));

            string name = PromptDialog(L10n.T("EnterNamePrompt"), L10n.T("EnterNameTitle"));
            if (string.IsNullOrEmpty(name)) name = L10n.T("SourceDefaultName", config.CustomProviders.Count + 1);

            config.CustomProviders.Add(new CustomProviderConfig
            {
                Name = name,
                Url = url,
                SiteUrl = string.IsNullOrEmpty(siteUrl) ? url : siteUrl,
                Enabled = true,
                LastHash = "",
                LastUpdated = ""
            });
            Program.SaveConfig(config);
            RefreshCustomProvidersList();
        }

        private void BtnUpdateNow_Click(object sender, EventArgs e)
        {
            config.GeoHideEnabled = chkGeoHide.Checked;
            config.GeoHideRegion = rbGeoEU.Checked ? "eu" : (rbGeoUS.Checked ? "us" : "ru");
            Program.SaveConfig(config);

            btnUpdateNow.Enabled = false;
            lblProviderStatus.Text = L10n.T("SyncInProgress");
            lblProviderStatus.ForeColor = Color.Blue;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                bool res = Program.UpdateHostsRoutine(false);

                this.BeginInvoke((Action)(() =>
                {
                    config = Program.LoadConfig();
                    LoadConfigToUI();
                    btnUpdateNow.Enabled = true;

                    if (res)
                    {
                        lblProviderStatus.Text = L10n.T("SyncSuccess", DateTime.Now.ToString("HH:mm:ss"));
                        lblProviderStatus.ForeColor = Color.DarkGreen;
                        MessageBox.Show(L10n.T("SyncSuccessBox"), L10n.T("Success"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        lblProviderStatus.Text = L10n.T("SyncError");
                        lblProviderStatus.ForeColor = Color.Red;
                    }
                }));
            });
        }

        private void BtnToggleTask_Click(object sender, EventArgs e)
        {
            string nextRun, state;
            bool exists = Program.CheckTaskStatus(out nextRun, out state);

            try
            {
                string taskName = "HostsManagerAutoUpdate";
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "schtasks.exe";
                psi.Verb = "runas";
                psi.UseShellExecute = true;

                if (exists)
                {
                    psi.Arguments = string.Format("/delete /tn \"{0}\" /f", taskName);
                    Process p = Process.Start(psi);
                    if (p != null) p.WaitForExit();
                    RefreshTaskStatus();
                    MessageBox.Show(L10n.T("TaskDeleted"), L10n.T("Done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string exePath = Application.ExecutablePath;
                    string scheduleArgs = "/sc daily /st 09:00";
                    string humanSchedule = L10n.T("HumanSchedDaily9");
                    bool isScheduleOnIdle = false;

                    if (cboSchedule != null)
                    {
                        switch (cboSchedule.SelectedIndex)
                        {
                            case 1:
                                scheduleArgs = "/sc daily /st 14:00";
                                humanSchedule = L10n.T("HumanSchedDaily14");
                                break;
                            case 2:
                                scheduleArgs = "/sc hourly /mo 6";
                                humanSchedule = L10n.T("HumanSchedEvery6h");
                                break;
                            case 3:
                                scheduleArgs = "/sc hourly /mo 12";
                                humanSchedule = L10n.T("HumanSchedEvery12h");
                                break;
                            case 4:
                                scheduleArgs = "/sc onlogon";
                                humanSchedule = L10n.T("HumanSchedLogon");
                                break;
                            case 5:
                                scheduleArgs = "/sc onidle /i 10";
                                humanSchedule = L10n.T("HumanSchedIdle");
                                isScheduleOnIdle = true;
                                break;
                            default:
                                scheduleArgs = "/sc daily /st 09:00";
                                humanSchedule = L10n.T("HumanSchedDaily9");
                                break;
                        }
                    }

                    psi.Arguments = string.Format("/create /tn \"{0}\" /tr \"\\\"{1}\\\" /update-silent\" {2} /rl highest /f", taskName, exePath, scheduleArgs);
                    Process p = Process.Start(psi);
                    if (p != null) p.WaitForExit();

                    // Если включен флаг «Обновлять только при простое» и расписание не чистое ONIDLE
                    if (chkOnlyIfIdle != null && chkOnlyIfIdle.Checked && !isScheduleOnIdle)
                    {
                        try
                        {
                            ProcessStartInfo qPsi = new ProcessStartInfo
                            {
                                FileName = "schtasks.exe",
                                Arguments = string.Format("/query /tn \"{0}\" /xml", taskName),
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                RedirectStandardOutput = true
                            };
                            Process qProc = Process.Start(qPsi);
                            string xml = qProc != null ? qProc.StandardOutput.ReadToEnd() : "";
                            if (qProc != null) qProc.WaitForExit();

                            if (!string.IsNullOrEmpty(xml))
                            {
                                if (!xml.Contains("<RunOnlyIfIdle>"))
                                {
                                    xml = xml.Replace("<Settings>", "<Settings>\r\n    <RunOnlyIfIdle>true</RunOnlyIfIdle>");
                                }
                                else
                                {
                                    xml = xml.Replace("<RunOnlyIfIdle>false</RunOnlyIfIdle>", "<RunOnlyIfIdle>true</RunOnlyIfIdle>");
                                }

                                string tmpXmlPath = Path.Combine(Path.GetTempPath(), "HostsAutoUpdateTask.xml");
                                File.WriteAllText(tmpXmlPath, xml, Encoding.Unicode);

                                ProcessStartInfo xmlPsi = new ProcessStartInfo
                                {
                                    FileName = "schtasks.exe",
                                    Arguments = string.Format("/create /tn \"{0}\" /xml \"{1}\" /f", taskName, tmpXmlPath),
                                    Verb = "runas",
                                    UseShellExecute = true
                                };
                                Process xmlProc = Process.Start(xmlPsi);
                                if (xmlProc != null) xmlProc.WaitForExit();

                                try { File.Delete(tmpXmlPath); } catch { }
                            }
                        }
                        catch { }
                    }

                    if (chkOnlyIfIdle != null) config.TaskOnlyIfIdle = chkOnlyIfIdle.Checked;
                    if (cboSchedule != null) config.TaskScheduleIndex = cboSchedule.SelectedIndex;
                    Program.SaveConfig(config);

                    RefreshTaskStatus();

                    string idleNotice = (chkOnlyIfIdle != null && chkOnlyIfIdle.Checked) ? L10n.T("TaskCreatedIdleNotice") : "";
                    MessageBox.Show(L10n.T("TaskCreatedDetails", humanSchedule, idleNotice), L10n.T("Done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(L10n.T("ErrorPrefix") + ex.Message, L10n.T("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnOpenTaskScheduler_Click(object sender, EventArgs e)
        {
            try
            {
                Process p = Process.Start("taskschd.msc");
                if (p != null)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        for (int i = 0; i < 25; i++)
                        {
                            Thread.Sleep(250);
                            p.Refresh();
                            if (p.MainWindowHandle != IntPtr.Zero)
                            {
                                try
                                {
                                    SetForegroundWindow(p.MainWindowHandle);
                                    Thread.Sleep(150);
                                    SendKeys.SendWait("{DOWN}"); // Выбираем "Библиотека планировщика заданий"
                                    Thread.Sleep(150);
                                    SendKeys.SendWait("{TAB}");  // Переходим в список задач справа
                                    Thread.Sleep(150);
                                    SendKeys.SendWait("H");      // Наводим курсор на HostsManagerAutoUpdate
                                }
                                catch { }
                                break;
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(L10n.T("ErrorLaunchScheduler", ex.Message), L10n.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string PromptDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 170,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            Label lblText = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true };
            TextBox txtInput = new TextBox() { Left = 20, Top = 50, Width = 440 };
            Button confirmation = new Button() { Text = "OK", Left = 280, Width = 85, Top = 85, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = L10n.T("Cancel"), Left = 375, Width = 85, Top = 85, DialogResult = DialogResult.Cancel };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            cancel.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(lblText);
            prompt.Controls.Add(txtInput);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? txtInput.Text : "";
        }
    }
}
