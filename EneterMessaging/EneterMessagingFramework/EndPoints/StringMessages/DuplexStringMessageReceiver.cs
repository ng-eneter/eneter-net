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
    internal class DuplexStringMessageReceiver : AttachableDuplexInputChannelBase, IDuplexStringMessageReceiver
    {
        public event EventHandler<StringRequestReceivedEventArgs> RequestReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public void SendResponseMessage(string responseReceiverId, string responseMessage)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedDuplexInputChannel == null)
                {
                    string anError = TracedObject + "failed to send the response message because it is not attached to any duplex input channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    AttachedDuplexInputChannel.SendResponseMessage(responseReceiverId, responseMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);
                    throw;
                }
            }
        }

        protected override void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (RequestReceived == null)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                    return;
                }

                if (e.Message is string == false)
                {
                    string anErrorMessage = TracedObject + "failed to receive the request message because the message is not string.";
                    EneterTrace.Error(anErrorMessage);
                    return;
                }

                try
                {
                    RequestReceived(this, new StringRequestReceivedEventArgs((string)e.Message, e.ResponseReceiverId, e.SenderAddress));
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverDisconnected != null)
                {
                    try
                    {
                        ResponseReceiverDisconnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverConnected != null)
                {
                    try
                    {
                        ResponseReceiverConnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
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
