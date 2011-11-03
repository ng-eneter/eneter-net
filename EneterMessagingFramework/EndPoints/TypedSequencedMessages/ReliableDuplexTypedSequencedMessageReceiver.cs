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
    internal class ReliableDuplexTypedSequencedMessageReceiver<_ResponseType, _RequestType> : AttachableReliableInputChannelBase, IReliableTypedSequencedMessageReceiver<_ResponseType, _RequestType>
    {
        public event EventHandler<TypedSequencedRequestReceivedEventArgs<_RequestType>> MessageReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public ReliableDuplexTypedSequencedMessageReceiver(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;

                FragmentDataFactory aFragmentDataFactory = new FragmentDataFactory();
                myMultiInstanceFragmentSequencer = aFragmentDataFactory.CreateMultiinstanceFragmentProcessor(aFragmentDataFactory.CreateFragmentSequencer);
            }
        }

        public string SendResponseMessage(string responseReceiverId, _ResponseType responseMessage, string sequenceId, bool isSequenceCompleted)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedReliableInputChannel == null)
                {
                    string anError = TracedObject + "failed to send the response message because it is not attached to any duplex input channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    lock (myResponseSequences)
                    {
                        if (!myResponseSequences.ContainsKey(sequenceId))
                        {
                            myResponseSequences[sequenceId] = 0;
                        }

                        ResponseMessageFragment<_ResponseType> aResponseMessageFragment = new ResponseMessageFragment<_ResponseType>(responseMessage, sequenceId, myResponseSequences[sequenceId], isSequenceCompleted);

                        object aSerializedResponseMessage = mySerializer.Serialize<ResponseMessageFragment<_ResponseType>>(aResponseMessageFragment);

                        string aResponseMessageId = AttachedReliableInputChannel.SendResponseMessage(responseReceiverId, aSerializedResponseMessage);

                        if (isSequenceCompleted)
                        {
                            myResponseSequences.Remove(sequenceId);
                        }
                        else
                        {
                            ++myResponseSequences[sequenceId];
                        }

                        return aResponseMessageId;
                    }
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
                if (MessageReceived == null)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                    return;
                }

                try
                {
                    // Deserialize the incoming message.
                    RequestMessageFragment<_RequestType> aResponseMessageFragment = mySerializer.Deserialize<RequestMessageFragment<_RequestType>>(e.Message);

                    // Get fragments following the fragment.
                    IEnumerable<IFragment> aSequencedFragments = myMultiInstanceFragmentSequencer.ProcessFragment(aResponseMessageFragment);

                    // Go via fragments and notify the subscriber
                    foreach (IFragment aFragment in aSequencedFragments)
                    {
                        RequestMessageFragment<_RequestType> aFragmentData = (RequestMessageFragment<_RequestType>)aFragment;
                        if (aFragmentData != null)
                        {
                            try
                            {
                                MessageReceived(this, new TypedSequencedRequestReceivedEventArgs<_RequestType>(e.ResponseReceiverId, aFragmentData));
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
                    EneterTrace.Error(TracedObject + "failed to process the response message.", err);

                    try
                    {
                        MessageReceived(this, new TypedSequencedRequestReceivedEventArgs<_RequestType>(e.ResponseReceiverId, err));
                    }
                    catch (Exception err2)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err2);
                    }
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



        private ISerializer mySerializer;

        private Dictionary<string, int> myResponseSequences = new Dictionary<string, int>();

        private IMultiInstanceFragmentProcessor myMultiInstanceFragmentSequencer;

        protected override string TracedObject
        {
            get
            {
                string aDuplexInputChannelId = (AttachedDuplexInputChannel != null) ? AttachedDuplexInputChannel.ChannelId : "";
                return "The TypedSequencedResponser<" + typeof(_ResponseType).Name + ", " + typeof(_RequestType).Name + "> atached to the duplex input channel '" + aDuplexInputChannelId + "' ";
            }
        }
    }
}
