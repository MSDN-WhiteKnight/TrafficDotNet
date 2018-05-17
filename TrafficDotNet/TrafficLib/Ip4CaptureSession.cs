using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
/* Project: TrafficDotNet library 
 * Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) */

namespace TrafficLib
{
    /// <summary>
    /// Captures network packets on specified IPv4 interface
    /// </summary>
    public class Ip4CaptureSession : CaptureSession
    {        
        /// <summary>
        /// Creates new Ip4CaptureSession object for the interface with specified IP address
        /// </summary>
        /// <param name="ifip"></param>
        public Ip4CaptureSession(IPAddress ifip)
        {
            base.Initialize(ifip);            
        }               

        //Public properties       

        /// <summary>
        /// Returns the collection of all captured IPv4 packets stored in this instance
        /// </summary>
        public new List<Ip4Packet> Packets
        {
            get
            {
                lock (_Sync)
                {
                    if (this._Packets == null) return new List<Ip4Packet>();
                    List<Ip4Packet> res = new List<Ip4Packet>(_Packets.Count);

                    foreach (var x in this._Packets)
                    {
                        res.Add((Ip4Packet)x);
                    }

                    return res;
                }
            }
        }        

        //***************************************

        /// <summary>
        /// Called in background thread to capture packets
        /// </summary>
        protected override void Listen()
        {
            if (this.BufferSize == 0) this.BufferSize = 1024;
            

            byte[] buffer = new byte[this.BufferSize];
            int res;
            Ip4Packet packet;
            System.Diagnostics.Debug.WriteLine("Capture thread started");

            while (true)
            {
                try
                {
                    res = _Sock.Receive(buffer); //receive packet in blocking mode

                    packet = new Ip4Packet(DateTime.Now, buffer, (uint)res, this._IfIp);
                }
                catch (ObjectDisposedException)
                {
                    //socket closed - capturing was stopped
                    this._Sock = null;
                    break;
                }
                catch (Exception ex)
                {
                    //error occured
                    packet = new Ip4Packet(DateTime.Now, ex);
                }

                lock (_Sync)
                {
                    //if there're too much packets, remove the oldest one
                    if (_Packets.Count > this.MaxPackets) _Packets.RemoveAt(0);

                    _Packets.Add(packet); //add new packet to the collection
                    this.OnNewPacket(packet); //raise event
                }
            }

            System.Diagnostics.Debug.WriteLine("Capture thread ended");
        }

        /// <summary>
        /// Starts the process of capturing packets, removing all previously stored data
        /// </summary>
        public override void Start()
        {
            if (IsRunning) return; //nothing to do here...

            lock (_Sync)
            {
                if (this.MaxPackets == 0) this.MaxPackets = 100;
                this._Packets = new List<IpPacket>((int)MaxPackets);
            }

            //create new raw IPv4 socket
            _Sock = new Socket(AddressFamily.InterNetwork, SocketType.Raw,
                       ProtocolType.IP);
            try
            {

                //https://www.codeproject.com/Articles/17031/A-Network-Sniffer-in-C
                _Sock.Bind(new IPEndPoint(_IfIp, 0));

                _Sock.SetSocketOption(SocketOptionLevel.IP,  //Applies only to IP packets
                               SocketOptionName.HeaderIncluded, //Set the include header
                               true);                           //option to true

                byte[] byTrue = new byte[4] { 1, 0, 0, 0 };
                byte[] byOut = new byte[4];

                //Socket.IOControl is analogous to the WSAIoctl method of Winsock 2
                _Sock.IOControl(IOControlCode.ReceiveAll,  //SIO_RCVALL of Winsock
                         byTrue, byOut);

                this._EndTime = DateTime.MinValue;
                this._StartTime = DateTime.Now;
                this._Thread = new Thread(Listen);
                this._Thread.IsBackground = true;
                this._Thread.Start();
            }
            catch (Exception)
            {
                _Sock.Dispose();
                _Sock = null;
                this._Thread = null;
                throw;
            }
        }

        /// <summary>
        /// Returns captured IPv4 packet with specified index. If "n" is out of range, the exception is thrown.
        /// </summary>
        public new Ip4Packet GetPacket(uint n)
        {
            lock (_Sync)
            {
                if (this._Packets == null) return null;
                else return (Ip4Packet)this._Packets[(int)n];
            }
        }

    }
}
