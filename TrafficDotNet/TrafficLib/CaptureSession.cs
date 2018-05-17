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
    /// Represents the network-layer events source. This is the base class from which specific protocol implementations derive.
    /// </summary>
    public abstract class CaptureSession : INetworkEvents
    {
        
        protected object _Sync = new object();//object for thread syncronization
        protected Socket _Sock = null;//Socket object used to capture network packets
        protected IPAddress _IfIp;//Network address of interface on which network packets are captured
        protected Thread _Thread; //capture worker thread
        protected List<IpPacket> _Packets;//the collection of captured packets
        protected DateTime _StartTime; //time when capture started
        protected DateTime _EndTime; //time when cappture ended

        /// <summary>
        /// Event raised when new network packed was captured on interface. 
        /// Cast NetworkEvent into protocol-specific type to obtain packet data.
        /// </summary>
        public event EventHandler<NetworkEvent> NewEvent;

        /// <summary>
        /// Sets default values for properties
        /// </summary>
        /// <param name="ifip">IP address of the interface on which network packets are captured</param>
        protected void Initialize(IPAddress ifip)
        {
            this.BufferSize = 4096;
            this.MaxPackets = 2500;
            this._IfIp = ifip;
        }

        //Public properties

        /// <summary>
        /// Size of the buffer used to store captured packets. If packet size exceeds this value, an error is generated.
        /// Set this value before calling Start(), otherwise it will have no effect on the currently running capture worker thread.
        /// </summary>
        public uint BufferSize { get; set; }

        /// <summary>
        /// Maximum amount of packets stored in this objects. 
        /// When the amount of packets exceeds this value, older ones are deleted.
        /// </summary>
        public uint MaxPackets { get; set; }

        /// <summary>
        /// IP address of an interface on which packets are captured
        /// </summary>
        public IPAddress IfIp { get { return _IfIp; } }

        /// <summary>
        /// Time when the capture process was started (value is undefined if IsRunning == false).
        /// </summary>
        public DateTime StartTime { get { return this._StartTime; } }

        /// <summary>
        /// Specifies whether the current instance is actually capturing data now.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                if (this._Thread == null) return false;

                if (this._Thread.ThreadState == ThreadState.Unstarted ||
                    this._Thread.ThreadState == ThreadState.Stopped)
                    return false;
                else return true;
            }
        }

        /// <summary>
        /// Specifies how long this object was capturing data since the last Start() call
        /// </summary>
        public TimeSpan ElapsedTime
        {
            get
            {
                if (IsRunning) return DateTime.Now.Subtract(_StartTime);
                else return _EndTime.Subtract(_StartTime);
            }
        }

        /// <summary>
        /// Returns the collection of all captured packets currently stored in this object
        /// </summary>
        public List<IpPacket> Packets
        {
            get
            {
                lock (_Sync)
                {
                    if (this._Packets == null) return new List<IpPacket>();
                    List<IpPacket> res = new List<IpPacket>(_Packets.Count);

                    foreach (var x in this._Packets)
                    {
                        res.Add(x);
                    }

                    return res;
                }
            }
        }

        /// <summary>
        /// Returns the amount of captured packets currently stored in this object
        /// </summary>
        public uint PacketsCount
        {
            get
            {
                lock (_Sync)
                {
                    if (this._Packets == null) return 0;
                    else return (uint)this._Packets.Count;
                }
            }
        }

        //***************************************

        /// <summary>
        /// Raises the "NewEvent" event
        /// </summary>        
        protected void OnNewPacket(NetworkEvent e)
        {
            // Make a temporary copy of the event to avoid possibility of
            // a race condition if the last subscriber unsubscribes
            // immediately after the null check and before the event is raised.
            EventHandler<NetworkEvent> handler = NewEvent;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Abstract method used by the worker thread to capture data. Protocol-specific implementations override it.
        /// </summary>
        protected abstract void Listen();

        /// <summary>
        /// Starts capturing data. Previously stored data will be deleted.
        /// </summary>
        public abstract void Start();

        /// <summary>
        /// Ends the process of capturing data
        /// </summary>
        public void End()
        {
            this._EndTime = DateTime.Now;
            if (this._Sock != null)
            {
                this._Sock.Close();
            }
            this._Thread = null;
        }

        /// <summary>
        /// Returns packet with the specified index. If "n" is out of range, the exception is generated.
        /// </summary>        
        public IpPacket GetPacket(uint n)
        {
            lock (_Sync)
            {
                if (this._Packets == null) return null;
                else return this._Packets[(int)n];
            }
        }

        /// <summary>
        /// Deletes all captured data currently stored in this instance.
        /// </summary>
        public void ClearPackets()
        {
            lock (_Sync)
            {
                if (this._Packets != null) this._Packets.Clear();
            }
        }

        //*** INetworkEvents ***

        /// <summary>
        /// Specifies when the process of capturing ended (value is undefined if IsRunning == true)
        /// </summary>
        public DateTime EndTime
        {
            get
            {
                if (IsRunning) return DateTime.Now;
                else return _EndTime;
            }
        }

        /// <summary>
        /// Returns the collection of NetworkEvent objects stored in this instance
        /// </summary>
        public IEnumerable<NetworkEvent> Events
        {
            get
            {
                lock (_Sync)
                {
                    if (this._Packets == null) return new List<NetworkEvent>();
                    List<NetworkEvent> res = new List<NetworkEvent>(_Packets.Count);

                    foreach (var x in this._Packets)
                    {
                        res.Add((NetworkEvent)x);
                    }

                    return res;
                }
            }
        }

        /// <summary>
        /// Returns the amount of NetworkEvent objects stored in this instance
        /// </summary>
        public uint EventsCount
        {
            get { return this.PacketsCount; }
        }

        /// <summary>
        /// Returns NetworkEvent object with specified index. If index is out of range, an exception is generated.
        /// </summary>        
        public NetworkEvent GetEvent(uint n)
        {
            return (NetworkEvent)this.GetPacket(n);
        }

        //*********************************************
    }
}
