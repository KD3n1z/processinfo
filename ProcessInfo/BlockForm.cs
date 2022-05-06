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
    public partial class BlockForm : Form
    {
        public bool CanClose = false;

        public BlockForm()
        {
            InitializeComponent();
            Opacity = 0.7;
        }

        private void BlockForm_Load(object sender, EventArgs e)
        {

        }

        private void BlockForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CanClose)
            {
                e.Cancel = true;
            }
        }
    }
}
