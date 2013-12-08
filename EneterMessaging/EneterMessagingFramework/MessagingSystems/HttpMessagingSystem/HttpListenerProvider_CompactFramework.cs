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
    internal class HttpListenerProvider : PathListenerProviderBase<HttpRequestContext>
    {
        public HttpListenerProvider(string absoluteUri)
        	: base(new HttpHostListenerFactory(), new Uri(absoluteUri, UriKind.Absolute))
        {
        }

        public HttpListenerProvider(Uri uri)
            : base(new HttpHostListenerFactory(), uri)
        {
        }

        public HttpListenerProvider(Uri uri, ISecurityFactory securityFactory)
             : base(new HttpHostListenerFactory(), uri, securityFactory)
        {
        }

        protected override string TracedObject { get { return GetType().Name + " "; } }
    }
}


#endif