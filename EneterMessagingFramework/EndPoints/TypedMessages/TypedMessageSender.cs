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

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class TypedMessageSender<_MessageData> : AttachableOutputChannelBase, ITypedMessageSender<_MessageData>
    {
        public TypedMessageSender(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Sends message via the output channel.
        /// </summary>
        public void SendMessage(_MessageData messageData)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedOutputChannel == null)
                {
                    string anError = TracedObject + "failed to send the message because it is not attached to any output channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    // Serialize the message
                    object aSerializedMessageData = mySerializer.Serialize<_MessageData>(messageData);

                    // Send the message
                    AttachedOutputChannel.SendMessage(aSerializedMessageData);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        private ISerializer mySerializer;

        private string TracedObject
        {
            get
            {
                string anOutputChannelId = (AttachedOutputChannel != null) ? AttachedOutputChannel.ChannelId : "";
                return "The TypedMessageSender<" + typeof(_MessageData).Name + "> atached to the duplex input channel '" + anOutputChannelId + "' ";
            }
        }
    }
}
