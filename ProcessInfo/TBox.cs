using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProcessInfo
{
    public partial class TBox : Form
    {
        bool CanClose = false;
        public TBox()
        {
            InitializeComponent();
            textBox1.BackColor = Program.bg;
            textBox1.ForeColor = Program.fg;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
            {
                CanClose = true;
                Close();
            }
        }

        private void TBox_Load(object sender, EventArgs e)
        {
            Location = new Point(Program.mainForm.Size.Width / 2 + Program.mainForm.Location.X - Size.Width / 2,
                Program.mainForm.Size.Height / 2 + Program.mainForm.Location.Y - Size.Height / 2);
        }

        private void TBox_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CanClose)
            {
                Program.mainForm.Close();
            }
        }
    }
}
