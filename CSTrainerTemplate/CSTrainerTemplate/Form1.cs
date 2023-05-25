using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace CSTrainerTemplate
{
    public partial class Form1 : MetroFramework.Forms.MetroForm
    {
        private mem m;
        private bool infAmmo = false;
        private int ammo = 0;
        private IntPtr ammoAddr;
        private bool infClip = false;
        private IntPtr clipAddr;

        public Form1()
        {
            InitializeComponent();
            StyleManager = msm;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            m = new mem("GenerationZero_F");
            bw.RunWorkerAsync();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (infAmmo)
            {
                m.WriteInt32(ammoAddr, ammo);
            }
            if (infClip)
            {
                m.ReplaceBytes(clipAddr, "89 91 7C 02 00 00");
            }
        }

        private void Log(string str)
        {
            // append text to the textbox with cross-threading check
            _ = rtb.BeginInvoke(new Action(() =>
            {
                rtb.AppendText(str + "\n");
            }));
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            if (infAmmo)
            {
                int[] ammoOffsets = {
                0x20,
                0xC68,
                0x148
            };
                IntPtr root = m.baseAddress + 0x2A05320;
                ammoAddr = m.FindDMAAddy(root, ammoOffsets);
                m.WriteInt32(ammoAddr, 999);
            }
            Thread.Sleep(100);
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bw.RunWorkerAsync();
        }

        private void metroToggle1_CheckedChanged(object sender, EventArgs e)
        {
            int[] ammoOffsets = {
            0x20,
            0xC68,
            0x148
        };
            IntPtr root = m.baseAddress + 0x2A05320;
            ammoAddr = m.FindDMAAddy(root, ammoOffsets);

            infAmmo = !infAmmo;
            if (infAmmo)
            {
                ammo = 171; //m.ReadInt32(ammoAddr);
            }
            else
            {
                m.WriteInt32(ammoAddr, ammo);
            }
        }

        private void metroToggle2_CheckedChanged(object sender, EventArgs e)
        {
            if (!infClip)
            {
                clipAddr = m.AOBScan("89 91 7C 02 00 00 48", "FF FF FF FF FF FF FF", 0);
            }
            infClip = !infClip;
            if (infClip)
            {
                m.ReplaceBytes(clipAddr, "90 90 90 90 90 90");
            }
            else
            {
                m.ReplaceBytes(clipAddr, "89 91 7C 02 00 00");
            }
        }
    }
}