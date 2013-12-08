/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/


using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.DataProcessing.MessageQueueing
{
    /// <summary>
    /// Thread processing messages from the queue.
    /// </summary>
    /// <remarks>
    /// It provides a working thread processing messages from the queue.
    /// If a message is put to the queue the working thread will remove it from the queue and will call the registered handler
    /// to process it.
    /// </remarks>
    /// <typeparam name="_MessageType">type of the message processed by the thread</typeparam>
    public class WorkingThread<_MessageType>
    {
        /// <summary>
        /// Registers the handler processing messages.
        /// </summary>
        /// <param name="messageHandler"></param>
        public void RegisterMessageHandler(Action<_MessageType> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myLock)
                {
                    if (myMessageHandler != null)
                    {
                        string aMessage = TracedObject + "has already registered the message handler.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    myMessageHandler = messageHandler;
                }
            }
        }

        /// <summary>
        /// Unregisters the handler processing messages.
        /// </summary>
        public void UnregisterMessageHandler()
        {
            using (EneterTrace.Entering())
            {
                lock (myLock)
                {
                    myMessageHandler = null;
                }
            }
        }

        /// <summary>
        /// Enqueues the message to the queue.
        /// </summary>
        /// <param name="message"></param>
        public void EnqueueMessage(_MessageType message)
        {
            using (EneterTrace.Entering())
            {
                lock (myLock)
                {
                    if (myMessageHandler == null)
                    {
                        string aMessage = TracedObject + "failed to enqueue the message because the message handler is not registered.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    myWorker.Invoke(() => myMessageHandler(message));
                }
            }
        }

        private SyncDispatcher myWorker = new SyncDispatcher();
        private Action<_MessageType> myMessageHandler;
        private object myLock = new object();
        private string TracedObject { get { return GetType().Name + " "; } }
    }
}
