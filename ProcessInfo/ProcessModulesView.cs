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
    public partial class ProcessModulesView : Form
    {
        public Process p;
        public ProcessModulesView()
        {
            InitializeComponent();
        }

        private void ProcessModulesView_Load(object sender, EventArgs e)
        {
            p.Refresh();

            Text = p.ProcessName + "'s Modules";

            listBox1.BackColor = Program.backColor;
            listBox1.ForeColor = Program.foreColor;

            foreach (ProcessModule m in p.Modules)
            {
                listBox1.Items.Add(new ModuleInfo(m));
            }
        }

        private void listBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                try
                {
                    Process.Start("explorer.exe", "/select,\"" + ((ModuleInfo)listBox1.Items[listBox1.SelectedIndex]).m.FileName + "\"");
                }
                catch { }
            }
        }
    }

    public class ModuleInfo
    {
        public ProcessModule m;

        public override string ToString()
        {
            return m.ModuleName;
        }

        public ModuleInfo(ProcessModule pm)
        {
            m = pm;
        }
    }
}
