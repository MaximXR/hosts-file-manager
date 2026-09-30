using System;
using System.Net;
using System.Windows.Forms;
using HostsLauncher.Localization;
using HostsLauncher.Models;
using HostsLauncher.Services;
using HostsLauncher.UI;

namespace HostsLauncher
{
    static class HostsManagerProgram
    {
        [STAThread]
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            AppConfig config = ConfigManager.LoadConfig();
            L10n.CurrentLang = string.IsNullOrEmpty(config.Language) ? "ru" : config.Language;

            if (args.Length > 0)
            {
                string cmd = args[0].ToLowerInvariant();
                if (cmd == "/update-silent" || cmd == "/update-now" || cmd == "/apply-hosts-elevated")
                {
                    bool isSilent = (cmd == "/update-silent" || cmd == "/apply-hosts-elevated");
                    bool success = HostsService.UpdateHostsRoutine(config, isSilent);
                    if (cmd == "/update-now")
                    {
                        if (success)
                        {
                            MessageBox.Show(
                                L10n.T("SyncSuccessBox"),
                                L10n.T("AppTitle"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                L10n.T("SyncError"),
                                L10n.T("Warning"),
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                    }
                    Environment.Exit(success ? 0 : 1);
                    return;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(config));
        }
    }
}
