/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    internal class LocalProtocolFormatter : ProtocolFormatterBase, IProtocolFormatter
    {
        public override object EncodeOpenConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.OpenConnectionRequest, responseReceiverId, null);
                return aProtocolMessage;
            }
        }

        public override object EncodeCloseConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, responseReceiverId, null);
                return aProtocolMessage;
            }
        }

        public override object EncodeMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, responseReceiverId, message);
                return aProtocolMessage;
            }
        }

        public override ProtocolMessage DecodeMessage(object readMessage)
        {
            using (EneterTrace.Entering())
            {
                return (ProtocolMessage)readMessage;
            }
        }
    }
}
