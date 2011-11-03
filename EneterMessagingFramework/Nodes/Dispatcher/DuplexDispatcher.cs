/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System.Collections.Generic;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;

namespace Eneter.Messaging.Nodes.Dispatcher
{
    internal class DuplexDispatcher : AttachableMultipleDuplexInputChannelsBase, IDuplexDispatcher
    {
        public DuplexDispatcher(IMessagingSystemFactory duplexOutputChannelMessagingSystem)
        {
            using (EneterTrace.Entering())
            {
                MessagingSystemFactory = duplexOutputChannelMessagingSystem;
            }
        }


        public void AddDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexOutputChannelIds)
                {
                    myDuplexOutputChannelIds.Add(channelId);
                }
            }
        }

        public void RemoveDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexOutputChannelIds)
                {
                    myDuplexOutputChannelIds.Remove(channelId);
                    CloseDuplexOutputChannel(channelId);
                }
            }
        }

        public void RemoveAllDuplexOutputChannels()
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexOutputChannelIds)
                {
                    try
                    {
                        foreach (string aDuplexOutputChannelId in myDuplexOutputChannelIds)
                        {
                            CloseDuplexOutputChannel(aDuplexOutputChannelId);
                        }
                    }
                    finally
                    {
                        myDuplexOutputChannelIds.Clear();
                    }
                }
            }
        }

        protected override void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexOutputChannelIds)
                {
                    foreach (string aDuplexOutputChannelId in myDuplexOutputChannelIds)
                    {
                        try
                        {
                            SendMessage(e.ChannelId, e.ResponseReceiverId, aDuplexOutputChannelId, e.Message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                        }
                    }
                }
            }
        }

        protected override void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    SendResponseMessage(e.ResponseReceiverId, e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);
                }
            }
        }

        protected List<string> myDuplexOutputChannelIds = new List<string>();

        protected override string TracedObject
        {
            get
            {
                return "The DuplexDispatcher ";
            }
        }
    }
}
