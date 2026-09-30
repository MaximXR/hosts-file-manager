using System;
using System.Collections.Generic;
using System.Windows.Forms;
using HostsLauncher.Localization;
using HostsLauncher.Models;
using HostsLauncher.Services;
using HostsLauncher.UI;

namespace HostsLauncher.Tests
{
    class TestSuite
    {
        private static int passedCount = 0;
        private static int failedCount = 0;

        [STAThread]
        static int Main(string[] args)
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("    Hosts Launcher & Manager — Test Suite        ");
            Console.WriteLine("=================================================");
            Console.WriteLine();

            RunTest("L10n: Dictionary Parity & Completeness", Test_L10n_ParityAndCompleteness);
            RunTest("L10n: Format Arguments Substitution", Test_L10n_Formatting);
            RunTest("HostsService: SHA256 Hash Computation", Test_HostsService_Hash);
            RunTest("HostsService: Block Isolation & Custom Lines Preservation", Test_HostsService_BlockIsolation);
            RunTest("HostsService: Block Replacement & Clean Removal", Test_HostsService_BlockReplacementAndRemoval);
            RunTest("SchedulerService: XML RunOnlyIfIdle Injection", Test_Scheduler_ProcessIdleXml);
            RunTest("ConfigManager: JSON Serialization & Defaults", Test_ConfigManager_Serialization);
            RunTest("UI: Form Controls Non-Empty on Startup (Russian)", Test_UI_ControlsNonEmpty_Russian);
            RunTest("UI: Form Controls Non-Empty & Localized on English Switch", Test_UI_ControlsNonEmpty_English);
            RunTest("UI: Recursive Deep Check of All Visual Controls", Test_UI_RecursiveControlsCheck);
            RunTest("UI: Elastic Custom Providers Section Dynamic Resizing", Test_UI_ElasticCustomSectionResizing);

            Console.WriteLine();
            Console.WriteLine("=================================================");
            if (failedCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(string.Format(" [SUCCESS] ALL {0} TESTS PASSED CLEANLY!", passedCount));
                Console.ResetColor();
                Console.WriteLine("=================================================");
                return 0;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(string.Format(" [FAILURE] {0} PASSED, {1} FAILED!", passedCount, failedCount));
                Console.ResetColor();
                Console.WriteLine("=================================================");
                return 1;
            }
        }

        private static void RunTest(string testName, Action testAction)
        {
            try
            {
                testAction();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  [PASS] ");
                Console.ResetColor();
                Console.WriteLine(testName);
                passedCount++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  [FAIL] ");
                Console.ResetColor();
                Console.WriteLine(testName);
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("         Reason: " + ex.Message);
                Console.ResetColor();
                failedCount++;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new Exception(string.Format("{0} (Expected: '{1}', Actual: '{2}')", message, expected, actual));
            }
        }

        // 1. L10n Tests
        private static void Test_L10n_ParityAndCompleteness()
        {
            var ru = L10n.GetDictionary("ru");
            var en = L10n.GetDictionary("en");

            Assert(ru.Count > 0, "Russian dictionary must not be empty");
            Assert(en.Count > 0, "English dictionary must not be empty");
            AssertEqual(ru.Count, en.Count, "Russian and English dictionaries must have exact same number of keys");

            foreach (var kvp in ru)
            {
                Assert(!string.IsNullOrWhiteSpace(kvp.Value), string.Format("Ru key '{0}' has empty translation", kvp.Key));
                Assert(en.ContainsKey(kvp.Key), string.Format("Key '{0}' exists in Ru but missing in En dictionary", kvp.Key));
                Assert(!string.IsNullOrWhiteSpace(en[kvp.Key]), string.Format("En key '{0}' has empty translation", kvp.Key));
            }
        }

        private static void Test_L10n_Formatting()
        {
            L10n.CurrentLang = "ru";
            string formattedRu = L10n.T("SourceDefaultName", 5);
            AssertEqual("Источник 5", formattedRu, "L10n format in Ru failed");

            L10n.CurrentLang = "en";
            string formattedEn = L10n.T("SourceDefaultName", 5);
            AssertEqual("Source 5", formattedEn, "L10n format in En failed");
        }

        // 2. Hosts Service Tests
        private static void Test_HostsService_Hash()
        {
            string hash1 = HostsService.ComputeHash("Hello World");
            string hash2 = HostsService.ComputeHash("Hello World");
            string hash3 = HostsService.ComputeHash("Different Content");

            AssertEqual(hash1, hash2, "Identical content must produce identical hash");
            Assert(hash1 != hash3, "Different content must produce different hash");
            AssertEqual(64, hash1.Length, "SHA-256 hash must be 64 hex characters");
        }

        private static void Test_HostsService_BlockIsolation()
        {
            string originalHosts =
                "# My Custom Header\r\n" +
                "127.0.0.1   localhost\r\n" +
                "::1         localhost\r\n" +
                "192.168.1.50 my-nas.local\r\n";

            string blockStart = "# === BEGIN HOSTS-MANAGER MANAGED BLOCK [Test] ===";
            string blockEnd = "# === END HOSTS-MANAGER MANAGED BLOCK [Test] ===";
            string ruleData = "1.2.3.4 adserver.com\r\n5.6.7.8 tracker.com";

            string updated = HostsService.ApplyBlock(originalHosts, blockStart, blockEnd, "https://test.org/rules.txt", ruleData);

            Assert(updated.Contains("127.0.0.1   localhost"), "User manual localhost entry was lost!");
            Assert(updated.Contains("192.168.1.50 my-nas.local"), "User manual NAS entry was lost!");
            Assert(updated.Contains(blockStart), "Managed block start marker not found");
            Assert(updated.Contains(blockEnd), "Managed block end marker not found");
            Assert(updated.Contains("1.2.3.4 adserver.com"), "Downloaded rule entry missing");
        }

        private static void Test_HostsService_BlockReplacementAndRemoval()
        {
            string baseHosts =
                "127.0.0.1 dev.test\r\n" +
                "# === BEGIN HOSTS-MANAGER MANAGED BLOCK [Test] ===\r\n" +
                "1.1.1.1 old.rule\r\n" +
                "# === END HOSTS-MANAGER MANAGED BLOCK [Test] ===\r\n" +
                "127.0.0.1 my-app.local\r\n";

            string blockStart = "# === BEGIN HOSTS-MANAGER MANAGED BLOCK [Test] ===";
            string blockEnd = "# === END HOSTS-MANAGER MANAGED BLOCK [Test] ===";

            // Test replacement
            string replaced = HostsService.ApplyBlock(baseHosts, blockStart, blockEnd, "https://new.org", "2.2.2.2 new.rule");
            Assert(!replaced.Contains("1.1.1.1 old.rule"), "Old rule data was not replaced!");
            Assert(replaced.Contains("2.2.2.2 new.rule"), "New rule data missing!");
            Assert(replaced.Contains("127.0.0.1 dev.test"), "Custom dev.test entry destroyed during replacement!");
            Assert(replaced.Contains("127.0.0.1 my-app.local"), "Custom my-app entry destroyed during replacement!");

            // Test clean removal
            string removed = HostsService.RemoveBlock(replaced, blockStart, blockEnd);
            Assert(!removed.Contains(blockStart), "Block start marker remains after removal");
            Assert(!removed.Contains(blockEnd), "Block end marker remains after removal");
            Assert(!removed.Contains("2.2.2.2 new.rule"), "Managed content remains after removal");
            Assert(removed.Contains("127.0.0.1 dev.test"), "Custom entry lost during removal");
            Assert(removed.Contains("127.0.0.1 my-app.local"), "Custom entry lost during removal");
        }

        // 3. Scheduler Tests
        private static void Test_Scheduler_ProcessIdleXml()
        {
            string xmlWithoutIdle =
                "<?xml version=\"1.0\"?>\r\n" +
                "<Task xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
                "  <Settings>\r\n" +
                "    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n" +
                "  </Settings>\r\n" +
                "</Task>";

            string processed = SchedulerService.ProcessIdleXml(xmlWithoutIdle);
            Assert(processed.Contains("<RunOnlyIfIdle>true</RunOnlyIfIdle>"), "Failed to inject RunOnlyIfIdle tag into task settings XML");

            string xmlWithFalseIdle = "<Settings>\r\n  <RunOnlyIfIdle>false</RunOnlyIfIdle>\r\n</Settings>";
            string fixedIdle = SchedulerService.ProcessIdleXml(xmlWithFalseIdle);
            Assert(fixedIdle.Contains("<RunOnlyIfIdle>true</RunOnlyIfIdle>"), "Failed to toggle false RunOnlyIfIdle to true");
            Assert(!fixedIdle.Contains("<RunOnlyIfIdle>false</RunOnlyIfIdle>"), "False RunOnlyIfIdle still present");
        }

        // 4. Config Manager Tests
        private static void Test_ConfigManager_Serialization()
        {
            AppConfig cfg = new AppConfig();
            cfg.PreferredEditor = "code";
            cfg.GeoHideRegion = "eu";
            cfg.AlwaysAdmin = true;
            cfg.Language = "en";
            cfg.WindowWidth = 750;
            cfg.WindowHeight = 820;

            string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_config_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                ConfigManager.SaveConfig(cfg, tempFile);
                Assert(System.IO.File.Exists(tempFile), "Config file was not saved");

                AppConfig loaded = ConfigManager.LoadConfig(tempFile);
                AssertEqual("code", loaded.PreferredEditor, "Loaded PreferredEditor mismatch");
                AssertEqual("eu", loaded.GeoHideRegion, "Loaded GeoHideRegion mismatch");
                AssertEqual(true, loaded.AlwaysAdmin, "Loaded AlwaysAdmin mismatch");
                AssertEqual("en", loaded.Language, "Loaded Language mismatch");
                AssertEqual(750, loaded.WindowWidth, "Loaded WindowWidth mismatch");
                AssertEqual(820, loaded.WindowHeight, "Loaded WindowHeight mismatch");
                Assert(loaded.CustomProviders != null && loaded.CustomProviders.Count > 0, "Loaded CustomProviders must not be empty");
            }
            finally
            {
                if (System.IO.File.Exists(tempFile)) System.IO.File.Delete(tempFile);
            }
        }

        // 5. UI Tests (Prevent blank controls bug)
        private static void Test_UI_ControlsNonEmpty_Russian()
        {
            AppConfig cfg = new AppConfig();
            cfg.Language = "ru";

            using (MainForm form = new MainForm(cfg))
            {
                Assert(form.FormBorderStyle == FormBorderStyle.Sizable, "Form should be resizable (Sizable)");
                Assert(form.MaximizeBox, "MaximizeBox should be true");
                Assert(form.MinimumSize.Height >= 560, "MinimumSize height should be >= 560");

                // CRITICAL CHECK: The exact bug from user's screenshot
                Assert(!string.IsNullOrWhiteSpace(form.Text), "Form Window Title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.tabShortcuts.Text), "tabShortcuts title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.tabProviders.Text), "tabProviders title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.gbEditor.Text), "gbEditor GroupBox title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.rbOpenWith.Text), "rbOpenWith text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.rbNpp.Text), "rbNpp text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.rbCode.Text), "rbCode text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.rbNotepad.Text), "rbNotepad text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.rbCustom.Text), "rbCustom text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnBrowseCustom.Text), "btnBrowseCustom text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.gbSettings.Text), "gbSettings GroupBox title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblShortcutName.Text), "lblShortcutName text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblIcon.Text), "lblIcon text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.chkAdmin.Text), "chkAdmin text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnCreateShortcut.Text), "btnCreateShortcut text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.gbExtraShortcuts.Text), "gbExtraShortcuts GroupBox title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblUpdateIcon.Text), "lblUpdateIcon text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnCreateUpdateShortcut.Text), "btnCreateUpdateShortcut text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnOpenHostsHeader.Text), "btnOpenHostsHeader text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.gbManagerShortcut.Text), "gbManagerShortcut title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblUpdateHint.Text), "lblUpdateHint text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblManagerHint.Text), "lblManagerHint text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblExtraHint.Text), "lblExtraHint text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblShortcutHint.Text), "lblShortcutHint text is empty!");

                // Providers tab checks
                Assert(!string.IsNullOrWhiteSpace(form.gbGeo.Text), "gbGeo GroupBox title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.chkGeoHide.Text), "chkGeoHide text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblRegionTitle.Text), "lblRegionTitle text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.rbGeoRU.Text), "rbGeoRU text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.rbGeoEU.Text), "rbGeoEU text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.rbGeoUS.Text), "rbGeoUS text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lnkGeoSite.Text), "lnkGeoSite text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.gbCustom.Text), "gbCustom GroupBox title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnAddCustom.Text), "btnAddCustom text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnResetPresets.Text), "btnResetPresets text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblCustomHint.Text), "lblCustomHint text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.gbScheduler.Text), "gbScheduler GroupBox title is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblTaskStatus.Text), "lblTaskStatus text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblFreq.Text), "lblFreq text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.chkOnlyIfIdle.Text), "chkOnlyIfIdle text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnCopyCmd.Text), "btnCopyCmd text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnToggleTask.Text), "btnToggleTask text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnOpenTaskScheduler.Text), "btnOpenTaskScheduler text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblPathHint.Text), "lblPathHint text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.btnUpdateNow.Text), "btnUpdateNow text is empty!");
                Assert(!string.IsNullOrWhiteSpace(form.lblProviderStatus.Text), "lblProviderStatus text is empty!");
            }
        }

        private static void Test_UI_ControlsNonEmpty_English()
        {
            AppConfig cfg = new AppConfig();
            cfg.Language = "en";

            using (MainForm form = new MainForm(cfg))
            {
                Assert(form.Text.Contains("Hosts Manager"), "Form Window Title not in English");
                Assert(form.tabShortcuts.Text.Contains("Shortcut"), "tabShortcuts not in English");
                Assert(form.tabProviders.Text.Contains("Subscriptions"), "tabProviders not in English");
                Assert(form.btnOpenHostsHeader.Text.Contains("Open hosts"), "btnOpenHostsHeader not in English");
                Assert(form.rbOpenWith.Text.Contains("Standard"), "rbOpenWith not in English");
                Assert(form.btnCreateShortcut.Text.Contains("Create shortcut"), "btnCreateShortcut not in English");
                Assert(form.btnCreateUpdateShortcut.Text.Contains("Update hosts in 1 click"), "btnCreateUpdateShortcut not in English");
                Assert(form.btnCreateManagerShortcut.Text.Contains("Hosts Manager Panel"), "btnCreateManagerShortcut not in English");
                Assert(form.chkGeoHide.Text.Contains("Enable GeoHide"), "chkGeoHide not in English");
                Assert(form.btnUpdateNow.Text.Contains("Sync hosts now"), "btnUpdateNow not in English");
            }
        }

        private static void Test_UI_RecursiveControlsCheck()
        {
            AppConfig cfg = new AppConfig();
            using (MainForm form = new MainForm(cfg))
            {
                CheckControlRecursive(form);
            }
        }

        private static void CheckControlRecursive(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                Button btn = c as Button;
                if (btn != null)
                {
                    Assert(!string.IsNullOrWhiteSpace(btn.Text), string.Format("Button '{0}' (Name: {1}) has empty Text!", btn.GetType().Name, btn.Name));
                }
                else
                {
                    RadioButton rb = c as RadioButton;
                    if (rb != null)
                    {
                        Assert(!string.IsNullOrWhiteSpace(rb.Text), string.Format("RadioButton (Name: {0}) has empty Text!", rb.Name));
                    }
                    else
                    {
                        CheckBox chk = c as CheckBox;
                        if (chk != null)
                        {
                            Assert(!string.IsNullOrWhiteSpace(chk.Text), string.Format("CheckBox (Name: {0}) has empty Text!", chk.Name));
                        }
                        else
                        {
                            GroupBox gb = c as GroupBox;
                            if (gb != null)
                            {
                                Assert(!string.IsNullOrWhiteSpace(gb.Text), string.Format("GroupBox (Name: {0}) has empty Title!", gb.Name));
                            }
                            else
                            {
                                TabPage tp = c as TabPage;
                                if (tp != null)
                                {
                                    Assert(!string.IsNullOrWhiteSpace(tp.Text), string.Format("TabPage (Name: {0}) has empty Title!", tp.Name));
                                }
                            }
                        }
                    }
                }

                if (c.HasChildren)
                {
                    CheckControlRecursive(c);
                }
            }
        }

        private static void Test_UI_ElasticCustomSectionResizing()
        {
            AppConfig cfg = new AppConfig();
            using (MainForm form = new MainForm(cfg))
            {
                form.Size = new System.Drawing.Size(690, 700);
                form.LayoutProvidersTab();

                int geoH1 = form.gbGeo.Height;
                int schedH1 = form.gbScheduler.Height;
                int btnH1 = form.btnUpdateNow.Height;
                int customH1 = form.gbCustom.Height;

                // Expand window height by 150px
                form.Size = new System.Drawing.Size(690, 850);
                form.LayoutProvidersTab();
                AssertEqual(geoH1, form.gbGeo.Height, "gbGeo height must remain fixed when form expands");
                AssertEqual(schedH1, form.gbScheduler.Height, "gbScheduler height must remain fixed when form expands");
                AssertEqual(btnH1, form.btnUpdateNow.Height, "btnUpdateNow height must remain fixed when form expands");
                Assert(form.gbCustom.Height > customH1 + 100, string.Format("gbCustom must expand elastically (was: {0}, now: {1})", customH1, form.gbCustom.Height));

                // Shrink window height to 600px
                form.Size = new System.Drawing.Size(690, 600);
                form.LayoutProvidersTab();
                AssertEqual(geoH1, form.gbGeo.Height, "gbGeo height must remain fixed when form shrinks");
                AssertEqual(schedH1, form.gbScheduler.Height, "gbScheduler height must remain fixed when form shrinks");
                AssertEqual(btnH1, form.btnUpdateNow.Height, "btnUpdateNow height must remain fixed when form shrinks");
                Assert(form.gbCustom.Height < customH1, string.Format("gbCustom must compress elastically (was: {0}, now: {1})", customH1, form.gbCustom.Height));
            }
        }
    }
}
