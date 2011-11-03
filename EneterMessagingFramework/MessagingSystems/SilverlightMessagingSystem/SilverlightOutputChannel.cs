/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT
#if !WINDOWS_PHONE

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Diagnostic;
using System.IO;
using Eneter.Messaging.DataProcessing.Streaming;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightOutputChannel : IOutputChannel
    {
        public SilverlightOutputChannel(string channelId, ILocalSenderReceiverFactory localSenderReceiverFactory)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    string anError = TracedObject + "failed during construction because the specified channel id is null or empty string.";
                    EneterTrace.Error(anError);
                    throw new ArgumentException(anError);
                }

                ChannelId = channelId;
                myMessageSender = localSenderReceiverFactory.CreateLocalMessageSender(ChannelId);
            }
        }

        public string ChannelId { get; set; }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                if (message is string == false)
                {
                    string anError = TracedObject + "failed to send the message. Messages in Silverlight must be serialized into string.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                string aMessage = (string)message;

                try
                {
                    // Split message and send it in fragments.
                    // Note: This is because of Silverlight limitation maximu 40.000 bytes per message.
                    //       And because of unicode -> it is 20.000 characters.
                    string aSequenceId = Guid.NewGuid().ToString();
                    int aStep = myMaxSplitMessageLength;
                    int aNumberOfParts = (aMessage.Length % aStep == 0) ? aMessage.Length / aStep : (aMessage.Length / aStep) + 1;

                    // Because the Silverlight size restriction we must send the message in fragments.
                    // Note: The message will be joined again by the SilverlightInputPort
                    for (int i = 0; i < aNumberOfParts; ++i)
                    {
                        string aSplitMessagePart = (i < aNumberOfParts - 1) ? aMessage.Substring(i * aStep, aStep) : aMessage.Substring(i * aStep);
                        bool isFinalPart = (i == aNumberOfParts - 1);

                        SilverlightFragmentMessage aSilverlightFragmentMessage = new SilverlightFragmentMessage(aSplitMessagePart, aSequenceId, i, isFinalPart);
                        string aMessageObject = (string)mySerializer.Serialize<SilverlightFragmentMessage>(aSilverlightFragmentMessage);

                        SendInSilverlightThread(aMessageObject);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        private void SendInSilverlightThread(string message)
        {
            using (EneterTrace.Entering())
            {
                myMessageSender.SendAsync(message);
            }
        }

        private ILocalMessageSender myMessageSender;

        private readonly int myMaxSplitMessageLength = 12000;

        private ISerializer mySerializer = new XmlStringSerializer();

        private string TracedObject
        {
            get
            {
                return "The silverlight output channel '" + ChannelId + "' ";
            }
        }
    }
}

#endif
#endif