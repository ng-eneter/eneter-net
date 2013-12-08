﻿/*
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
    internal interface IOutputConnector : ISender
    {
        void OpenConnection(Func<MessageContext, bool> responseMessageHandler);
        void CloseConnection();
        bool IsConnected { get; }
    }
}
