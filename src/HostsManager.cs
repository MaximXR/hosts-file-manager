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
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace HostsManager
{
    public class ProviderConfig
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
        public bool SchedulerEnabled { get; set; }
        public string SchedulerInterval { get; set; }
        public List<ProviderConfig> Providers { get; set; }

        public AppConfig()
        {
            PreferredEditor = "openwith";
            CustomEditorPath = "";
            AlwaysAdmin = false;
            BackupBeforeUpdate = true;
            SchedulerEnabled = false;
            SchedulerInterval = "daily";
            Providers = new List<ProviderConfig>
            {
                new ProviderConfig
                {
                    Name = "GeoHide (RU)",
                    Url = "https://geohide.ru/hosts",
                    Enabled = true,
                    LastHash = "",
                    LastUpdated = ""
                },
                new ProviderConfig
                {
                    Name = "GeoHide (EU)",
                    Url = "https://geohide.ru/eu/hosts",
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                },
                new ProviderConfig
                {
                    Name = "GeoHide (US)",
                    Url = "https://geohide.ru/us/hosts",
                    Enabled = false,
                    LastHash = "",
                    LastUpdated = ""
                }
            };
        }
    }

    static class Program
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache")]
        public static extern int DnsFlushResolverCache();

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
                    return serializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch { }
            return new AppConfig();
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
                // Запуск с повышением прав
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = Application.ExecutablePath;
                    psi.Arguments = silent ? "/update-silent" : "/apply-hosts-elevated";
                    psi.Verb = "runas";
                    psi.UseShellExecute = true;
                    Process p = Process.Start(psi);
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                string hostsContent = File.Exists(HostsPath) ? File.ReadAllText(HostsPath, Encoding.UTF8) : "";

                // Бэкап перед записью
                if (config.BackupBeforeUpdate && File.Exists(HostsPath))
                {
                    string backupPath = HostsPath + ".bak";
                    File.Copy(HostsPath, backupPath, true);
                }

                bool hasAnyChanges = false;
                using (WebClient client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) HostsManager";

                    foreach (var provider in config.Providers)
                    {
                        string blockStartMarker = string.Format("# === BEGIN HOSTS-MANAGER MANAGED BLOCK [{0}] ===", provider.Name);
                        string blockEndMarker = string.Format("# === END HOSTS-MANAGER MANAGED BLOCK [{0}] ===", provider.Name);

                        if (!provider.Enabled)
                        {
                            // Удаляем блок, если он был
                            if (hostsContent.Contains(blockStartMarker))
                            {
                                hostsContent = RemoveBlock(hostsContent, blockStartMarker, blockEndMarker);
                                hasAnyChanges = true;
                                provider.LastHash = "";
                                provider.LastUpdated = "Отключен";
                            }
                            continue;
                        }

                        // Скачиваем данные
                        try
                        {
                            string downloadedData = client.DownloadString(provider.Url);
                            string newHash = ComputeHash(downloadedData);

                            if (provider.LastHash == newHash && hostsContent.Contains(blockStartMarker))
                            {
                                // Изменений нет
                                continue;
                            }

                            // Обновляем блок
                            StringBuilder newBlock = new StringBuilder();
                            newBlock.AppendLine(blockStartMarker);
                            newBlock.AppendLine(string.Format("# Source: {0}", provider.Url));
                            newBlock.AppendLine(string.Format("# Updated: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                            newBlock.AppendLine(downloadedData.Trim());
                            newBlock.AppendLine(blockEndMarker);

                            if (hostsContent.Contains(blockStartMarker))
                            {
                                hostsContent = ReplaceBlock(hostsContent, blockStartMarker, blockEndMarker, newBlock.ToString());
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
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Ошибка скачивания " + provider.Name + ": " + ex.Message);
                        }
                    }
                }

                if (hasAnyChanges)
                {
                    File.WriteAllText(HostsPath, hostsContent, Encoding.UTF8);
                    DnsFlushResolverCache();
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

            // Удаляем перенос строки после маркера
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
        private TabControl tabs;
        private TabPage tabShortcuts;
        private TabPage tabProviders;
        private TabPage tabScheduler;

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

        // Providers tab
        private ListView lvProviders;
        private Button btnAddProvider;
        private Button btnRemoveProvider;
        private Button btnUpdateNow;
        private Label lblProviderStatus;

        // Scheduler tab
        private CheckBox chkScheduler;
        private ComboBox cboInterval;
        private Button btnSaveScheduler;
        private Label lblSchedulerInfo;

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
            this.Size = new Size(640, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F) };

            // Tab 1: Ярлыки
            tabShortcuts = new TabPage("🚀 Создать ярлык");
            InitShortcutsTab();
            tabs.TabPages.Add(tabShortcuts);

            // Tab 2: Подписки
            tabProviders = new TabPage("🌐 Подписки hosts (GeoHide)");
            InitProvidersTab();
            tabs.TabPages.Add(tabProviders);

            // Tab 3: Планировщик
            tabScheduler = new TabPage("⏱️ Автообновление");
            InitSchedulerTab();
            tabs.TabPages.Add(tabScheduler);

            this.Controls.Add(tabs);
        }

        private void InitShortcutsTab()
        {
            GroupBox gbEditor = new GroupBox
            {
                Text = "В каком редакторе открывать по клику",
                Location = new Point(15, 15),
                Size = new Size(595, 175)
            };

            rbOpenWith = new RadioButton { Text = "Стандартное окно Windows «Открыть с помощью...» (выбор редактора)", Location = new Point(20, 25), AutoSize = true, Checked = true };
            rbNpp = new RadioButton { Text = "Notepad++ (при наличии в системе)", Location = new Point(20, 50), AutoSize = true };
            rbCode = new RadioButton { Text = "Visual Studio Code", Location = new Point(20, 75), AutoSize = true };
            rbNotepad = new RadioButton { Text = "Системный Блокнот (всегда с правами администратора)", Location = new Point(20, 100), AutoSize = true };
            rbCustom = new RadioButton { Text = "Другой редактор:", Location = new Point(20, 125), AutoSize = true };

            txtCustomPath = new TextBox { Location = new Point(160, 124), Size = new Size(330, 23), Enabled = false };
            btnBrowseCustom = new Button { Text = "Обзор...", Location = new Point(500, 123), Size = new Size(80, 25), Enabled = false };

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
                Location = new Point(15, 200),
                Size = new Size(595, 140)
            };

            Label lblName = new Label { Text = "Имя ярлыка:", Location = new Point(20, 30), AutoSize = true };
            txtShortcutName = new TextBox { Text = "Hosts", Location = new Point(140, 27), Size = new Size(200, 23) };

            Label lblIcon = new Label { Text = "Иконка:", Location = new Point(20, 65), AutoSize = true };
            cboIcon = new ComboBox { Location = new Point(140, 62), Size = new Size(430, 23), DropDownStyle = ComboBoxStyle.DropDownList };
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
                Location = new Point(15, 355),
                Size = new Size(595, 42),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 245, 255)
            };
            btnCreateShortcut.Click += BtnCreateShortcut_Click;

            lblShortcutHint = new Label
            {
                Text = "💡 Подсказка: После создания просто нажмите правой кнопкой по ярлыку на Рабочем столе и выберите «Закрепить на панели задач».",
                Location = new Point(15, 405),
                Size = new Size(595, 35),
                ForeColor = Color.Gray
            };

            tabShortcuts.Controls.Add(gbEditor);
            tabShortcuts.Controls.Add(gbSettings);
            tabShortcuts.Controls.Add(btnCreateShortcut);
            tabShortcuts.Controls.Add(lblShortcutHint);
        }

        private void InitProvidersTab()
        {
            Label lblTitle = new Label
            {
                Text = "Внешние источники правил hosts (автоматическое объединение с вашим файлом):",
                Location = new Point(15, 12),
                AutoSize = true
            };

            lvProviders = new ListView
            {
                Location = new Point(15, 35),
                Size = new Size(595, 260),
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                GridLines = true
            };
            lvProviders.Columns.Add("Вкл", 50);
            lvProviders.Columns.Add("Имя провайдера", 160);
            lvProviders.Columns.Add("URL источника", 250);
            lvProviders.Columns.Add("Обновлено", 120);

            btnAddProvider = new Button { Text = "➕ Добавить URL...", Location = new Point(15, 305), Size = new Size(130, 30) };
            btnAddProvider.Click += BtnAddProvider_Click;

            btnRemoveProvider = new Button { Text = "🗑️ Удалить", Location = new Point(155, 305), Size = new Size(100, 30) };
            btnRemoveProvider.Click += BtnRemoveProvider_Click;

            btnUpdateNow = new Button
            {
                Text = "🔄 Синхронизировать hosts сейчас",
                Location = new Point(360, 305),
                Size = new Size(250, 30),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnUpdateNow.Click += BtnUpdateNow_Click;

            lblProviderStatus = new Label
            {
                Text = "Статус: Готов к синхронизации. Ваши личные записи в hosts не затрагиваются.",
                Location = new Point(15, 350),
                Size = new Size(595, 40),
                ForeColor = Color.DarkSlateGray
            };

            tabProviders.Controls.Add(lblTitle);
            tabProviders.Controls.Add(lvProviders);
            tabProviders.Controls.Add(btnAddProvider);
            tabProviders.Controls.Add(btnRemoveProvider);
            tabProviders.Controls.Add(btnUpdateNow);
            tabProviders.Controls.Add(lblProviderStatus);
        }

        private void InitSchedulerTab()
        {
            GroupBox gbSched = new GroupBox
            {
                Text = "Фоновое тихое обновление через Планировщик Windows",
                Location = new Point(15, 15),
                Size = new Size(595, 230)
            };

            chkScheduler = new CheckBox
            {
                Text = "Включить тихое автоматическое обновление hosts (без всплывающих окон)",
                Location = new Point(20, 35),
                AutoSize = true
            };

            Label lblFreq = new Label { Text = "Частота обновления:", Location = new Point(20, 75), AutoSize = true };
            cboInterval = new ComboBox
            {
                Location = new Point(170, 72),
                Size = new Size(250, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboInterval.Items.Add("Каждые 6 часов");
            cboInterval.Items.Add("Каждые 12 часов");
            cboInterval.Items.Add("Раз в день (в 09:00)");
            cboInterval.Items.Add("При каждом входе в Windows");
            cboInterval.SelectedIndex = 2;

            btnSaveScheduler = new Button
            {
                Text = "💾 Сохранить настройки автообновления",
                Location = new Point(20, 120),
                Size = new Size(300, 35),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSaveScheduler.Click += BtnSaveScheduler_Click;

            lblSchedulerInfo = new Label
            {
                Text = "ℹ️ При обновлении утилита проверяет хэш файла на сервере. Если изменений нет — hosts файл не перезаписывается. Все изменения вносятся строго в изолированный блок.",
                Location = new Point(20, 170),
                Size = new Size(550, 50),
                ForeColor = Color.Gray
            };

            gbSched.Controls.Add(chkScheduler);
            gbSched.Controls.Add(lblFreq);
            gbSched.Controls.Add(cboInterval);
            gbSched.Controls.Add(btnSaveScheduler);
            gbSched.Controls.Add(lblSchedulerInfo);

            tabScheduler.Controls.Add(gbSched);
        }

        private void LoadConfigToUI()
        {
            // Radiobuttons
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

            // Providers
            RefreshProvidersList();

            // Scheduler
            chkScheduler.Checked = config.SchedulerEnabled;
            if (config.SchedulerInterval == "6h") cboInterval.SelectedIndex = 0;
            else if (config.SchedulerInterval == "12h") cboInterval.SelectedIndex = 1;
            else if (config.SchedulerInterval == "logon") cboInterval.SelectedIndex = 3;
            else cboInterval.SelectedIndex = 2;
        }

        private void RefreshProvidersList()
        {
            lvProviders.Items.Clear();
            foreach (var p in config.Providers)
            {
                ListViewItem item = new ListViewItem("");
                item.Checked = p.Enabled;
                item.SubItems.Add(p.Name);
                item.SubItems.Add(p.Url);
                item.SubItems.Add(string.IsNullOrEmpty(p.LastUpdated) ? "Никогда" : p.LastUpdated);
                item.Tag = p;
                lvProviders.Items.Add(item);
            }
        }

        private void BtnCreateShortcut_Click(object sender, EventArgs e)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string launcherPath = Path.Combine(baseDir, "HostsLauncher.exe");
                if (!File.Exists(launcherPath))
                {
                    // Проверяем родительскую папку dist
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

                // Сохраняем в конфиг
                config.PreferredEditor = rbNpp.Checked ? "npp" : (rbCode.Checked ? "code" : (rbNotepad.Checked ? "notepad" : (rbCustom.Checked ? "custom" : "openwith")));
                config.CustomEditorPath = txtCustomPath.Text;
                config.AlwaysAdmin = chkAdmin.Checked;
                Program.SaveConfig(config);

                MessageBox.Show(
                    "Ярлык \"" + linkName + ".lnk\" успешно создан на вашем Рабочем столе!\n\n" +
                    "Чтобы закрепить его в панели задач:\n" +
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

        private void BtnAddProvider_Click(object sender, EventArgs e)
        {
            string url = PromptDialog("Введите URL списка hosts (например, https://geohide.ru/hosts):", "Добавление провайдера");
            if (string.IsNullOrEmpty(url)) return;

            string name = PromptDialog("Введите краткое имя провайдера:", "Имя провайдера");
            if (string.IsNullOrEmpty(name)) name = "Провайдер " + (config.Providers.Count + 1);

            config.Providers.Add(new ProviderConfig
            {
                Name = name,
                Url = url,
                Enabled = true,
                LastHash = "",
                LastUpdated = ""
            });
            Program.SaveConfig(config);
            RefreshProvidersList();
        }

        private void BtnRemoveProvider_Click(object sender, EventArgs e)
        {
            if (lvProviders.SelectedItems.Count == 0) return;
            var item = lvProviders.SelectedItems[0];
            var provider = item.Tag as ProviderConfig;
            if (provider != null)
            {
                config.Providers.Remove(provider);
                Program.SaveConfig(config);
                RefreshProvidersList();
            }
        }

        private void BtnUpdateNow_Click(object sender, EventArgs e)
        {
            // Обновляем состояние чекбоксов в конфиге
            for (int i = 0; i < lvProviders.Items.Count; i++)
            {
                var p = lvProviders.Items[i].Tag as ProviderConfig;
                if (p != null) p.Enabled = lvProviders.Items[i].Checked;
            }
            Program.SaveConfig(config);

            lblProviderStatus.Text = "Синхронизация... Пожалуйста, подождите.";
            lblProviderStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            bool res = Program.UpdateHostsRoutine(false);
            config = Program.LoadConfig();
            RefreshProvidersList();

            if (res)
            {
                lblProviderStatus.Text = "✅ Hosts успешно синхронизирован! DNS-кэш сброшен (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                lblProviderStatus.ForeColor = Color.DarkGreen;
                MessageBox.Show("Файл hosts успешно обновлен!\nDNS-кэш Windows автоматически очищен.", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblProviderStatus.Text = "⚠️ Ошибка обновления hosts (возможно, отменено UAC или нет интернета).";
                lblProviderStatus.ForeColor = Color.Red;
            }
        }

        private void BtnSaveScheduler_Click(object sender, EventArgs e)
        {
            config.SchedulerEnabled = chkScheduler.Checked;
            int sel = cboInterval.SelectedIndex;
            if (sel == 0) config.SchedulerInterval = "6h";
            else if (sel == 1) config.SchedulerInterval = "12h";
            else if (sel == 3) config.SchedulerInterval = "logon";
            else config.SchedulerInterval = "daily";

            Program.SaveConfig(config);

            try
            {
                string taskName = "HostsManagerAutoUpdate";
                if (!chkScheduler.Checked)
                {
                    Process pDel = Process.Start(new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = "/delete /tn \"" + taskName + "\" /f",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    if (pDel != null) pDel.WaitForExit();

                    MessageBox.Show("Автообновление выключено. Задача в Планировщике Windows удалена.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // Создаем задачу
                    string exePath = Application.ExecutablePath;
                    string scheduleArg = "/sc daily /st 09:00";
                    if (config.SchedulerInterval == "6h") scheduleArg = "/sc minute /mo 360";
                    else if (config.SchedulerInterval == "12h") scheduleArg = "/sc minute /mo 720";
                    else if (config.SchedulerInterval == "logon") scheduleArg = "/sc onlogon";

                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "schtasks.exe";
                    psi.Arguments = string.Format("/create /tn \"{0}\" /tr \"\\\"{1}\\\" /update-silent\" {2} /rl highest /f", taskName, exePath, scheduleArg);
                    psi.Verb = "runas";
                    psi.UseShellExecute = true;
                    Process p = Process.Start(psi);
                    if (p != null) p.WaitForExit();

                    MessageBox.Show("Автообновление включено!\nЗадача успешно зарегистрирована в Планировщике Windows с наивысшими правами.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка настройки планировщика: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
