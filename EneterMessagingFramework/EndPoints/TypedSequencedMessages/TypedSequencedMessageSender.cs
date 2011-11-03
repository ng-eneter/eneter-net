/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;


namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    internal class TypedSequencedMessageSender<_MessageDataType> : AttachableOutputChannelBase, ITypedSequencedMessageSender<_MessageDataType>
    {
        public TypedSequencedMessageSender(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Sends the message of specified type as a fragment of a sequence.
        /// </summary>
        /// <param name="message">typed message</param>
        /// <param name="sequenceId">sequence identifier</param>
        /// <param name="isSequenceCompleted">flag indicating if the sequence is completed</param>
        public void SendMessage(_MessageDataType message, string sequenceId, bool isSequenceCompleted)
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
                    lock (myActiveMessageSequences)
                    {
                        // If the sequence id is not registered yet then put it to the dictionary
                        // and set the index to 0.
                        if (!myActiveMessageSequences.ContainsKey(sequenceId))
                        {
                            myActiveMessageSequences[sequenceId] = 0;
                        }

                        // Create the message fragment.
                        TypedMessageFragment<_MessageDataType> aRequestMessageFragment = new TypedMessageFragment<_MessageDataType>(message, sequenceId, myActiveMessageSequences[sequenceId], isSequenceCompleted);

                        // Serialize the fragment.
                        object aSerializedRequestMessage = mySerializer.Serialize<TypedMessageFragment<_MessageDataType>>(aRequestMessageFragment);

                        // Send the serialized message.
                        AttachedOutputChannel.SendMessage(aSerializedRequestMessage);

                        // If this is the last fragment for the sequence then remove the sequence from the dictionary.
                        if (isSequenceCompleted)
                        {
                            myActiveMessageSequences.Remove(sequenceId);
                        }
                        else
                        {
                            ++myActiveMessageSequences[sequenceId];
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        private Dictionary<string, int> myActiveMessageSequences = new Dictionary<string, int>();

        private ISerializer mySerializer;

        private string TracedObject
        {
            get
            {
                string anOutputChannelId = (AttachedOutputChannel != null) ? AttachedOutputChannel.ChannelId : "";
                return "TypedSequencedMessageSender<" + typeof(_MessageDataType).Name + "> atached to the duplex input channel '" + anOutputChannelId + "' ";
            }
        }
    }
}