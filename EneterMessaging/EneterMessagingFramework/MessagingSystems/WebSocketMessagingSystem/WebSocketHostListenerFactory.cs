/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

using System;
using System.Net;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.PathListeningBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal class WebSocketHostListenerFactory : IHostListenerFactory
    {
        public Type ListenerType
        {
            get { return typeof(WebSocketHostListener); }
        }

        public HostListenerBase CreateHostListener(IPEndPoint address, ISecurityFactory securityFactory)
        {
            using (EneterTrace.Entering())
            {
                WebSocketHostListener aPathListener = new WebSocketHostListener(address, securityFactory);
                return aPathListener;
            }
        }
    }
}


#endif