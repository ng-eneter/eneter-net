﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    internal class ReliableDuplexStringMessageReceiver : AttachableReliableInputChannelBase, IReliableStringMessageReceiver
    {
        public event EventHandler<StringRequestReceivedEventArgs> RequestReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public string SendResponseMessage(string responseReceiverId, string responseMessage)
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
                    return AttachedReliableInputChannel.SendResponseMessage(responseReceiverId, responseMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);
                    throw;
                }
            }
        }


        protected override void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (RequestReceived == null)
                {
                    EneterTrace.Warning(TracedObject + "received the request message but nobody was subscribed.");
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
                    RequestReceived(this, new StringRequestReceivedEventArgs((string)e.Message, e.ResponseReceiverId));
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
                string aDuplexInputChannelId = (AttachedDuplexInputChannel != null) ? AttachedDuplexInputChannel.ChannelId : "";
                return "The ReliableDuplexStringMessageReceiver '" + aDuplexInputChannelId + "' ";
            }
        }
    }
}
