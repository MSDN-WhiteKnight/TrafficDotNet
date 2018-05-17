using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using EtwNetwork;

namespace TrafficLib
{
    public class TransportLayerEvents : INetworkEvents
    {
        protected static TransportLayerEvents _Instance;
        
        protected object _Sync = new object();        
        protected Thread _Thread;
        protected List<NetworkEvent> _Events;
        protected DateTime _StartTime;
        protected DateTime _EndTime;

        public event EventHandler<NetworkEvent> NewEvent;

        protected void Initialize()
        {
            this.MaxEvents = 2500;            
        }

        protected TransportLayerEvents()
        {
            this.MaxEvents = 2500;             
        }      


        //Public properties    

        public static TransportLayerEvents Instance
        {
            get
            {
                if (_Instance == null) _Instance = new TransportLayerEvents();
                return _Instance;
            }
        }

        public uint MaxEvents { get; set; }        
        public DateTime StartTime { get { return this._StartTime; } }

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

        public TimeSpan ElapsedTime
        {
            get
            {
                if (IsRunning) return DateTime.Now.Subtract(_StartTime);
                else return _EndTime.Subtract(_StartTime);
            }
        }

        

        //***************************************

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

        protected void EventHandler(object sender, EtwEvent e)
        {
            NetworkEvent ev = new TransportLayerEvent(e);

            lock (_Sync)
            {
                if (_Events.Count > this.MaxEvents) _Events.RemoveAt(0);
                _Events.Add(ev);
                this.OnNewEvent(ev);
            }
        }

        protected void Listen()
        {
            EtwSession.NewEvent += this.EventHandler;
            EtwSession.Start();
            System.Diagnostics.Debug.WriteLine("Tracing session ended");
        }

        public void Start()
        {
            if (IsRunning) return;

            lock (_Sync)
            {
                if (this.MaxEvents == 0) this.MaxEvents = 100;
                this._Events = new List<NetworkEvent>((int)MaxEvents);
            }

            
            try
            {               

                this._EndTime = DateTime.MinValue;
                this._StartTime = DateTime.Now;
                this._Thread = new Thread(Listen);
                this._Thread.IsBackground = true;
                this._Thread.Start();
            }
            catch (Exception)
            {                
                this._Thread = null;
                throw;
            }
        }

        public void End()
        {
            this._EndTime = DateTime.Now;
            EtwSession.NewEvent -= this.EventHandler;
            EtwSession.Stop();
            this._Thread = null;
        }

        //*** INetworkEvents ***
        public DateTime EndTime
        {
            get
            {
                if (IsRunning) return DateTime.Now;
                else return _EndTime;
            }
        }

        public IEnumerable<NetworkEvent> Events
        {
            get
            {
                lock (_Sync)
                {
                    if (this._Events == null) return new List<NetworkEvent>();
                    List<NetworkEvent> res = new List<NetworkEvent>(this._Events.Count);

                    foreach (var x in this._Events)
                    {
                        res.Add(x);
                    }

                    return res;
                }
            }
        }

        public uint EventsCount
        {
            get
            {
                lock (_Sync)
                {
                    if (this._Events == null) return 0;
                    return (uint)this._Events.Count;
                }
            }
        }

        public NetworkEvent GetEvent(uint n)
        {
            lock (_Sync)
            {
                if (this._Events == null) return null;
                return this._Events[(int)n];
            }
        }

        //*********************************************
    }
}

