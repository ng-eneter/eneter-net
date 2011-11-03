/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class SimpleMessagingSystem : IMessagingSystemBase
    {
        public SimpleMessagingSystem(IMessagingProvider inputChannelMessaging)
        {
            myInputChannelMessaging = inputChannelMessaging;
        }

        public void SendMessage(string channelId, object message)
        {
            myInputChannelMessaging.SendMessage(channelId, message);
        }

        public void RegisterMessageHandler(string channelId, Action<object> messageHandler)
        {
            myInputChannelMessaging.RegisterMessageHandler(channelId, messageHandler);
        }

        public void UnregisterMessageHandler(string channelId)
        {
            myInputChannelMessaging.UnregisterMessageHandler(channelId);
        }



        private IMessagingProvider myInputChannelMessaging;
    }
}
