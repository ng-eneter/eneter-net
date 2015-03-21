/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.IO;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    internal class LocalProtocolFormatter : IProtocolFormatter
    {
        public object EncodeOpenConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.OpenConnectionRequest, responseReceiverId, null);
                return aProtocolMessage;
            }
        }

        public void EncodeOpenConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            throw new NotSupportedException("This protocol formatter does not support encoding to stream.");
        }

        public object EncodeCloseConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, responseReceiverId, null);
                return aProtocolMessage;
            }
        }

        public void EncodeCloseConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            throw new NotSupportedException("This protocol formatter does not support encoding to stream.");
        }

        public object EncodeMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, responseReceiverId, message);
                return aProtocolMessage;
            }
        }

        public void EncodeMessage(string responseReceiverId, object message, Stream outputSream)
        {
            throw new NotSupportedException("This protocol formatter does not support decoding from stream.");
        }

        public ProtocolMessage DecodeMessage(object readMessage)
        {
            using (EneterTrace.Entering())
            {
                return (ProtocolMessage)readMessage;
            }
        }

        public ProtocolMessage DecodeMessage(Stream readStream)
        {
            throw new NotSupportedException("This protocol formatter does not support decoding from stream.");
        }
    }
}
