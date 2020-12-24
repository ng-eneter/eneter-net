

using System;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    internal class MessageBusOutputConnector : IOutputConnector
    {
        public MessageBusOutputConnector(string inputConnectorAddress, ISerializer serializer, IDuplexOutputChannel messageBusOutputChannel,
            int openConnectionTimeout)
        {
            using (EneterTrace.Entering())
            {
                myServiceId = inputConnectorAddress;
                mySerializer = serializer;
                myMessageBusOutputChannel = messageBusOutputChannel;
                myOpenConnectionTimeout = (openConnectionTimeout == 0) ? -1 : openConnectionTimeout;
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
                        // Open physical connection.
                        myResponseMessageHandler = responseMessageHandler;
                        myMessageBusOutputChannel.ResponseMessageReceived += OnMessageFromMessageBusReceived;
                        myMessageBusOutputChannel.ConnectionClosed += OnConnectionWithMessageBusClosed;
                        myMessageBusOutputChannel.OpenConnection();

                        myOpenConnectionConfirmed.Reset();

                        // Tell message bus which service shall be associated with this connection.
                        MessageBusMessage aMessage = new MessageBusMessage(EMessageBusRequest.ConnectClient, myServiceId, null);
                        object aSerializedMessage = mySerializer.Serialize<MessageBusMessage>(aMessage);
                        myMessageBusOutputChannel.SendMessage(aSerializedMessage);

                        if (!myOpenConnectionConfirmed.WaitOne(myOpenConnectionTimeout))
                        {
                            throw new TimeoutException(TracedObject + "failed to open the connection within the timeout: " + myOpenConnectionTimeout);
                        }

                        if (!myMessageBusOutputChannel.IsConnected)
                        {
                            throw new InvalidOperationException(TracedObject + ErrorHandler.FailedToOpenConnection);
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
                using (ThreadLock.Lock(myConnectionManipulatorLock))
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
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    return myMessageBusOutputChannel.IsConnected;
                }
            }
        }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                // Note: do not send the client id. It will be automatically assign in the message bus before forwarding the message to the service.
                //       It is done like this due to security reasons. So that some client cannot pretend other client just by sending a different id.
                MessageBusMessage aMessage = new MessageBusMessage(EMessageBusRequest.SendRequestMessage, "", message);
                object aSerializedMessage = mySerializer.Serialize<MessageBusMessage>(aMessage);
                myMessageBusOutputChannel.SendMessage(aSerializedMessage);
            }
        }

        private void OnMessageFromMessageBusReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                MessageBusMessage aMessageBusMessage;
                try
                {
                    aMessageBusMessage = mySerializer.Deserialize<MessageBusMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize message.", err);
                    return;
                }

                if (aMessageBusMessage.Request == EMessageBusRequest.ConfirmClient)
                {
                    // Indicate the connection is open and relase the waiting in the OpenConnection().
                    myOpenConnectionConfirmed.Set();

                    EneterTrace.Debug("CONNECTION CONFIRMED");
                }
                else if (aMessageBusMessage.Request == EMessageBusRequest.SendResponseMessage)
                {
                    Action<MessageContext> aResponseHandler = myResponseMessageHandler;

                    if (aResponseHandler != null)
                    {
                        ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, myServiceId, aMessageBusMessage.MessageData);
                        MessageContext aMessageContext = new MessageContext(aProtocolMessage, e.SenderAddress);

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

        private int myOpenConnectionTimeout;
        private ISerializer mySerializer;
        private IDuplexOutputChannel myMessageBusOutputChannel;
        private string myServiceId;
        private Action<MessageContext> myResponseMessageHandler;
        private object myConnectionManipulatorLock = new object();
        private ManualResetEvent myOpenConnectionConfirmed = new ManualResetEvent(false);

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
