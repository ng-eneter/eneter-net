/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Declares the output connector which provides a basic low-level functionality to open connection and send messages.
    /// </summary>
    internal interface IOutputConnector
    {
        void OpenConnection(Action<MessageContext> responseMessageHandler);
        void CloseConnection(bool sendCloseMessageFlag);
        bool IsConnected { get; }
        void SendRequestMessage(object message);
    }
}
