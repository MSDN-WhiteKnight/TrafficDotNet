using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
/* Project: TrafficDotNet library 
 * Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) */

namespace TrafficLib
{
    /// <summary>
    /// The direction of network traffic relative to the node being analyzed
    /// </summary>
    public enum TrafficDirections
    {
        Unknown = 0,
        Send = 1,
        Recv = 2
    }

    /// <summary>
    /// Represents the act of data transfer over the network. This is the base class which specific protocol events derive from.
    /// </summary>
    public abstract class NetworkEvent : EventArgs
    {        
        /// <summary>
        /// Total length of the data transferred during this event (bytes)
        /// </summary>
        protected uint _TotalLen = 0;

        /// <summary>
        /// Source network address
        /// </summary>
        protected IPAddress _Src = IPAddress.Any;

        /// <summary>
        /// Destination network address
        /// </summary>
        protected IPAddress _Dst = IPAddress.Any;

        /// <summary>
        /// Transport protocol used
        /// </summary>
        protected TransportProtocols _Proto;

        /// <summary>
        /// Was traffic sent or received
        /// </summary>
        protected TrafficDirections _Direction;

        /// <summary>
        /// Error data associated with this event (null if no error occured)
        /// </summary>
        protected Exception _ErrorData = null;

        /// <summary>
        /// Time when this event occured
        /// </summary>
        protected DateTime _Timestamp;

        //Public properties

        /// <summary>
        /// Transport protocol used in this network event
        /// </summary>
        public TransportProtocols TransportProtocol { get { return _Proto; } }

        /// <summary>
        /// Total length of data transferred during this event, in bytes
        /// </summary>
        public uint TotalLength { get { return _TotalLen; } }

        /// <summary>
        /// Source network address
        /// </summary>
        public IPAddress SourceIp { get { return _Src; } }

        /// <summary>
        /// Destination IP address
        /// </summary>
        public IPAddress DestinationIp { get { return _Dst; } }     
   
        /// <summary>
        /// Specifies whether data was sent or received by network node being analyzed during this event
        /// </summary>
        public TrafficDirections Direction { get { return _Direction; } }

        /// <summary>
        /// Error data associated with this event (null if no error occured)
        /// </summary>
        public Exception ErrorData { get { return this._ErrorData; } }

        /// <summary>
        /// Time when this event occured
        /// </summary>
        public DateTime Timestamp { get { return _Timestamp; } }

        /**** Methods *****/
        
        /// <summary>
        /// Returns text representation of this NetworkEvent object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(500);

            if (this._ErrorData == null)
            {
                sb.AppendFormat(
                    "{0} | Network Event | Size: {1} bytes | Source: {2} | Destination: {3}\r\n",
                    this._Timestamp, this._TotalLen, this._Src, this._Dst
                    );
            }
            else
            {
                sb.AppendLine(this._Timestamp.ToString() +
                    " | Network error: " + this._ErrorData.Message);
            }

            return sb.ToString();
        }


    }
}
