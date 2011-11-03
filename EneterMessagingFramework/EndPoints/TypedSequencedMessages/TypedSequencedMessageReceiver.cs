/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.Sequencing;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    internal class TypedSequencedMessageReceiver<_MessageDataType> : AttachableInputChannelBase, ITypedSequencedMessageReceiver<_MessageDataType>
    {
        /// <summary>
        /// The event is invoked when the typed message has received.
        /// </summary>
        public event EventHandler<TypedSequencedMessageReceivedEventArgs<_MessageDataType>> MessageReceived;


        public TypedSequencedMessageReceiver(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                // Create multisequence processor processing sequenceses by sequencing.
                // Note: It does not wait until the whole sequence is processed but returns ordered sequences dynamically.
                FragmentDataFactory aFragmentDataFactory = new FragmentDataFactory();
                myMultiInstanceFragmentSequencer = aFragmentDataFactory.CreateMultiinstanceFragmentProcessor(aFragmentDataFactory.CreateFragmentSequencer);

                mySerializer = serializer;
            }
        }

        protected override void OnMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    try
                    {
                        // Create instance of the message.
                        TypedMessageFragment<_MessageDataType> aMessageFragment = mySerializer.Deserialize<TypedMessageFragment<_MessageDataType>>(e.Message);

                        // Get fragments following the fragment.
                        IEnumerable<IFragment> aSequencedFragments = myMultiInstanceFragmentSequencer.ProcessFragment(aMessageFragment);

                        // Go via fragments and notify the user.
                        foreach (IFragment aFragment in aSequencedFragments)
                        {
                            TypedMessageFragment<_MessageDataType> aFragmentData = aFragment as TypedMessageFragment<_MessageDataType>;
                            if (aFragmentData != null)
                            {
                                try
                                {
                                    MessageReceived(this, new TypedSequencedMessageReceivedEventArgs<_MessageDataType>(aFragmentData));
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
                        string aMessage = TracedObject + "failed to process the incoming message.";
                        EneterTrace.Error(aMessage, err);

                        try
                        {
                            MessageReceived(this, new TypedSequencedMessageReceivedEventArgs<_MessageDataType>(err));
                        }
                        catch (Exception err2)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err2);
                        }
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }


        private IMultiInstanceFragmentProcessor myMultiInstanceFragmentSequencer;

        private ISerializer mySerializer;

        private string TracedObject
        {
            get
            {
                string anInputChannelId = (AttachedInputChannel != null) ? AttachedInputChannel.ChannelId : "";
                return "The TypedSequencedMessageReceiver<" + typeof(_MessageDataType).Name + "> atached to the duplex input channel '" + anInputChannelId + "' ";
            }
        }

    }
}
