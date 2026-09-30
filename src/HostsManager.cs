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

        // Дополнительные источники
        public List<CustomProviderConfig> CustomProviders { get; set; }

        public AppConfig()
        {
            PreferredEditor = "openwith";
            CustomEditorPath = "";
            AlwaysAdmin = false;
            BackupBeforeUpdate = true;

            GeoHideEnabled = true;
            GeoHideRegion = "ru";
            GeoHideLastHash = "";
            GeoHideLastUpdated = "";

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
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                },
                new CustomProviderConfig
                {
                    Name = "Windows SpyBlocker (Телеметрия)",
                    Url = "https://raw.githubusercontent.com/crazy-max/WindowsSpyBlocker/master/data/hosts/spy.txt",
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                },
                new CustomProviderConfig
                {
                    Name = "AdAway (Блокировка рекламы)",
                    Url = "https://adaway.org/hosts.txt",
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                },
                new CustomProviderConfig
                {
                    Name = "StevenBlack (Анти-реклама и фишинг)",
                    Url = "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts",
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

        public static bool CheckTaskStatus(out string nextRun, out string state)
        {
            nextRun = "";
            state = "";
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
                            return true;
                        }
                        return true;
                    }
                }
            }
            catch { }
            return false;
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
                    bool success = UpdateHostsRoutine(cmd == "/update-silent");
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

    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private TabControl tabs;
        private TabPage tabShortcuts;
        private TabPage tabProviders;

        // Shortcuts tab
        private RadioButton rbOpenWith;
        private RadioButton rbNpp;
        private RadioButton rbCode;
        private RadioButton rbNotepad;
        private RadioButton rbCustom;
        private TextBox txtCustomPath;
        private Button btnBrowseCustom;
        private CheckBox chkAdmin;
        private TextBox txtShortcutName;
        private ComboBox cboIcon;
        private Button btnCreateShortcut;
        private Label lblShortcutHint;

        // Providers tab (GeoHide + Sources + Task Scheduler)
        private CheckBox chkGeoHide;
        private RadioButton rbGeoRU;
        private RadioButton rbGeoEU;
        private RadioButton rbGeoUS;
        private Label lblGeoHideInfo;

        private ListView lvCustomProviders;
        private Button btnAddCustom;
        private Button btnRemoveCustom;
        private Button btnResetPresets;

        private Label lblTaskStatus;
        private TextBox txtSchedulerCmd;
        private Button btnCopyCmd;
        private Button btnToggleTask;
        private Button btnOpenTaskScheduler;

        private Button btnUpdateNow;
        private Label lblProviderStatus;

        private AppConfig config;

        public MainForm()
        {
            config = Program.LoadConfig();
            InitUI();
            LoadConfigToUI();
        }

        private void InitUI()
        {
            this.Text = "Hosts Manager & Shortcut Creator";
            this.Size = new Size(690, 715);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F) };

            tabShortcuts = new TabPage("🚀 Создать ярлык");
            InitShortcutsTab();
            tabs.TabPages.Add(tabShortcuts);

            tabProviders = new TabPage("🌐 Подписки и автообновление");
            InitProvidersTab();
            tabs.TabPages.Add(tabProviders);

            this.Controls.Add(tabs);
        }

        private void InitShortcutsTab()
        {
            GroupBox gbEditor = new GroupBox
            {
                Text = "В каком редакторе открывать hosts по клику на ярлык",
                Location = new Point(15, 15),
                Size = new Size(645, 175)
            };

            rbOpenWith = new RadioButton { Text = "Стандартное окно Windows «Открыть с помощью...» (выбор редактора)", Location = new Point(20, 25), AutoSize = true, Checked = true };
            rbNpp = new RadioButton { Text = "Notepad++ (при наличии в системе)", Location = new Point(20, 50), AutoSize = true };
            rbCode = new RadioButton { Text = "Visual Studio Code", Location = new Point(20, 75), AutoSize = true };
            rbNotepad = new RadioButton { Text = "Системный Блокнот (всегда с правами администратора)", Location = new Point(20, 100), AutoSize = true };
            rbCustom = new RadioButton { Text = "Другой редактор:", Location = new Point(20, 125), AutoSize = true };

            txtCustomPath = new TextBox { Location = new Point(160, 124), Size = new Size(380, 23), Enabled = false };
            btnBrowseCustom = new Button { Text = "Обзор...", Location = new Point(550, 123), Size = new Size(80, 25), Enabled = false };

            rbCustom.CheckedChanged += (s, e) =>
            {
                txtCustomPath.Enabled = rbCustom.Checked;
                btnBrowseCustom.Enabled = rbCustom.Checked;
            };

            btnBrowseCustom.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*";
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

            GroupBox gbSettings = new GroupBox
            {
                Text = "Параметры ярлыка",
                Location = new Point(15, 205),
                Size = new Size(645, 140)
            };

            Label lblName = new Label { Text = "Имя ярлыка:", Location = new Point(20, 30), AutoSize = true };
            txtShortcutName = new TextBox { Text = "Hosts", Location = new Point(140, 27), Size = new Size(200, 23) };

            Label lblIcon = new Label { Text = "Иконка:", Location = new Point(20, 65), AutoSize = true };
            cboIcon = new ComboBox { Location = new Point(140, 62), Size = new Size(480, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cboIcon.Items.Add("Системная иконка файла без расширения (shell32.dll, 0) [Рекомендуется]");
            cboIcon.Items.Add("Иконка Блокнота (notepad.exe, 0)");
            cboIcon.Items.Add("Иконка Notepad++ (при наличии)");
            cboIcon.SelectedIndex = 0;

            chkAdmin = new CheckBox { Text = "Запрашивать права администратора при открытии (/admin)", Location = new Point(20, 100), AutoSize = true };

            gbSettings.Controls.Add(lblName);
            gbSettings.Controls.Add(txtShortcutName);
            gbSettings.Controls.Add(lblIcon);
            gbSettings.Controls.Add(cboIcon);
            gbSettings.Controls.Add(chkAdmin);

            btnCreateShortcut = new Button
            {
                Text = "✨ Создать ярлык на Рабочем столе",
                Location = new Point(15, 360),
                Size = new Size(645, 44),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 245, 255)
            };
            btnCreateShortcut.Click += BtnCreateShortcut_Click;

            lblShortcutHint = new Label
            {
                Text = "💡 Подсказка: После создания нажмите правой кнопкой по ярлыку на Рабочем столе и выберите «Закрепить на панели задач».",
                Location = new Point(15, 415),
                Size = new Size(645, 35),
                ForeColor = Color.Gray
            };

            tabShortcuts.Controls.Add(gbEditor);
            tabShortcuts.Controls.Add(gbSettings);
            tabShortcuts.Controls.Add(btnCreateShortcut);
            tabShortcuts.Controls.Add(lblShortcutHint);
        }

        private void InitProvidersTab()
        {
            // Блок 1: GeoHide
            GroupBox gbGeo = new GroupBox
            {
                Text = "1. Сервис GeoHide (Антиблокировка / DNS Прокси)",
                Location = new Point(15, 8),
                Size = new Size(645, 175)
            };

            chkGeoHide = new CheckBox
            {
                Text = "Включить GeoHide (автоматически обновлять правила обхода)",
                Location = new Point(15, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            Label lblRegionTitle = new Label
            {
                Text = "Серверный регион (выбирается строго один для маршрутизации):",
                Location = new Point(15, 45),
                AutoSize = true
            };

            rbGeoRU = new RadioButton { Text = "Россия (RU) — наименьшая задержка (по умолчанию)", Location = new Point(35, 68), AutoSize = true, Checked = true };
            rbGeoEU = new RadioButton { Text = "Европа (EU)", Location = new Point(35, 92), AutoSize = true };
            rbGeoUS = new RadioButton { Text = "США (US)", Location = new Point(35, 116), AutoSize = true };

            lblGeoHideInfo = new Label
            {
                Text = "Статус: Обновлено: " + (string.IsNullOrEmpty(config.GeoHideLastUpdated) ? "Никогда" : config.GeoHideLastUpdated),
                Location = new Point(15, 145),
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
            gbGeo.Controls.Add(lblGeoHideInfo);

            // Блок 2: Дополнительные источники
            GroupBox gbCustom = new GroupBox
            {
                Text = "2. Каталог популярных подписок и свои источники (отметьте нужные галочками):",
                Location = new Point(15, 190),
                Size = new Size(645, 220)
            };

            lvCustomProviders = new ListView
            {
                Location = new Point(15, 22),
                Size = new Size(615, 125),
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true
            };
            lvCustomProviders.Columns.Add("Вкл", 45);
            lvCustomProviders.Columns.Add("Название", 240);
            lvCustomProviders.Columns.Add("URL источника", 205);
            lvCustomProviders.Columns.Add("Обновлено", 115);

            btnAddCustom = new Button { Text = "➕ Свой URL...", Location = new Point(15, 153), Size = new Size(115, 28) };
            btnAddCustom.Click += BtnAddCustom_Click;

            btnRemoveCustom = new Button { Text = "🗑️ Удалить", Location = new Point(135, 153), Size = new Size(90, 28) };
            btnRemoveCustom.Click += BtnRemoveCustom_Click;

            btnResetPresets = new Button { Text = "🔄 Восстановить каталог пресетов", Location = new Point(385, 153), Size = new Size(245, 28) };
            btnResetPresets.Click += (s, e) =>
            {
                if (MessageBox.Show("Сбросить список к популярным встроенным пресетам?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    config.CustomProviders = Program.GetDefaultPresets();
                    Program.SaveConfig(config);
                    RefreshCustomProvidersList();
                }
            };

            Label lblCustomHint = new Label
            {
                Text = "💡 Пресеты: GitHub520 (ускорение GitHub), Windows SpyBlocker (анти-слежка), AdAway и StevenBlack.",
                Location = new Point(15, 190),
                Size = new Size(615, 22),
                ForeColor = Color.DarkSlateBlue
            };

            gbCustom.Controls.Add(lvCustomProviders);
            gbCustom.Controls.Add(btnAddCustom);
            gbCustom.Controls.Add(btnRemoveCustom);
            gbCustom.Controls.Add(btnResetPresets);
            gbCustom.Controls.Add(lblCustomHint);

            // Блок 3: Планировщик Windows
            GroupBox gbScheduler = new GroupBox
            {
                Text = "3. Фоновое автообновление (Планировщик задач Windows)",
                Location = new Point(15, 418),
                Size = new Size(645, 140)
            };

            lblTaskStatus = new Label
            {
                Text = "Статус: Проверка задачи...",
                Location = new Point(15, 22),
                Size = new Size(615, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            string cmdString = "\"" + Application.ExecutablePath + "\" /update-silent";
            txtSchedulerCmd = new TextBox
            {
                Text = cmdString,
                Location = new Point(15, 48),
                Size = new Size(505, 23),
                ReadOnly = true,
                BackColor = Color.WhiteSmoke
            };

            btnCopyCmd = new Button
            {
                Text = "📋 Копировать",
                Location = new Point(525, 47),
                Size = new Size(105, 25)
            };
            btnCopyCmd.Click += (s, e) =>
            {
                Clipboard.SetText(txtSchedulerCmd.Text);
                MessageBox.Show("Команда скопирована в буфер обмена!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnToggleTask = new Button
            {
                Location = new Point(15, 82),
                Size = new Size(310, 36),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnToggleTask.Click += BtnToggleTask_Click;

            btnOpenTaskScheduler = new Button
            {
                Text = "📅 Открыть в Планировщике Windows",
                Location = new Point(335, 82),
                Size = new Size(295, 36),
                Font = new Font("Segoe UI", 9F)
            };
            btnOpenTaskScheduler.Click += BtnOpenTaskScheduler_Click;

            Label lblPathHint = new Label
            {
                Text = "📍 В Планировщике задача находится в папке: «Библиотека планировщика заданий»",
                Location = new Point(15, 120),
                Size = new Size(615, 18),
                ForeColor = Color.Gray
            };

            gbScheduler.Controls.Add(lblTaskStatus);
            gbScheduler.Controls.Add(txtSchedulerCmd);
            gbScheduler.Controls.Add(btnCopyCmd);
            gbScheduler.Controls.Add(btnToggleTask);
            gbScheduler.Controls.Add(btnOpenTaskScheduler);
            gbScheduler.Controls.Add(lblPathHint);

            // Кнопка синхронизации
            btnUpdateNow = new Button
            {
                Text = "🔄 Синхронизировать hosts сейчас",
                Location = new Point(15, 568),
                Size = new Size(645, 44),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 255, 240)
            };
            btnUpdateNow.Click += BtnUpdateNow_Click;

            lblProviderStatus = new Label
            {
                Text = "Личные ручные записи в hosts изолированы и надежно защищены от перезаписи.",
                Location = new Point(15, 620),
                Size = new Size(645, 30),
                ForeColor = Color.Gray
            };

            tabProviders.Controls.Add(gbGeo);
            tabProviders.Controls.Add(gbCustom);
            tabProviders.Controls.Add(gbScheduler);
            tabProviders.Controls.Add(btnUpdateNow);
            tabProviders.Controls.Add(lblProviderStatus);
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

            lblGeoHideInfo.Text = "Статус: Обновлено: " + (string.IsNullOrEmpty(config.GeoHideLastUpdated) ? "Никогда" : config.GeoHideLastUpdated);

            RefreshCustomProvidersList();
            RefreshTaskStatus();
        }

        private void RefreshTaskStatus()
        {
            string nextRun, state;
            bool exists = Program.CheckTaskStatus(out nextRun, out state);

            if (exists)
            {
                lblTaskStatus.Text = string.Format("🟢 Задача активна в Windows. Следующий запуск: {0} ({1})", nextRun, state);
                lblTaskStatus.ForeColor = Color.DarkGreen;
                btnToggleTask.Text = "🗑️ Удалить задачу из Планировщика";
                btnToggleTask.BackColor = Color.FromArgb(255, 235, 235);
            }
            else
            {
                lblTaskStatus.Text = "⚪ Задача не создана (фоновое автообновление выключено)";
                lblTaskStatus.ForeColor = Color.DimGray;
                btnToggleTask.Text = "⚡ Создать задачу (раз в день в 09:00)";
                btnToggleTask.BackColor = Color.FromArgb(235, 255, 240);
            }
        }

        private void RefreshCustomProvidersList()
        {
            lvCustomProviders.Items.Clear();
            if (config.CustomProviders == null) config.CustomProviders = Program.GetDefaultPresets();

            foreach (var p in config.CustomProviders)
            {
                ListViewItem item = new ListViewItem("");
                item.Checked = p.Enabled;
                item.SubItems.Add(p.Name);
                item.SubItems.Add(p.Url);
                item.SubItems.Add(string.IsNullOrEmpty(p.LastUpdated) ? "Никогда" : p.LastUpdated);
                item.Tag = p;
                lvCustomProviders.Items.Add(item);
            }
        }

        private void BtnCreateShortcut_Click(object sender, EventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string launcherPath = Path.Combine(baseDir, "OpenHostsFile.exe");
                if (!File.Exists(launcherPath)) launcherPath = Path.Combine(baseDir, "OpenHosts.exe");
                if (!File.Exists(launcherPath)) launcherPath = Path.Combine(baseDir, "HostsLauncher.exe");
                if (!File.Exists(launcherPath))
                {
                    launcherPath = Application.ExecutablePath;
                }

                string args = "";
                if (rbNpp.Checked) args = "/npp";
                else if (rbCode.Checked) args = "/code";
                else if (rbNotepad.Checked) args = "/notepad";
                else if (rbCustom.Checked)
                {
                    if (string.IsNullOrEmpty(txtCustomPath.Text))
                    {
                        MessageBox.Show("Укажите путь к редактору!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string linkName = string.IsNullOrEmpty(txtShortcutName.Text) ? "Hosts" : txtShortcutName.Text.Trim();
                string shortcutPath = Path.Combine(desktop, linkName + ".lnk");

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = launcherPath;
                shortcut.Arguments = args;
                shortcut.IconLocation = iconLoc;
                shortcut.Description = "Файл hosts";
                shortcut.Save();

                config.PreferredEditor = rbNpp.Checked ? "npp" : (rbCode.Checked ? "code" : (rbNotepad.Checked ? "notepad" : (rbCustom.Checked ? "custom" : "openwith")));
                config.CustomEditorPath = txtCustomPath.Text;
                config.AlwaysAdmin = chkAdmin.Checked;
                Program.SaveConfig(config);

                MessageBox.Show(
                    "Ярлык \"" + linkName + ".lnk\" успешно создан на Рабочем столе!\n\n" +
                    "Чтобы закрепить его на панели задач:\n" +
                    "Нажмите правой кнопкой мыши по созданному ярлыку -> «Закрепить на панели задач».",
                    "Готово",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка создания ярлыка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddCustom_Click(object sender, EventArgs e)
        {
            string url = PromptDialog("Введите URL списка правил hosts:", "Добавление источника");
            if (string.IsNullOrEmpty(url)) return;

            string name = PromptDialog("Введите краткое имя источника:", "Имя источника");
            if (string.IsNullOrEmpty(name)) name = "Источник " + (config.CustomProviders.Count + 1);

            config.CustomProviders.Add(new CustomProviderConfig
            {
                Name = name,
                Url = url,
                Enabled = true,
                LastHash = "",
                LastUpdated = ""
            });
            Program.SaveConfig(config);
            RefreshCustomProvidersList();
        }

        private void BtnRemoveCustom_Click(object sender, EventArgs e)
        {
            if (lvCustomProviders.SelectedItems.Count == 0) return;
            var item = lvCustomProviders.SelectedItems[0];
            var provider = item.Tag as CustomProviderConfig;
            if (provider != null)
            {
                config.CustomProviders.Remove(provider);
                Program.SaveConfig(config);
                RefreshCustomProvidersList();
            }
        }

        private void BtnUpdateNow_Click(object sender, EventArgs e)
        {
            config.GeoHideEnabled = chkGeoHide.Checked;
            config.GeoHideRegion = rbGeoEU.Checked ? "eu" : (rbGeoUS.Checked ? "us" : "ru");

            for (int i = 0; i < lvCustomProviders.Items.Count; i++)
            {
                var p = lvCustomProviders.Items[i].Tag as CustomProviderConfig;
                if (p != null) p.Enabled = lvCustomProviders.Items[i].Checked;
            }
            Program.SaveConfig(config);

            btnUpdateNow.Enabled = false;
            lblProviderStatus.Text = "⏳ Синхронизация правил... Пожалуйста, подождите.";
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
                        lblProviderStatus.Text = "✅ Hosts успешно синхронизирован! DNS-кэш сброшен (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                        lblProviderStatus.ForeColor = Color.DarkGreen;
                        MessageBox.Show("Файл hosts успешно обновлен!\nDNS-кэш Windows автоматически сброшен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        lblProviderStatus.Text = "⚠️ Ошибка обновления (отменено в окне UAC или нет сети).";
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
                    MessageBox.Show("Задача успешно удалена из Планировщика Windows.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string exePath = Application.ExecutablePath;
                    psi.Arguments = string.Format("/create /tn \"{0}\" /tr \"\\\"{1}\\\" /update-silent\" /sc daily /st 09:00 /rl highest /f", taskName, exePath);
                    Process p = Process.Start(psi);
                    if (p != null) p.WaitForExit();
                    RefreshTaskStatus();
                    MessageBox.Show("Задача успешно создана в Планировщике Windows!\nЗапуск: каждый день в 09:00 с наивысшими правами.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                MessageBox.Show("Ошибка запуска taskschd.msc: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            Button cancel = new Button() { Text = "Отмена", Left = 375, Width = 85, Top = 85, DialogResult = DialogResult.Cancel };
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
