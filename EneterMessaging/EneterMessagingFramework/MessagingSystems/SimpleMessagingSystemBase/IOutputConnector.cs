﻿


using System;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Declares the output connector which provides a basic low-level functionality to open connection and send messages.
    /// </summary>
    internal interface IOutputConnector
    {
        void OpenConnection(Action<MessageContext> responseMessageHandler);
        void CloseConnection();
        bool IsConnected { get; }
        void SendRequestMessage(object message);
    }
}
