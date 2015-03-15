/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/


using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    public enum EMessageBusRequest
    {
        RegisterService = 10,
        ConnectClient = 20,
        DisconnectClient = 30,
        ConfirmClient = 40,
        SendMessage = 50
    }

#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class MessageBusMessage
    {
        public MessageBusMessage()
        {
        }

        public MessageBusMessage(EMessageBusRequest request, string id, object messageData)
        {
            Request = request;
            Id = id;
            MessageData = messageData;
        }

        public EMessageBusRequest Request { get; set; }
        public string Id { get; set; }
        public object MessageData { get; set; }
    }
}
