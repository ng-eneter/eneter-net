/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Nodes.Broker;

namespace Eneter.Messaging.Nodes.MessageBus
{
    internal class MessageBusOutputConnector : IOutputConnector
    {
        public MessageBusOutputConnector(string inputConnectorAddresss, string outputConnectorAddress, IDuplexBrokerClient brokerClient, IDuplexOutputChannel brokerOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myInputConnectorAddress = inputConnectorAddresss;
                myOutputConnectorAddress = outputConnectorAddress;
                myBrokerClient = brokerClient;
                myBrokerClientOutputChannel = brokerOutputChannel;
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
                        myBrokerClient.BrokerMessageReceived += OnBrokerMessageReceived;
                        myBrokerClient.AttachDuplexOutputChannel(myBrokerClientOutputChannel);

                        // Subscribe to get messages from the service.
                        myBrokerClient.Subscribe(myOutputConnectorAddress);
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
                    try
                    {
                        myBrokerClient.Unsubscribe();
                    }
                    catch
                    {
                    }

                    // Unsubscribe from all notifications.
                    myBrokerClient.DetachDuplexOutputChannel();
                    myBrokerClient.BrokerMessageReceived -= OnBrokerMessageReceived;

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
                    return myBrokerClient.IsDuplexOutputChannelAttached && myBrokerClientOutputChannel.IsConnected;
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
                 myBrokerClient.SendMessage(myInputConnectorAddress, message);
            }
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException();
        }


        private void OnBrokerMessageReceived(object sender, BrokerMessageReceivedEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (e.ReceivingError != null)
                {
                    EneterTrace.Warning(TracedObject + "detected error when receiving the message from the message bus.", e.ReceivingError);
                    return;
                }

                MessageContext aMessageContext = new MessageContext(e.Message, "", null);
                if (myResponseMessageHandler != null)
                {
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
        }


        private IDuplexBrokerClient myBrokerClient;
        private IDuplexOutputChannel myBrokerClientOutputChannel;
        private string myInputConnectorAddress;
        private string myOutputConnectorAddress;
        private Func<MessageContext, bool> myResponseMessageHandler;
        private object myConnectionManipulator = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
