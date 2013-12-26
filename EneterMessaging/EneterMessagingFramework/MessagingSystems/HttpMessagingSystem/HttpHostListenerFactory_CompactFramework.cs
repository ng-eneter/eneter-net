/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if COMPACT_FRAMEWORK

using System;
using System.Net;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.PathListeningBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal class HttpHostListenerFactory : IHostListenerFactory
    {
        public Type ListenerType
        {
            get { return typeof(HttpHostListener); }
        }

        public HostListenerBase CreateHostListener(IPEndPoint address, ISecurityFactory securityFactory)
        {
            using (EneterTrace.Entering())
            {
                HttpHostListener aPathListener = new HttpHostListener(address, securityFactory);
                return aPathListener;
            }
        }
    }
}


#endif