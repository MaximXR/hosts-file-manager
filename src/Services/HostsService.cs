using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using HostsLauncher.Models;

namespace HostsLauncher.Services
{
    public class FastWebClient : WebClient
    {
        private int timeoutMs;

        public FastWebClient(int timeoutMs = 10000)
        {
            this.timeoutMs = timeoutMs;
            this.Proxy = null;
            this.Encoding = Encoding.UTF8;
            this.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) HostsManager/1.2";
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

    public static class HostsService
    {
        [DllImport("dnsapi.dll", EntryPoint = "DnsFlushResolverCache", SetLastError = true)]
        private static extern int DnsFlushResolverCache();

        public static bool IsAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static string GetHostsPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        }

        public static void FlushDnsSafe()
        {
            try
            {
                DnsFlushResolverCache();
            }
            catch { }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ipconfig.exe",
                    Arguments = "/flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process p = Process.Start(psi);
                if (p != null) p.WaitForExit(3000);
            }
            catch { }
        }

        public static string ComputeHash(string text)
        {
            if (text == null) text = "";
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static string RemoveBlock(string content, string startMarker, string endMarker)
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

        public static string ReplaceBlock(string content, string startMarker, string endMarker, string newBlock)
        {
            int start = content.IndexOf(startMarker);
            if (start < 0) return content;
            int end = content.IndexOf(endMarker, start);
            if (end < 0) return content;
            end += endMarker.Length;

            return content.Substring(0, start) + newBlock + content.Substring(end);
        }

        public static string ApplyBlock(string hostsContent, string blockStart, string blockEnd, string sourceUrl, string downloadedData)
        {
            StringBuilder newBlock = new StringBuilder();
            newBlock.AppendLine(blockStart);
            newBlock.AppendLine(string.Format("# Source: {0}", sourceUrl));
            newBlock.AppendLine(string.Format("# Updated: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            newBlock.AppendLine(downloadedData.Trim());
            newBlock.AppendLine(blockEnd);

            if (hostsContent.Contains(blockStart))
            {
                return ReplaceBlock(hostsContent, blockStart, blockEnd, newBlock.ToString());
            }
            else
            {
                if (hostsContent.Length > 0 && !hostsContent.EndsWith(Environment.NewLine))
                {
                    hostsContent += Environment.NewLine;
                }
                return hostsContent + Environment.NewLine + newBlock.ToString();
            }
        }

        public static bool UpdateHostsRoutine(AppConfig config, bool isSilent)
        {
            if (!IsAdmin())
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        Arguments = isSilent ? "/update-silent" : "/apply-hosts-elevated",
                        Verb = "runas",
                        UseShellExecute = true
                    };
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
                string hostsPath = GetHostsPath();
                if (!File.Exists(hostsPath)) return false;

                string hostsContent = File.ReadAllText(hostsPath, Encoding.UTF8);
                string backupPath = hostsPath + ".bak";
                try
                {
                    File.Copy(hostsPath, backupPath, true);
                }
                catch { }

                bool hasAnyChanges = false;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                using (FastWebClient client = new FastWebClient(10000))
                {
                    // 1. GeoHide Provider
                    string geoStart = "# === BEGIN HOSTS-MANAGER MANAGED BLOCK [GeoHide] ===";
                    string geoEnd = "# === END HOSTS-MANAGER MANAGED BLOCK [GeoHide] ===";

                    if (!config.GeoHideEnabled)
                    {
                        if (hostsContent.Contains(geoStart))
                        {
                            hostsContent = RemoveBlock(hostsContent, geoStart, geoEnd);
                            hasAnyChanges = true;
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

                            if (config.GeoHideLastHash != newHash || !hostsContent.Contains(geoStart))
                            {
                                hostsContent = ApplyBlock(hostsContent, geoStart, geoEnd, geoUrl, downloadedData);
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

                    // 2. Custom Providers
                    if (config.CustomProviders != null)
                    {
                        for (int i = 0; i < config.CustomProviders.Count; i++)
                        {
                            var provider = config.CustomProviders[i];
                            string blockStart = string.Format("# === BEGIN HOSTS-MANAGER MANAGED BLOCK [{0}] ===", provider.Name);
                            string blockEnd = string.Format("# === END HOSTS-MANAGER MANAGED BLOCK [{0}] ===", provider.Name);

                            if (!provider.Enabled)
                            {
                                if (hostsContent.Contains(blockStart))
                                {
                                    hostsContent = RemoveBlock(hostsContent, blockStart, blockEnd);
                                    hasAnyChanges = true;
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
                                    hostsContent = ApplyBlock(hostsContent, blockStart, blockEnd, provider.Url, downloadedData);
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
                    File.WriteAllText(hostsPath, hostsContent, Encoding.UTF8);
                    FlushDnsSafe();
                }

                ConfigManager.SaveConfig(config);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Ошибка записи hosts: " + ex.Message);
                return false;
            }
        }
    }
}
