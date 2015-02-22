/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT && !MONO && !COMPACT_FRAMEWORK

using System;
using System.IO;
using System.IO.Pipes;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeOutputConnector : IOutputConnector
    {
        public NamedPipeOutputConnector(string inputConnectorAddress, string outputConnectorAddress, int timeOut, int numberOfListeningInstances, IProtocolFormatter protocolFormatter, PipeSecurity security)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myOutputConnectorAddress = outputConnectorAddress;
                myTimeout = timeOut;
                myNumberOfListeningInstances = numberOfListeningInstances;
                myProtocolFormatter = protocolFormatter;
                mySecurity = security;
            }
        }

        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (IsConnected)
                    {
                        throw new InvalidOperationException(TracedObject + ErrorHandler.IsAlreadyConnected);
                    }

                    mySender = new NamedPipeSender(myInputConnectorAddress, myTimeout);

                    myResponseMessageHandler = responseMessageHandler;
                    myResponseReceiver = new NamedPipeReceiver(myOutputConnectorAddress, myNumberOfListeningInstances, myTimeout, mySecurity);
                    myResponseReceiver.StartListening(HandleResponseMessages);

                    // Send the open connection request.
                    object anEncodedMessage = myProtocolFormatter.EncodeOpenConnectionMessage(myOutputConnectorAddress);
                    mySender.SendMessage(anEncodedMessage);
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (myResponseReceiver != null)
                    {
                        myResponseReceiver.StopListening();
                        myResponseReceiver = null;
                    }

                    if (mySender != null)
                    {
                        // Send close connection message.
                        try
                        {
                            object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(myOutputConnectorAddress);
                            mySender.SendMessage(anEncodedMessage);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to send close connection message.", err);
                        }

                        mySender.Dispose();
                        mySender = null;
                    }
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                return myResponseReceiver.IsListening;
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeMessage(myOutputConnectorAddress, message);
                    mySender.SendMessage(anEncodedMessage);
                }
            }
        }

        private void HandleResponseMessages(Stream message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)message);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, "", null);
                myResponseMessageHandler(aMessageContext);
            }
        }

        private string myInputConnectorAddress;
        private string myOutputConnectorAddress;
        private int myNumberOfListeningInstances;
        private int myTimeout;
        private PipeSecurity mySecurity;
        private NamedPipeSender mySender;
        private NamedPipeReceiver myResponseReceiver;
        private IProtocolFormatter myProtocolFormatter;
        private Func<MessageContext, bool> myResponseMessageHandler;

        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}


#endif