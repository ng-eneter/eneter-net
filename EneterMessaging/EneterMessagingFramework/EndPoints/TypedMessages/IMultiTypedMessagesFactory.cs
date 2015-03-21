/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/


namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Creates senders and receivers of multityped messages.
    /// </summary>
    public interface IMultiTypedMessagesFactory
    {
        /// <summary>
        /// Creates multi typed message sender which can send request messages of various types and receive response messages
        /// of various types.
        /// </summary>
        /// <returns>multi typed message sender</returns>
        IMultiTypedMessageSender CreateMultiTypedMessageSender();

        /// <summary>
        /// Creates multi typed message receiver which can receive messages of various types and send response messages of various types.
        /// </summary>
        /// <returns>multi typed message receiver</returns>
        IMultiTypedMessageReceiver CreateMultiTypedMessageReceiver();
    }
}
