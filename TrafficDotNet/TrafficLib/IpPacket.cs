using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
/* Project: TrafficDotNet library 
 * Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) */

namespace TrafficLib
{

    /// <summary>
    /// Represents Internet Protocol network packet (datagram) or capture error.
    /// This is the base class from which version-specific implementations are derived
    /// </summary>
    public abstract class IpPacket : NetworkEvent
    {
        /// <summary>
        /// Raw binary data associated with this packet. Null value if this object represents capture error.
        /// </summary>
        protected byte[] _RawData = null;
                
        protected byte _Ver; 
        protected uint _HeaderLen = 0;
        protected IPAddress _IfIp = IPAddress.None; //IP address of an interface on which this packet was captured
               
        //Public properties

        /// <summary>
        /// IP protocol version (such as, 4 for IPv4)
        /// </summary>
        public byte Ver { get { return _Ver; } }

        /// <summary>
        /// The length of datagram header, in bytes
        /// </summary>
        public uint HeaderLength { get { return _HeaderLen; } }

        /// <summary>
        /// IP address of an interface on which this packet was captured
        /// </summary>
        public IPAddress InterfaceIp { get { return _IfIp; } }
        
        /// <summary>
        /// Binary data of this datagram's header
        /// </summary>
        public byte[] Header
        {
            get
            {
                if (_RawData == null) return null;

                byte[] res = new byte[this._HeaderLen];
                Array.Copy(this._RawData, res, this._HeaderLen);
                return res;
            }
        }

        /// <summary>
        /// Binary data of this datagram's user content (everything besides header)
        /// </summary>
        public byte[] Data
        {
            get
            {
                if (_RawData == null) return null;

                uint size = this._TotalLen - this._HeaderLen;
                byte[] res = new byte[size];
                Array.Copy(this._RawData, (int)this._HeaderLen, res, 0, size);
                return res;
            }
        }

        /// <summary>
        /// Returns textual representation of this IP packet
        /// </summary>        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(500);

            if (this._RawData != null)
            {
                sb.AppendFormat(
                    "{0} | IP Packet | Length: {1} bytes | Source: {2} | Destination: {3}\r\n",
                    this._Timestamp, this._TotalLen, this._Src, this._Dst
                    );
            }

            if (this._ErrorData != null)
            {
                sb.AppendLine(this._Timestamp.ToString() +
                    " | Capture error: " + this._ErrorData.Message);
            }

            return sb.ToString();
        }


    }
}
