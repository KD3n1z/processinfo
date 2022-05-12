using Microsoft.Win32;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace ProcessInfo
{
    public partial class Pref : Form
    {
        RegistryKey alReg;

        string themes = Path.Combine(Program.generalPath, "themes");
        public Pref()
        {
            InitializeComponent();
        }

        string themeFile = Path.Combine(Program.settings, "theme.txt");
        private void Pref_Load(object sender, EventArgs e)
        {
            try
            {
                alReg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run", true);
            }
            catch { }

            if(alReg != null)
            {
                checkBox1.Checked = alReg.GetValue("ProcessInfo") != null;
            }
            else
            {
                checkBox1.Text = "start up as administrator";
            }

            if (!Directory.Exists(themes))
            {
                Directory.CreateDirectory(themes);
                try
                {
                    string dPath = Path.Combine(themes, "themes.zip");

                    new WebClient().DownloadFile("https://github.com/KD3n1z/kd3n1z-com/raw/main/themes.zip", dPath);

                    ZipFile.ExtractToDirectory(dPath, themes);

                    File.Delete(dPath);
                }
                catch { }
                File.WriteAllText(themeFile, "default");
            }

            foreach(string f in Directory.GetFiles(themes).OrderByDescending(t => Path.GetFileNameWithoutExtension(t).StartsWith("def") || Path.GetFileNameWithoutExtension(t).StartsWith("light")))
            {
                if (f.EndsWith(".pit"))
                {
                    comboBox1.Items.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            comboBox1.Text = File.ReadAllText(themeFile);

            button1.BackColor = Program.fg;
            button2.BackColor = Program.bg;
            button3.BackColor = Program.dbg;
            label1.Text += Program.build;

            switch (Program.ub)
            {
                case UpdateBehaviour.Always:
                    radioButton1.Checked = true;
                    break;
                case UpdateBehaviour.Never:
                    radioButton2.Checked = true;
                    break;
                case UpdateBehaviour.Ask:
                    radioButton3.Checked = true;
                    break;
            }

            button5.Text = Program.UpdateKey.ToString();
            button6.Text = Program.KillKey.ToString();
            button8.Text = Program.ShowKey.ToString();

            MarkUpdateBtn();
        }

        void MarkUpdateBtn()
        {
            if (Program.latest > Program.build)
            {
                button4.Text = "Update (github=b" + Program.latest + "; local=b" + Program.build + ")";
            }
        }

        private void changeFG(object sender, EventArgs e)
        {
            Control c = sender as Control;

            c.ForeColor = c.BackColor.R + c.BackColor.G + c.BackColor.B > 382 ? Color.Black : Color.White;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = button1.BackColor;

            if(colorDialog1.ShowDialog() == DialogResult.OK)
            {
                button1.BackColor = colorDialog1.Color;
            }

            Program.fg = button1.BackColor;

            Program.mainForm.LoadTheme();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = button2.BackColor;

            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                button2.BackColor = colorDialog1.Color;
            }

            Program.bg = button2.BackColor;

            Program.mainForm.LoadTheme();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = button3.BackColor;

            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                button3.BackColor = colorDialog1.Color;
            }

            Program.dbg = button3.BackColor;

            Program.mainForm.LoadTheme();
        }

        private void Pref_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (radioButton1.Checked)
            {
                Program.ub = UpdateBehaviour.Always;
            }
            else if (radioButton2.Checked)
            {
                Program.ub = UpdateBehaviour.Never;
            }
            else
            {
                Program.ub = UpdateBehaviour.Ask;
            }

            File.WriteAllText(themeFile, comboBox1.Text);

            Program.Save();

            try
            {
                if (checkBox1.Checked)
                {
                    alReg.SetValue("ProcessInfo", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\" -hidden");
                }
                else if (alReg.GetValue("ProcessInfo") != null)
                {
                    alReg.DeleteValue("ProcessInfo");
                }
            }
            catch { }

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://kd3n1z.com/index.php?app=ProcessInfo");
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void button5_KeyDown(object sender, KeyEventArgs e)
        {
            Program.UpdateKey = e.KeyCode;
            button5.Text = Program.UpdateKey.ToString();
        }

        private void button6_KeyDown(object sender, KeyEventArgs e)
        {
            Program.KillKey = e.KeyCode;
            button6.Text = Program.KillKey.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Program.latest = Program.GetLatestVersion();

            if (Program.latest <= Program.build)
            {
                button4.Text = "Updates not found (github=b" + Program.latest + "; local=b" + Program.build + ")";
            }
            else if (Program.latest > Program.build)
            {
                MarkUpdateBtn();
                Program.AskForUpdate();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", "\"" + Program.path + "\"");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if(File.Exists(Path.Combine(themes, comboBox1.Text + ".pit")))
            {
                if (File.ReadAllText(Path.Combine(themes, comboBox1.Text + ".pit")).StartsWith("onlyread"))
                {
                    MessageBox.Show("you can't edit this theme (" + comboBox1.Text + "), enter another name to save it");
                    return;
                }
                else
                {
                    SaveTheme();

                    if (!comboBox1.Items.Contains(comboBox1.Text))
                    {
                        comboBox1.Items.Add(comboBox1.Text);
                    }
                }
            }
            else
            {
                SaveTheme();

                if (!comboBox1.Items.Contains(comboBox1.Text))
                {
                    comboBox1.Items.Add(comboBox1.Text);
                }
            }
        }

        void SaveTheme()
        {
            File.WriteAllText(Path.Combine(themes, comboBox1.Text + ".pit"),
                "custom\n"
                + button1.BackColor.ToArgb().ToString() + "\n"
                + button2.BackColor.ToArgb().ToString() + "\n"
                + button3.BackColor.ToArgb().ToString() + "\n"
                );
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadPIT(comboBox1.Text);
        }

        void LoadPIT(string pitName)
        {
            string[] vals = File.ReadAllText(Path.Combine(themes, pitName + ".pit")).Split('\n');
            button1.BackColor = Program.fg = Color.FromArgb(int.Parse(vals[1]));
            button2.BackColor = Program.bg =  Color.FromArgb(int.Parse(vals[2]));
            button3.BackColor = Program.dbg = Color.FromArgb(int.Parse(vals[3]));
            trackBar1.Value = Program.radius;
            trackBar1_Scroll(this, null);


            Program.mainForm.LoadTheme();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        private void button8_KeyDown(object sender, KeyEventArgs e)
        {
            Program.ShowKey = e.KeyCode;
            button8.Text = Program.ShowKey.ToString();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label6.Text = "radius: " + trackBar1.Value + "px";

            Program.radius = trackBar1.Value;

            Program.mainForm.LoadTheme();
        }
    }
}
