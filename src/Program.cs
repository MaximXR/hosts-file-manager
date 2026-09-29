using System;
using System.Diagnostics;
using System.IO;

namespace HostsLauncher
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

                if (args.Length == 0)
                {
                    // По умолчанию: системное окно Windows «Открыть с помощью...»
                    OpenWithDialog(hostsPath);
                    return;
                }

                bool runAsAdmin = false;
                string editorTarget = null;

                foreach (string rawArg in args)
                {
                    string arg = rawArg.Trim('"', ' ');
                    if (arg.Equals("/admin", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("-admin", StringComparison.OrdinalIgnoreCase))
                    {
                        runAsAdmin = true;
                    }
                    else if (editorTarget == null)
                    {
                        editorTarget = arg;
                    }
                }

                if (string.IsNullOrEmpty(editorTarget))
                {
                    OpenWithDialog(hostsPath);
                    return;
                }

                // 1. Проверяем популярные алиасы
                string exePath = null;
                string customArgs = hostsPath;

                if (editorTarget.Equals("/npp", StringComparison.OrdinalIgnoreCase) ||
                    editorTarget.Equals("npp", StringComparison.OrdinalIgnoreCase) ||
                    editorTarget.Equals("notepad++", StringComparison.OrdinalIgnoreCase))
                {
                    string npp64 = @"C:\Program Files\Notepad++\notepad++.exe";
                    string npp32 = @"C:\Program Files (x86)\Notepad++\notepad++.exe";
                    if (File.Exists(npp64)) exePath = npp64;
                    else if (File.Exists(npp32)) exePath = npp32;
                    else exePath = "notepad++.exe";
                }
                else if (editorTarget.Equals("/notepad", StringComparison.OrdinalIgnoreCase) ||
                         editorTarget.Equals("notepad", StringComparison.OrdinalIgnoreCase) ||
                         editorTarget.Equals("блокнот", StringComparison.OrdinalIgnoreCase))
                {
                    exePath = "notepad.exe";
                    // Стандартный Блокнот запускаем от админа для возможности сохранения hosts
                    runAsAdmin = true;
                }
                else if (editorTarget.Equals("/code", StringComparison.OrdinalIgnoreCase) ||
                         editorTarget.Equals("code", StringComparison.OrdinalIgnoreCase) ||
                         editorTarget.Equals("/vscode", StringComparison.OrdinalIgnoreCase) ||
                         editorTarget.Equals("vscode", StringComparison.OrdinalIgnoreCase))
                {
                    string userCode = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Microsoft VS Code\Code.exe");
                    string progCode = @"C:\Program Files\Microsoft VS Code\Code.exe";
                    if (File.Exists(userCode)) exePath = userCode;
                    else if (File.Exists(progCode)) exePath = progCode;
                    else exePath = "code";
                }
                else if (editorTarget.Equals("/subl", StringComparison.OrdinalIgnoreCase) ||
                         editorTarget.Equals("subl", StringComparison.OrdinalIgnoreCase) ||
                         editorTarget.Equals("sublime", StringComparison.OrdinalIgnoreCase))
                {
                    string subl = @"C:\Program Files\Sublime Text\sublime_text.exe";
                    if (File.Exists(subl)) exePath = subl;
                    else exePath = "subl.exe";
                }
                else
                {
                    // Произвольный путь или имя программы
                    exePath = editorTarget;
                }

                // Запуск процесса
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = exePath;
                psi.Arguments = "\"" + customArgs + "\"";
                psi.UseShellExecute = true;

                if (runAsAdmin)
                {
                    psi.Verb = "runas";
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        static void OpenWithDialog(string filePath)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "rundll32.exe";
            psi.Arguments = "shell32.dll,OpenAs_RunDLL " + filePath;
            psi.UseShellExecute = true;
            Process.Start(psi);
        }
    }
}
