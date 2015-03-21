/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    /// <summary>
    /// TCP policy server needed for the communication with Silverlight applications.
    /// </summary>
    /// <remarks>
    /// The policy server is required by Silverlight for the communication via HTTP or TCP.
    /// <br/><br/>
    /// The TCP policy server is a special service listening on the port 943 (by default for all Ip adresses).
    /// When it receives &lt;policy-file-request/&gt; request, it returns the content of the policy file.
    /// <br/><br/>
    /// Silverlight automatically uses this service before the TCP connection is created.
    /// If a Silverlight application wants to open the TCP connection,
    /// Silverlight first sends the request on the port 943 and expects the policy file.
    /// If the policy server is not there or the content of the policy file does not allow
    /// the communication, the Tcp connection is not created.
    /// </remarks>
    public class TcpPolicyServer
    {
        /// <summary>
        /// Constructs the TCP policy server providing the policy file on the port 943 for all IP addresses.
        /// </summary>
        public TcpPolicyServer()
            : this(IPAddress.Any)
        {
        }

        /// <summary>
        /// Constructs the TCP policy server for the specified IP address.
        /// </summary>
        /// <param name="ipAddress"></param>
        public TcpPolicyServer(IPAddress ipAddress)
        {
            using (EneterTrace.Entering())
            {
                PolicyXml = GetSilverlightDefaultPolicyXml();

                myTcpListenerProvider = new TcpListenerProvider(ipAddress, 943);
            }
        }


        /// <summary>
        /// Gets or sets the policy xml.
        /// </summary>
        /// <remarks>
        /// When the class is instantiated, the default policy XML is set.
        /// The default policy XML allows the communication with everybody on all Silverlight ports 4502 - 4532.
        /// <br/>
        /// You can use this property to set your own policy file if needed.
        /// </remarks>
        public string PolicyXml { get; set; }

        /// <summary>
        /// Returns true, if this instance of policy server is listening to requests.
        /// </summary>
        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myTcpListenerProvider.IsListening;
                }
            }
        }

        /// <summary>
        /// Starts the policy server.
        /// </summary>
        /// <remarks>
        /// It starts the thread listening to requests on port 943 and responding the policy XML.
        /// </remarks>
        public void StartPolicyServer()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myTcpListenerProvider.StartListening(HandleConnection);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);
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
                myTcpListenerProvider.StopListening();
            }
        }

        private void HandleConnection(TcpClient tcpClient)
        {
            using (EneterTrace.Entering())
            {
                // Source stream.
                NetworkStream anInputStream = tcpClient.GetStream();

                string aReceivedRequest = "";
                int aSize = 0;
                byte[] aBuffer = new byte[512];
                while (aReceivedRequest.Length < myPolicyRequestString.Length &&
                        (aSize = anInputStream.Read(aBuffer, 0, aBuffer.Length)) > 0)
                {
                    aReceivedRequest += Encoding.UTF8.GetString(aBuffer, 0, aSize);
                }

                // If it is the policy request then return the policy xml
                if (StringComparer.InvariantCultureIgnoreCase.Compare(aReceivedRequest, myPolicyRequestString) == 0)
                {
                    byte[] aResponse = Encoding.UTF8.GetBytes(PolicyXml);
                    anInputStream.Write(aResponse, 0, aResponse.Length);
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
                builder.Append("      <allow-from>");
                builder.Append("        <domain uri=\"*\" />");
                builder.Append("      </allow-from>");
                builder.Append("      <grant-to>");
                builder.Append("        <socket-resource port=\"4502-4532\" protocol=\"tcp\" />");
                builder.Append("      </grant-to>");
                builder.Append("    </policy>");
                builder.Append("  </cross-domain-access>");
                builder.Append("</access-policy>");

                return builder.ToString();
            }
        }

        private TcpListenerProvider myTcpListenerProvider;
        private readonly string myPolicyRequestString = "<policy-file-request/>";

        private string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }
    }
}

#endif