/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightOutputConnector : IOutputConnector
    {
        public SilverlightOutputConnector(string inputConnectorAddress, string outputConnectorAddress, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myOutputConnectorAddress = outputConnectorAddress;
                myProtocolFormatter = protocolFormatter;
            }
        }

        public void OpenConnection(Action<MessageContext> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                if (responseMessageHandler == null)
                {
                    throw new ArgumentNullException("responseMessageHandler is null.");
                }

                lock (myConnectionManipulatorLock)
                {
                    try
                    {
                        myResponseMessageHandler = responseMessageHandler;
                        myResponseReceiver = new SilverlightReceiver(myOutputConnectorAddress);
                        myResponseReceiver.StartListening(HandleResponseMessage);

                        mySender = new SilverlightSender(myInputConnectorAddress);

                        string anEncodedMessage = (string)myProtocolFormatter.EncodeOpenConnectionMessage(myOutputConnectorAddress);
                        mySender.SendMessage(anEncodedMessage);
                    }
                    catch
                    {
                        CloseConnection();
                        throw;
                    }
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                CleanConnection(true);
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulatorLock)
                {
                    return myResponseReceiver != null && myResponseReceiver.IsListening;
                }
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    string anEncodedMessage = (string)myProtocolFormatter.EncodeMessage(myOutputConnectorAddress, message);
                    mySender.SendMessage(anEncodedMessage);
                }
            }
        }


        private void HandleResponseMessage(string message)
        {
            using (EneterTrace.Entering())
            {
                Action<MessageContext> aResponseHandler = myResponseMessageHandler;

                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(message);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                if (aProtocolMessage != null && aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    CleanConnection(false);
                }

                try
                {
                    aResponseHandler(aMessageContext);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        private void CleanConnection(bool sendMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (mySender != null)
                    {
                        if (sendMessageFlag)
                        {
                            try
                            {
                                string anEncodedMessage = (string)myProtocolFormatter.EncodeCloseConnectionMessage(myOutputConnectorAddress);
                                mySender.SendMessage(anEncodedMessage);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + "failed to send close connection message.", err);
                            }
                        }

                        mySender = null;
                    }

                    if (myResponseReceiver != null)
                    {
                        myResponseReceiver.StopListening();
                        myResponseReceiver = null;
                    }
                }
            }
        }


        private Action<MessageContext> myResponseMessageHandler;
        private IProtocolFormatter myProtocolFormatter;
        private object myConnectionManipulatorLock = new object();
        private string myInputConnectorAddress;
        private string myOutputConnectorAddress;
        private SilverlightSender mySender;
        private SilverlightReceiver myResponseReceiver;


        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif