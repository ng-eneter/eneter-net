

#if !NETSTANDARD

using System;
using System.IO;
using System.IO.Pipes;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeOutputConnector : IOutputConnector
    {
        public NamedPipeOutputConnector(string inputConnectorAddress, string outputConnectorAddress, IProtocolFormatter protocolFormatter, int timeOut, PipeSecurity security)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myOutputConnectorAddress = outputConnectorAddress;
                myProtocolFormatter = protocolFormatter;
                myTimeout = timeOut;
                mySecurity = security;
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

                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    try
                    {
                        mySender = new NamedPipeSender(myInputConnectorAddress, myTimeout);

                        myResponseMessageHandler = responseMessageHandler;
                        myResponseReceiver = new NamedPipeReceiver(myOutputConnectorAddress, 1, myTimeout, mySecurity);
                        myResponseReceiver.StartListening(HandleResponseMessages);

                        // Send the open connection request.
                        object anEncodedMessage = myProtocolFormatter.EncodeOpenConnectionMessage(myOutputConnectorAddress);
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
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    return myResponseReceiver != null && myResponseReceiver.IsListening;
                }
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
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
                Action<MessageContext> aResponseHandler = myResponseMessageHandler;

                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)message);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                if (aProtocolMessage != null && aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    CleanConnection(false);
                }

                try
                {
                    myResponseMessageHandler(aMessageContext);
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
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    if (myResponseReceiver != null)
                    {
                        myResponseReceiver.StopListening();
                        myResponseReceiver = null;
                    }

                    if (mySender != null)
                    {
                        if (sendMessageFlag)
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
                        }

                        mySender.Dispose();
                        mySender = null;
                    }
                }
            }
        }

        private string myInputConnectorAddress;
        private string myOutputConnectorAddress;
        private int myTimeout;
        private PipeSecurity mySecurity;
        private NamedPipeSender mySender;
        private NamedPipeReceiver myResponseReceiver;
        private IProtocolFormatter myProtocolFormatter;
        private Action<MessageContext> myResponseMessageHandler;

        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}


#endif