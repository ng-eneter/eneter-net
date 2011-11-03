/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class TypedMessageReceiver<_MessageDataType> : AttachableInputChannelBase, ITypedMessageReceiver<_MessageDataType>
    {
        /// <summary>
        /// The event is rised when the message is received.
        /// </summary>
        public event EventHandler<TypedMessageReceivedEventArgs<_MessageDataType>> MessageReceived;

        public TypedMessageReceiver(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        protected override void OnMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    // Deserialize the message value.
                    TypedMessageReceivedEventArgs<_MessageDataType> aMessageReceivedEventArgs = null;
                    try
                    {
                        _MessageDataType aMessageData = mySerializer.Deserialize<_MessageDataType>(e.Message);
                        aMessageReceivedEventArgs = new TypedMessageReceivedEventArgs<_MessageDataType>(aMessageData);
                    }
                    catch (Exception err)
                    {
                        string aMessage = TracedObject + "failed to deserialize the incoming message.";
                        EneterTrace.Error(aMessage, err);
                        aMessageReceivedEventArgs = new TypedMessageReceivedEventArgs<_MessageDataType>(err);
                    }

                    try
                    {
                        MessageReceived(this, aMessageReceivedEventArgs);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }

        private ISerializer mySerializer;

        private string TracedObject
        {
            get
            {
                string anInputChannelId = (AttachedInputChannel != null) ? AttachedInputChannel.ChannelId : "";
                return "The TypedMessageReceiver<" + typeof(_MessageDataType).Name + "> atached to the duplex input channel '" + anInputChannelId + "' ";
            }
        }
    }
}
