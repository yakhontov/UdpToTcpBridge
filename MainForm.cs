using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UdpToTcp
{
    public partial class MainForm : Form
    {
        TCPAutoConClient TcpClient = new TCPAutoConClient();

        public MainForm()
        {
            InitializeComponent();
        }

        private void SimTranslator_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                Hide();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void SimTranslator_Shown(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        }

        private void SimTranslator_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private UdpClient UdpConnection;
        private void applyButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (UdpConnection != null)
                    UdpConnection.Close();
                UdpConnection = new UdpClient(int.Parse(udpPort.Text));
                UdpConnection.BeginReceive(new AsyncCallback(GotUdpData), UdpConnection);
                pictureBox1.Image = Properties.Resources.connectedgreen32;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "UDP Server error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UdpConnection = null;
                pictureBox1.Image = Properties.Resources.disconnectedred32;
            }

            try
            {
                TcpClient.SetHostPort(tcpHost.Text, int.Parse(tcpPort.Text));
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "TCP Client error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        int PacketCounter = 0;
        public void GotUdpData(IAsyncResult ar)
        {
            UdpClient c = (UdpClient)ar.AsyncState;
            if(c.Client != null)
            {
                try
                {
                    IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    TcpClient.Send(c.EndReceive(ar, ref receivedIpEndPoint));
                    PacketCounter++;
                }
                catch {}
                if(c.Client != null)
                    c.BeginReceive(new AsyncCallback(GotUdpData), ar.AsyncState);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label9.Text = PacketCounter.ToString();
            PacketCounter = 0;
            if (TcpClient.IsConnected())
                pictureBox2.Image = Properties.Resources.connectedgreen32;
            else
                pictureBox2.Image = Properties.Resources.disconnectedred32;
        }

        private void SimTranslator_Load(object sender, EventArgs e)
        {
            applyButton_Click(null, null);
        }
    }
}
