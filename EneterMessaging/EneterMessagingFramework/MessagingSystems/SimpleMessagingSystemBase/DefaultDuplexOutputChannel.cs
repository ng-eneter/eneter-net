


using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public DefaultDuplexOutputChannel(string channelId, string responseReceiverId,
            IThreadDispatcher eventDispatcher,
            IThreadDispatcher dispatcherAfterResponseReading,
            IOutputConnectorFactory outputConnectorFactory)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                ChannelId = channelId;

                if (string.IsNullOrEmpty(responseReceiverId))
                {
                    string aResponseReceiverId = channelId.TrimEnd('/');
                    aResponseReceiverId += "_" + Guid.NewGuid().ToString() + "/";
                    ResponseReceiverId = aResponseReceiverId;
                }
                else if (responseReceiverId == "*")
                {
                    string anErrorMessage = "responseReceiverId cannot be '*'. It is reserved for broadcast response messages.";
                    EneterTrace.Error(anErrorMessage);
                    throw new ArgumentException(anErrorMessage);
                }
                else
                {
                    ResponseReceiverId = responseReceiverId;
                }

                Dispatcher = eventDispatcher;

                // Internal dispatcher used when the message is decoded.
                // E.g. Shared memory meesaging needs to return looping immediately the protocol message is decoded
                //      so that other senders are not blocked.
                myDispatchingAfterResponseReading = dispatcherAfterResponseReading;

                myOutputConnector = outputConnectorFactory.CreateOutputConnector(ChannelId, ResponseReceiverId);
            }
        }

        public string ChannelId { get; private set; }
        public string ResponseReceiverId { get; private set; }


        public void OpenConnection()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    if (IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        myConnectionIsCorrectlyOpen = true;

                        // Connect and start listening to response messages.
                        myOutputConnector.OpenConnection(HandleResponse);
                    }
                    catch (Exception err)
                    {
                        myConnectionIsCorrectlyOpen = false;

                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToOpenConnection, err);

                        try
                        {
                            CloseConnection();
                        }
                        catch
                        {
                            // We tried to clean after failure. The exception can be ignored.
                        }

                        throw;
                    }
                }

                // Invoke the event notifying, the connection was opened.
                Dispatcher.Invoke(() => Notify(ConnectionOpened));
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                CleanAfterConnection(true, false);
            }
        }

        public bool IsConnected
        {
            get
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    return (myOutputConnector.IsConnected);
                }
            }
        }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    if (!IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.FailedToSendMessageBecauseNotConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Send the message.
                        myOutputConnector.SendRequestMessage(message);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendMessage, err);
                        CleanAfterConnection(true, true);
                        throw;
                    }
                }
            }
        }

        public IThreadDispatcher Dispatcher { get; private set; }

        private void HandleResponse(MessageContext messageContext)
        {
            using (EneterTrace.Entering())
            {
                if (messageContext == null ||
                    messageContext.ProtocolMessage == null ||
                    messageContext.ProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    EneterTrace.Debug("CLIENT DISCONNECTED RECEIVED");
                    myDispatchingAfterResponseReading.Invoke(() => CleanAfterConnection(false, true));
                }
                else if (messageContext.ProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                {
                    EneterTrace.Debug("RESPONSE MESSAGE RECEIVED");
                    myDispatchingAfterResponseReading.Invoke(() => Dispatcher.Invoke(() => NotifyResponseMessageReceived(messageContext.ProtocolMessage.Message, messageContext.SenderAddress)));
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToReceiveMessageBecauseIncorrectFormat);
                }
            }
        }

        private void CleanAfterConnection(bool sendCloseMessageFlag, bool notifyConnectionClosedFlag)
        {
            using (EneterTrace.Entering())
            {
                bool aConnectionWasCorrectlyOpen = false;

                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    if (sendCloseMessageFlag)
                    {
                        try
                        {
                            myOutputConnector.CloseConnection();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                        }
                    }

                    // Note: the notification must run outside the lock because of potententional deadlock.
                    aConnectionWasCorrectlyOpen = myConnectionIsCorrectlyOpen;
                    myConnectionIsCorrectlyOpen = false;
                }

                // Notify the connection closed only if it was successfuly open before.
                // E.g. It will be not notified if the CloseConnection() is called for already closed connection.
                if (aConnectionWasCorrectlyOpen && notifyConnectionClosedFlag)
                {
                    Dispatcher.Invoke(() => Notify(ConnectionClosed));
                }
            }
        }


        private void NotifyResponseMessageReceived(object message, string senderAddress)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseMessageReceived != null)
                {
                    try
                    {
                        ResponseMessageReceived(this, new DuplexChannelMessageEventArgs(ChannelId, message, ResponseReceiverId, senderAddress));
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }

        private void Notify(EventHandler<DuplexChannelEventArgs> eventHandler)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    if (eventHandler != null)
                    {
                        DuplexChannelEventArgs aMsg = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, "");
                        eventHandler(this, aMsg);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }


        private IThreadDispatcher myDispatchingAfterResponseReading;
        private IOutputConnector myOutputConnector;
        private bool myConnectionIsCorrectlyOpen;
        private object myConnectionManipulatorLock = new object();

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
        
    }
}
