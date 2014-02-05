/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Nodes.Broker;
using System.Threading;

namespace Eneter.Messaging.MessagingSystems.Composites.BusMessaging
{
    internal class MessageBusOutputConnector : IOutputConnector
    {
        public MessageBusOutputConnector(string serviceAddressInMessageBus, string clientIdInMessageBus,
            IDuplexOutputChannel messageBusOutputChannel, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myServiceAddressInMessageBus = serviceAddressInMessageBus;
                myClientIdInMessageBus = clientIdInMessageBus;
                myMessageBusOutputChannel = messageBusOutputChannel;
                myProtocolFormatter = protocolFormatter;
            }
        }

        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulator)
                {
                    try
                    {
                        myResponseMessageHandler = responseMessageHandler;
                        myMessageBusOutputChannel.ResponseMessageReceived += OnMessageFromMessageBusReceived;
                        myMessageBusOutputChannel.ConnectionClosed += OnConnectionWithMessageBusClosed;
                        myMessageBusOutputChannel.OpenConnection();

                        // Inform the message bus which service this client wants to connect.
                        myOpenConnectionConfirmed.Reset();
                        myMessageBusOutputChannel.SendMessage(myServiceAddressInMessageBus);

                        if (!myOpenConnectionConfirmed.WaitOne(5000))
                        {
                            throw new TimeoutException(TracedObject + "failed to open the connection within the timeout.");
                        }
                    }
                    catch
                    {
                        CloseConnection();
                        throw;
                    }

                    if (!myMessageBusOutputChannel.IsConnected)
                    {
                        throw new InvalidOperationException(TracedObject + ErrorHandler.OpenConnectionFailure);
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
                    if (myMessageBusOutputChannel != null)
                    {
                        myMessageBusOutputChannel.CloseConnection();
                        myMessageBusOutputChannel.ResponseMessageReceived -= OnMessageFromMessageBusReceived;
                        myMessageBusOutputChannel.ConnectionClosed -= OnConnectionWithMessageBusClosed;
                    }

                    myResponseMessageHandler = null;
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

        public bool IsStreamWritter
        {
            get { return false; }
        }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                myMessageBusOutputChannel.SendMessage(message);
            }
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException();
        }


        private void OnMessageFromMessageBusReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (e.Message is string && ((string)e.Message) == "OK")
                {
                    // Indicate the connection is open.
                    myOpenConnectionConfirmed.Set();
                }
                else if (myResponseMessageHandler != null)
                {
                    try
                    {
                        MessageContext aMessageContext = new MessageContext(e.Message, e.SenderAddress, null);
                        myResponseMessageHandler(aMessageContext);
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
                if (myResponseMessageHandler != null)
                {
                    try
                    {
                        myResponseMessageHandler(null);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private IDuplexOutputChannel myMessageBusOutputChannel;
        private IProtocolFormatter myProtocolFormatter;
        private string myServiceAddressInMessageBus;
        private string myClientIdInMessageBus;
        private Func<MessageContext, bool> myResponseMessageHandler;
        private object myConnectionManipulator = new object();
        private ManualResetEvent myOpenConnectionConfirmed = new ManualResetEvent(false);

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
