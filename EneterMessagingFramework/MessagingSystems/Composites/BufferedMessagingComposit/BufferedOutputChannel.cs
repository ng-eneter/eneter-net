/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class BufferedOutputChannel : IOutputChannel, ICompositeOutputChannel
    {
        public BufferedOutputChannel(IOutputChannel underlyingOutputChannel, TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                UnderlyingOutputChannel = underlyingOutputChannel;
                myMaxOfflineTime = maxOfflineTime;
            }
        }

        public IOutputChannel UnderlyingOutputChannel { get; private set; }

        public string ChannelId { get; private set; }
        

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myEnqueuedMessages)
                {
                    myEnqueuedMessages.Add(message);

                    // If the thread sending messages from the queue is not running, then invoke one.
                    if (!myThreadIsSendingFlag)
                    {
                        myThreadIsSendingFlag = true;

                        WaitCallback aSender = x => MessageSender();
                        ThreadPool.QueueUserWorkItem(aSender);
                    }
                }
            }
        }

        private void MessageSender()
        {
            using (EneterTrace.Entering())
            {
                // Loop taking messages from the queue, until the queue is empty.
                while (true)
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
                            myThreadIsSendingFlag = false;
                            return;
                        }
                    }

                    // Loop trying to send the message until the send is successsful or timeouted.
                    DateTime aStartSendingTime = DateTime.Now;
                    while (true)
                    {
                        // Try to send the message.
                        try
                        {
                            // Send the message using the underlying output channel.
                            UnderlyingOutputChannel.SendMessage(aMessage);

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

                        // If not timeout, then wait a wail and try again.
                        if (DateTime.Now - aStartSendingTime <= myMaxOfflineTime)
                        {
                            Thread.Sleep(300);
                        }
                        else
                        {
                            // The timeout occured, therefore, clean the queue and stop sending.
                            lock (myEnqueuedMessages)
                            {
                                myEnqueuedMessages.Clear();

                                myThreadIsSendingFlag = false;
                                return;
                            }
                        }
                    }
                }
            }
        }



        private List<object> myEnqueuedMessages = new List<object>();
        private bool myThreadIsSendingFlag = false;
        private TimeSpan myMaxOfflineTime;


        private string TracedObject { get { return "Queued output channel '" + ChannelId + "' "; } }
    }
}
