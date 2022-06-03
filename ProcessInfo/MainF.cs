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

        public List<Info> rem = new List<Info>();
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
                c.ForeColor = Program.foreColor;
                if ((string)c.Tag == "dark")
                {
                    c.BackColor = Program.darkBackColor;
                }
                else
                {
                    c.BackColor = Program.backColor;
                }
            }

            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, Program.radius, Program.radius));
        }

        void Updater()
        {
            while (true)
            {
                UpdateList();
            }
        }

        bool canUpdate = true;


        bool firstUpdate = true;
        void UpdateList() => UpdateList("");
        void UpdateList(string filter)
        {
            if (canUpdate)
            {
                canUpdate = false;
                rem.Clear();
                foreach (object obj in listBox1.Items)
                {
                    ((Info)obj).Refresh();
                    if (filter != "")
                    {
                        Info i = (Info)obj;
                        if (!rem.Contains(i))
                        {
                            if (!i.p.ProcessName.ToLower().StartsWith(filter) && !i.p.MainWindowTitle.StartsWith(filter))
                            {
                                rem.Add(i);
                            }
                        }
                    }
                }
                foreach (Info i in rem)
                {
                    listBox1.Invoke(new MethodInvoker(() =>
                    {
                        listBox1.Items.Remove(i);
                    }));
                }
                foreach (Process p in Process.GetProcesses().OrderBy(p => p.ProcessName).ThenBy(p => p.Id))
                {
                    Info i = new Info(p);
                    if (!listBox1.Items.Contains(i))
                    {
                        listBox1.Invoke(new MethodInvoker(() =>
                        {
                            listBox1.Items.Add(i);
                        }));
                    }

                    // cache icons
                    if (firstUpdate)
                    {
                        i.CacheIcon();
                    }
                }
                canUpdate = true;
            }
            firstUpdate = false;
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

                label1.Text = "ProcessInfo - process " + i.p.Id.ToString();
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
                    contextMenuStrip1.BackColor = Program.backColor;
                    contextMenuStrip1.ForeColor = Program.foreColor;

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
                e.Graphics.FillRectangle(new SolidBrush(Program.selColor), e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(listBox1.BackColor), e.Bounds);
            }

            string text = item.ToString();
            SizeF stringSize = e.Graphics.MeasureString(text, Font);

            int offset = 5;

            e.Graphics.DrawIcon(item.Icon, new Rectangle(offset + e.Bounds.X, 1 + e.Bounds.Y, e.Bounds.Height - 2, e.Bounds.Height - 2));

            e.Graphics.DrawString(
                    text,
                    listBox1.Font,
                    new SolidBrush(listBox1.ForeColor),
                    new PointF(offset + 1 + e.Bounds.Height, e.Bounds.Y + (e.Bounds.Height - stringSize.Height) / 2)
                );
        }

        private void openPrefs(object sender, EventArgs e)
        {
            new Pref().ShowDialog();
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

        public void Refresh()
        {
            try
            {
                p = Process.GetProcessById(p.Id);
            }
            catch (ArgumentException)
            {
                p = null;
            }
            if (p == null)
            {
                MainF.me.rem.Add(this);
            }
        }
        public override string ToString()
        {
            string name = p.ProcessName;
            if(name.Length > 20)
            {
                name = name.Substring(0,17) + "...";
            }
            return new string('0', 5 - p.Id.ToString().Length) + p.Id + " | " + name + new string(' ', 20 - name.Length) + " | " + p.MainWindowTitle;
        }

        public Info(Process pr)
        {
            p = pr;
        }
    }
}
