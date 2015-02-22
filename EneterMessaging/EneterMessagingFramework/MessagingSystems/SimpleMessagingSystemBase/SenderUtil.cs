/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal static class SenderUtil
    {
        public static void SendOpenConnection(ISender sender, string responseReceiverId, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                if (sender.IsStreamWritter)
                {
                    sender.SendMessage(x => protocolFormatter.EncodeOpenConnectionMessage(responseReceiverId, x));
                }
                else
                {
                    object anEncodedMessage = protocolFormatter.EncodeOpenConnectionMessage(responseReceiverId);

                    // If open connection message is supported by the protocol formatter.
                    // Note: if it is null it means the connection can be open without an explicit open connection message.
                    if (anEncodedMessage != null)
                    {
                        sender.SendMessage(anEncodedMessage);
                    }
                }
            }
        }

        public static void SendCloseConnection(ISender sender, string responseReceiverId, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                if (sender.IsStreamWritter)
                {
                    sender.SendMessage(x => protocolFormatter.EncodeCloseConnectionMessage(responseReceiverId, x));
                }
                else
                {
                    object anEncodedMessage = protocolFormatter.EncodeCloseConnectionMessage(responseReceiverId);

                    // If close connection message is supported by the protocol formatter.
                    // Note: if it is null it means the connection can be closed without an explicit close connection message.
                    if (anEncodedMessage != null)
                    {
                        sender.SendMessage(anEncodedMessage);
                    }
                }
            }
        }

        public static void SendMessage(ISender sender, string responseReceiverId, object message, IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                if (sender.IsStreamWritter)
                {
                    sender.SendMessage(x => protocolFormatter.EncodeMessage(responseReceiverId, message, x));
                }
                else
                {
                    object anEncodedMessage = protocolFormatter.EncodeMessage(responseReceiverId, message);
                    sender.SendMessage(anEncodedMessage);
                }
            }
        }
    }
}
