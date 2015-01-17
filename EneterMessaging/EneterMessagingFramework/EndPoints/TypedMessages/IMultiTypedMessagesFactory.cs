/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/


namespace Eneter.Messaging.EndPoints.TypedMessages
{
    public interface IMultiTypedMessagesFactory
    {
        IMultiTypedMessageSender CreateMultiTypedMessageSender();

        IMultiTypedMessageReceiver CreateMultiTypedMessageReceiver();
    }
}
