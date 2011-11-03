using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.DataProcessing.Sequencing;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    internal class ReliableDuplexTypedSequencedMessageSender<_ResponseType, _RequestType> : AttachableReliableOutputChannelBase, IReliableTypedSequencedMessageSender<_ResponseType, _RequestType>
    {
        public event EventHandler<TypedSequencedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;

        public ReliableDuplexTypedSequencedMessageSender(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;

                FragmentDataFactory aFragmentDataFactory = new FragmentDataFactory();
                myMultiInstanceFragmentSequencer = aFragmentDataFactory.CreateMultiinstanceFragmentProcessor(aFragmentDataFactory.CreateFragmentSequencer);
            }
        }

        public string SendMessage(_RequestType message, string sequenceId, bool isSequenceCompleted)
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
                    lock (myRequestSequences)
                    {
                        // If the sequence id is not registered yet then put it to the dictionary
                        // and set the index to 0.
                        if (!myRequestSequences.ContainsKey(sequenceId))
                        {
                            myRequestSequences[sequenceId] = 0;
                        }

                        // Create the message fragment.
                        RequestMessageFragment<_RequestType> aRequestMessageFragment = new RequestMessageFragment<_RequestType>(message, sequenceId, myRequestSequences[sequenceId], isSequenceCompleted);

                        // Serialize the fragment.
                        object aSerializedRequestMessage = mySerializer.Serialize<RequestMessageFragment<_RequestType>>(aRequestMessageFragment);

                        // Send the serialized message.
                        string aMessageId = AttachedReliableOutputChannel.SendMessage(aSerializedRequestMessage);

                        // If this is the last fragment for the sequence then remove the sequence from the dictionary.
                        if (isSequenceCompleted)
                        {
                            lock (myRequestSequences)
                            {
                                myRequestSequences.Remove(sequenceId);
                            }
                        }
                        else
                        {
                            // If this is not the last fragment for the sequence then increase the index for the sequence.
                            ++myRequestSequences[sequenceId];
                        }

                        return aMessageId;
                    }

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

                try
                {
                    // Deserialize the incoming message.
                    ResponseMessageFragment<_ResponseType> aResponseMessageFragment = mySerializer.Deserialize<ResponseMessageFragment<_ResponseType>>(e.Message);

                    // Get fragments following the fragment.
                    IEnumerable<IFragment> aSequencedFragments = myMultiInstanceFragmentSequencer.ProcessFragment(aResponseMessageFragment);

                    // Go via fragments and notify the subscriber
                    foreach (IFragment aFragment in aSequencedFragments)
                    {
                        ResponseMessageFragment<_ResponseType> aFragmentData = (ResponseMessageFragment<_ResponseType>)aFragment;
                        if (aFragmentData != null)
                        {
                            try
                            {
                                ResponseReceived(this, new TypedSequencedResponseReceivedEventArgs<_ResponseType>(aFragmentData));
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize the response message.", err);

                    try
                    {
                        ResponseReceived(this, new TypedSequencedResponseReceivedEventArgs<_ResponseType>(err));
                    }
                    catch (Exception err2)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err2);
                    }
                }
            }
        }

        private Dictionary<string, int> myRequestSequences = new Dictionary<string, int>();

        private ISerializer mySerializer;

        private IMultiInstanceFragmentProcessor myMultiInstanceFragmentSequencer;

        protected override string TracedObject
        {
            get
            {
                string aDuplexOutputChannelId = (AttachedDuplexOutputChannel != null) ? AttachedDuplexOutputChannel.ChannelId : "";
                return "The ReliableTypedSequencedRequester<" + typeof(_ResponseType).Name + ", " + typeof(_RequestType).Name + "> '" + aDuplexOutputChannelId + "' ";
            }
        }
    }
}
