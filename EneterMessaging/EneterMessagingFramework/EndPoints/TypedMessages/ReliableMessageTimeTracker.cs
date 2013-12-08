/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.Collections.Generic;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class ReliableMessageTimeTracker
    {
        private class TrackItem
        {
            public TrackItem(string messageId)
            {
            
                MessageId = messageId;
                SendTime = DateTime.Now;
            }

            public string MessageId { get; private set; }
            public DateTime SendTime { get; private set; }
        }

        public event EventHandler<ReliableMessageIdEventArgs> TrackingTimeout;

        public ReliableMessageTimeTracker(TimeSpan timeout)
        {
            using (EneterTrace.Entering())
            {
                myTimeout = timeout;
                myTimer = new Timer(OnTimerTick, null, -1, -1);
            }
        }

        public void AddTracking(string id)
        {
            using (EneterTrace.Entering())
            {
                lock (myTrackedMessages)
                {
                    // Create the tracking item.
                    TrackItem aTrackedMessage = new TrackItem(id);
                    myTrackedMessages.Add(aTrackedMessage);

                    // If the timer is not running then start it.
                    if (myTrackedMessages.Count == 1)
                    {
                        myTimer.Change(500, Timeout.Infinite);
                    }
                }
            }
        }

        public void RemoveTracking(string id)
        {
            using (EneterTrace.Entering())
            {
                lock (myTrackedMessages)
                {
                    myTrackedMessages.RemoveWhere(x => x.MessageId == id);
                }
            }
        }

        private void OnTimerTick(object o)
        {
            using (EneterTrace.Entering())
            {
                List<string> aTimeoutedMessages = new List<string>();

                DateTime aCurrentTime = DateTime.Now;

                lock (myTrackedMessages)
                {
                    // Remove all timeouted messages from the tracking.
                    myTrackedMessages.RemoveWhere(x =>
                        {
                            if (aCurrentTime - x.SendTime > myTimeout)
                            {
                                // Store the timeouted id.
                                aTimeoutedMessages.Add(x.MessageId);

                                // Indicate the tracking can be removed.
                                return true;
                            }

                            // Indicate the tracking cannot be removed.
                            return false;
                        });
                }

                // Notify timeouted messages.
                foreach (string aMessageId in aTimeoutedMessages)
                {
                    if (TrackingTimeout != null)
                    {
                        try
                        {
                            ReliableMessageIdEventArgs aMsg = new ReliableMessageIdEventArgs(aMessageId);
                            TrackingTimeout(this, aMsg);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }

                // if the timer shall continue
                lock (myTrackedMessages)
                {
                    if (myTrackedMessages.Count > 0)
                    {
                        myTimer.Change(500, Timeout.Infinite);
                    }
                }
            }
        }

        private Timer myTimer;
        private TimeSpan myTimeout;
        private HashSet<TrackItem> myTrackedMessages = new HashSet<TrackItem>();


        string TracedObject { get { return GetType().Name + ' '; } }
    }
}
