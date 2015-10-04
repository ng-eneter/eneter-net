/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class SyncTypedMessageSender<TResponse, TRequest> : ISyncDuplexTypedMessageSender<TResponse, TRequest>
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public SyncTypedMessageSender(TimeSpan responseReceiveTimeout, ISerializer serializer, IThreadDispatcher threadDispatcher)
        {
            using (EneterTrace.Entering())
            {
                if (serializer == null)
                {
                    string anError = "Input parameter serializer is null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                myResponseReceiveTimeout = responseReceiveTimeout;

                IDuplexTypedMessagesFactory aSenderFactory = new DuplexTypedMessagesFactory(serializer);
                mySender = aSenderFactory.CreateDuplexTypedMessageSender<TResponse, TRequest>();
                mySender.ConnectionOpened += OnConnectionOpened;
                mySender.ConnectionClosed += OnConnectionClosed;

                myThreadDispatcher = threadDispatcher;
            }
        }

        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myAttachDetachLock))
                {
                    mySender.AttachDuplexOutputChannel(duplexOutputChannel);
                }
            }
        }


        public void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myAttachDetachLock))
                {
                    // Stop waiting for the response.
                    myResponseAvailableEvent.Set();

                    mySender.DetachDuplexOutputChannel();
                }
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get
            {
                return mySender.IsDuplexOutputChannelAttached;
            }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel
        {
            get
            {
                return mySender.AttachedDuplexOutputChannel;
            }
        }


        public TResponse SendRequestMessage(TRequest message)
        {
            using (EneterTrace.Entering())
            {
                // During sending and receiving only one caller is allowed.
                using (ThreadLock.Lock(myRequestResponseLock))
                {
                    TypedResponseReceivedEventArgs<TResponse> aReceivedResponse = null;
                    EventHandler<TypedResponseReceivedEventArgs<TResponse>> aResponseHandler = (x, y) =>
                        {
                            aReceivedResponse = y;
                            myResponseAvailableEvent.Set();
                        };

                    mySender.ResponseReceived += aResponseHandler;

                    try
                    {
                        myResponseAvailableEvent.Reset();

                        try
                        {
                            mySender.SendRequestMessage(message);
                        }
                        catch (Exception err)
                        {
                            string anErrorMessage = TracedObject + ErrorHandler.FailedToSendMessage;
                            EneterTrace.Error(anErrorMessage, err);
                            throw;
                        }

                        // Wait auntil the response is received or the waiting was interrupted or timeout.
                        // Note: use int instead of TimeSpan due to compatibility reasons. E.g. Compact Framework does not support TimeSpan in WaitOne().
                        if (!myResponseAvailableEvent.WaitOne((int)myResponseReceiveTimeout.TotalMilliseconds))
                        {
                            string anErrorMessage = TracedObject + "failed to receive the response within the timeout. " + myResponseReceiveTimeout;
                            EneterTrace.Error(anErrorMessage);
                            throw new InvalidOperationException(anErrorMessage);
                        }

                        // If response data does not exist.
                        if (aReceivedResponse == null)
                        {
                            string anErrorMessage = TracedObject + "failed to receive the response.";

                            IDuplexOutputChannel anAttachedOutputChannel = mySender.AttachedDuplexOutputChannel;
                            if (anAttachedOutputChannel == null)
                            {
                                anErrorMessage += " The duplex outputchannel was detached.";
                            }
                            else if (!anAttachedOutputChannel.IsConnected)
                            {
                                anErrorMessage += " The connection was closed.";
                            }
                            
                            EneterTrace.Error(anErrorMessage);
                            throw new InvalidOperationException(anErrorMessage);
                        }

                        // If an error occured during receving the response then throw exception.
                        if (aReceivedResponse.ReceivingError != null)
                        {
                            string anErrorMessage = TracedObject + "failed to receive the response.";
                            EneterTrace.Error(anErrorMessage, aReceivedResponse.ReceivingError);
                            throw new InvalidOperationException(anErrorMessage, aReceivedResponse.ReceivingError);
                        }

                        return aReceivedResponse.ResponseMessage;
                    }
                    finally
                    {
                        mySender.ResponseReceived -= aResponseHandler;
                    }
                }
            }
        }

        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                myThreadDispatcher.Invoke(() => Notify(ConnectionOpened, e));
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // The connection was interrupted therefore we must unblock the thread waiting for the response.
                myResponseAvailableEvent.Set();

                myThreadDispatcher.Invoke(() => Notify(ConnectionClosed, e));
            }
        }

        private void Notify(EventHandler<DuplexChannelEventArgs> handler, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private object myAttachDetachLock = new object();
        private object myRequestResponseLock = new object();

        private ManualResetEvent myResponseAvailableEvent = new ManualResetEvent(false);

        private TimeSpan myResponseReceiveTimeout;
        private IDuplexTypedMessageSender<TResponse, TRequest> mySender;

        private IThreadDispatcher myThreadDispatcher;

        private string TracedObject
        {
            get
            {
                return GetType().Name + "<" + typeof(TResponse).Name + ", " + typeof(TRequest).Name + "> ";
            }
        }
    }
}
