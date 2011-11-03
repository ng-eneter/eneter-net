/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using System.Threading;
using Eneter.Messaging.Diagnostic;


namespace Eneter.Messaging.DataProcessing.MessageQueueing
{
    /// <summary>
    /// Implements the thread that has the message queue.
    /// If a message is put to the queue, the thread removes it from the queue and calls a user defined
    /// method to handle it.
    /// </summary>
    /// <typeparam name="_MessageType">type of the message processed by the thread</typeparam>
    public class WorkingThread<_MessageType>
    {
        /// <summary>
        /// Constructs the working thread.
        /// </summary>
        public WorkingThread()
        {
            using (EneterTrace.Entering())
            {
                myWorkingThreadName = "";
            }
        }

        /// <summary>
        /// Constructs the working thread with the specified name.
        /// </summary>
        /// <param name="workingThreadName">name of the thread</param>
        public WorkingThread(string workingThreadName)
        {
            using (EneterTrace.Entering())
            {
                myWorkingThreadName = workingThreadName;
            }
        }

        /// <summary>
        /// Registers the method handling messages from the queue and starts the thread reading messages from the queue.
        /// </summary>
        /// <param name="messageHandler">Callback called from the working thread to process the message</param>
        /// <exception cref="InvalidOperationException">Thrown when the message handler is already registered.</exception>
        public void RegisterMessageHandler(Action<_MessageType> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    if (myMessageHandler != null)
                    {
                        string aMessage = TracedObject + "has already registered the message handler.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    myMessageHandler = messageHandler;

                    // Set the flag so that the loop processing the message queue works.
                    myStopRequestedFlag = false;

                    // Create thread processing the message queue.
                    myWorkingThread = new Thread(Worker);
                    myWorkingThread.Name = myWorkingThreadName;
                    myWorkingThread.Start();
                }
            }
        }

        /// <summary>
        /// Unregisters the method handling messages from the queue and stops the thread reading messages.
        /// If the thread does not stop within 5 seconds it is aborted.
        /// </summary>
        public void UnregisterMessageHandler()
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    // Indicate that the loop processing messages shall stop.
                    myStopRequestedFlag = true;

                    if (myWorkingThread != null && myWorkingThread.ThreadState != ThreadState.Unstarted)
                    {
                        // If the thread processing messages waits (is blocked) for a message then release it.
                        myMessageQueue.UnblockProcessingThreads();

                        // If the thread calling this method is not the working thread.
                        if (myWorkingThread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId)
                        {
                            // Wait until the thread is finished
                            if (!myWorkingThread.Join(5000))
                            {
                                myWorkingThread.Abort();
                                EneterTrace.Warning(TracedObject + myWorkingThread.ManagedThreadId.ToString() + " failed to stop the thread processing messages in the queue. The thread was aborted.");
                            }
                        }

                        myWorkingThread = null;
                    }

                    myMessageHandler = null;
                }
            }
        }

        /// <summary>
        /// Puts the message to the queue.
        /// </summary>
        /// <param name="message">message</param>
        public void EnqueueMessage(_MessageType message)
        {
            using (EneterTrace.Entering())
            {
                myMessageQueue.EnqueueMessage(message);
            }
        }


        /// <summary>
        /// Worker method cycling to take messages from the queue and calling the registered handler to process them.
        /// </summary>
        private void Worker()
        {
            using (EneterTrace.Entering())
            {
                while (myStopRequestedFlag == false)
                {
                    // Get the message from the queue
                    object aMessage = myMessageQueue.DequeueMessage();

                    // Call handler to process the message
                    if (aMessage != null && myStopRequestedFlag == false)
                    {
                        if (myMessageHandler != null)
                        {
                            try
                            {
                                myMessageHandler((_MessageType)aMessage);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Error(TracedObject + "detected the exception from the registered callback handling the message from the queue.", err.Message);
                            }
                        }
                        else
                        {
                            EneterTrace.Warning(TracedObject + "processed message from the queue but the processing callback method was not registered.");
                        }
                    }
                }
            }
        }

        private string TracedObject
        {
            get { return "The Working Thread with the name '" + myWorkingThreadName + "' "; }
        }

        /// <summary>
        /// Handler called to process the message from the queue.
        /// </summary>
        private Action<_MessageType> myMessageHandler;

        private MessageQueue<object> myMessageQueue = new MessageQueue<object>();
        private Thread myWorkingThread;
        
        private volatile bool myStopRequestedFlag;

        private readonly string myWorkingThreadName;
    }
}
