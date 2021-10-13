using AForge.Video;
using AForge.Video.DirectShow;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;

namespace RRCDominoTicketSystem
{
    public partial class Form1 : Form
    {

        // Instance for capture video from camera
        private FilterInfoCollection CaptureDevice;
        // Instance for lastest camera frame
        private VideoCaptureDevice FinalFrame;
        private string code;

        public delegate void updateDataDelegate(bool onlyRead);
        public updateDataDelegate updateData;
        void updateDataMethod(bool onlyRead) 
        {

            string json = WebReader.Read(code, onlyRead);
            JObject obj = JObject.Parse(json);

            // Bad password
            if(((int)obj["status"]) == 401)
            {
                label10.BackColor = Color.Red;
                label10.Text = "Nejste opravnen nahlizet na listky";
            }
            else if (((int)obj["status"]) == 402)
            {
                label10.BackColor = Color.Red;
                label10.Text = "Spatny pocet zadanych parametru";
            }
            else if (((int)obj["status"]) == 404)
            {
                label10.BackColor = Color.Red;
                label10.Text = "Neexistujici listek - neexistuje v databazi / spatne nacten";
            }
            else if (((int)obj["status"]) == 202)
            {

                if (((string)obj["ticket_person"]) == null)
                {
                    label10.BackColor = Color.Red;
                    label10.Text = "Vstupenka neexistuje";
                    MessageBox.Show("Bohuzel tato vstupenka neexistuje.\nCode: " + code);
                    return;
                }

                if (((string)obj["ticket_canceled"]) == null)
                {
                    if (((string)obj["ticket_validate"]) == null)
                    {
                        label10.BackColor = Color.Lime;
                        label10.Text = "Listek pouzit";

                    }
                    else
                    {
                        label10.BackColor = Color.Red;
                        label10.Text = "Listek jiz pouzit";
                        textBox3.Text = ((DateTime)obj["ticket_validate"]).ToString("dd.MM.yyyy HH:mm:ss");
                    }
                }
                else
                {
                    label10.BackColor = Color.Red;
                    label10.Text = "Listek byl zrusen";
                    textBox4.Text = ((DateTime)obj["ticket_canceled"]).ToString("dd.MM.yyyy HH:mm:ss");
                }

                switch ((int)obj["ticket_type"])
                {
                    case 1:
                        textBox5.Text = "Detsky listek do 10 let (online)";
                        break;
                    case 2:
                        textBox5.Text = "Listek pro dospeleho (online)";
                        break;
                    case 3:
                        textBox5.Text = "Detsky listek do 10 let (na miste)";
                        break;
                    case 4:
                        textBox5.Text = "Listek pro dospeleho (na miste)";
                        break;
                    default:
                        textBox5.Text = "Something wen't wrong!";
                        break;
                }
                textBox6.Text = (string)obj["ticket_person"];

            }

        }

        public Form1()
        {
            InitializeComponent();

            updateData = new updateDataDelegate(updateDataMethod);

            CaptureDevice = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            FinalFrame = null;
            listBox1.Items.Clear();
            foreach (FilterInfo fi in CaptureDevice)
            {
                listBox1.Items.Add(fi.Name);
            }
            if (listBox1.Items.Count == 0)
            {
                button3.Text = "Kamera nenalezena";
                button3.Enabled = false;
            }
            else
                listBox1.SetSelected(0, true);
            label6.Text = "Cekam...";

        }

        private void FinalFrame_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {

            pictureBox2.Image = new Bitmap((Bitmap)eventArgs.Frame.Clone());
            try
            {
                BarcodeReader reader = new BarcodeReader { AutoRotate = true };
                Result result = reader.Decode(new Bitmap((Bitmap)eventArgs.Frame.Clone()));
                string decoded = result.ToString().Trim();

                Debug.WriteLine(decoded);
                //capture a snapshot if there is a match
                if (FinalFrame.IsRunning == true)
                {
                    exitcamera();
                }
                code = decoded;
                label5.Text = "QR Kod nalezen";
                updateData(checkBox1.Checked); 
                button3.Text = "Skenovat";

            }
            catch (Exception ex)
            {
                label5.Text = "QR Kod nerozpoznan";
            }
        }

        private void exitcamera()
        {
            FinalFrame.SignalToStop();
            // FinalVideo.WaitForStop();  << marking out that one solved it
            FinalFrame.NewFrame -= new NewFrameEventHandler(FinalFrame_NewFrame); // as sugested
            FinalFrame = null;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (FinalFrame != null)
            {
                if (FinalFrame.IsRunning)
                {
                    exitcamera();
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (button3.Text.Equals("Skenovat"))
            {
                button3.Text = "Stop sken";
                label5.Text = "Nacitam QR kod...";
                textBox3.Text = textBox4.Text = textBox5.Text = textBox6.Text = "";
                label10.BackColor = Color.FromArgb(44, 44, 44);
                label10.Text = "cekam...";
                FinalFrame = new VideoCaptureDevice(CaptureDevice[listBox1.SelectedIndex].MonikerString);
                FinalFrame.NewFrame += new NewFrameEventHandler(FinalFrame_NewFrame);
                FinalFrame.Start();
            }
            else if(button3.Text.Equals("Stop sken"))
            {
                exitcamera();
                button3.Text = "Skenovat";
                label10.BackColor = Color.FromArgb(44,44,44);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if(!textBox1.Text.Equals("") && !textBox2.Text.Equals(""))
            {
                textBox3.Text = textBox4.Text = textBox5.Text = textBox6.Text = "";
                label6.Text = "Nacitam...";
                code = textBox1.Text + "-" + textBox2.Text;
                updateData(checkBox2.Checked);
                label6.Text = "Nacteno";
            }
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://rnr.sokoljinonice.cz/novinky.php");
        }
    }
}
