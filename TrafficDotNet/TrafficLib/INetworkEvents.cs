using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
/* Project: TrafficDotNet library 
 * Author: MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) */

namespace TrafficLib
{
    /// <summary>
    /// Represents the source of network events
    /// </summary>
    public interface INetworkEvents
    {
        /// <summary>
        /// Specifies whether this object is currently tracing network events
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Specifies when this object started tracing events (undefined if IsRunning == false)
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Specifies when this object stopped tracing events (undefined if IsRunning == true)
        /// </summary>
        DateTime EndTime { get; }

        /// <summary>
        /// Specifies how long this object was tracing network events since the last Start() call
        /// </summary>
        TimeSpan ElapsedTime { get; }

        /// <summary>
        /// Returns the collection of NetworkEvent objects stored in this object
        /// </summary>
        IEnumerable<NetworkEvent> Events { get; }

        /// <summary>
        /// Returns the amount of NetworkEvent objects stored in this object
        /// </summary>
        uint EventsCount { get; }

        /// <summary>
        /// Raised when new network event occurs
        /// </summary>
        event EventHandler<NetworkEvent> NewEvent;

        /// <summary>
        /// Returns NetworkEvent object with specified index. Can throw exceptions if "n" is out of range.
        /// </summary>        
        NetworkEvent GetEvent(uint n);
        
        /// <summary>
        /// Starts the process of tracing network events
        /// </summary>
        void Start();

        /// <summary>
        /// Ends the process of tracing network events
        /// </summary>
        void End();
    }
}
