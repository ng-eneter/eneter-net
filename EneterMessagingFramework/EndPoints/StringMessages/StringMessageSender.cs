/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    internal class StringMessageSender : AttachableOutputChannelBase, IStringMessageSender
    {
        public void SendMessage(string message)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedOutputChannel == null)
                {
                    string anError = "The StringMessageSender failed to send the message because it is not attached to any output channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    AttachedOutputChannel.SendMessage(message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        private string TracedObject
        {
            get
            {
                string anOutputChannelId = (AttachedOutputChannel != null) ? AttachedOutputChannel.ChannelId : "";
                return "The StringMessageSender attached to the output channel '" + anOutputChannelId + "' ";
            }
        }
    }
}
