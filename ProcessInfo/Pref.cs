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
       public bool restartRequired = false;

        RegistryKey alReg;

        string themes = Path.Combine(Program.generalPath, "themes");
        public Pref()
        {
            InitializeComponent();
        }

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
                Program.ThemeFile = "venus";
            }

            foreach(string f in Directory.GetFiles(themes).OrderByDescending(t => Path.GetFileNameWithoutExtension(t).StartsWith("def") || Path.GetFileNameWithoutExtension(t).StartsWith("light")))
            {
                if (f.EndsWith(".pit"))
                {
                    comboBox1.Items.Add(Path.GetFileNameWithoutExtension(f));
                }
            }
            comboBox1.Text = Program.ThemeFile;

            textCB.BackColor = Program.ForeColor;
            backCB.BackColor = Program.BackColor;
            dbackCB.BackColor = Program.DarkBackColor;
            selCB.BackColor = Program.SelColor;
            label1.Text += Program.build;
            checkBox2.Checked = Program.Shadow;

            switch (Program.UpdateAction)
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

            trackBar1.Value = Program.Radius;
            trackBar2.Value = Program.AutoUpdateRate;
            trackBar1_Scroll(this, null);
            trackBar2_Scroll(this, null);

            MarkUpdateBtn();

            loaded = true;
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

        private void changeColor(object sender, EventArgs e)
        {
            Button b = (Button)sender;

            colorDialog1.Color = b.BackColor;

            if(colorDialog1.ShowDialog() == DialogResult.OK)
            {
                b.BackColor = colorDialog1.Color;
            }

            switch (b.Tag.ToString())
            {
                case "text":
                    Program.ForeColor = b.BackColor;
                    break;
                case "bg":
                    Program.BackColor = b.BackColor;
                    break;
                case "dbg":
                    Program.DarkBackColor = b.BackColor;
                    break;
                case "sel":
                    Program.SelColor = b.BackColor;
                    break;
                default:
                    break;
            }

            Program.mainForm.LoadTheme();
        }

        private void Pref_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (radioButton1.Checked)
            {
                Program.UpdateAction = UpdateBehaviour.Always;
            }
            else if (radioButton2.Checked)
            {
                Program.UpdateAction = UpdateBehaviour.Never;
            }
            else
            {
                Program.UpdateAction = UpdateBehaviour.Ask;
            }

            Program.ThemeFile = comboBox1.Text;

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
            Process.Start("https://kd3n1z.com/index.php?app=ProcessInfo");
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

        private void saveThemeButton_Click(object sender, EventArgs e)
        {
            if(File.Exists(Path.Combine(themes, comboBox1.Text + ".pit")))
            {
#if DEBUG
                if (false)
#else
                if(File.ReadAllText(Path.Combine(themes, comboBox1.Text + ".pit")).StartsWith("onlyread"))
#endif
                {
                    MessageBox.Show("you can't edit this theme, enter another name to save it", "ProcessInfo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                + textCB.BackColor.ToArgb().ToString() + "\n"
                + backCB.BackColor.ToArgb().ToString() + "\n"
                + dbackCB.BackColor.ToArgb().ToString() + "\n"
                + selCB.BackColor.ToArgb().ToString() + "\n"
                );
        }


        bool loaded = false;
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loaded)
            {
                LoadPIT(comboBox1.Text);
            }
        }

        void LoadPIT(string pitName)
        {
            string[] vals = File.ReadAllText(Path.Combine(themes, pitName + ".pit")).Split('\n');
            textCB.BackColor = Program.ForeColor = Color.FromArgb(int.Parse(vals[1]));
            backCB.BackColor = Program.BackColor =  Color.FromArgb(int.Parse(vals[2]));
            dbackCB.BackColor = Program.DarkBackColor = Color.FromArgb(int.Parse(vals[3]));
            try
            {
                selCB.BackColor = Program.SelColor = Color.FromArgb(int.Parse(vals[4]));
            }
            catch { }


            Program.mainForm.LoadTheme();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", "/select,\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label6.Text = "Radius: " + trackBar1.Value + "px";

            Program.Radius = trackBar1.Value;

            Program.mainForm.LoadTheme();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Program.Shadow = checkBox2.Checked;

            restartRequired = true;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            string val = trackBar2.Value.ToString();

            if(val == "21")
            {
                val = "never";
            }
            else
            {
                val += "s";
            }

            label8.Text = "Auto-Update rate: " + val;

            Program.AutoUpdateRate = trackBar2.Value;
        }
    }
}
