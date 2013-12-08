/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.ThreadMessagingSystem
{
    internal class ThreadMessagingProvider : IMessagingProvider
    {
        public void SendMessage(string receiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                // Get the thread handling the input channel.
                WorkingThread<object> aWorkingThread = null;

                lock (myRegisteredMessageHandlers)
                {
                    myRegisteredMessageHandlers.TryGetValue(receiverId, out aWorkingThread);
                }

                if (aWorkingThread != null)
                {
                    aWorkingThread.EnqueueMessage(message);
                }
                else
                {
                    string anError = "The receiver '" + receiverId + "' does not exist.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }
            }
        }

        public void RegisterMessageHandler(string receiverId, Action<object> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myRegisteredMessageHandlers)
                {
                    if (myRegisteredMessageHandlers.ContainsKey(receiverId))
                    {
                        string aMessage = "The receiver '" + receiverId + "' is already registered.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    // Create and register the working thread for the registering input channel.
                    WorkingThread<object> aWorkingThread = new WorkingThread<object>();
                    aWorkingThread.RegisterMessageHandler(messageHandler);
                    myRegisteredMessageHandlers[receiverId] = aWorkingThread;
                }
            }
        }

        public void UnregisterMessageHandler(string receiverId)
        {
            using (EneterTrace.Entering())
            {
                WorkingThread<object> aWorkingThread = null;

                lock (myRegisteredMessageHandlers)
                {
                    myRegisteredMessageHandlers.TryGetValue(receiverId, out aWorkingThread);
                    myRegisteredMessageHandlers.Remove(receiverId);
                }
             
                if (aWorkingThread != null)
                {
                    aWorkingThread.UnregisterMessageHandler();
                }
            }
        }

        private Dictionary<string, WorkingThread<object>> myRegisteredMessageHandlers = new Dictionary<string, WorkingThread<object>>();
    }
}
