/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class ResponseMessageSender
    {
        public ResponseMessageSender(string responseReceiverId, IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                myResponseReceiverId = responseReceiverId;
                myDuplexInputChannel = duplexInputChannel;
            }
        }

        public void SendResponseMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myEnqueuedMessages)
                {
                    myEnqueuedMessages.Add(message);

                    // If the thread sending messages from the queue is not running, then invoke one.
                    if (!myThreadIsSendingFlag)
                    {
                        mySendingThreadShallStopFlag = false;
                        myThreadIsSendingFlag = true;
                        mySendingThreadStoppedEvent.Reset();

                        WaitCallback aSender = x => MessageSender();
                        ThreadPool.QueueUserWorkItem(aSender);
                    }
                }
            }
        }

        public void StopSending()
        {
            using (EneterTrace.Entering())
            {
                lock (myEnqueuedMessages)
                {
                    // Indicate to the running thread, that the sending shall stop.
                    mySendingThreadShallStopFlag = true;

                    // Wait until the thread is stopped.
                    if (!mySendingThreadStoppedEvent.WaitOne(5000))
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure);
                    }

                    // Remove all messages.
                    myEnqueuedMessages.Clear();
                }
            }
        }

        private void MessageSender()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Loop taking messages from the queue, until the queue is empty.
                    while (!mySendingThreadShallStopFlag)
                    {
                        object aMessage;

                        lock (myEnqueuedMessages)
                        {
                            // If there is a message in the queue, read it.
                            if (myEnqueuedMessages.Count > 0)
                            {
                                aMessage = myEnqueuedMessages[0];
                            }
                            else
                            {
                                // There are no messages in the queue, therefore the thread can end.
                                return;
                            }
                        }

                        while (!mySendingThreadShallStopFlag)
                        {
                            // Try to send the message.
                            try
                            {
                                // Send the message using the underlying output channel.
                                myDuplexInputChannel.SendResponseMessage(myResponseReceiverId, aMessage);

                                // The message was successfuly sent, therefore it can be removed from the queue.
                                lock (myEnqueuedMessages)
                                {
                                    myEnqueuedMessages.RemoveAt(0);
                                }

                                // The message was successfuly sent.
                                break;
                            }
                            catch
                            {
                                // The receiver is not available. Therefore try again if not timeout.
                            }


                            // If sending thread is not asked to stop.
                            if (!mySendingThreadShallStopFlag)
                            {
                                Thread.Sleep(300);
                            }
                        }
                    }
                }
                finally
                {
                    myThreadIsSendingFlag = false;
                    mySendingThreadStoppedEvent.Set();
                }
            }
        }


        private string myResponseReceiverId;
        private List<object> myEnqueuedMessages = new List<object>();
        
        private bool myThreadIsSendingFlag;
        private bool mySendingThreadShallStopFlag;
        private ManualResetEvent mySendingThreadStoppedEvent = new ManualResetEvent(true);

        private IDuplexInputChannel myDuplexInputChannel;


        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + myResponseReceiverId + "' ";
            }
        }
    }
}
