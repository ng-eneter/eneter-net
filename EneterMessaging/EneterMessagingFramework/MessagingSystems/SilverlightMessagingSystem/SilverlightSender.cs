
#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using System.IO;
using Eneter.Messaging.Diagnostic;
using System.Windows.Messaging;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightSender : ISender
    {
        public SilverlightSender(string receiverAddress)
        {
            using (EneterTrace.Entering())
            {
                mySender = new LocalMessageSender(receiverAddress);
            }
        }

        public bool IsStreamWritter
        {
            get { return false; }
        }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                string aMessage = (string)message;

                // Split message and send it in fragments.
                // Note: This is because of Silverlight limitation maximu 40.000 bytes per message.
                //       And because of unicode -> it is 20.000 characters.
                string aSequenceId = Guid.NewGuid().ToString();
                int aStep = myMaxSplitMessageLength;
                int aNumberOfParts = (aMessage.Length % aStep == 0) ? aMessage.Length / aStep : (aMessage.Length / aStep) + 1;

                // Because of Silverlight size restriction we must send the message in fragments.
                // Note: The message will be joined again by the SilverlightInputPort
                for (int i = 0; i < aNumberOfParts; ++i)
                {
                    string aSplitMessagePart = (i < aNumberOfParts - 1) ? aMessage.Substring(i * aStep, aStep) : aMessage.Substring(i * aStep);
                    bool isFinalPart = (i == aNumberOfParts - 1);

                    SilverlightFragmentMessage aSilverlightFragmentMessage = new SilverlightFragmentMessage(aSplitMessagePart, aSequenceId, i, isFinalPart);
                    string aMessageFragment = (string)mySerializer.Serialize<SilverlightFragmentMessage>(aSilverlightFragmentMessage);

                    ToSilverlightThread(() => mySender.SendAsync(aMessageFragment));
                }
            }
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException("SilverlightSender does not support toStreamWritter.");
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