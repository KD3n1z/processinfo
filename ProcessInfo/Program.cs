using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        public static Keys KillKey = Keys.Delete;
        public static Keys UpdateKey = Keys.F5;
        public static Keys ShowKey = Keys.F7;
        public static UpdateBehaviour ub = UpdateBehaviour.Ask;
        public static Color bg = Color.FromArgb(28, 28, 28);
        public static Color dbg = Color.FromArgb(16, 16, 16);
        public static Color fg = Color.White;

        public static int build = 4;
        public static MainF mainForm;

        public static string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProcessInfo3");
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
            string updater = Path.Combine(path, "updater.exe");

            if (!File.Exists(updater))
            {
                new WebClient().DownloadFile("http://kd3n1z.com/apps/pi/updater.exe", updater);
            }

            Process.Start(updater, Process.GetCurrentProcess().MainModule.FileName);
            Application.Exit();
        }

        public static int GetLatestVersion()
        {
            try
            {
                return int.Parse(new WebClient().DownloadString("http://kd3n1z.com/apps.php?app=pi&action=latest"));
            }
            catch
            {
                return 0;
            }
        }

        public static void Save()
        {
            File.WriteAllText(Path.Combine(settings, "bg.txt"), bg.ToArgb().ToString());
            File.WriteAllText(Path.Combine(settings, "dbg.txt"), dbg.ToArgb().ToString());
            File.WriteAllText(Path.Combine(settings, "fg.txt"), fg.ToArgb().ToString());
            File.WriteAllText(Path.Combine(settings, "ub.txt"), ((int)ub).ToString());
            File.WriteAllText(Path.Combine(settings, "kk.txt"), ((int)KillKey).ToString());
            File.WriteAllText(Path.Combine(settings, "uk.txt"), ((int)UpdateKey).ToString());
            File.WriteAllText(Path.Combine(settings, "sk.txt"), ((int)ShowKey).ToString());
        }

        static void Load()
        {
            try
            {
                bg = Color.FromArgb(int.Parse(File.ReadAllText(Path.Combine(settings, "bg.txt"))));
            } catch { }
            try
            {
                dbg = Color.FromArgb(int.Parse(File.ReadAllText(Path.Combine(settings, "dbg.txt"))));
            } catch { }
            try
            {
                fg = Color.FromArgb(int.Parse(File.ReadAllText(Path.Combine(settings, "fg.txt"))));
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
