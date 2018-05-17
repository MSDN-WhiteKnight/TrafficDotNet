using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using TrafficLib;

namespace Traffic
{
    public partial class Form1 : Form
    {
        INetworkEvents session;
        NetworkEvent packet;
        NetworkCounter counter;

        INetworkEvents session2;
        NetworkCounter counter2;

        public Form1()
        {
            InitializeComponent();

            //MessageBox.Show(System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntPtr)).ToString());

            var ifs = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var x in ifs)
            {
                if (x.GetIPProperties() == null) continue;
                if (x.GetIPProperties().UnicastAddresses == null) continue;

                foreach (var y in x.GetIPProperties().UnicastAddresses)
                {
                    if (y.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    comboBox1.Items.Add(y.Address.ToString());
                    break;
                }     
            }

            if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;

            
            
        }
                
        private void button1_Click_1(object sender, EventArgs e)
        {
            textBox1.Text = "";
            listBox1.Items.Clear();

            var packets = session.Events;
            foreach (var p in packets)
            {
                listBox1.Items.Add(p.ToString());
            }           
            
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (session != null)
            {
                session.NewEvent -= OnNewPacket;
                session.End();
            }

            
            var ses = new Ip4CaptureSession(IPAddress.Parse(comboBox1.SelectedItem.ToString()));
            ses.NewEvent += OnNewPacket;
            ses.Start();
            this.session = ses;
        }

        private void btnEnd_Click(object sender, EventArgs e)
        {
            session.NewEvent -= OnNewPacket;
            session.End();

            if (session2 != null)
            {
                session2.NewEvent -= OnNewPacket;
                session2.End();
                session2 = null;
                counter2 = null;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0) return;
            packet = session.GetEvent((uint)listBox1.SelectedIndex);
            propertyGrid1.SelectedObject = packet;

            if (packet is Ip4Packet)
            {

                if ((packet as Ip4Packet).Data != null)
                {
                    byte[] data = (packet as Ip4Packet).Data;
                    StringBuilder sb = new StringBuilder(500);

                    foreach (byte b in data)
                    {
                        if ((b >= 32 && b <= 123) || b == 10 || b == 13)
                        {
                            char c = (char)b;
                            sb.Append(c);
                        }
                    }
                    textBox1.Text = sb.ToString();
                }
                else
                    textBox1.Text = packet.ErrorData.ToString();
            }
        }

        void OnNewPacket(object sender, NetworkEvent e)
        {
            this.label1.BeginInvoke((MethodInvoker)(
                delegate {

                    if (this.counter != null)
                    {
                        this.label1.Text = "Recv: " + this.counter.RecvBytes.ToString() +
                            " Send: " + this.counter.SentBytes.ToString()+
                            " Total: " + this.counter.TotalBytes.ToString();

                    }
                    else this.label1.Text = e.TotalLength.ToString();

                    if (this.counter2 != null)
                    {
                        this.label2.Text = "Recv: " + this.counter2.RecvBytes.ToString() +
                            " Send: " + this.counter2.SentBytes.ToString() +
                            " Total: " + this.counter2.TotalBytes.ToString();
                    }
                    else label2.Text = "";

                    /*this.label1.Text += "."; 
                    if(this.label1.Text.Length > 10)this.label1.Text=".";*/
                    
                }));
        }

        private void btnStartAll_Click(object sender, EventArgs e)
        {
            var st = new NetworkStats();
            st.NewEvent += this.OnNewPacket;

            //this.counter = new SpecificAddressCounter(IPAddress.Parse("89.108.91.180"));
            this.counter = new AllTrafficCounter();
            st.AddCounter(this.counter);

            st.Start();
            this.session = st;

            //********
            var events = NetworkStats.TransportLayerStats;
            events.NewEvent += this.OnNewPacket;
                        
            this.counter2 = new AllTrafficCounter();
            //this.counter2 = new SpecificAddressCounter(IPAddress.Parse("89.108.91.180"));
            events.AddCounter(this.counter2);
            events.Start();
            this.session2 = events;
        }

        private void btnTransport_Click(object sender, EventArgs e)
        {
            var events = NetworkStats.TransportLayerStats;
            events.NewEvent += this.OnNewPacket;

            //this.counter = new SpecificAddressCounter(IPAddress.Parse("89.108.91.180"));
            this.counter = new AllTrafficCounter();
            events.AddCounter(this.counter);  
            ;

            events.Start();
            this.session = events;
        }
    }
}
