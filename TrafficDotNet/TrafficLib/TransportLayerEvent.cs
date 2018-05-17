using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using EtwNetwork;
/* Project: TrafficDotNet library 
 * Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) */

namespace TrafficLib
{
    /// <summary>
    /// Represents the type of transport layer event
    /// </summary>
    public enum TransportLayerEventTypes:int
    {
        EVENT_TRACE_TYPE_ACCEPT = 15,
        /*Accept event for IPv4 protocol. The TcpIp_TypeGroup2 MOF class defines the event data for this event.*/

        EVENT_TRACE_TYPE_CONNECT = 12,
        /*Connect event for IPv4 protocol. The TcpIp_TypeGroup2 MOF class defines the event data for this event.*/

        EVENT_TRACE_TYPE_DISCONNECT = 13,
        /*Disconnect event for IPv4 protocol. The TcpIp_TypeGroup1 MOF class defines the event data for this event.*/

        EVENT_TRACE_TYPE_RECEIVE = 11,
        /*Receive event for IPv4 protocol. The TcpIp_TypeGroup1 MOF class defines the event data for this event.*/

        EVENT_TRACE_TYPE_RECONNECT = 16,
        /*Reconnect event for IPv4 protocol. (A connect attempt failed and another attempt is made.) The TcpIp_TypeGroup1 MOF class defines the event data for this event.*/

        EVENT_TRACE_TYPE_RETRANSMIT = 14,
        /*Retransmit event for IPv4 protocol. The TcpIp_TypeGroup1 MOF class defines the event data for this event.*/

        EVENT_TRACE_TYPE_SEND = 10,
        /*Send event for IPv4 protocol. The TcpIp_SendIPV4 MOF class defines the event data for this event.*/

         FAIL_EVENT = 17,	/*Fail event. The TcpIp_Fail MOF class defines the event data for this event.*/
        TCP_COPY_IP4 = 18,	/*TCP copy event for IPv4 protocol. The TcpIp_TypeGroup1 MOF class defines the event data for this event.*/
        SEND_IP6_EVENT = 26,	/*Send event for IPv6 protocol. The TcpIp_SendIPV6 MOF class defines the event data for this event.*/
        RECV_IP6_EVENT = 27,	/*Receive event for IPv6 protocol. The TcpIp_TypeGroup3 MOF class defines the event data for this event.*/
        CONN_IP6_EVENT = 28,	/*Connect event for IPv6 protocol. The TcpIp_TypeGroup4 MOF class defines the event data for this event.*/
        DISC_IP6_EVENT = 29,	/*Disconnect event for IPv6 protocol. The TcpIp_TypeGroup3 MOF class defines the event data for this event.*/
        RETRANSMIT_IP6_EVENT = 30,	/*Retransmit event for IPv6 protocol. The TcpIp_TypeGroup3 MOF class defines the event data for this event.*/
        ACCEPT_IP6_EVENT = 31,	/*Accept event for IPv6 protocol. The TcpIp_TypeGroup4 MOF class defines the event data for this event.*/
        RECONN_IP6_EVENT = 32,	/*Reconnect event for IPv6 protocol. (A connect attempt failed and another attempt is made.) The TcpIp_TypeGroup3 MOF class defines the event data for this event.*/
        TCP_COPY_IP6 = 34	/*TCP copy event for IPv6 protocol. The TcpIp_TypeGroup3 MOF class defines the event data for this event.*/
    }

    /// <summary>
    /// Represent transport layer network event
    /// </summary>
    public class TransportLayerEvent : NetworkEvent
    {
        public static readonly Guid TcpEventGuid = 
            new Guid("9a280ac0-c8e0-11d1-84e2-00c04fb998a2");
        public static readonly Guid UdpEventGuid = 
            new Guid("BF3A50C5-A9C9-4988-A005-2DF0B7C80F80");

        
        protected int _PID; //Identifier of the process that generated this event
        protected TransportLayerEventTypes _EventType;
        protected int _EventVersion;
        protected Guid _EventGuid;
        protected int _DstPort;
        protected int _SrcPort;
        protected int _seqnum;
        protected string _connid;

        //Public properties
        
        /// <summary>
        /// Identifier of the process that generated this event
        /// </summary>
        public int PID { get { return _PID; } }

        /// <summary>
        /// The type of this transport layer event
        /// </summary>
        public TransportLayerEventTypes EventType { get { return _EventType; } }
              
        /// <summary>
        /// Destination TCP/UDP port
        /// </summary>
        public int DstPort { get { return _DstPort; } }

        /// <summary>
        /// Source TCP/UDP port
        /// </summary>
        public int SrcPort { get { return _SrcPort; } }

        public int EventVersion { get { return _EventVersion; } }
        public Guid EventGuid { get { return _EventGuid; } }
        public int seqnum { get { return _seqnum; } }
        public string connid { get { return _connid; } }

        /// <summary>
        /// Creates new TransportLayerEvent based on the data returned from "Event Tracing for Windows" native API
        /// </summary>
        /// <param name="ev"></param>
        public TransportLayerEvent(EtwEvent ev)
        {
            this._EventGuid = ev.guid;
            this._Timestamp = ev.timestamp;
            this._EventType = (TransportLayerEventTypes)ev.type;
            this._EventVersion = ev.version;

            if (this._EventType == TransportLayerEventTypes.EVENT_TRACE_TYPE_RECEIVE ||
                this._EventType == TransportLayerEventTypes.RECV_IP6_EVENT)
                this._Direction = TrafficDirections.Recv;
            else if (this._EventType == TransportLayerEventTypes.EVENT_TRACE_TYPE_SEND ||
                this._EventType == TransportLayerEventTypes.SEND_IP6_EVENT)
                this._Direction = TrafficDirections.Send;

            if (this._EventGuid.Equals(TcpEventGuid))
                this._Proto = TransportProtocols.TCP;
            else if (this._EventGuid.Equals(UdpEventGuid))
                this._Proto = TransportProtocols.UDP;

            foreach (EtwEventProperty prop in ev.properties)
            {
                try
                {
                    switch (prop.name)
                    {
                        case "PID": this._PID = Convert.ToInt32(prop.value); break;
                        case "size": this._TotalLen = Convert.ToUInt32(prop.value); break;
                        case "daddr": this._Dst = IPAddress.Parse(prop.value); break;
                        case "saddr": this._Src = IPAddress.Parse(prop.value); break;
                        case "dport": this._DstPort = Convert.ToInt32(prop.value); break;
                        case "sport": this._SrcPort = Convert.ToInt32(prop.value); break;
                        case "seqnum": this._seqnum = Convert.ToInt32(prop.value); break;
                        case "connid":
                            this._connid = prop.value;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Error while processing event property: "+prop.name
                        );
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }
            

        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(500);


            sb.AppendFormat(
                "{0} | {1} Event | Length: {2} bytes | Source: {3} | Destination: {4}\r\n",
                this._Timestamp, this._Proto, this._TotalLen, this._Src, this._Dst
                );


            if (this._ErrorData != null)
            {
                sb.AppendLine(this._Timestamp.ToString() +
                    " | ETW error: " + this._ErrorData.Message);
            }

            return sb.ToString();
        }


    }
}

