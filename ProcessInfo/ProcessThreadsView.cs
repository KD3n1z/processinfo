using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProcessInfo
{
    public partial class ProcessThreadsView : Form
    {
        public Process p;
        public ProcessThreadsView()
        {
            InitializeComponent();
        }

        private void ProcessView_Load(object sender, EventArgs e)
        {
            p.Refresh();

            Text = p.ProcessName + "'s Threads";

            listBox1.BackColor = Program.backColor;
            listBox1.ForeColor = Program.foreColor;

            foreach(ProcessThread t in p.Threads)
            {
                listBox1.Items.Add(new ThreadInfo(t));
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                //((ThreadInfo)listBox1.Items[listBox1.SelectedIndex]).t;
            }
        }
    }

    class ThreadInfo
    {
        public ProcessThread t;

        public override string ToString()
        {
            return new string('0', 5 - t.Id.ToString().Length) + t.Id + " - " + t.ThreadState.ToString() + (t.ThreadState == ThreadState.Wait ? "/" + t.WaitReason : "");
        }

        public ThreadInfo(ProcessThread pt)
        {
            t = pt;
        }
    }
}
