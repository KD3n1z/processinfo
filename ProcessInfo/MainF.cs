using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProcessInfo
{
    public partial class MainF : Form
    {
        public static MainF me;

        bool hidden = false;

        bool running = true;

        #region extrenal methods

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto,SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);
        [DllImport("user32.dll")]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

        [DllImport("user32.dll")]
        static extern int GetAsyncKeyState(Int32 i);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd); 

        [DllImport("user32.dll")]
        private static extern long LockWindowUpdate(long Handle);

        // DLL INJECTION

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            UIntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out IntPtr lpThreadId
        );

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            Int32 bInheritHandle,
            Int32 dwProcessId
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(
            IntPtr hModule,
            string procName
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect
        );

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            string lpBuffer,
            UIntPtr nSize,
            out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern Int32 WaitForSingleObject(
            IntPtr handle,
            Int32 milliseconds
        );

        #endregion

        #region shadow

        private const int CS_DropShadow = 0x20000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (Program.Shadow)
                {
                    cp.ClassStyle |= CS_DropShadow;
                }
                return cp;
            }
        }

        #endregion


        SolidBrush foreBrush;
        SolidBrush selBrush;
        SolidBrush linesBrush;
        SolidBrush backBrush;

        public MainF()
        {
            InitializeComponent();

            LoadTheme();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            me = this;

            new Thread(CheckForOtherInstances).Start();

            new Thread(ShortcutOpen).Start();

            Task.Factory.StartNew(() =>
            {
                AutoUpdate();
            });

            UpdateList();

            if (Program.hidden)
            {
                BeginInvoke(new MethodInvoker(delegate
                {
                    HideWindow();
                }));
            }
        }

        void AutoUpdate()
        {
            while (running)
            {
                if (hidden && Program.AutoUpdateRate != 21)
                {
                    UpdateList();
                }

                Thread.Sleep(Program.AutoUpdateRate * 1000);
            }
        }

        void ShortcutOpen()
        {
            int tilde = (int)Keys.Oemtilde;
            int control = (int)Keys.LControlKey;
            int shift = (int)Keys.ShiftKey;

            while (running)
            {
                if(GetAsyncKeyState(shift) != 0 && GetAsyncKeyState(control) != 0 && GetAsyncKeyState(tilde) != 0)
                {
                    Invoke(new MethodInvoker(() =>
                    {
                        ShowWindow();
                    }));
                }

                Thread.Sleep(100);
            }
        }

        public string KeyCodeToUnicode(Keys key)
        {
            byte[] keyboardState = new byte[255];
            bool keyboardStateStatus = GetKeyboardState(keyboardState);

            if (!keyboardStateStatus)
            {
                return "";
            }

            uint virtualKeyCode = (uint)key;
            uint scanCode = MapVirtualKey(virtualKeyCode, 0);
            IntPtr inputLocaleIdentifier = GetKeyboardLayout(0);

            StringBuilder result = new StringBuilder();
            ToUnicodeEx(virtualKeyCode, scanCode, keyboardState, result, (int)5, (uint)0, inputLocaleIdentifier);

            return result.ToString();
        }


        private static void SuspendProcess(Process process)
        {
             foreach (ProcessThread pT in process.Threads)
             {
                 IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                 if (pOpenThread == IntPtr.Zero)
                 {
                 continue;
                 }
                 SuspendThread(pOpenThread);

                 CloseHandle(pOpenThread);
             }
        }

        public static void ResumeProcess(Process process)
        {
            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }

                var suspendCount = 0;
                do
                {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }

        bool mouseDown;
        Point mouseOffsetForm;

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point senderLoc = ((Control)sender).Location;
                mouseDown = true;
                mouseOffsetForm = new Point(e.Location.X + senderLoc.X, e.Location.Y + senderLoc.Y);
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseDown = false;
            }
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                Point mPos = Control.MousePosition;
                Location = new Point(mPos.X - mouseOffsetForm.X, mPos.Y - mouseOffsetForm.Y);
            }
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void hideButton_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        public void CheckForOtherInstances()
        {
            while (running)
            {
                try
                {
                    if(Process.GetProcessesByName("ProcessInfo").Length > 1)
                    {
                        ShowWindow();
                    }
                    Thread.Sleep(100);
                }
                catch { }
            }
        }

        public void LoadTheme()
        {
            foreBrush = new SolidBrush(Program.ForeColor);
            backBrush = new SolidBrush(Program.BackColor);
            linesBrush = new SolidBrush(Program.LinesColor);
            selBrush = new SolidBrush(Program.SelColor);

            foreach (Control c in Controls)
            {
                c.ForeColor = Program.ForeColor;
                if ((string)c.Tag == "dark")
                {
                    c.BackColor = Program.DarkBackColor;
                }
                else if ((string)c.Tag == "sel")
                {
                    c.BackColor = Program.SelColor;
                }
                else if ((string)c.Tag == "lines")
                {
                    c.BackColor = Program.LinesColor;
                }
                else
                {
                    c.BackColor = Program.BackColor;
                }
            }

            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, Program.Radius, Program.Radius));
        }

        void Updater()
        {
            while (true)
            {
                UpdateList();
            }
        }

        bool canUpdate = true;


        void UpdateList()
        {
            if (canUpdate)
            {
                int index = 0;
                int topIndex = 0;
                string text = "...";


                Invoke(new MethodInvoker(() =>
                {
                    index = processesList.SelectedIndex;
                    text = statusLabel.Text;
                    statusLabel.Text = "updating...";
                    topIndex = processesList.TopIndex;
                    LockWindowUpdate((long)processesList.Handle);
                }));

                canUpdate = false;

                processesList.Invoke(new MethodInvoker(() =>
                {
                    processesList.Items.Clear();
                }));

                int processNum = 0;

                Process[] processes = Process.GetProcesses();

                foreach (Process p in processes.OrderBy(p => p.ProcessName).ThenBy(p => string.IsNullOrWhiteSpace(p.MainWindowTitle)).ThenBy(p => p.Id))
                {
                    DateTime startDT = DateTime.Now;

                    processNum++;
                    Info i = new Info(p);
                    if (!Program.BlackListEnabled || !Program.BlackList.Contains(p.ProcessName))
                    {
                        bool cachedIcon = i.CacheIcon();

                        if (DateTime.Now.Subtract(startDT).TotalMilliseconds > 500)
                        {
                            Program.BlackList.Add(p.ProcessName);
                        }
                    }

                    Invoke(new MethodInvoker(() =>
                    {
                        processesList.Items.Add(i);
                        statusLabel.Text = "updating... " + processNum + "/" + processes.Length;
                    }));
                }

                Program.SaveBlackList();

                Invoke(new MethodInvoker(() =>
                {
                    try
                    {
                        processesList.SelectedIndex = index;
                        processesList.TopIndex = topIndex;
                    }
                    catch { }
                    statusLabel.Text = text;
                    LockWindowUpdate(0);
                }));

                canUpdate = true;

            }
        }

        string sText = "";
        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Program.UpdateKey)
            {
                new Thread(UpdateList).Start();
            }
            else if (e.KeyCode == Program.KillKey)
            {
                Kill();
            }
            else if(e.KeyCode == Keys.Back)
            {
                try
                {
                    sText = sText.Remove(sText.Length - 1, 1);
                }
                catch { }
                titleLabel.Text = "ProcessInfo" + (sText == "" ? "" : " - ") + sText;
            }
            else if(e.KeyCode == Keys.Escape)
            {
                sText = "";
                titleLabel.Text = "ProcessInfo";
            }
            else{
                string key = KeyCodeToUnicode(e.KeyCode).ToLower();
                if (key.Length == 1 || e.KeyCode == Keys.Space)
                {
                    if (e.KeyCode == Keys.Space)
                    {
                        sText += " ";
                    }
                    else
                    {
                        sText += key;
                    }
                }
                else
                {
                    return;
                }
                titleLabel.Text = "ProcessInfo" + (sText == "" ? "" : " - ") + sText;
                
                for (int i = 0; i < processesList.Items.Count; i++)
                {
                    Process p = ((Info)processesList.Items[i]).p;
                    if (p.ProcessName.ToLower().StartsWith(sText) || p.MainWindowTitle.ToLower().StartsWith(sText))
                    {
                        processesList.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        void Kill()
        {
            try
            {
                ((Info)processesList.Items[processesList.SelectedIndex]).p.Kill();
                Thread.Sleep(100);
                new Thread(UpdateList).Start();
            }
            catch (Exception err)
            {
                statusLabel.Text = err.Message;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSel();
        }

        void UpdateSel()
        {
            try
            {
                Info i = (Info)processesList.Items[processesList.SelectedIndex];

                statusLabel.Text = i.FileName;

                try
                {
                    titleLabel.Text = "ProcessInfo - process " + i.p.Id.ToString();
                }
                catch { }
            }
            catch (Exception err)
            {
                statusLabel.Text = err.Message;
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void listBox1_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void listBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Right)
            {
                try
                {
                    contextMenuStrip1.BackColor = Program.DarkBackColor;
                    foreach (object item in contextMenuStrip1.Items)
                    {
                        if (item is ToolStripMenuItem)
                        {
                            (item as ToolStripMenuItem).ForeColor = Program.ForeColor;
                        }

                    }

                    contextMenuStrip1.Items[0].Text =((Info)processesList.Items[processesList.SelectedIndex]).p.ProcessName;
                    contextMenuStrip1.Show(Cursor.Position);
                }
                catch { }
            }
        }

        private void killToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Kill();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            ShowProcessFile(((Label)sender).Text);
        }

        private void ShowProcessFile(string path)
        {
            if (File.Exists(path))
            {
                Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }
        }

        private void changeWindowNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process p = ((Info)processesList.Items[processesList.SelectedIndex]).p;
            TBox tb = new TBox();
            tb.textBox1.Text = p.MainWindowTitle;

            BlockForm bf = new BlockForm();
            bf.Size = Size;
            bf.Location = Location;
            bf.Show(this);
            tb.ShowDialog(bf);
            bf.CanClose = true;
            bf.Close();

            SetWindowText(p.MainWindowHandle, tb.textBox1.Text);
        }



        private void listBox1_MouseClick_1(object sender, MouseEventArgs e)
        {
            sText = "";
            UpdateSel();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                ShowWindow();
            }
        }

        void HideWindow()
        {
            hidden = true;
            Hide();
            notifyIcon1.Visible = true;
        }

        void ShowWindow()
        {
            hidden = false;
            Show();
            WindowState = FormWindowState.Normal;
            Thread.Sleep(30);
            SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            running = false;
            Close();
        }

        private void suspendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SuspendProcess(((Info)processesList.Items[processesList.SelectedIndex]).p);
        }

        private void resumeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResumeProcess(((Info)processesList.Items[processesList.SelectedIndex]).p);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ProcessThreadsView pv = new ProcessThreadsView();
            pv.p = ((Info)processesList.Items[processesList.SelectedIndex]).p;
            pv.ShowDialog();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            ProcessModulesView pv = new ProcessModulesView();
            pv.p = ((Info)processesList.Items[processesList.SelectedIndex]).p;
            pv.ShowDialog();
        }

        private void kIllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IntPtr windowHandle = ((Info)processesList.Items[processesList.SelectedIndex]).p.MainWindowHandle;

            if (windowHandle != IntPtr.Zero)
            {
                SetForegroundWindow(windowHandle);
            }
        }

        private void MainF_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = running;

            if(running)
            {
                HideWindow();
            }
        }


        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= processesList.Items.Count || e.Index <= -1)
            {
                return;
            }

            Info item = (Info)processesList.Items[e.Index];

            if (item == null)
            {
                return;
            }

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;



            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(selBrush, e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            string text = "?";
            if (item.p != null)
            {
                text = item.ToString();
            }
            SizeF stringSize = e.Graphics.MeasureString(text, Font);

            e.Graphics.FillRectangle(
                    linesBrush,
                    new Rectangle(new Point(108, e.Bounds.Y), new Size(1, e.Bounds.Height))
                );


            e.Graphics.FillRectangle(
                    linesBrush,
                    new Rectangle(new Point(342, e.Bounds.Y), new Size(1, e.Bounds.Height))
                );

            /*e.Graphics.FillRectangle(
                    linesBrush,
                    new Rectangle(new Point(622, e.Bounds.Y), new Size(1, e.Bounds.Height))
                );*/

            e.Graphics.DrawImage(
                    item._Icon,
                    new Rectangle(16 + e.Bounds.X, 1 + e.Bounds.Y, e.Bounds.Height - 2, e.Bounds.Height - 2)
                );

            e.Graphics.DrawString(
                    item.FormattedPID,
                    processesList.Font,
                    foreBrush,
                    new PointF(20 + e.Bounds.Height, 2 + e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2)
                );

            e.Graphics.DrawString(
                    item.FormattedName,
                    processesList.Font,
                    foreBrush,
                    new PointF(121, 2 + e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2)
                );

            /*e.Graphics.DrawString(
                    (item.p.WorkingSet64 / 1024 / 1024).ToString(),
                    processesList.Font,
                    foreBrush,
                    new PointF(640, 2 + e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2)
                );*/

            e.Graphics.DrawString(
                    item.FormattedTitle,
                    processesList.Font,
                    foreBrush,
                    new PointF(352, 2 + e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2)
                );
        }

        private void openPrefs(object sender, EventArgs e)
        {
            Pref p = new Pref();
            p.ShowDialog();
            if (p.restartRequired)
            {
                Application.Restart();
                Environment.Exit(0);
            }
        }

        private void hideButton_MouseEnter(object sender, EventArgs e)
        {
            hideButton.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_hide_hover;
        }

        private void hideButton_MouseLeave(object sender, EventArgs e)
        {
            hideButton.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_hide;
        }

        private void closeButton_MouseEnter(object sender, EventArgs e)
        {
            closeButton.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_close_hover;
        }

        private void closeButton_MouseLeave(object sender, EventArgs e)
        {
            closeButton.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_close;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowWindow();
        }

        private void closeButton_MouseDown(object sender, MouseEventArgs e)
        {
            closeButton.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_close_down;
        }

        private void closeButton_MouseUp(object sender, MouseEventArgs e)
        {
            closeButton.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_close_hover;
        }

        private void hideButton_MouseDown(object sender, MouseEventArgs e)
        {
            hideButton.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_hide_down;
        }

        private void hideButton_MouseUp(object sender, MouseEventArgs e)
        {
            hideButton.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_hide_hover;
        }

        private void injectDLLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dllDialog = new OpenFileDialog();
            dllDialog.Filter = "*.dll|*.dll";
            if(dllDialog.ShowDialog() == DialogResult.OK)
            {
                string path = dllDialog.FileName;
                Process p = ((Info)processesList.Items[processesList.SelectedIndex]).p;
                string text = statusLabel.Text;
                new Thread(() => Inject(path, p, text)).Start();
            }
        }

        void Inject(string path, Process process, string text)
        {
            string baseText = "injecting " + Path.GetFileName(path) + "... ";

            IntPtr hProcess = OpenProcess(0x1F0FF, 1, process.Id);

            if (hProcess == null)
            {
                statusLabel.Invoke(new MethodInvoker(() =>
                {
                    statusLabel.Text = baseText + "OpenProcess failed!";
                }));

                Thread.Sleep(1000);

                statusLabel.Invoke(new MethodInvoker(() =>
                {
                    statusLabel.Text = text;
                }));

                return;
            }

            uint WriteLen = (uint)(path.Length + 1);

            IntPtr AllocMem = VirtualAllocEx(hProcess, (IntPtr)null, WriteLen, 0x1000, 0x40);

            IntPtr bytesout;

            WriteProcessMemory(hProcess, AllocMem, path, (UIntPtr)WriteLen, out bytesout);

            UIntPtr Injector = (UIntPtr)GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (Injector == null)
            {
                statusLabel.Invoke(new MethodInvoker(() =>
                {
                    statusLabel.Text = baseText + "injector error!";
                }));

                Thread.Sleep(1000);

                statusLabel.Invoke(new MethodInvoker(() =>
                {
                    statusLabel.Text = text;
                }));

                return;
            }

            IntPtr hThread = (IntPtr)CreateRemoteThread(hProcess, (IntPtr)null, 0, Injector, AllocMem, 0, out bytesout);

            if (hThread == null)
            {
                statusLabel.Invoke(new MethodInvoker(() =>
                {
                    statusLabel.Text = baseText + "CreateRemoteThread error!";
                }));

                Thread.Sleep(1000);

                statusLabel.Invoke(new MethodInvoker(() =>
                {
                    statusLabel.Text = text;
                }));

                return;
            }

            int result = WaitForSingleObject(hThread, 10 * 1000);

            if (result == 0x00000080L || result == 0x00000102L || result == 0xFFFFFFFF)
            {
                if (hThread != null)
                {
                    CloseHandle(hThread);
                }

                statusLabel.Invoke(new MethodInvoker(() =>
                {
                    statusLabel.Text = baseText + "CreateRemoteThread error!";
                }));

                Thread.Sleep(1000);

                statusLabel.Invoke(new MethodInvoker(() =>
                {
                    statusLabel.Text = text;
                }));

                return;
            }

            Task.Delay(1000);

            VirtualFreeEx(hProcess, AllocMem, (UIntPtr)0, 0x8000);

            if (hThread != null)
            {
                CloseHandle(hThread);
            }

            statusLabel.Invoke(new MethodInvoker(() =>
            {
                statusLabel.Text = baseText + "done!";
            }));

            Thread.Sleep(1000);

            statusLabel.Invoke(new MethodInvoker(() =>
            {
                statusLabel.Text = text;
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            processesList.Update();
        }
    }

    public class Info
    {
        public Process p;

        Image icon = null;
        public Image _Icon
        {
            get
            {
                if(icon == null)
                {
                    CacheIcon();
                }
                return icon;
            }
        }

        public bool CacheIcon()
        {
            try
            {
                if (Program.BlackListEnabled && Program.BlackList.Contains(p.ProcessName))
                {
                    icon = global::ProcessInfo.Properties.Resources.error.ToBitmap();
                    return true;
                }
                icon = new Icon(Icon.ExtractAssociatedIcon(FileName), 16, 16).ToBitmap();
                return true;
            }
            catch
            {
                icon = global::ProcessInfo.Properties.Resources.error.ToBitmap();
            }
            return false;
        }

        public string FileName
        {
            get
            {
                try
                {
                    return p.MainModule.FileName;
                }
                catch (Exception err)
                {
                    return err.Message;
                }
            }
        }

        public string FormattedPID
        {
            get
            {
                return new string('0', 5 - p.Id.ToString().Length) + p.Id;
            }
        }

        public string FormattedName
        {
            get
            {
                string name = p.ProcessName;
                if (name.Length > 20)
                {
                    name = name.Substring(0, 17) + "...";
                }
                return name + new string(' ', 20 - name.Length);
            }
        }

        public string FormattedTitle
        {
            get
            {
                string title = p.MainWindowTitle;
                if (title.Length > 33)
                {
                    title = title.Substring(0, 30) + "...";
                }
                return title;
            }
        }

        public bool Refresh()
        {
            try
            {
                p = Process.GetProcessById(p.Id);
                return true;
            }
            catch (ArgumentException)
            {
                p = null;
                return false;
            }
        }
        public override string ToString()
        {
            return FormattedPID + " | " + FormattedName + " | " + p.MainWindowTitle;
        }

        public Info(Process pr)
        {
            p = pr;
        }
    }
}
