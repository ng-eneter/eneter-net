using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    internal class ReliableDuplexStringMessageSender : AttachableReliableOutputChannelBase, IReliableStringMessageSender
    {
        public event EventHandler<StringResponseReceivedEventArgs> ResponseReceived;


        public string SendMessage(string message)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedReliableOutputChannel == null)
                {
                    string anError = TracedObject + "failed to send the request message because it is not attached to any duplex output channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    string aMessageId = AttachedReliableOutputChannel.SendMessage(message);
                    return aMessageId;
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
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
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
                string aDuplexOutputChannelId = (AttachedReliableOutputChannel != null) ? AttachedReliableOutputChannel.ChannelId : "";
                string aResponseReceiverId = (AttachedReliableOutputChannel != null) ? AttachedReliableOutputChannel.ResponseReceiverId : "";
                return "The ReliableDuplexStringMessageSender '" + aDuplexOutputChannelId + "', '" + aResponseReceiverId + "' ";
            }
        }
    }
}
