using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
/* Project: TrafficDotNet library 
 * Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) */

namespace TrafficLib
{
    /// <summary>
    /// Base class for network counters: objects used to calculate amount of traffic matching certain conditions.
    /// Add network counters into NetworkStats object to perform the trafic measuring
    /// </summary>
    public abstract class NetworkCounter
    {
        protected object _Sync = new object(); //Object for thread syncronization
        protected long _RecvBytes = 0; //Amount of received bytes counted by this object
        protected long _SentBytes = 0; //Amount of sent bytes counted by this object
        protected long _TotalBytes = 0; //Total amount of bytes counted by this object

        /// <summary>
        /// Abstract method that determines whether specified network event matches the condition
        /// </summary>
        /// <param name="e">NetworkEvent object being tested</param>
        /// <returns>true if network event matches the condition, otherwise false</returns>
        public abstract bool EventFilter(NetworkEvent e);

        /// <summary>
        /// Adds new network event into this counter object, incrementing counters if it matches the condition
        /// </summary>
        /// <param name="e"></param>
        public void ProcessEvent(NetworkEvent e)
        {

            if (this.EventFilter(e))
            {
                lock (_Sync)
                { 
                    if (e.Direction == TrafficDirections.Send)
                    {
                        _SentBytes += e.TotalLength;
                        _TotalBytes += e.TotalLength;
                    }
                    else if (e.Direction == TrafficDirections.Recv)
                    {
                        _RecvBytes += e.TotalLength;
                        _TotalBytes += e.TotalLength;
                    }
                }
            }

        }

        /// <summary>
        /// Amount of received bytes counted by this object
        /// </summary>
        public long RecvBytes { get { lock (_Sync) { return _RecvBytes; } } }

        /// <summary>
        /// Amount of sent bytes counted by this object
        /// </summary>
        public long SentBytes { get { lock (_Sync) { return _SentBytes; } } }

        /// <summary>
        /// Total amount of bytes counted by this object
        /// </summary>
        public long TotalBytes { get { lock (_Sync) { return _TotalBytes; } } }
        
    }

    /// <summary>
    /// Represents NetworkCounter that counts all traffic being analysed
    /// </summary>
    public class AllTrafficCounter : NetworkCounter
    {
        public override bool EventFilter(NetworkEvent e)
        {
            return true;
        }
    }

    /// <summary>
    /// Represents NetworkCounter that counts only traffic send to or received from specific IP address
    /// </summary>
    public class SpecificAddressCounter : NetworkCounter
    {
        protected IPAddress _ip; // IP address of node for which the trafic is being counted

        /// <summary>
        /// Creates new SpecificAddressCounter object that counts traffic for specified IP address
        /// </summary>        
        public SpecificAddressCounter(IPAddress ip)
        {
            this._ip = ip;
        }

        public override bool EventFilter(NetworkEvent e)
        {
            if (e.ErrorData != null) return false;

            if (e.SourceIp.Equals(_ip) || e.DestinationIp.Equals(_ip)) return true;
            else return false;
        }
    }
}
