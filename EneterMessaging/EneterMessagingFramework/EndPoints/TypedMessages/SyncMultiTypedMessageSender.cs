/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class SyncMultiTypedMessageSender : ISyncMultitypedMessageSender
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public SyncMultiTypedMessageSender(TimeSpan syncResponseReceiveTimeout, ISerializer serializer, IThreadDispatcherProvider syncDuplexTypedSenderThreadMode)
        {
            using (EneterTrace.Entering())
            {
                if (serializer == null)
                {
                    string anError = "Input parameter serializer is null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                mySerializer = serializer;
                IDuplexTypedMessagesFactory aFactory = new DuplexTypedMessagesFactory(syncResponseReceiveTimeout, serializer)
                {
                    SyncDuplexTypedSenderThreadMode = syncDuplexTypedSenderThreadMode
                };

                mySender = aFactory.CreateSyncDuplexTypedMessageSender<MultiTypedMessage, MultiTypedMessage>();
                mySender.ConnectionOpened += OnConnectionOpened;
                mySender.ConnectionClosed += OnConnectionClosed;
            }
        }

        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                mySender.AttachDuplexOutputChannel(duplexOutputChannel);
            }
        }

        public void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                mySender.DetachDuplexOutputChannel();
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get { return mySender.IsDuplexOutputChannelAttached; }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel
        {
            get { return mySender.AttachedDuplexOutputChannel; }
        }

        public TResponse SendRequestMessage<TResponse, TRequest>(TRequest message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    ISerializer aSerializer = mySerializer.ForResponseReceiver(AttachedDuplexOutputChannel.ResponseReceiverId);

                    MultiTypedMessage aRequest = new MultiTypedMessage();
                    aRequest.TypeName = typeof(TRequest).Name;
                    aRequest.MessageData = aSerializer.Serialize<TRequest>(message);

                    MultiTypedMessage aResponse = mySender.SendRequestMessage(aRequest);

                    TResponse aResult = aSerializer.Deserialize<TResponse>(aResponse.MessageData);
                    return aResult;
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + ErrorHandler.FailedToSendMessage;
                    EneterTrace.Error(anErrorMessage, err);
                    throw;
                }
            }
        }


        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ConnectionOpened != null)
                {
                    ConnectionOpened(this, e);
                }
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ConnectionClosed != null)
                {
                    ConnectionClosed(this, e);
                }
            }
        }

        private ISerializer mySerializer;
        private ISyncDuplexTypedMessageSender<MultiTypedMessage, MultiTypedMessage> mySender;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}
