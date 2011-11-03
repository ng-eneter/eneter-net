/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    /// <summary>
    /// The interface declares the factory to create LocalMessageSender and LocalMessageReceiver.
    /// The factory is intended for testing purposes if a mock replacing Silverlight sender and receiver is needed.
    /// </summary>
    public interface ILocalSenderReceiverFactory
    {
        /// <summary>
        /// Creates LocalMessageSender.
        /// </summary>
        ILocalMessageSender CreateLocalMessageSender(string receiverName);

        /// <summary>
        /// Creates LocalMessageReceiver.
        /// </summary>
        ILocalMessageReceiver CreateLocalMessageReceiver(string receiverName);
    }
}

#endif
