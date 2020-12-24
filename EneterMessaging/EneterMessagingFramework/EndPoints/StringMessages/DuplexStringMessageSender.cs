

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    internal class DuplexStringMessageSender : AttachableDuplexOutputChannelBase, IDuplexStringMessageSender
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;
        public event EventHandler<StringResponseReceivedEventArgs> ResponseReceived;

        public void SendMessage(string message)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedDuplexOutputChannel == null)
                {
                    string anError = TracedObject + ErrorHandler.FailedToSendMessageBecauseNotAttached;
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    AttachedDuplexOutputChannel.SendMessage(message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendMessage, err);
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

        protected override void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionOpened, e);
            }
        }

        protected override void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionClosed, e);
            }
        }

        private void Notify(EventHandler<DuplexChannelEventArgs> handler, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        protected override string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }
    }
}
