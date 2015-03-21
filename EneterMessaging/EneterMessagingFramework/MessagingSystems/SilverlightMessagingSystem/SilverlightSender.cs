/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using System.Windows;
using System.Windows.Messaging;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightSender
    {
        public SilverlightSender(string receiverAddress)
        {
            using (EneterTrace.Entering())
            {
                mySender = new LocalMessageSender(receiverAddress);
            }
        }

        public void SendMessage(string message)
        {
            using (EneterTrace.Entering())
            {
                // Split message and send it in fragments.
                // Note: This is because of Silverlight limitation maximu 40.000 bytes per message.
                //       And because of unicode -> it is 20.000 characters.
                string aSequenceId = Guid.NewGuid().ToString();
                int aStep = myMaxSplitMessageLength;
                int aNumberOfParts = (message.Length % aStep == 0) ? message.Length / aStep : (message.Length / aStep) + 1;

                // Because of Silverlight size restriction we must send the message in fragments.
                // Note: The message will be joined again by the SilverlightInputPort
                for (int i = 0; i < aNumberOfParts; ++i)
                {
                    string aSplitMessagePart = (i < aNumberOfParts - 1) ? message.Substring(i * aStep, aStep) : message.Substring(i * aStep);
                    bool isFinalPart = (i == aNumberOfParts - 1);

                    SilverlightFragmentMessage aSilverlightFragmentMessage = new SilverlightFragmentMessage(aSplitMessagePart, aSequenceId, i, isFinalPart);
                    string aMessageFragment = (string)mySerializer.Serialize<SilverlightFragmentMessage>(aSilverlightFragmentMessage);

                    ToSilverlightThread(() => mySender.SendAsync(aMessageFragment));
                }
            }
        }

        private void ToSilverlightThread(Action a)
        {
            if (Deployment.Current.Dispatcher.CheckAccess())
            {
                a();
            }
            else
            {
                // Call this method again from the right Silverlight thread
                Deployment.Current.Dispatcher.BeginInvoke(a);
            }
        }

        private LocalMessageSender mySender;
        private ISerializer mySerializer = new XmlStringSerializer();
        private readonly int myMaxSplitMessageLength = 12000;
    }
}

#endif