/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security
{
    /// <summary>
    /// The factory class creating the security stream allowing authentication as client and encrypted communication
    /// based on SSL protocol.
    /// </summary>
    public class ClientSslFactory : ISecurityFactory
    {
        /// <summary>
        /// Constructs the factory for the case where only the server identity is verifyied.
        /// </summary>
        /// <param name="hostName">The host the client wants to communicate with. The host name must match with the certificate name.</param>
        public ClientSslFactory(string hostName)
            : this(hostName, null, null, null)
        {
        }

        /// <summary>
        /// Constructs the factory for the case where server identity and also the client identity are verifyied.
        /// </summary>
        /// <param name="hostName">The host the client wants to communicate with. The host name must match with the certificate name.</param>
        /// <param name="clientCertificates">List of certificates provided by the client. If null, the client is not verified.</param>
        public ClientSslFactory(string hostName, X509CertificateCollection clientCertificates)
            : this(hostName, clientCertificates, null, null)
        {
        }

        /// <summary>
        /// Constructs the factory for the case where server identity and also the client identity can be verifyied.
        /// </summary>
        /// <param name="hostName">The host the client wants to communicate with. The host name must match with the certificate name.</param>
        /// <param name="certificates">List of certificates provided by the client. If null, the client is not verified.</param>
        /// <param name="userCertificateValidationCallback">User provided delegate for validating the certificate supplied by the remote party.</param>
        /// <param name="userCertificateSelectionCallback">User provided delegate for selecting the certificate used for authentication.</param>
        public ClientSslFactory(string hostName,
                                X509CertificateCollection certificates,
                                RemoteCertificateValidationCallback userCertificateValidationCallback,
                                LocalCertificateSelectionCallback userCertificateSelectionCallback)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(hostName))
                {
                    string anError = TracedObject + "failed in constructor because the input parameter 'hostName' was null or empty.";
                    EneterTrace.Error(anError);
                    throw new ArgumentException(anError);
                }

                myHostName = hostName;
                myCertificates = certificates;
                myUserCertificateValidationCallback = userCertificateValidationCallback;
                myUserCertificateSelectionCallback = userCertificateSelectionCallback;
            }
        }

        /// <summary>
        /// Creates the security stream and performs the authentication as client.
        /// </summary>
        /// <param name="source">The stream wrapped by the security stream.</param>
        /// <returns>Security stream of type SslStream.</returns>
        public Stream CreateSecurityStreamAndAuthenticate(Stream source)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    SslStream anSslStream = new SslStream(source, false, myUserCertificateValidationCallback, myUserCertificateSelectionCallback);
                    anSslStream.AuthenticateAsClient(myHostName, myCertificates, System.Security.Authentication.SslProtocols.Default, true);
                    return anSslStream;
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to authenticate the client.", err);
                    throw;
                }
            }
        }

        private string myHostName;
        private X509CertificateCollection myCertificates;
        private RemoteCertificateValidationCallback myUserCertificateValidationCallback;
        private LocalCertificateSelectionCallback myUserCertificateSelectionCallback;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}