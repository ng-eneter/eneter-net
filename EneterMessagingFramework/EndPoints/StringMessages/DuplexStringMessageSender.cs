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
    internal class DuplexStringMessageSender : AttachableDuplexOutputChannelBase, IDuplexStringMessageSender
    {
        public event EventHandler<StringResponseReceivedEventArgs> ResponseReceived;

        public void SendMessage(string message)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedDuplexOutputChannel == null)
                {
                    string anError = TracedObject + "failed to send the request message because it is not attached to any duplex output channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    AttachedDuplexOutputChannel.SendMessage(message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        protected override void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceived == null)
                {
                    EneterTrace.Warning(TracedObject + "received the response message but nobody was subscribed.");
                    return;
                }

                if (e.Message is string == false)
                {
                    string anErrorMessage = TracedObject + "failed to receive the response message because the message is not string.";
                    EneterTrace.Error(anErrorMessage);
                    return;
                }

                try
                {
                    ResponseReceived(this, new StringResponseReceivedEventArgs((string)e.Message));
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        protected override string TracedObject
        {
            get
            {
                string aDuplexOutputChannelId = (AttachedDuplexOutputChannel != null) ? AttachedDuplexOutputChannel.ChannelId : "";
                return "The DuplexStringMessageSender atached to the duplex output channel '" + aDuplexOutputChannelId + "' ";
            }
        }
    }
}
