using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
/* Project: TrafficDotNet library 
 * Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) */

namespace TrafficLib
{
    /// <summary>
    /// Aggregates network events from multiple event sources and performs traffic measuring by means of NetworkCounter objects
    /// </summary>
    public class NetworkStats : INetworkEvents
    {
        protected static NetworkStats _TransportLayerStats; //NetworkStats singleton instance for transport layer
        protected object _Sync = new object(); //object for thread syncronization
        protected List<NetworkEvent> _Events; //collection of events stored in this instance
        protected bool _Running = false; 
        protected DateTime _StartTime;
        protected DateTime _EndTime;

        /// <summary>
        /// collection of event source that this object aggregates data from
        /// </summary>
        protected List<INetworkEvents> _EventSources = new List<INetworkEvents>(30); 

        /// <summary>
        /// Collection of network counters that this object uses to measure traffic
        /// </summary>
        protected List<NetworkCounter> _Counters = new List<NetworkCounter>(30);

        
        /// <summary>
        /// Gets IP addresses of all local IPv4 interfaces
        /// </summary>        
        public static List<IPAddress> GetLocalAddresses()
        {
            List<IPAddress> res = new List<IPAddress>(10);

            var ifs = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var x in ifs)
            {
                if (x.GetIPProperties() == null) continue;
                var uni = x.GetIPProperties().UnicastAddresses;
                if (uni == null) continue;

                foreach (var y in uni)
                {
                    if (y.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (!res.Contains(y.Address)) res.Add(y.Address);                    
                }
            }

            return res;
        }

        /// <summary>
        /// Creates new NetworkStats object that aggregates data from all local network interfaces
        /// (Currently only supports Ipv4 interfaces)
        /// </summary>
        public NetworkStats()
        {
            this.MaxEvents = 2500;
            
            Ip4CaptureSession session;
            List<IPAddress> ips = GetLocalAddresses();

            lock (_Sync)
            {
                foreach (var ip in ips)
                {
                    session = new Ip4CaptureSession(ip);
                    session.NewEvent += this.EventHandler;
                    _EventSources.Add(session);                    
                }
            }
        }

        /// <summary>
        /// Creates new NetworkStats object that aggregates data from specified event sources
        /// </summary>
        /// <param name="sources"></param>
        public NetworkStats(IEnumerable<INetworkEvents> sources)
        {
            this.MaxEvents = 2500;                       

            lock (_Sync)
            {
                foreach (var src in sources)
                {                    
                    src.NewEvent += this.EventHandler;
                    _EventSources.Add(src);                    
                }
            }
        }        

        /// <summary>
        /// Returns NetworkStats singleton object that is used to analyze and measure transport layer traffic
        /// </summary>
        public static NetworkStats TransportLayerStats
        {
            get
            {
                if (_TransportLayerStats == null)
                {
                    List<INetworkEvents> list = new List<INetworkEvents>(1);
                    list.Add(TransportLayerEvents.Instance);
                    _TransportLayerStats = new NetworkStats(list);
                }
                return _TransportLayerStats;
            }
        }

        /// <summary>
        /// Starts tracing events on all event sources used by this object
        /// </summary>
        public void Start()
        {            
            lock (_Sync)
            {
                if (_Running) return;
                this._Events = new List<NetworkEvent>((int)this.MaxEvents);

                foreach (var sess in this._EventSources)
                {
                    sess.Start();
                }
                _Running = true;
            }
        }

        /// <summary>
        /// Ends the process of tracing events on all event sources used by this object
        /// </summary>
        public void End()
        {
            lock (_Sync)
            {
                if (!this._Running) return;
                foreach (var sess in this._EventSources)
                {
                    sess.End();
                }
                _Running = false;
            }
        }

        /// <summary>
        /// Raised when new network event occurs on any event source associated with this object
        /// </summary>
        public event EventHandler<NetworkEvent> NewEvent;

        /// <summary>
        /// Specifies the maximum amount of events stored in this object
        /// </summary>
        public uint MaxEvents { get; set; }

        /// <summary>
        /// Specifies when the process of tracing events was started. Undefined if IsRunning == false
        /// </summary>
        public DateTime StartTime
        {
            get { return this._StartTime; }
        }

        /// <summary>
        /// Specifies whether the process of tracing events is currently running
        /// </summary>
        public bool IsRunning { get { lock (_Sync) { return _Running; } } }
                
        public uint CountersCount { get { lock (_Sync) { return (uint)_Counters.Count; } } }
        
        /// <summary>
        /// Specifies when the process of tracing events ended. Undefined if IsRunning == true
        /// </summary>
        public DateTime EndTime
        {
            get
            {
                if (_Running) return DateTime.Now;
                else return _EndTime;
            }
        }

        /// <summary>
        /// Specifies how long this object was tracing network events since the last Start() call
        /// </summary>
        public TimeSpan ElapsedTime
        {
            get
            {
                if (_Running) return DateTime.Now.Subtract(_StartTime);
                else return _EndTime.Subtract(_StartTime);
            }
        }

        /// <summary>
        /// Returns the collection of all network events stored in this object
        /// </summary>
        public IEnumerable<NetworkEvent> Events
        {
            get
            {
                lock (_Sync)
                {
                    if (this._Events == null) return new List<NetworkEvent>();
                    List<NetworkEvent> res = new List<NetworkEvent>(_Events.Count);

                    foreach (var x in this._Events)
                    {
                        res.Add(x);
                    }

                    return res;
                }
            }
        }

        /// <summary>
        /// Specifies the amount of network events stored in this object
        /// </summary>
        public uint EventsCount
        {
            get
            {
                lock (_Sync)
                {
                    if (this._Events == null) return 0;
                    else return (uint)this._Events.Count;
                }
            }
        }
               
        /// <summary>
        /// Gets NetworkEvent with specified index. If "n" is out of range, an exception will be thrown
        /// </summary>        
        public NetworkEvent GetEvent(uint n)
        {
            lock (_Sync)
            {
                if (this._Events == null) return null;
                else return this._Events[(int)n];
            }
        }

        /// <summary>
        /// Called when new event is generated by underlying event source
        /// </summary>        
        protected void EventHandler(object sender, NetworkEvent e)
        {
            lock (_Sync)
            {
                //store event
                if (_Events.Count > this.MaxEvents) _Events.RemoveAt(0);
                this._Events.Add(e);

                //pass event to each of the NetworkCounter objects
                foreach (var counter in this._Counters)
                {
                    counter.ProcessEvent(e);
                }
            }
            OnNewEvent(e); //raise the event on this object
        }

        /// <summary>
        /// Raises "NewEvent" event
        /// </summary>        
        protected void OnNewEvent(NetworkEvent e)
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
        /// Adds specified NetworkCounter into this object, so it will be used to measure traffic subsequently
        /// </summary>        
        public void AddCounter(NetworkCounter c)
        {
            if (c == null) throw new ArgumentNullException("NetworkCounter must not be null");

            lock (_Sync)
            {
                if (this._Counters.Contains(c)) return;
                this._Counters.Add(c);
            }
        }

        /// <summary>
        /// Removes specified NetworkCounter from this object, so it will no longer measure traffic
        /// </summary>        
        public void RemoveCounter(NetworkCounter c)
        {
            if (c == null) throw new ArgumentNullException("NetworkCounter must not be null");

            lock (_Sync)
            {
                if (!this._Counters.Contains(c)) return;
                this._Counters.Remove(c);
            }
        }

        /// <summary>
        /// Removes all NetworkCounters from this object
        /// </summary>
        public void RemoveAllCounters()
        {
            lock (_Sync) { this._Counters.Clear(); }
        }

        

    }
}
