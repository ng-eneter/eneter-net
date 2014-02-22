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
    /// Thread with the message queue.
    /// </summary>
    /// <remarks>
    /// If a message is put to the queue, the thread removes it from the queue and calls a call-back
    /// method to process it.
    /// </remarks>
    /// <typeparam name="TMessage">type of the message processed by the thread</typeparam>
    public class WorkingThread<TMessage>
    {
        /// <summary>
        /// Registers the handler processing messages.
        /// </summary>
        /// <param name="messageHandler"></param>
        public void RegisterMessageHandler(Action<TMessage> messageHandler)
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
        public void EnqueueMessage(TMessage message)
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

                    // Note: If the message handler is unregistered before the message handler is processed from the queue
                    //       then myMessageHandler will be null and the exception will occur. Therefore we need to store it locally.
                    Action<TMessage> aMessageHandler = myMessageHandler;
                    myWorker.Invoke(() => aMessageHandler(message));
                }
            }
        }

        private SyncDispatcher myWorker = new SyncDispatcher();
        private Action<TMessage> myMessageHandler;
        private object myLock = new object();
        private string TracedObject { get { return GetType().Name + " "; } }
    }
}
