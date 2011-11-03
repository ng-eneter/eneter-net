﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT && !MONO

using System;
using System.IO;
using System.Net.Security;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security
{
    /// <summary>
    /// The factory class creating the security stream allowing authentication as server and encrypted communication
    /// based on 'Negotiate communication protocol'.
    /// </summary>
    public class ServerNegotiateFactory : ISecurityFactory
    {
        /// <summary>
        /// Creates security stream and authenticates as server.
        /// </summary>
        /// <param name="source">The stream wrapped by the security stream.</param>
        /// <returns>Security stream of type NegotiateStream.</returns>
        public Stream CreateSecurityStreamAndAuthenticate(Stream source)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    NegotiateStream aNegotiateStream = new NegotiateStream(source, true);
                    aNegotiateStream.AuthenticateAsServer();
                    return aNegotiateStream;
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to authenticate the server.", err);
                    throw;
                }
            }
        }

        private readonly string TracedObject = "ServerNegotiateFactory ";
    }
}

#endif