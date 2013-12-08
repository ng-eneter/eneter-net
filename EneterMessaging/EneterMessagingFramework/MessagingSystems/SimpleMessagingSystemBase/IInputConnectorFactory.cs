/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/



namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal interface IInputConnectorFactory
    {
        IInputConnector CreateInputConnector(string inputConnectorAddress);
    }
}
