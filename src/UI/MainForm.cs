using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using HostsLauncher.Localization;
using HostsLauncher.Models;
using HostsLauncher.Services;

namespace HostsLauncher.UI
{
    public class MainForm : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETICON = 0x80;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                if (this.Icon != null)
                {
                    SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_BIG, this.Icon.Handle);
                    SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_SMALL, this.Icon.Handle);
                }
            }
            catch { }
        }
        // Header and Language
        internal Panel pnlHeader;
        internal Label lblAppInfo;
        internal Label lblLang;
        internal ComboBox cboLanguage;

        internal TabControl tabs;
        internal TabPage tabShortcuts;
        internal TabPage tabProviders;

        // Shortcuts tab
        internal GroupBox gbEditor;
        internal RadioButton rbOpenWith;
        internal RadioButton rbNpp;
        internal RadioButton rbCode;
        internal RadioButton rbNotepad;
        internal RadioButton rbCustom;
        internal TextBox txtCustomPath;
        internal Button btnBrowseCustom;
        internal CheckBox chkAdmin;
        internal Label lblShortcutName;
        internal TextBox txtShortcutName;
        internal Label lblIcon;
        internal ComboBox cboIcon;
        internal Button btnCreateShortcut;
        internal Button btnCreateUpdateShortcut;
        internal Button btnCreateManagerShortcut;
        internal Label lblShortcutHint;
        internal GroupBox gbSettings;
        internal GroupBox gbExtraShortcuts;
        internal Label lblUpdateIcon;
        internal ComboBox cboUpdateIcon;
        internal Label lblExtraHint;

        // Providers tab (GeoHide + Sources + Task Scheduler)
        internal GroupBox gbGeo;
        internal CheckBox chkGeoHide;
        internal Label lblRegionTitle;
        internal RadioButton rbGeoRU;
        internal RadioButton rbGeoEU;
        internal RadioButton rbGeoUS;
        internal LinkLabel lnkGeoSite;
        internal Label lblGeoHideInfo;

        internal GroupBox gbCustom;
        internal Panel pnlCustomProviders;
        internal Button btnAddCustom;
        internal Button btnResetPresets;
        internal Label lblCustomHint;

        internal GroupBox gbScheduler;
        internal Label lblTaskStatus;
        internal Label lblFreq;
        internal ComboBox cboSchedule;
        internal CheckBox chkOnlyIfIdle;
        internal TextBox txtSchedulerCmd;
        internal Button btnCopyCmd;
        internal Button btnToggleTask;
        internal Button btnOpenTaskScheduler;
        internal Label lblPathHint;

        internal Button btnUpdateNow;
        internal Label lblProviderStatus;

        internal ToolTip toolTip;
        internal AppConfig config;

        public MainForm(AppConfig initialConfig = null)
        {
            config = initialConfig ?? ConfigManager.LoadConfig();
            L10n.CurrentLang = string.IsNullOrEmpty(config.Language) ? "ru" : config.Language;

            toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 400,
                ReshowDelay = 200,
                ShowAlways = true
            };

            try
            {
                Stream resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
                if (resStream != null)
                {
                    this.Icon = new Icon(resStream);
                }
                else
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"resources\app.ico");
                    if (File.Exists(iconPath))
                    {
                        byte[] icoBytes = File.ReadAllBytes(iconPath);
                        using (MemoryStream ms = new MemoryStream(icoBytes))
                        {
                            this.Icon = new Icon(ms);
                        }
                    }
                    else
                    {
                        this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                    }
                }
            }
            catch { }

            InitUI();
            ApplyLocalization();
            LoadConfigToUI();
        }

        private void InitUI()
        {
            this.Size = new Size(690, 725);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ShowInTaskbar = true;

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
                    ConfigManager.SaveConfig(config);
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
                Location = new Point(15, 10),
                Size = new Size(645, 160)
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
                Location = new Point(15, 172),
                Size = new Size(645, 170)
            };

            lblShortcutName = new Label { Location = new Point(20, 26), Size = new Size(200, 20) };
            txtShortcutName = new TextBox { Text = "Hosts", Location = new Point(225, 23), Size = new Size(395, 23) };

            lblIcon = new Label { Location = new Point(20, 58), Size = new Size(200, 20) };
            cboIcon = new ComboBox { Location = new Point(225, 55), Size = new Size(395, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cboIcon.Items.Add(L10n.T("IconSystem"));
            cboIcon.Items.Add(L10n.T("IconNotepad"));
            cboIcon.Items.Add(L10n.T("IconNpp"));
            cboIcon.Items.Add(L10n.T("IconShield"));
            cboIcon.SelectedIndex = 0;

            chkAdmin = new CheckBox { Location = new Point(20, 88), AutoSize = true };

            btnCreateShortcut = new Button
            {
                Location = new Point(20, 118),
                Size = new Size(605, 38),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 245, 255)
            };
            btnCreateShortcut.Click += BtnCreateShortcut_Click;

            gbSettings.Controls.Add(lblShortcutName);
            gbSettings.Controls.Add(txtShortcutName);
            gbSettings.Controls.Add(lblIcon);
            gbSettings.Controls.Add(cboIcon);
            gbSettings.Controls.Add(chkAdmin);
            gbSettings.Controls.Add(btnCreateShortcut);

            gbExtraShortcuts = new GroupBox
            {
                Location = new Point(15, 350),
                Size = new Size(645, 192)
            };

            lblUpdateIcon = new Label { Location = new Point(20, 26), Size = new Size(200, 20) };
            cboUpdateIcon = new ComboBox { Location = new Point(225, 23), Size = new Size(395, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            cboUpdateIcon.Items.Add(L10n.T("UpdateIconSyncShield"));
            cboUpdateIcon.Items.Add(L10n.T("UpdateIconSystem"));
            cboUpdateIcon.Items.Add(L10n.T("UpdateIconApp"));
            cboUpdateIcon.SelectedIndex = 0;

            btnCreateUpdateShortcut = new Button
            {
                Location = new Point(20, 56),
                Size = new Size(605, 38),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 255, 240)
            };
            btnCreateUpdateShortcut.Click += BtnCreateUpdateShortcut_Click;

            btnCreateManagerShortcut = new Button
            {
                Location = new Point(20, 100),
                Size = new Size(605, 38),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(245, 245, 250)
            };
            btnCreateManagerShortcut.Click += BtnCreateManagerShortcut_Click;

            lblExtraHint = new Label
            {
                Location = new Point(20, 146),
                Size = new Size(605, 36),
                ForeColor = Color.DarkSlateGray
            };

            gbExtraShortcuts.Controls.Add(lblUpdateIcon);
            gbExtraShortcuts.Controls.Add(cboUpdateIcon);
            gbExtraShortcuts.Controls.Add(btnCreateUpdateShortcut);
            gbExtraShortcuts.Controls.Add(btnCreateManagerShortcut);
            gbExtraShortcuts.Controls.Add(lblExtraHint);

            lblShortcutHint = new Label
            {
                Location = new Point(15, 550),
                Size = new Size(645, 35),
                ForeColor = Color.DimGray
            };

            tabShortcuts.Controls.Add(gbEditor);
            tabShortcuts.Controls.Add(gbSettings);
            tabShortcuts.Controls.Add(gbExtraShortcuts);
            tabShortcuts.Controls.Add(lblShortcutHint);
        }

        private void InitProvidersTab()
        {
            // Блок 1: GeoHide (Компактный)
            gbGeo = new GroupBox
            {
                Location = new Point(15, 8),
                Size = new Size(645, 158)
            };

            chkGeoHide = new CheckBox
            {
                Location = new Point(15, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            lnkGeoSite = new LinkLabel
            {
                Location = new Point(415, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F)
            };
            lnkGeoSite.LinkClicked += (s, e) =>
            {
                try { Process.Start("https://geohide.ru/"); }
                catch (Exception ex) { MessageBox.Show(L10n.T("ErrorOpenBrowser", ex.Message)); }
            };

            lblRegionTitle = new Label
            {
                Location = new Point(15, 46),
                AutoSize = true,
                ForeColor = Color.FromArgb(70, 80, 95)
            };

            rbGeoRU = new RadioButton { Location = new Point(30, 68), AutoSize = true, Checked = true };
            rbGeoEU = new RadioButton { Location = new Point(30, 90), AutoSize = true };
            rbGeoUS = new RadioButton { Location = new Point(30, 112), AutoSize = true };

            lblGeoHideInfo = new Label
            {
                Location = new Point(30, 134),
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
            gbGeo.Controls.Add(lnkGeoSite);
            gbGeo.Controls.Add(lblRegionTitle);
            gbGeo.Controls.Add(rbGeoRU);
            gbGeo.Controls.Add(rbGeoEU);
            gbGeo.Controls.Add(rbGeoUS);
            gbGeo.Controls.Add(lblGeoHideInfo);

            // Блок 2: Дополнительные источники (Просторный и компактный список)
            gbCustom = new GroupBox
            {
                Location = new Point(15, 172),
                Size = new Size(645, 255)
            };

            pnlCustomProviders = new Panel
            {
                Location = new Point(12, 22),
                Size = new Size(621, 188),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            btnAddCustom = new Button { Location = new Point(12, 218), Size = new Size(175, 27) };
            btnAddCustom.Click += BtnAddCustom_Click;

            btnResetPresets = new Button { Location = new Point(195, 218), Size = new Size(165, 27) };
            btnResetPresets.Click += (s, e) =>
            {
                if (MessageBox.Show(L10n.T("ConfirmResetPresets"), L10n.T("Confirmation"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    config.CustomProviders = ConfigManager.GetDefaultPresets();
                    ConfigManager.SaveConfig(config);
                    RefreshCustomProvidersList();
                }
            };

            lblCustomHint = new Label
            {
                Location = new Point(370, 216),
                Size = new Size(260, 30),
                ForeColor = Color.DarkSlateBlue
            };

            gbCustom.Controls.Add(pnlCustomProviders);
            gbCustom.Controls.Add(btnAddCustom);
            gbCustom.Controls.Add(btnResetPresets);
            gbCustom.Controls.Add(lblCustomHint);

            // Блок 3: Планировщик Windows
            gbScheduler = new GroupBox
            {
                Location = new Point(15, 434),
                Size = new Size(645, 165)
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
                Location = new Point(95, 43),
                Size = new Size(225, 23),
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
                Location = new Point(328, 43),
                Size = new Size(190, 23),
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
                Location = new Point(15, 72),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Checked = true
            };

            btnToggleTask = new Button
            {
                Location = new Point(15, 96),
                Size = new Size(305, 34),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnToggleTask.Click += BtnToggleTask_Click;

            btnOpenTaskScheduler = new Button
            {
                Location = new Point(328, 96),
                Size = new Size(302, 34),
                Font = new Font("Segoe UI", 9F)
            };
            btnOpenTaskScheduler.Click += (s, e) => SchedulerService.OpenTaskScheduler();

            lblPathHint = new Label
            {
                Location = new Point(15, 134),
                Size = new Size(615, 24),
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
                Location = new Point(15, 606),
                Size = new Size(645, 40),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 255, 240)
            };
            btnUpdateNow.Click += BtnUpdateNow_Click;

            lblProviderStatus = new Label
            {
                Location = new Point(15, 650),
                Size = new Size(645, 20),
                ForeColor = Color.Gray
            };

            tabProviders.Controls.Add(gbGeo);
            tabProviders.Controls.Add(gbCustom);
            tabProviders.Controls.Add(gbScheduler);
            tabProviders.Controls.Add(btnUpdateNow);
            tabProviders.Controls.Add(lblProviderStatus);
        }

        public void ApplyLocalization()
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
                cboIcon.Items.Add(L10n.T("IconShield"));
                cboIcon.SelectedIndex = (iconIdx >= 0 && iconIdx < cboIcon.Items.Count) ? iconIdx : 0;
            }

            if (btnCreateShortcut != null) btnCreateShortcut.Text = L10n.T("BtnCreateHostsShortcut");
            if (gbExtraShortcuts != null) gbExtraShortcuts.Text = L10n.T("GbExtraShortcutsTitle");
            if (lblUpdateIcon != null) lblUpdateIcon.Text = L10n.T("LblUpdateIcon");

            if (cboUpdateIcon != null)
            {
                int updateIdx = cboUpdateIcon.SelectedIndex;
                cboUpdateIcon.Items.Clear();
                cboUpdateIcon.Items.Add(L10n.T("UpdateIconSyncShield"));
                cboUpdateIcon.Items.Add(L10n.T("UpdateIconSystem"));
                cboUpdateIcon.Items.Add(L10n.T("UpdateIconApp"));
                cboUpdateIcon.SelectedIndex = (updateIdx >= 0 && updateIdx < cboUpdateIcon.Items.Count) ? updateIdx : 0;
            }

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

        public void LoadConfigToUI()
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

        public void RefreshTaskStatus()
        {
            string nextRun, state;
            bool isIdleOnly;
            bool exists = SchedulerService.CheckTaskStatus(out nextRun, out state, out isIdleOnly);

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

        public void RefreshCustomProvidersList()
        {
            pnlCustomProviders.SuspendLayout();
            pnlCustomProviders.Controls.Clear();
            if (config.CustomProviders == null) config.CustomProviders = ConfigManager.GetDefaultPresets();

            int cardWidth = 595;
            int cardHeight = 32;
            int yOffset = 4;
            int index = 0;

            foreach (var p in config.CustomProviders)
            {
                Color bg = (index % 2 == 0) ? Color.White : Color.FromArgb(248, 250, 252);
                Panel card = new Panel
                {
                    Location = new Point(4, yOffset),
                    Size = new Size(cardWidth, cardHeight),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = bg
                };

                CheckBox chk = new CheckBox
                {
                    Text = p.Name,
                    Checked = p.Enabled,
                    Font = new Font("Segoe UI", 9F, p.Enabled ? FontStyle.Bold : FontStyle.Regular),
                    Location = new Point(8, 5),
                    Size = new Size(335, 20),
                    AutoEllipsis = true
                };
                chk.CheckedChanged += (s, e) =>
                {
                    p.Enabled = chk.Checked;
                    chk.Font = new Font("Segoe UI", 9F, chk.Checked ? FontStyle.Bold : FontStyle.Regular);
                    ConfigManager.SaveConfig(config);
                };

                LinkLabel lnkSite = new LinkLabel
                {
                    Text = L10n.T("CardSiteBadge"),
                    Location = new Point(348, 7),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5F),
                    LinkColor = Color.FromArgb(0, 102, 204),
                    ActiveLinkColor = Color.FromArgb(0, 70, 150)
                };
                string siteUrl = !string.IsNullOrEmpty(p.SiteUrl) ? p.SiteUrl : p.Url;
                if (toolTip != null) toolTip.SetToolTip(lnkSite, siteUrl);
                lnkSite.LinkClicked += (s, e) =>
                {
                    try { Process.Start(siteUrl); }
                    catch (Exception ex) { MessageBox.Show(L10n.T("ErrorOpenBrowser", ex.Message)); }
                };

                LinkLabel lnkRaw = new LinkLabel
                {
                    Text = L10n.T("CardRawBadge"),
                    Location = new Point(410, 7),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5F),
                    LinkColor = Color.FromArgb(70, 80, 95),
                    ActiveLinkColor = Color.FromArgb(40, 50, 65)
                };
                if (toolTip != null) toolTip.SetToolTip(lnkRaw, p.Url);
                lnkRaw.LinkClicked += (s, e) =>
                {
                    try { Process.Start(p.Url); }
                    catch (Exception ex) { MessageBox.Show(L10n.T("ErrorOpenRaw", ex.Message)); }
                };

                string shortDate = string.IsNullOrEmpty(p.LastUpdated) ? L10n.T("StatusNever") : (p.LastUpdated == "Отключен" ? L10n.T("StatusDisabled") : p.LastUpdated);
                if (shortDate.Length > 16) shortDate = shortDate.Substring(0, 16);
                Label lblUpd = new Label
                {
                    Text = shortDate,
                    Location = new Point(475, 7),
                    Size = new Size(88, 18),
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = Color.Gray,
                    Font = new Font("Segoe UI", 8F)
                };
                if (toolTip != null) toolTip.SetToolTip(lblUpd, L10n.T("CardUpdatedPrefix") + (string.IsNullOrEmpty(p.LastUpdated) ? L10n.T("StatusNever") : p.LastUpdated));

                Button btnDel = new Button
                {
                    Text = "✕",
                    Size = new Size(22, 22),
                    Location = new Point(cardWidth - 26, 4),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.FromArgb(180, 100, 100),
                    Cursor = Cursors.Hand
                };
                btnDel.FlatAppearance.BorderSize = 0;
                btnDel.MouseEnter += (s, e) => { btnDel.BackColor = Color.FromArgb(254, 226, 226); };
                btnDel.MouseLeave += (s, e) => { btnDel.BackColor = Color.Transparent; };
                btnDel.Click += (s, e) =>
                {
                    if (MessageBox.Show(L10n.T("ConfirmDeleteSource", p.Name), L10n.T("Confirmation"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        config.CustomProviders.Remove(p);
                        ConfigManager.SaveConfig(config);
                        RefreshCustomProvidersList();
                    }
                };

                card.Controls.Add(chk);
                card.Controls.Add(lnkSite);
                card.Controls.Add(lnkRaw);
                card.Controls.Add(lblUpd);
                card.Controls.Add(btnDel);

                pnlCustomProviders.Controls.Add(card);
                yOffset += cardHeight + 3;
                index++;
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
            else if (cboIcon.SelectedIndex == 3)
            {
                iconLoc = Application.ExecutablePath + ",0";
            }

            string linkName = string.IsNullOrEmpty(txtShortcutName.Text) ? L10n.T("ShortcutDefaultName") : txtShortcutName.Text.Trim();

            config.PreferredEditor = rbNpp.Checked ? "npp" : (rbCode.Checked ? "code" : (rbNotepad.Checked ? "notepad" : (rbCustom.Checked ? "custom" : "openwith")));
            config.CustomEditorPath = txtCustomPath.Text;
            config.AlwaysAdmin = chkAdmin.Checked;
            ConfigManager.SaveConfig(config);

            CreateDesktopShortcut(launcherPath, args, iconLoc, linkName, L10n.T("ShortcutHostsDesc"));
        }

        private void BtnCreateUpdateShortcut_Click(object sender, EventArgs e)
        {
            string exePath = Application.ExecutablePath;
            string iconLoc = @"C:\Windows\System32\shell32.dll,238";

            int sel = (cboUpdateIcon != null) ? cboUpdateIcon.SelectedIndex : 0;
            if (sel == 0)
            {
                // Изумрудный щит со стрелками обновления (новинка)
                string syncIcoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"resources\sync.ico");
                if (!File.Exists(syncIcoPath))
                {
                    EnsureResourceExtracted("sync.ico", syncIcoPath);
                }

                if (File.Exists(syncIcoPath))
                {
                    iconLoc = syncIcoPath + ",0";
                }
                else
                {
                    iconLoc = exePath + ",0";
                }
            }
            else if (sel == 1)
            {
                iconLoc = @"C:\Windows\System32\shell32.dll,238";
            }
            else if (sel == 2)
            {
                iconLoc = exePath + ",0";
            }

            CreateDesktopShortcut(exePath, "/update-now", iconLoc, L10n.T("ShortcutUpdateName"), L10n.T("ShortcutUpdateDesc"));
        }

        private static void EnsureResourceExtracted(string resName, string targetPath)
        {
            try
            {
                if (!File.Exists(targetPath))
                {
                    string dir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName))
                    {
                        if (stream != null)
                        {
                            using (FileStream fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                            {
                                byte[] buf = new byte[8192];
                                int read;
                                while ((read = stream.Read(buf, 0, buf.Length)) > 0)
                                {
                                    fs.Write(buf, 0, read);
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void BtnCreateManagerShortcut_Click(object sender, EventArgs e)
        {
            string exePath = Application.ExecutablePath;
            string iconLoc = exePath + ",0";
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
            ConfigManager.SaveConfig(config);
            RefreshCustomProvidersList();
        }

        private void BtnUpdateNow_Click(object sender, EventArgs e)
        {
            config.GeoHideEnabled = chkGeoHide.Checked;
            config.GeoHideRegion = rbGeoEU.Checked ? "eu" : (rbGeoUS.Checked ? "us" : "ru");
            ConfigManager.SaveConfig(config);

            btnUpdateNow.Enabled = false;
            lblProviderStatus.Text = L10n.T("SyncInProgress");
            lblProviderStatus.ForeColor = Color.Blue;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                bool res = HostsService.UpdateHostsRoutine(config, false);

                this.BeginInvoke((Action)(() =>
                {
                    config = ConfigManager.LoadConfig();
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
            bool exists = SchedulerService.CheckTaskStatus(out nextRun, out state);

            try
            {
                if (exists)
                {
                    SchedulerService.DeleteTask();
                    RefreshTaskStatus();
                    MessageBox.Show(L10n.T("TaskDeleted"), L10n.T("Done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    string exePath = Application.ExecutablePath;
                    int schedIdx = cboSchedule != null ? cboSchedule.SelectedIndex : 0;
                    bool onlyIfIdle = chkOnlyIfIdle != null && chkOnlyIfIdle.Checked;
                    string humanSchedule;

                    SchedulerService.CreateTask(exePath, schedIdx, onlyIfIdle, out humanSchedule);

                    if (chkOnlyIfIdle != null) config.TaskOnlyIfIdle = chkOnlyIfIdle.Checked;
                    if (cboSchedule != null) config.TaskScheduleIndex = cboSchedule.SelectedIndex;
                    ConfigManager.SaveConfig(config);

                    RefreshTaskStatus();

                    string idleNotice = onlyIfIdle ? L10n.T("TaskCreatedIdleNotice") : "";
                    MessageBox.Show(L10n.T("TaskCreatedDetails", humanSchedule, idleNotice), L10n.T("Done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(L10n.T("ErrorPrefix") + ex.Message, L10n.T("Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
