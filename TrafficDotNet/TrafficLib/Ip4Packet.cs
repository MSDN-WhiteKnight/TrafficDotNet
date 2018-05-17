using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;
/* Project: TrafficDotNet library 
 * Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) */

namespace TrafficLib
{
    /// <summary>
    /// Represents transport protocols, with their codes as defined by IP specification
    /// </summary>
    public enum TransportProtocols : byte
    {
        ICMP = 1,
        IGMP = 2,
        TCP = 6,
        EGP = 8,
        IGP = 9,
        UDP = 17
    }    

    /// <summary>
    /// Represents IPv4 packet (datagram). If ErrorData is not null it represents capture error, 
    /// otherwise it's an actual captured packet
    /// </summary>
    public class Ip4Packet : IpPacket
    {        
        protected byte _Ttl;        
        
        public Ip4Packet()
        {
            
        }

        /// <summary>
        /// Creates new Ipv4Packet object
        /// </summary>
        /// <param name="time">Time when this packet was captured</param>
        /// <param name="data">Raw data associated with this packet</param>
        /// <param name="size">Total size of this packet</param>
        /// <param name="this_ip">IP address of interface on which this packet was captured</param>
        public Ip4Packet(DateTime time, byte[] data, uint size, IPAddress this_ip)
        {
            if (data == null)
                throw new ArgumentNullException("'data' parameter can't be null");

            if (size < 20)
                throw new ArgumentOutOfRangeException("Packet size can't be less then 20");

            if (size > 65535)
                throw new ArgumentOutOfRangeException("Packet size can't be greater then 65535");

            if (data.Length < size)
                throw new ArgumentException("Array does not have specified number of bytes");

            this._ErrorData = null;
            this._Timestamp = time;
            this._IfIp = this_ip;

            try
            {
                this._RawData = new byte[size];
                Array.Copy(data, this._RawData, size);

                ushort dummy;
                byte b;

                MemoryStream ms = new MemoryStream(this._RawData);
                BinaryReader br = new BinaryReader(ms);

                //Parse packet header
                using (ms)
                using (br)
                {
                    b = br.ReadByte();
                    this._Ver = (byte)((b & (byte)0xF0) >> 4);
                    byte ihl = (byte)(b & (byte)0x0F);//header len
                    this._HeaderLen = ihl * 4u;

                    b = br.ReadByte();

                    //packet length
                    byte[] temp = new byte[2];
                    temp[1] = br.ReadByte();
                    temp[0] = br.ReadByte();
                    this._TotalLen = BitConverter.ToUInt16(temp, 0);

                    dummy = br.ReadUInt16();
                    dummy = br.ReadUInt16();

                    this._Ttl = br.ReadByte();
                    this._Proto = (TransportProtocols)br.ReadByte();

                    dummy = br.ReadUInt16();

                    //source IP
                    temp = new byte[4];
                    temp[0] = br.ReadByte();
                    temp[1] = br.ReadByte();
                    temp[2] = br.ReadByte();
                    temp[3] = br.ReadByte();
                    this._Src = new IPAddress(temp);

                    //destination IP
                    temp[0] = br.ReadByte();
                    temp[1] = br.ReadByte();
                    temp[2] = br.ReadByte();
                    temp[3] = br.ReadByte();
                    this._Dst = new IPAddress(temp);

                    if (this._Src.Equals(this_ip))
                        this._Direction = TrafficDirections.Send;
                    else if (this._Dst.Equals(this_ip))
                        this._Direction = TrafficDirections.Recv;

                }
                
            }
            catch (Exception ex)
            {
                this._RawData = null;
                this._HeaderLen = 0;
                this._TotalLen = 0;
                this._Ver = 0;
                this._ErrorData = ex;
            }
        }

        /// <summary>
        /// Creates new Ipv4 packet object that represents capture error rather then actual data
        /// </summary>
        /// <param name="time"></param>
        /// <param name="error"></param>
        public Ip4Packet(DateTime time, Exception error)
        {
            if(error == null)
                throw new ArgumentNullException("'error' parameter can't be null");

            this._RawData = null;
            this._HeaderLen = 0;
            this._TotalLen = 0;
            this._Ver = 0;
            this._Timestamp = time;
            this._ErrorData = error;
        }

        //Public properties
        
        /// <summary>
        /// Returns the value the specifies maximum allowed amount of hops until this packet would be dropped by IPv4 routing.
        /// </summary>
        public byte Ttl { get { return _Ttl; } }   

        /// <summary>
        /// Returns textual representation of this IPv4 packet
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(500);
            
            if (this._RawData != null)
            {
                sb.AppendFormat(
                    "{0} | IPv4 Packet | Length: {1} bytes | Source: {2} | Destination: {3}\r\n",
                    this._Timestamp, this._TotalLen, this._Src, this._Dst
                    );
            }

            if (this._ErrorData != null)
            {
                sb.AppendLine(this._Timestamp.ToString() +
                    " | IPv4 capture error: " + this._ErrorData.Message);
            }

            return sb.ToString();
        }


    }
}
