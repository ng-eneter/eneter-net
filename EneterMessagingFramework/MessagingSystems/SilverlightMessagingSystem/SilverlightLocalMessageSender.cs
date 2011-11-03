/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System.Windows;
using System.Windows.Messaging;
using System;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightLocalMessageSender : ILocalMessageSender
    {
        public SilverlightLocalMessageSender(string receiverName)
        {
            using (EneterTrace.Entering())
            {
                MessageSender = new LocalMessageSender(receiverName);
            }
        }

        public void SendAsync(string message)
        {
            using (EneterTrace.Entering())
            {
                // If we are in the right Silverlight thread then send it directly
                if (Deployment.Current.Dispatcher.CheckAccess())
                {
                    try
                    {
                        MessageSender.SendAsync(message);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error("The sending the message fragment via the Silverlight failed.", err);
                        throw;
                    }
                }
                else
                {
                    // Call this method again from the right Silverlight thread
                    Deployment.Current.Dispatcher.BeginInvoke(new TSendToSilverlight(SendAsync), message);
                }
            }
        }

        public string ReceiverName
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return MessageSender.ReceiverName;
                }
            }
        }

        private LocalMessageSender MessageSender { get; set; }

        private delegate void TSendToSilverlight(string message);
    }
}

#endif
