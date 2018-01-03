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
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading;

namespace Eneter.Messaging.MessagingSystems.ThreadPoolMessagingSystem
{
    internal class ThreadPoolMessagingProvider : IMessagingProvider
    {
        public void SendMessage(string receiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                // Get the message handler
                Action<object> aMessageHandler = null;

                using (ThreadLock.Lock(myRegisteredMessageHandlers))
                {
                    myRegisteredMessageHandlers.TryGetValue(receiverId, out aMessageHandler);
                }

                // If the message handler was found then send the message
                if (aMessageHandler != null)
                {
                    Action aHelperCallback = () =>
                        {
                            aMessageHandler(message);
                        };

                    EneterThreadPool.QueueUserWorkItem(aHelperCallback.Invoke);
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
                using (ThreadLock.Lock(myRegisteredMessageHandlers))
                {
                    if (myRegisteredMessageHandlers.ContainsKey(receiverId))
                    {
                        string aMessage = "The receiver '" + receiverId + "' is already registered.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    myRegisteredMessageHandlers[receiverId] = messageHandler;
                }
            }
        }

        public void UnregisterMessageHandler(string receiverId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myRegisteredMessageHandlers))
                {
                    myRegisteredMessageHandlers.Remove(receiverId);
                }
            }
        }


        private Dictionary<string, Action<object>> myRegisteredMessageHandlers = new Dictionary<string, Action<object>>();

    }
}
