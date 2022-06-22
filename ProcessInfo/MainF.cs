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
using System.Windows.Forms;

namespace ProcessInfo
{
    public partial class MainF : Form
    {
        bool hidden = false;

        public static MainF me;

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
        #endregion

        #region shadow

        private const int CS_DropShadow = 0x20000;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (Program.shadow)
                {
                    cp.ClassStyle |= CS_DropShadow;
                }
                return cp;
            }
        }

        #endregion

        public MainF()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            me = this;

            new Thread(CheckForOtherInstances).Start();

            new Thread(ShortcutOpen).Start();

            LoadTheme();

            UpdateList();

            if (Program.hidden)
            {
                BeginInvoke(new MethodInvoker(delegate
                {
                    HideWindow();
                }));
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

        private void button1_Click(object sender, EventArgs e)
        {
            HideWindow();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        void HideWindow()
        {
            Hide();
            notifyIcon1.Visible = true;
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
            foreach (Control c in Controls)
            {
                c.ForeColor = Program.ForeColor;
                if ((string)c.Tag == "dark")
                {
                    c.BackColor = Program.DarkBackColor;
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
                string text = "...";


                Invoke(new MethodInvoker(() =>
                {
                    index = listBox1.SelectedIndex;
                    text = label2.Text;
                    label2.Text = "updating...";
                }));

                canUpdate = false;

                listBox1.Invoke(new MethodInvoker(() =>
                {
                    listBox1.Items.Clear();
                }));

                int processNum = 0;

                Process[] processes = Process.GetProcesses();

                foreach (Process p in processes.OrderBy(p => p.ProcessName).ThenBy(p => p.Id).ThenByDescending(p => p.MainWindowHandle != IntPtr.Zero))
                {
                    processNum++;
                    Info i = new Info(p);
                    i.CacheIcon();

                    Invoke(new MethodInvoker(() =>
                    {
                        listBox1.Items.Add(i);
                        label2.Text = "updating... " + processNum + "/" + processes.Length;
                    }));
                }

                Invoke(new MethodInvoker(() =>
                {
                    listBox1.SelectedIndex = index;
                    label2.Text = text;
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
                label1.Text = "ProcessInfo" + (sText == "" ? "" : " - ") + sText;
            }
            else if(e.KeyCode == Keys.Escape)
            {
                sText = "";
                label1.Text = "ProcessInfo";
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
                label1.Text = "ProcessInfo" + (sText == "" ? "" : " - ") + sText;
                
                for (int i = 0; i < listBox1.Items.Count; i++)
                {
                    Process p = ((Info)listBox1.Items[i]).p;
                    if (p.ProcessName.ToLower().StartsWith(sText) || p.MainWindowTitle.ToLower().StartsWith(sText))
                    {
                        listBox1.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        void Kill()
        {
            try
            {
                ((Info)listBox1.Items[listBox1.SelectedIndex]).p.Kill();
                Thread.Sleep(100);
                UpdateList();
            }
            catch (Exception err)
            {
                label2.Text = err.Message;
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
                Info i = (Info)listBox1.Items[listBox1.SelectedIndex];

                label2.Text = i.FileName;

                try
                {
                    label1.Text = "ProcessInfo - process " + i.p.Id.ToString();
                }
                catch { }
            }
            catch (Exception err)
            {
                label2.Text = err.Message;
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

                    contextMenuStrip1.Items[0].Text =((Info)listBox1.Items[listBox1.SelectedIndex]).p.ProcessName;
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
            Process p = ((Info)listBox1.Items[listBox1.SelectedIndex]).p;
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
            Close();
        }

        private void suspendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SuspendProcess(((Info)listBox1.Items[listBox1.SelectedIndex]).p);
        }

        private void resumeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResumeProcess(((Info)listBox1.Items[listBox1.SelectedIndex]).p);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ProcessThreadsView pv = new ProcessThreadsView();
            pv.p = ((Info)listBox1.Items[listBox1.SelectedIndex]).p;
            pv.ShowDialog();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            ProcessModulesView pv = new ProcessModulesView();
            pv.p = ((Info)listBox1.Items[listBox1.SelectedIndex]).p;
            pv.ShowDialog();
        }

        private void kIllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IntPtr windowHandle = ((Info)listBox1.Items[listBox1.SelectedIndex]).p.MainWindowHandle;

            if (windowHandle != IntPtr.Zero)
            {
                SetForegroundWindow(windowHandle);
            }
        }

        private void MainF_FormClosing(object sender, FormClosingEventArgs e)
        {
            running = false;
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= listBox1.Items.Count || e.Index <= -1)
            {
                return;
            }

            Info item = (Info)listBox1.Items[e.Index];

            if (item == null)
            {
                return;
            }

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                e.Graphics.FillRectangle(new SolidBrush(Program.SelColor), e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(listBox1.BackColor), e.Bounds);
            }

            string text = "?";
            if (item.p != null)
            {
                text = item.ToString();
            }
            SizeF stringSize = e.Graphics.MeasureString(text, Font);

            e.Graphics.DrawIcon(
                item.Icon,
                new Rectangle(16 + e.Bounds.X, 1 + e.Bounds.Y, e.Bounds.Height - 2, e.Bounds.Height - 2)
                );

            e.Graphics.DrawString(
                    item.FormattedPID,
                    listBox1.Font,
                    new SolidBrush(listBox1.ForeColor),
                    new PointF(20 + e.Bounds.Height, 2 + e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2)
                );

            e.Graphics.DrawString(
                    item.FormattedName,
                    listBox1.Font,
                    new SolidBrush(listBox1.ForeColor),
                    new PointF(121, 2 + e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2)
                );

            e.Graphics.DrawString(
                    item.p.MainWindowTitle,
                    listBox1.Font,
                    new SolidBrush(listBox1.ForeColor),
                    new PointF(352, 2 + e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2)
                );


            e.Graphics.DrawRectangle(
                new Pen(new SolidBrush(Program.DarkBackColor)),
                new Rectangle(new Point(108, e.Bounds.Y), new Size(1, e.Bounds.Height))
                );


            e.Graphics.DrawRectangle(
                new Pen(new SolidBrush(Program.DarkBackColor)),
                new Rectangle(new Point(342, e.Bounds.Y), new Size(1, e.Bounds.Height))
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

        private void button5_MouseEnter(object sender, EventArgs e)
        {
            button5.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_hide_hover;
        }

        private void button5_MouseLeave(object sender, EventArgs e)
        {
            button5.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_hide;
        }

        private void button4_MouseEnter(object sender, EventArgs e)
        {
            button4.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_close_hover;
        }

        private void button4_MouseLeave(object sender, EventArgs e)
        {
            button4.BackgroundImage = global::ProcessInfo.Properties.Resources.mac_close;
        }
    }

    public class Info
    {
        public Process p;

        Icon icon = null;
        public Icon Icon
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

        public void CacheIcon()
        {
            try
            {
                icon = new Icon(Icon.ExtractAssociatedIcon(FileName), 16, 16);
            }
            catch
            {
                icon = global::ProcessInfo.Properties.Resources.error;
            }
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
