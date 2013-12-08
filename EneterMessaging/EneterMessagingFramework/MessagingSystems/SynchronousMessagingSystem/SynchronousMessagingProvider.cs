/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem
{
    internal class SynchronousMessagingProvider : IMessagingProvider
    {
        public void SendMessage(string receiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                // Get the message handler
                Action<object> aMessageHandler = null;

                lock (myRegisteredMessageHandlers)
                {
                    myRegisteredMessageHandlers.TryGetValue(receiverId, out aMessageHandler);
                }

                // If the message handler was found then send the message
                if (aMessageHandler != null)
                {
                    aMessageHandler(message);
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

                    myRegisteredMessageHandlers[receiverId] = messageHandler;
                }
            }
        }


        public void UnregisterMessageHandler(string receiverId)
        {
            using (EneterTrace.Entering())
            {
                lock (myRegisteredMessageHandlers)
                {
                    myRegisteredMessageHandlers.Remove(receiverId);
                }
            }
        }

        private Dictionary<string, Action<object>> myRegisteredMessageHandlers = new Dictionary<string, Action<object>>();
    }
}
