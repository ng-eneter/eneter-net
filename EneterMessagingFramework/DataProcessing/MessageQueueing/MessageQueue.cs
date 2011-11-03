/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.MessageQueueing
{
    /// <summary>
    /// Implements the queue for messages of type object.
    /// One or more threads can put messages into the queue and other threads
    /// can remove them.
    /// If the queue is empty, then the thread reading messages is blocked until the message
    /// is put to the queue or the thread is unblocked.
    /// </summary>
    public class MessageQueue : MessageQueue<object>
    {
    }


    /// <summary>
    /// Implements the message queue.
    /// One or more threads can put messages into the queue and other threads
    /// can remove them.
    /// If the queue is empty, then the thread reading the messages is blocked until the message
    /// is put to the queue or the thread is unblocked.
    /// </summary>
    /// <typeparam name="_MessageType">Type of the message.</typeparam>
    public class MessageQueue<_MessageType>
    {
        /// <summary>
        /// Puts message to the queue.
        /// </summary>
        /// <param name="message">message that shall be enqueued</param>
        public void EnqueueMessage(_MessageType message)
        {
            using (EneterTrace.Entering())
            {
                lock (myMessageQueue)
                {
                    myMessageQueue.Enqueue(message);

                    // Release the lock and signal that a message was added to the queue.
                    // Note: The signal causes that if there is a blocked thread waitng for a message it is unblocked
                    //       and the thread can read and process the message.
                    Monitor.Pulse(myMessageQueue);
                }
            }
        }

        /// <summary>
        /// Removes the first message from the queue. If the queue is empty the thread is blocked until a message is put to the queue.
        /// To unblock waiting threads, use UnblockProcesseingThreads().
        /// </summary>
        /// <returns>message, it returns null if the waiting thread was unblocked but there is no message in the queue.</returns>
        public _MessageType DequeueMessage()
        {
            using (EneterTrace.Entering())
            {
                return WaitForQueueCall(myMessageQueue.Dequeue);
            }
        }

        /// <summary>
        /// Reads the first message from the queue. If the queue is empty the thread is blocked until a message is put to the queue.
        /// To unblock waiting threads, use UnblockProcesseingThreads().
        /// </summary>
        /// <returns>
        /// message, it returns null if the waiting thread was unblocked but there is no message in the queue.
        /// </returns>
        public _MessageType PeekMessage()
        {
            using (EneterTrace.Entering())
            {
                return WaitForQueueCall(myMessageQueue.Peek);
            }
        }

        /// <summary>
        /// Removes the first message from the queue. If the queue is empty the thread is blocked until the specified timeout.
        /// The method UnblockProcesseingThreads() unblocks threads waiting in this method.
        /// </summary>
        /// <param name="millisecondsTimeout">Maximum waiting time for the message. If the time is exceeded the TimeoutException is thrown.</param>
        /// <returns>message</returns>
        /// <exception cref="TimeoutException">when the specified timeout is exceeded</exception>
        public _MessageType DequeueMessage(int millisecondsTimeout)
        {
            using (EneterTrace.Entering())
            {
                return WaitForQueueCall(myMessageQueue.Dequeue, millisecondsTimeout);
            }
        }

        /// <summary>
        /// Reads the first message from the queue. If the queue is empty the thread is blocked until a message is put to the queue.
        /// To unblock waiting threads, use UnblockProcesseingThreads().
        /// </summary>
        /// <param name="millisecondsTimeout">
        /// Maximum waiting time for the message. If the time is exceeded the TimeoutException is thrown.
        /// </param>
        /// <returns>message</returns>
        /// <exception cref="TimeoutException">when the specified timeout is exceeded</exception>
        public _MessageType PeekMessage(int millisecondsTimeout)
        {
            using (EneterTrace.Entering())
            {
                return WaitForQueueCall(myMessageQueue.Peek, millisecondsTimeout);
            }
        }

        /// <summary>
        /// Releases all threads waiting for messages in DequeueMessage().
        /// </summary>
        public void UnblockProcessingThreads()
        {
            using (EneterTrace.Entering())
            {
                lock (myMessageQueue)
                {
                    Monitor.PulseAll(myMessageQueue);
                }
            }
        }

        /// <summary>
        /// Returns number of messages in the queue.
        /// </summary>
        public int Count
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myMessageQueue)
                    {
                        return myMessageQueue.Count;
                    }
                }
            }
        }

        /// <summary>
        /// Waits until something is in the queue and then calls the specified delegate.
        /// If the waiting thread is released (by UnblockProcessingThreads) but the queue is still empty, it returns the default
        /// value of the specified template type.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        private _MessageType WaitForQueueCall(Func<_MessageType> func)
        {
            using (EneterTrace.Entering())
            {
                lock (myMessageQueue)
                {
                    if (myMessageQueue.Count == 0)
                    {
                        // Release the lock and wait for a signal that something is in the queue.
                        // Note: To unblock threads waiting here use UnblockProcesseingThreads().
                        Monitor.Wait(myMessageQueue);

                        // This can happen if the waiting thread was released to end.
                        if (myMessageQueue.Count == 0)
                        {
                            return default(_MessageType);
                        }
                    }

                    return func();
                }
            }
        }

        private _MessageType WaitForQueueCall(Func<_MessageType> func, int millisecondsTimeout)
        {
            using (EneterTrace.Entering())
            {
                lock (myMessageQueue)
                {
                    if (myMessageQueue.Count == 0)
                    {
                        // Release the lock and wait for a signal that something is in the queue.
                        // Note: To unblock threads waiting here use UnblockProcesseingThreads().
                        if (!Monitor.Wait(myMessageQueue, millisecondsTimeout))
                        {
                            throw new TimeoutException("The thread waiting for a message from the message queue exceeded " + millisecondsTimeout.ToString() + " ms.");
                        }

                        // This can happen if the waiting thread was released to end.
                        if (myMessageQueue.Count == 0)
                        {
                            return default(_MessageType);
                        }
                    }

                    return func();
                }
            }
        }

        /// <summary>
        /// Queue for messages.
        /// </summary>
        private Queue<_MessageType> myMessageQueue = new Queue<_MessageType>();
    }
}
