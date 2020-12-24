

using System;
using System.Collections.Generic;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.MessageQueueing
{
    /// <summary>
    /// Memory message queue.
    /// </summary>
    /// <remarks>
    /// One or more threads can put messages into the queue and other threads
    /// can remove them.
    /// If the queue is empty the thread reading messages is blocked until a message
    /// is put to the queue or the thread is unblocked.
    /// </remarks>
    /// <typeparam name="TMessage">Type of the message.</typeparam>
    public class MessageQueue<TMessage>
    {
        /// <summary>
        /// Puts message to the queue.
        /// </summary>
        /// <param name="message">message that shall be enqueued</param>
        public void EnqueueMessage(TMessage message)
        {
            using (EneterTrace.Entering())
            {
                lock (myMessageQueue)
                {
                    myMessageQueue.Enqueue(message);
                    Monitor.Pulse(myMessageQueue);
                }
            }
        }

        /// <summary>
        /// Removes the first message from the queue. If the queue is empty the thread is blocked until a message is put to the queue.
        /// To unblock waiting threads, use UnblockProcesseingThreads().
        /// </summary>
        /// <returns>message, it returns null if the waiting thread was unblocked but there is no message in the queue.</returns>
        public TMessage DequeueMessage()
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
        public TMessage PeekMessage()
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
        public TMessage DequeueMessage(int millisecondsTimeout)
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
        public TMessage PeekMessage(int millisecondsTimeout)
        {
            using (EneterTrace.Entering())
            {
                return WaitForQueueCall(myMessageQueue.Peek, millisecondsTimeout);
            }
        }

        /// <summary>
        /// Deletes all items from the message queue.
        /// </summary>
        public void Clear()
        {
            using (EneterTrace.Entering())
            {
                lock (myMessageQueue)
                {
                    myMessageQueue.Clear();
                }
            }
        }

        /// <summary>
        /// Releases all threads waiting for messages in DequeueMessage() and sets the queue to the unblocking mode.
        /// </summary>
        /// <remarks>
        /// When the queue is in unblocking mode, the dequeue or peek will not block if data is not available but
        /// it will return null or default values.
        /// </remarks>
        public void UnblockProcessingThreads()
        {
            using (EneterTrace.Entering())
            {
                lock (myMessageQueue)
                {
                    myIsBlockingMode = false;
                    Monitor.PulseAll(myMessageQueue);
                }
            }
        }

        /// <summary>
        /// Sets the queue to the blocking mode.
        /// </summary>
        /// <remarks>
        /// When the queue is in blocking mode, the dequeue and peek will block until data is available.
        /// </remarks>
        public void BlockProcessingThreads()
        {
            using (EneterTrace.Entering())
            {
                lock (myMessageQueue)
                {
                    myIsBlockingMode = true;
                }
            }
        }

        /// <summary>
        /// Returns true if the queue blocks threads during dequeue and peek.
        /// </summary>
        public bool IsBlockingMode
        {
            get
            {
                return myIsBlockingMode;
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
        private TMessage WaitForQueueCall(Func<TMessage> func)
        {
            //using (EneterTrace.Entering())
            //{
                lock (myMessageQueue)
                {
                    while (myIsBlockingMode && myMessageQueue.Count == 0)
                    {
                    // Release the lock and wait for a signal that something is in the queue.
                    // Note: To unblock threads waiting here use UnblockProcesseingThreads().
                    Monitor.Wait(myMessageQueue);
                }

                    if (myMessageQueue.Count == 0)
                    {
                        return default(TMessage);
                    }

                    return func();
                }
            //}
        }

        private TMessage WaitForQueueCall(Func<TMessage> func, int millisecondsTimeout)
        {
            //using (EneterTrace.Entering())
            //{
                lock (myMessageQueue)
                {
                    while (myIsBlockingMode && myMessageQueue.Count == 0)
                    {
                        // Release the lock and wait for a signal that something is in the queue.
                        // Note: To unblock threads waiting here use UnblockProcesseingThreads().
                        if (!Monitor.Wait(myMessageQueue, millisecondsTimeout))
                        {
                            throw new TimeoutException("The thread waiting for a message from the message queue exceeded " + millisecondsTimeout.ToString() + " ms.");
                        }
                    }

                    if (myMessageQueue.Count == 0)
                    {
                        return default(TMessage);
                    }

                    return func();
                }
            //}
        }

        /// <summary>
        /// Queue for messages.
        /// </summary>
        private Queue<TMessage> myMessageQueue = new Queue<TMessage>();

        /// <summary>
        /// Indicates weather the reading from the queue blocks until data is available.
        /// </summary>
        private volatile bool myIsBlockingMode = true;
    }
}
