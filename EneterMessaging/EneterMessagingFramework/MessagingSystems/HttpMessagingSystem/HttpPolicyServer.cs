/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT


using System;
using System.Net;
using System.Text;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// HTTP policy server needed for the communication with Silverlight applications.
    /// </summary>
    /// <remarks>
    /// The policy server is required by Silverlight for the communication via HTTP or TCP.
    /// (See also <see cref="TcpPolicyServer"/>.)
    /// Windows Phone 7 (based on Silverlight 3 too) does not require the policy server.
    /// <br/><br/>
    /// The HTTP policy server is a special service listening to the HTTP root address. When it receives the
    /// request with the path '/clientaccesspolicy.xml', it returns the content of the policy file.
    /// <br/><br/>
    /// Silverlight automatically uses this service before an HTTP request is invoked.
    /// If the Silverlight application invokes the HTTP request (e.g. http://127.0.0.1/MyService/),
    /// Silverlight first sends the request on the root (i.e. http://127.0.0.1/) and expects the policy file.
    /// If the policy server is not there or the content of the policy file does not allow the communication,
    /// the original HTTP request is not performed.
    /// </remarks>
    public class HttpPolicyServer
    {
        /// <summary>
        /// Constructs the Http policy server.
        /// </summary>
        /// <param name="httpRootAddress">
        /// root Http address. E.g. if the serivice has the address http://127.0.0.1/MyService/, the root
        /// address will be http://127.0.0.1/.
        /// </param>
        public HttpPolicyServer(string httpRootAddress)
        {
            using (EneterTrace.Entering())
            {
                HttpRootAddress = httpRootAddress;
                PolicyXml = GetSilverlightDefaultPolicyXml();

                Uri aUri = new Uri(HttpRootAddress);
                myHttpListenerProvider = new HttpListenerProvider(aUri.AbsoluteUri);
            }
        }

        /// <summary>
        /// Returns the root http address, that is served by the policy server.
        /// </summary>
        public string HttpRootAddress { get; private set; }

        /// <summary>
        /// Gets or sets the policy xml.
        /// </summary>
        /// <remarks>
        /// When the class is instantiated, the default policy file is set.
        /// The default policy file allows to communicate with everybody.
        /// <br/>
        /// You can use this property to set your own policy file if needed.
        /// </remarks>
        public string PolicyXml { get; set; }

        /// <summary>
        /// Starts the policy server.
        /// </summary>
        /// <remarks>
        /// It starts the thread listening to HTTP requests and responding the policy file.
        /// </remarks>
        public void StartPolicyServer()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myHttpListenerProvider.StartListening(HandleConnection);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);
                    throw;
                }
            }
        }

        /// <summary>
        /// Stops the policy server.
        /// </summary>
        /// <remarks>
        /// It stops the listening and responding for requests.
        /// </remarks>
        public void StopPolicyServer()
        {
            using (EneterTrace.Entering())
            {
                myHttpListenerProvider.StopListening();
            }
        }

        /// <summary>
        /// Returns true, if this instance of policy server is listening to requests.
        /// </summary>
        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myHttpListenerProvider.IsListening;
                }
            }
        }

        private void HandleConnection(HttpRequestContext httpClientContext)
        {
            using (EneterTrace.Entering())
            {
                string aRequestedUrl = httpClientContext.Uri.AbsolutePath.ToLower();
                if (!string.IsNullOrEmpty(aRequestedUrl) && aRequestedUrl == "/clientaccesspolicy.xml")
                {
                    byte[] aRespondedXmlPolicy = Encoding.UTF8.GetBytes(PolicyXml);
                    httpClientContext.Response(aRespondedXmlPolicy);
                }
            }
        }

        /// <summary>
        /// Returns the default xml policy file.
        /// </summary>
        /// <returns></returns>
        private string GetSilverlightDefaultPolicyXml()
        {
            using (EneterTrace.Entering())
            {
                StringBuilder builder = new StringBuilder();

                builder.Append("<?xml version=\"1.0\" encoding =\"utf-8\"?>");
                builder.Append("<access-policy>");
                builder.Append("  <cross-domain-access>");
                builder.Append("    <policy>");
                builder.Append("      <allow-from http-methods=\"*\" http-request-headers=\"*\">           ");
                builder.Append("        <domain uri=\"*\"/>");
                builder.Append("      </allow-from>      ");
                builder.Append("      <grant-to>      ");
                builder.Append("        <resource path=\"/resources\" include-subpaths=\"true\"/>");
                builder.Append("      </grant-to>      ");
                builder.Append("    </policy>");
                builder.Append("    <policy>");
                builder.Append("      <allow-from>");
                builder.Append("        <domain uri=\"*\" />");
                builder.Append("      </allow-from>");
                builder.Append("      <grant-to>");
                builder.Append("        <resource path=\"/\" include-subpaths=\"true\" />");
                builder.Append("      </grant-to>");
                builder.Append("    </policy>");
                builder.Append("  </cross-domain-access>");
                builder.Append("</access-policy>");

                return builder.ToString();
            }
        }


        private HttpListenerProvider myHttpListenerProvider;

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + HttpRootAddress + "' ";
            }
        }
    }
}

#endif