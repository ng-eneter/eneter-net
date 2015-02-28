/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    internal class MessageBusOutputConnector : IOutputConnector
    {
        public MessageBusOutputConnector(string inputConnectorAddress, IProtocolFormatter protocolFormatter, IDuplexOutputChannel messageBusOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddress;
                myOutputConnectorAddress = messageBusOutputChannel.ResponseReceiverId;
                myProtocolFormatter = protocolFormatter;
                myMessageBusOutputChannel = messageBusOutputChannel;
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

                lock (myConnectionManipulator)
                {
                    try
                    {
                        // Open physical connection.
                        myResponseMessageHandler = responseMessageHandler;
                        myMessageBusOutputChannel.ResponseMessageReceived += OnMessageFromMessageBusReceived;
                        myMessageBusOutputChannel.ConnectionClosed += OnConnectionWithMessageBusClosed;
                        myMessageBusOutputChannel.OpenConnection();

                        // Tell message bus which service shall be associated with this connection.
                        myMessageBusOutputChannel.SendMessage(myInputConnectorAddress);

                        myOpenConnectionConfirmed.Reset();

                        if (!myOpenConnectionConfirmed.WaitOne(30000))
                        {
                            throw new TimeoutException(TracedObject + "failed to open the connection within the timeout.");
                        }

                        if (!myMessageBusOutputChannel.IsConnected)
                        {
                            throw new InvalidOperationException(TracedObject + ErrorHandler.OpenConnectionFailure);
                        }
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
                lock (myConnectionManipulator)
                {
                    myResponseMessageHandler = null;

                    // Close connection with the message bus.
                    myMessageBusOutputChannel.CloseConnection();
                    myMessageBusOutputChannel.ResponseMessageReceived -= OnMessageFromMessageBusReceived;
                    myMessageBusOutputChannel.ConnectionClosed -= OnConnectionWithMessageBusClosed;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulator)
                {
                    return myMessageBusOutputChannel.IsConnected;
                }
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                object anEncodedMessage = myProtocolFormatter.EncodeMessage(myOutputConnectorAddress, message);
                myMessageBusOutputChannel.SendMessage(anEncodedMessage);
            }
        }

        private void OnMessageFromMessageBusReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // If it is a confirmation message that the connection was really open.
                if (e.Message is string && ((string)e.Message) == "OK")
                {
                    // Indicate the connection is open and relase the waiting in the OpenConnection().
                    myOpenConnectionConfirmed.Set();
                }
                else
                {
                    Action<MessageContext> aResponseHandler = myResponseMessageHandler;

                    ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(e.Message);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, e.SenderAddress);

                    if (aProtocolMessage != null && aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                    {
                        CloseConnection();
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
        }

        private void OnConnectionWithMessageBusClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // In case the OpenConnection() is waiting until the connection is open relase it.
                myOpenConnectionConfirmed.Set();

                Action<MessageContext> aResponseHandler = myResponseMessageHandler;
                CloseConnection();

                if (aResponseHandler != null)
                {
                    try
                    {
                        aResponseHandler(null);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private string myOutputConnectorAddress;
        private IProtocolFormatter myProtocolFormatter;
        private IDuplexOutputChannel myMessageBusOutputChannel;
        private string myInputConnectorAddress;
        private Action<MessageContext> myResponseMessageHandler;
        private object myConnectionManipulator = new object();
        private ManualResetEvent myOpenConnectionConfirmed = new ManualResetEvent(false);

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
