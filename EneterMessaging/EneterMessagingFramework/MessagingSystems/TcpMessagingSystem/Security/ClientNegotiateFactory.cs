/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/


#if !SILVERLIGHT

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security
{
    /// <summary>
    /// The factory class creating the security stream allowing the authentication and encrypted communication
    /// based on 'Negotiate communication protocol'.
    /// </summary>
    public class ClientNegotiateFactory : ISecurityFactory
    {
        /// <summary>
        /// Constructs the factory that will use client's default credentials and the service principal name will not be specified.
        /// </summary>
        public ClientNegotiateFactory()
        {
            using (EneterTrace.Entering())
            {
            }
        }

        /// <summary>
        /// Constructs the factory that will use the specified user credentials and specified service principal name.
        /// </summary>
        /// <param name="networkCredential">identity of the client</param>
        /// <param name="servicePrincipalName">service principal name that uniquely identifies the server to authenticate</param>
        public ClientNegotiateFactory(NetworkCredential networkCredential, string servicePrincipalName)
        {
            using (EneterTrace.Entering())
            {
                if (networkCredential == null)
                {
                    string anError = TracedObject + "failed in constructor because the input parameter 'networkCredential' was null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                if (string.IsNullOrEmpty(servicePrincipalName))
                {
                    string anError = TracedObject + "failed in constructor because the input parameter 'servicePrincipalName' was null or empty.";
                    EneterTrace.Error(anError);
                    throw new ArgumentException(anError);
                }

                myNetworkCredential = networkCredential;
                myServicePrincipalName = servicePrincipalName;
            }
        }

        /// <summary>
        /// Creates security stream and authenticates as client.
        /// </summary>
        /// <param name="source">The stream wrapped by the security stream.</param>
        /// <returns>Security stream of type NegotiateStream.</returns>
        public Stream CreateSecurityStreamAndAuthenticate(Stream source)
        {
            using (EneterTrace.Entering())
            {
                NegotiateStream aNegotiateStream = new NegotiateStream(source, false);

                try
                {
                    if (myNetworkCredential != null && !string.IsNullOrEmpty(myServicePrincipalName))
                    {
                        aNegotiateStream.AuthenticateAsClient(myNetworkCredential, myServicePrincipalName);
                    }
                    else
                    {
                        aNegotiateStream.AuthenticateAsClient();
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to authenticate the client.", err);
                    throw;
                }

                return aNegotiateStream;
            }
        }

        private NetworkCredential myNetworkCredential;
        private string myServicePrincipalName;


        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif