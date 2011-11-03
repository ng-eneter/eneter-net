/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.DataProcessing.Wrapping;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    internal class ChannelUnwrapper : AttachableInputChannelBase, IChannelUnwrapper
    {
        public ChannelUnwrapper(IMessagingSystemFactory outputMessagingFactory, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myOutputMessagingFactory = outputMessagingFactory;
                mySerializer = serializer;
            }
        }


        /// <summary>
        /// The method is called when the channel unwrapper receives a messae to be unwrapped and sent to the
        /// correct output channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                WrappedData aWrappedData = null;

                try
                {
                    // Unwrap the incoming message.
                    aWrappedData = DataWrapper.Unwrap(e.Message, mySerializer);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to unwrap the message.", err);
                    return;
                }

                // WrappedData.AddedData represents the channel id.
                // Therefore if everything is ok then it must be string.
                if (aWrappedData.AddedData is string)
                {
                    string anOutputChannelId = (string)aWrappedData.AddedData;

                    // Get the output channel according to the channel id.
                    IOutputChannel anOutputChannel = myOutputMessagingFactory.CreateOutputChannel(anOutputChannelId);

                    try
                    {
                        // Send the unwrapped message.
                        anOutputChannel.SendMessage(aWrappedData.OriginalData);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed to send the message to the output channel '" + anOutputChannelId + "'.", err);
                    }
                }
                else
                {
                    EneterTrace.Error(TracedObject + "detected that the unwrapped message contian the channel id as the string type.");
                }
            }
        }

        private IMessagingSystemFactory myOutputMessagingFactory;
        private ISerializer mySerializer;

        private string TracedObject
        {
            get 
            {
                string anInputChannelId = (AttachedInputChannel != null) ? AttachedInputChannel.ChannelId : "";
                return "The ChannelUnwrapper attached to the input channel '" + anInputChannelId + "' "; 
            }
        }
    }
}
