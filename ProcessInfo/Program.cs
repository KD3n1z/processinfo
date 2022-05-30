using System;
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
        public static int build = 7;

        public static Keys KillKey = Keys.Delete;
        public static Keys UpdateKey = Keys.F5;
        public static Keys ShowKey = Keys.F7;
        public static UpdateBehaviour ub = UpdateBehaviour.Ask;
        public static Color backColor = Color.FromArgb(47, 47, 93);
        public static Color darkBackColor = Color.FromArgb(39, 39, 78);
        public static Color foreColor = Color.White;
        public static Color selColor = Color.Blue;
        public static int radius = 9;

        public static MainF mainForm;

        public static string generalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "kd3n1z-general");
        public static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProcessInfo");
        public static string settings = Path.Combine(path, "settings");

        public static int latest = -1;


        static bool updating = false;

        public static bool hidden = false;

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr handle, int nCmdShow);

        static void Main(string[] args)
        {
            foreach (Process p in Process.GetProcessesByName("ProcessInfo"))
            {
                if (p.Id != Process.GetCurrentProcess().Id)
                {
                    /*SetForegroundWindow(p.MainWindowHandle);
                    ShowWindow(p.MainWindowHandle, 9);*/
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
            if (!Directory.Exists(settings))
            {
                Directory.CreateDirectory(settings);
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

            if (ub != UpdateBehaviour.Never)
            {
                try
                {
                    if(latest > build)
                    {
                        if(ub == UpdateBehaviour.Always)
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
            File.WriteAllText(Path.Combine(settings, "bg.txt"), backColor.ToArgb().ToString());
            File.WriteAllText(Path.Combine(settings, "dbg.txt"), darkBackColor.ToArgb().ToString());
            File.WriteAllText(Path.Combine(settings, "fg.txt"), foreColor.ToArgb().ToString());
            File.WriteAllText(Path.Combine(settings, "fg.txt"), foreColor.ToArgb().ToString());
            File.WriteAllText(Path.Combine(settings, "sc.txt"), selColor.ToArgb().ToString());
            File.WriteAllText(Path.Combine(settings, "br.txt"), radius.ToString());
            File.WriteAllText(Path.Combine(settings, "ub.txt"), ((int)ub).ToString());
            File.WriteAllText(Path.Combine(settings, "kk.txt"), ((int)KillKey).ToString());
            File.WriteAllText(Path.Combine(settings, "uk.txt"), ((int)UpdateKey).ToString());
            File.WriteAllText(Path.Combine(settings, "sk.txt"), ((int)ShowKey).ToString());
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
                backColor = Color.FromArgb(int.Parse(File.ReadAllText(Path.Combine(settings, "bg.txt"))));
            } catch { }
            try
            {
                darkBackColor = Color.FromArgb(int.Parse(File.ReadAllText(Path.Combine(settings, "dbg.txt"))));
            } catch { }
            try
            {
                foreColor = Color.FromArgb(int.Parse(File.ReadAllText(Path.Combine(settings, "fg.txt"))));
            } catch { }
            try
            {
                selColor = Color.FromArgb(int.Parse(File.ReadAllText(Path.Combine(settings, "sc.txt"))));
            } catch { }
            try
            {
                radius = int.Parse(File.ReadAllText(Path.Combine(settings, "br.txt")));
            } catch { }
            try
            {
                ub = (UpdateBehaviour)int.Parse(File.ReadAllText(Path.Combine(settings, "ub.txt")));
            } catch { }
            try
            {
                KillKey = (Keys)int.Parse(File.ReadAllText(Path.Combine(settings, "kk.txt")));
            } catch { }
            try
            {
                UpdateKey = (Keys)int.Parse(File.ReadAllText(Path.Combine(settings, "uk.txt")));
            } catch { }
            try
            {
                ShowKey = (Keys)int.Parse(File.ReadAllText(Path.Combine(settings, "sk.txt")));
            } catch { }
            
        }
    }
}
