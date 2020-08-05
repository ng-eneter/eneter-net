/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security
{
    /// <summary>
    /// The factory class creating the security stream allowing authentication as server and encrypted communication
    /// based on SSL protocol.
    /// </summary>
    public class ServerSslFactory : ISecurityFactory
    {
        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="certificate">Server certificate.</param>
        public ServerSslFactory(X509Certificate certificate)
            : this(certificate, false, null, null)
        {
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="certificate">Server certificate.</param>
        /// <param name="isClientCertificateRequired">true - if the client identity is verified too.</param>
        public ServerSslFactory(X509Certificate certificate, bool isClientCertificateRequired)
            : this(certificate, isClientCertificateRequired, null, null)
        {
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="certificate">Server certificate.</param>
        /// <param name="isClientCertificateRequired">true - if the client identity is verified too.</param>
        /// <param name="userCertificateValidationCallback">User provided delegate for validating the certificate supplied by the remote party.</param>
        /// <param name="userCertificateSelectionCallback">User provided delegate for selecting the certificate used for authentication.</param>
        public ServerSslFactory(X509Certificate certificate,
                                bool isClientCertificateRequired,
                                RemoteCertificateValidationCallback userCertificateValidationCallback,
                                LocalCertificateSelectionCallback userCertificateSelectionCallback)
        {
            using (EneterTrace.Entering())
            {
                if (certificate == null)
                {
                    string anError = TracedObject + "failed in constructor because the input parameter 'certificate' was null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                AcceptedProtocols = SslProtocols.Ssl3 | SslProtocols.Tls;

#if NETSTANDARD || NET472
                    AcceptedProtocols |= SslProtocols.Tls11 | SslProtocols.Tls12;
#endif
#if MONOANDROID || XAMARIN_IOS
                    AcceptedProtocols |= SslProtocols.Tls11 | SslProtocols.Tls12 |SslProtocols.Tls13;
#endif

                myCertificate = certificate;
                myIsClientCertificateRequired = isClientCertificateRequired;
                myUserCertificateValidationCallback = userCertificateValidationCallback;
                myUserCertificateSelectionCallback = userCertificateSelectionCallback;
            }
        }

        /// <summary>
        /// Sets or gets accepted versions of SSL/TLS protocols.
        /// </summary>
        public SslProtocols AcceptedProtocols { get; set; }

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
                    anSslStream.AuthenticateAsServer(myCertificate, myIsClientCertificateRequired, AcceptedProtocols, true);
                    return anSslStream;
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to authenticate the server.", err);
                    throw;
                }
            }
        }

        private X509Certificate myCertificate;
        private bool myIsClientCertificateRequired;
        private RemoteCertificateValidationCallback myUserCertificateValidationCallback;
        private LocalCertificateSelectionCallback myUserCertificateSelectionCallback;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}