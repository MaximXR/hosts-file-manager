using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HostsLauncher.Localization;

namespace HostsLauncher.Services
{
    public static class SchedulerService
    {
        public const string TaskName = "HostsManagerAutoUpdate";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static Encoding GetOemEncoding()
        {
            try
            {
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            }
            catch
            {
                return Encoding.Default;
            }
        }

        public static bool CheckTaskStatus(out string nextRun, out string state)
        {
            bool isIdleOnly;
            return CheckTaskStatus(out nextRun, out state, out isIdleOnly);
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
                    Arguments = string.Format("/query /tn \"{0}\" /fo csv /v /nh", TaskName),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = GetOemEncoding()
                };
                Process p = Process.Start(psi);
                string output = p != null ? p.StandardOutput.ReadToEnd() : "";
                if (p != null) p.WaitForExit();

                if (!string.IsNullOrEmpty(output) && !output.StartsWith("ERROR:") && !output.StartsWith("ОШИБКА:"))
                {
                    string[] parts = output.Split(new[] { "\",\"" }, StringSplitOptions.None);
                    if (parts.Length > 2) nextRun = parts[2].Trim('"', ' ');
                    if (parts.Length > 3) state = parts[3].Trim('"', ' ');

                    // Проверяем флаг RunOnlyIfIdle через /xml
                    try
                    {
                        ProcessStartInfo qPsi = new ProcessStartInfo
                        {
                            FileName = "schtasks.exe",
                            Arguments = string.Format("/query /tn \"{0}\" /xml", TaskName),
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            StandardOutputEncoding = GetOemEncoding()
                        };
                        Process qProc = Process.Start(qPsi);
                        string xml = qProc != null ? qProc.StandardOutput.ReadToEnd() : "";
                        if (qProc != null) qProc.WaitForExit();

                        if (!string.IsNullOrEmpty(xml) && xml.Contains("<RunOnlyIfIdle>true</RunOnlyIfIdle>"))
                        {
                            isIdleOnly = true;
                        }
                    }
                    catch { }

                    return true;
                }
            }
            catch { }
            return false;
        }

        public static void GetScheduleArgs(int scheduleIndex, out string scheduleArgs, out string humanSchedule, out bool isScheduleOnIdle)
        {
            isScheduleOnIdle = false;
            switch (scheduleIndex)
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

        public static string ProcessIdleXml(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return xml;
            if (!xml.Contains("<RunOnlyIfIdle>"))
            {
                return xml.Replace("<Settings>", "<Settings>\r\n    <RunOnlyIfIdle>true</RunOnlyIfIdle>");
            }
            else
            {
                return xml.Replace("<RunOnlyIfIdle>false</RunOnlyIfIdle>", "<RunOnlyIfIdle>true</RunOnlyIfIdle>");
            }
        }

        public static bool CreateTask(string exePath, int scheduleIndex, bool onlyIfIdle, out string humanSchedule)
        {
            string scheduleArgs;
            bool isScheduleOnIdle;
            GetScheduleArgs(scheduleIndex, out scheduleArgs, out humanSchedule, out isScheduleOnIdle);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Verb = "runas",
                UseShellExecute = true,
                Arguments = string.Format("/create /tn \"{0}\" /tr \"\\\"{1}\\\" /update-silent\" {2} /rl highest /f", TaskName, exePath, scheduleArgs)
            };
            Process p = Process.Start(psi);
            if (p != null) p.WaitForExit();

            if (onlyIfIdle && !isScheduleOnIdle)
            {
                try
                {
                        ProcessStartInfo qPsi = new ProcessStartInfo
                        {
                            FileName = "schtasks.exe",
                            Arguments = string.Format("/query /tn \"{0}\" /xml", TaskName),
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            StandardOutputEncoding = GetOemEncoding()
                        };
                    Process qProc = Process.Start(qPsi);
                    string xml = qProc != null ? qProc.StandardOutput.ReadToEnd() : "";
                    if (qProc != null) qProc.WaitForExit();

                    if (!string.IsNullOrEmpty(xml))
                    {
                        string modifiedXml = ProcessIdleXml(xml);
                        string tmpXmlPath = Path.Combine(Path.GetTempPath(), "HostsAutoUpdateTask.xml");
                        File.WriteAllText(tmpXmlPath, modifiedXml, Encoding.Unicode);

                        ProcessStartInfo xmlPsi = new ProcessStartInfo
                        {
                            FileName = "schtasks.exe",
                            Arguments = string.Format("/create /tn \"{0}\" /xml \"{1}\" /f", TaskName, tmpXmlPath),
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

            return true;
        }

        public static bool DeleteTask()
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Verb = "runas",
                UseShellExecute = true,
                Arguments = string.Format("/delete /tn \"{0}\" /f", TaskName)
            };
            Process p = Process.Start(psi);
            if (p != null) p.WaitForExit();
            return true;
        }

        public static void OpenTaskScheduler()
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
                                SendKeys.SendWait("{DOWN}");
                                Thread.Sleep(150);
                                SendKeys.SendWait("{TAB}");
                                Thread.Sleep(150);
                                SendKeys.SendWait("H");
                            }
                            catch { }
                            break;
                        }
                    }
                });
            }
        }
    }
}
