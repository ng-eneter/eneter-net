/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

using System;
using System.Net;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.PathListeningBase
{
    /// <summary>
    /// Instantiates the host listener for the given IP address and port.
    /// </summary>
    /// <remarks>
    /// The host listener then processes the TCP connection and parses out the path.
    /// Then it forwards the message to the correct path listener.
    /// </remarks>
    internal interface IHostListenerFactory
    {
    	/// <summary>
    	/// Returns the type of the of the host listener. The host listener is derived from HostListenerBase.
    	/// </summary>
        Type ListenerType { get; }

        /// <summary>
        /// Creates the host listener.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="securityFactory"></param>
        /// <returns></returns>
        HostListenerBase CreateHostListener(IPEndPoint address, ISecurityFactory securityFactory);
    }
}


#endif