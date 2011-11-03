/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Internal basic implementation for the output channel.
    /// </summary>
    internal class SimpleOutputChannel : IOutputChannel
    {
        public SimpleOutputChannel(string channelId, IMessagingSystemBase messagingSystem)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                ChannelId = channelId;
                MessagingSystem = messagingSystem;
            }
        }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    MessagingSystem.SendMessage(ChannelId, message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        public string ChannelId { get; private set; }

        private IMessagingSystemBase MessagingSystem { get; set; }

        private string TracedObject
        {
            get
            {
                return "The output channel '" + ChannelId + "' ";
            }
        }
    }
}
