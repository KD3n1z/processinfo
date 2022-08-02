﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ProcessInfo
{
    enum UpdateBehaviour
    {
        Never,
        Always,
        Ask
    }
    internal static class Program
    {
        public static int build = 15;

        public static Keys KillKey = Keys.Delete;
        public static Keys UpdateKey = Keys.F5;
        public static UpdateBehaviour UpdateAction = UpdateBehaviour.Ask;

        // THEME SETTINGS (colors)
        public static string ThemeFile = "gourd";
        public static Color BackColor = Color.FromArgb(45, 42, 46);
        public static Color DarkBackColor = Color.FromArgb(34, 31, 34);
        public static Color ForeColor = Color.FromArgb(235, 219, 178);
        public static Color SelColor = Color.FromArgb(100, 123, 100);
        public static Color LinesColor = Color.FromArgb(34, 31, 34);

        // THEME SETTINGS (individual)
        public static int Radius = 15;
        public static bool Shadow = false;

        public static int AutoUpdateRate = 10;

        public static MainF mainForm;

        public static string generalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "kd3n1z-general");
        public static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProcessInfo");
        public static string settingsPath = Path.Combine(path, "settings.txt");

        public static int latest = -1;


        static bool updating = false;

        public static bool hidden = false;

        [STAThread]
        static void Main(string[] args)
        {
            foreach (Process p in Process.GetProcessesByName("ProcessInfo"))
            {
                if (p.Id != Process.GetCurrentProcess().Id)
                {
                    Thread.Sleep(1000);
                    return;
                }
            }

            hidden = args.Contains("-hidden");

            if (!Directory.Exists(generalPath))
            {
                Directory.CreateDirectory(generalPath);
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            Load();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CheckForUpdates();

            mainForm = new MainF();

            if (!updating)
            {
                Application.Run(mainForm);
            }
        }

        public static void CheckForUpdates()
        {
            latest = GetLatestVersion();

            if (UpdateAction != UpdateBehaviour.Never)
            {
                try
                {
                    if(latest > build)
                    {
                        if(UpdateAction == UpdateBehaviour.Always)
                        {
                            Update();
                        }
                        else
                        {
                            AskForUpdate();
                        }
                    }
                }
                catch { }
            }
        }

        public static void AskForUpdate()
        {
            if (MessageBox.Show("Update available! Do you want to update ProcessInfo?", "ProcessInfo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Update();
            }
        }

        public static void Update()
        {
            updating = true;
            string updater = Path.Combine(generalPath, "updater.exe");

            if (!File.Exists(updater))
            {
                new WebClient().DownloadFile("https://github.com/KD3n1z/updater/releases/download/main/updater.exe", updater);
            }

            if(latestRelease == null)
            {
                latestRelease = ParseJsonFromUrl("https://api.github.com/repos/KD3n1z/processinfo/releases/latest");
            }

            Process.Start(updater, "\"path=" + Process.GetCurrentProcess().MainModule.FileName + "\" \"url=" + latestRelease.XPathSelectElement("//assets").FirstNode.XPathSelectElement("//browser_download_url").Value + "\" \"app=ProcessInfo\"");
            Application.Exit();
        }

        static XElement latestRelease = null;
        public static int GetLatestVersion()
        {
            try
            {
                latestRelease = ParseJsonFromUrl("https://api.github.com/repos/KD3n1z/processinfo/releases/latest");
                return int.Parse(latestRelease.XPathSelectElement("/name").Value.ToLower().Replace("build ", ""));
            }
            catch
            {
                return 0;
            }
        }

        public static void Save()
        {
            SettingsFile settingsFile = new SettingsFile();

            settingsFile["backColor"] = BackColor.ToArgb().ToString();
            settingsFile["darkBackColor"] = DarkBackColor.ToArgb().ToString();
            settingsFile["foreColor"] = ForeColor.ToArgb().ToString();
            settingsFile["selectionColor"] = SelColor.ToArgb().ToString();
            settingsFile["radius"] = Radius.ToString();
            settingsFile["update"] = ((int)UpdateAction).ToString();
            settingsFile["killKey"] = ((int)KillKey).ToString();
            settingsFile["updateKey"] = ((int)UpdateKey).ToString();
            settingsFile["theme"] = ThemeFile;
            settingsFile["shadow"] = Shadow.ToString();
            settingsFile["autoUpdateRate"] = AutoUpdateRate.ToString();

            File.WriteAllText(settingsPath, settingsFile.ToString());

        }

        static XElement ParseJsonFromUrl(string url)
        {
            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "request");
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(wc.DownloadString(url)), new System.Xml.XmlDictionaryReaderQuotas());
            return XElement.Load(jsonReader);
        }

        static void Load()
        {
            try
            {
                SettingsFile settingsFile = new SettingsFile(File.ReadAllText(settingsPath));

                BackColor = Color.FromArgb(int.Parse(settingsFile["backColor"]));
                DarkBackColor = Color.FromArgb(int.Parse(settingsFile["darkBackColor"]));
                ForeColor = Color.FromArgb(int.Parse(settingsFile["foreColor"]));
                SelColor = Color.FromArgb(int.Parse(settingsFile["selectionColor"]));
                Radius = int.Parse(settingsFile["radius"]);
                UpdateAction = (UpdateBehaviour)int.Parse(settingsFile["update"]);
                KillKey = (Keys)int.Parse(settingsFile["killKey"]);
                UpdateKey = (Keys)int.Parse(settingsFile["updateKey"]);
                ThemeFile = settingsFile["theme"];
                Shadow = settingsFile["shadow"].ToLower() == "true";
                AutoUpdateRate = int.Parse(settingsFile["autoUpdateRate"]);
            }
            catch { }
        }
    }
}
