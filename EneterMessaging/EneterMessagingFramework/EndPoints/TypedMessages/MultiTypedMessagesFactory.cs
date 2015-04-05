/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Factory to create multi typed sender and receiver.
    /// </summary>
    public class MultiTypedMessagesFactory : IMultiTypedMessagesFactory
    {
        /// <summary>
        /// Constructs the factory with default parameters.
        /// </summary>
        /// <remarks>
        /// It instantiates the factory which will create multi typed senders and receivers which will use
        /// default XmlStringSerializer and with infinite timeout for SyncMultiTypedMessageSender.
        /// </remarks>
        public MultiTypedMessagesFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <remarks>
        /// It instantiates the factory with infinite timeout for SyncMultiTypedMessageSender.
        /// </remarks>
        /// <param name="serializer">serializer which will be used to serializer/deserialize messages</param>
        public MultiTypedMessagesFactory(ISerializer serializer)
        {
            SyncResponseReceiveTimeout = TimeSpan.FromMilliseconds(-1);
            Serializer = serializer;
            SyncDuplexTypedSenderThreadMode = new SyncDispatching();
        }

        /// <summary>
        /// Creates multi typed message sender.
        /// </summary>
        /// <remarks>
        /// The sender is able to send messages of various types and receive response messages of various types.
        /// </remarks>
        /// <returns>multi typed sender</returns>
        public IMultiTypedMessageSender CreateMultiTypedMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new MultiTypedMessageSender(Serializer);
            }
        }

        /// <summary>
        /// Creates multi typed message sender which waits for the response message.
        /// </summary>
        /// <returns>synchronous multi typed sender</returns>
        public ISyncMultitypedMessageSender CreateSyncMultiTypedMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new SyncMultiTypedMessageSender(SyncResponseReceiveTimeout, Serializer, SyncDuplexTypedSenderThreadMode);
            }
        }

        /// <summary>
        /// Creates multi typed message receiver.
        /// </summary>
        /// <remarks>
        /// The receiver is able to receive messages of various types and send response messages
        /// of various types.
        /// </remarks>
        /// <returns></returns>
        public IMultiTypedMessageReceiver CreateMultiTypedMessageReceiver()
        {
            using (EneterTrace.Entering())
            {
                return new MultiTypedMessageReceiver(Serializer);
            }
        }

        /// <summary>
        /// Gets or sets the threading mode for receiving ConnectionOpened and ConnectionClosed events for SyncDuplexTypedMessageSender.
        /// </summary>
        /// <remarks>
        /// E.g. you use SyncDuplexTypedMessageSender and you want to route ConnectionOpened and ConnectionClosed events
        /// to the main UI thread of your WPF based application. Therefore you specify WindowsDispatching when you create your
        /// TCP duplex output channel which you then attach to the SyncDuplexTypedMessageSender.<br/>
        /// Later when the application is running you call SyncDuplexTypedMessageSender.SendRequestMessage(..).<br/>
        /// However if you call it from the main UI thread the deadlock occurs.
        /// Because this component is synchronous the SendRequestMessage(..) will stop the calling main UI thread and will wait
        /// for the response. But the problem is when the response comes the underlying TCP messaging will try to route it to
        /// the main UI thread (as was specified during creating TCP duplex output channel).<br/>
        /// But because the main UI thread is suspending and waiting the message will never arrive.<br/>
        /// <br/>
        /// Solution:<br/>
        /// Do not specify the threading mode when you create yur duplex output channel but specify it using the
        /// SyncDuplexTypedSenderThreadMode property when you create SyncDuplexTypedMessageSender.
        /// </remarks>
        public IThreadDispatcherProvider SyncDuplexTypedSenderThreadMode { get; set; }

        public ISerializer Serializer { get; set; }

        public TimeSpan SyncResponseReceiveTimeout { get; set; }
    }
}
