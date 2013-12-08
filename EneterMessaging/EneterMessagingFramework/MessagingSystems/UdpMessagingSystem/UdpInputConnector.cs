/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System;
using System.Net;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpInputConnector : IInputConnector
    {
        public UdpInputConnector(string ipAddressAndPort)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(ipAddressAndPort))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                Uri anServiceUri;
                try
                {
                    // just check if the address is valid
                    anServiceUri = new Uri(ipAddressAndPort, UriKind.Absolute);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(ipAddressAndPort + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                myServiceEndpoint = new IPEndPoint(IPAddress.Parse(anServiceUri.Host), anServiceUri.Port);
            }
        }


        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myListenerManipulatorLock)
                {
                    myReceiver = new UdpReceiver(myServiceEndpoint, true);
                    myReceiver.StartListening(messageHandler);
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListenerManipulatorLock)
                {
                    if (myReceiver != null)
                    {
                        myReceiver.StopListening();
                    }
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListenerManipulatorLock)
                {
                    return myReceiver != null && myReceiver.IsListening;
                }
            }
        }

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            throw new NotSupportedException("CreateResponseSender is not supported in UdpServiceConnector.");
        }

        private IPEndPoint myServiceEndpoint;
        private UdpReceiver myReceiver;
        private object myListenerManipulatorLock = new object();
    }
}

#endif