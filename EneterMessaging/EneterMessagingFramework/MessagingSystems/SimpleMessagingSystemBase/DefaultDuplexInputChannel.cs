


using System;
using System.Collections.Generic;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultDuplexInputChannel : IDuplexInputChannel
    {
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public DefaultDuplexInputChannel(string channelId,  // address to listen
            IThreadDispatcher dispatcher,                         // threading model used to notify messages and events
            IThreadDispatcher dispatchingAfterMessageReading,
            IInputConnector inputConnector)                 // listener used for listening to messages
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                ChannelId = channelId;

                Dispatcher = dispatcher;

                // Internal dispatcher used when the message is decoded.
                // E.g. Shared memory messaging needs to return looping immediately the protocol message is decoded
                //      so that other senders are not blocked.
                myDispatchingAfterMessageReading = dispatchingAfterMessageReading;

                myInputConnector = inputConnector;
            }
        }

        public string ChannelId { get; private set; }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    // If the channel is already listening.
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Start listen to messages.
                        myInputConnector.StartListening(HandleMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);

                        // The listening did not start correctly.
                        // So try to clean.
                        StopListening();

                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    try
                    {
                        myInputConnector.StopListening();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.IncorrectlyStoppedListening, err);
                    }
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myListeningManipulatorLock))
                    {
                        return myInputConnector.IsListening;
                    }
                }
            }
        }


        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(responseReceiverId))
                {
                    string aMessage = TracedObject + "detected the input parameter responseReceiverId is null or empty string.";
                    EneterTrace.Error(aMessage);
                    throw new ArgumentException(aMessage);
                }

                if (!IsListening)
                {
                    string aMessage = TracedObject + ErrorHandler.FailedToSendResponseBecauseNotListening;
                    EneterTrace.Error(aMessage);
                    throw new InvalidOperationException(aMessage);
                }

                // If broadcast to all connected response receivers.
                if (responseReceiverId == "*")
                {
                    myInputConnector.SendBroadcast(message);
                }
                else
                {
                    try
                    {
                        // Send the response message.
                        myInputConnector.SendResponseMessage(responseReceiverId, message);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                        throw;
                    }
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myInputConnector.CloseConnection(responseReceiverId);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                }
            }
        }


        public IThreadDispatcher Dispatcher { get; private set; }

        // Note: This method is called from the message receiving thread.
        private void HandleMessage(MessageContext messageContext)
        {
            using (EneterTrace.Entering())
            {
                if (messageContext.ProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                {
                    EneterTrace.Debug("REQUEST MESSAGE RECEIVED");

                    myDispatchingAfterMessageReading.Invoke(() =>
                        {
                            Dispatcher.Invoke(() => NotifyMessageReceived(messageContext, messageContext.ProtocolMessage));
                        });
                }
                else if (messageContext.ProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                {
                    EneterTrace.Debug("CLIENT CONNECTION RECEIVED");
                    myDispatchingAfterMessageReading.Invoke(() =>
                        {
                            Dispatcher.Invoke(() => Notify(ResponseReceiverConnected, messageContext.ProtocolMessage.ResponseReceiverId, messageContext.SenderAddress));
                        });
                }
                else if (messageContext.ProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    EneterTrace.Debug("CLIENT DISCONNECTION RECEIVED");
                    myDispatchingAfterMessageReading.Invoke(() => 
                        {
                            Dispatcher.Invoke(() => Notify(ResponseReceiverDisconnected, messageContext.ProtocolMessage.ResponseReceiverId, messageContext.SenderAddress));
                        });
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToReceiveMessageBecauseIncorrectFormat);
                }
            }
        }

        private void Notify(EventHandler<ResponseReceiverEventArgs> handler, string responseReceiverId, string senderAddress)
        {
            using (EneterTrace.Entering())
            {
                ResponseReceiverEventArgs anEventArgs = new ResponseReceiverEventArgs(responseReceiverId, senderAddress);
                NotifyGeneric<ResponseReceiverEventArgs>(handler, anEventArgs, false);
            }
        }


        private void NotifyMessageReceived(MessageContext messageContext, ProtocolMessage protocolMessage)
        {
            using (EneterTrace.Entering())
            {
                DuplexChannelMessageEventArgs anEventArgs = new DuplexChannelMessageEventArgs(ChannelId, protocolMessage.Message, protocolMessage.ResponseReceiverId, messageContext.SenderAddress);
                NotifyGeneric<DuplexChannelMessageEventArgs>(MessageReceived, anEventArgs, true);
            }
        }

        private void NotifyGeneric<T>(EventHandler<T> handler, T eventArgs, bool isNobodySubscribedWarning)
            where T : EventArgs
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, eventArgs);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else if (isNobodySubscribedWarning)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }


        

        private IThreadDispatcher myDispatchingAfterMessageReading;
        private IInputConnector myInputConnector;

        private object myListeningManipulatorLock = new object();

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
