/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    internal class StringMessageReceiver : AttachableInputChannelBase, IStringMessageReceiver
    {
        public event EventHandler<StringMessageEventArgs> MessageReceived;

        protected override void OnMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    if (e.Message is string)
                    {
                        try
                        {
                            MessageReceived(this, new StringMessageEventArgs((string)e.Message));
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                    else
                    {
                        EneterTrace.Error(TracedObject + "received the message that was not type of string.");
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }

        private string TracedObject
        {
            get
            {
                string anInputChannelId = (AttachedInputChannel != null) ? AttachedInputChannel.ChannelId : "";
                return "The StringMessageReceiver atached to the input channel '" + anInputChannelId + "' ";
            }
        }
    }
}
