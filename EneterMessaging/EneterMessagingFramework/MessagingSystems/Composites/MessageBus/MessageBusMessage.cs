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
        /// <summary>
        /// Used by service when registering to the message bus.
        /// </summary>
        /// <remarks>
        /// MessageBusMessage Id parameter is service id which shall be registered.
        /// </remarks>
        RegisterService = 10,

        /// <summary>
        /// Used by client when connecting the service via the message bus.
        /// </summary>
        /// <remarks>
        /// MessageBusMessage Id parameter is service id which shall be connected.
        /// </remarks>
        ConnectClient = 20,

        /// <summary>
        /// Used by service when it wants to disconnect a particular client.
        /// </summary>
        /// <remarks>
        /// MessageBusMessage Id parameter is client id which shall be disconnected.
        /// </remarks>
        DisconnectClient = 30,

        /// <summary>
        /// Used by service when it confirms the client was connected.
        /// </summary>
        /// <remarks>
        /// MessageBusMessage Id parameter is client id which was connected to the service.
        /// </remarks>
        ConfirmClient = 40,

        /// <summary>
        /// Used by client when sending a message to the service.
        /// </summary>
        /// <remarks>
        /// MessageBusMessage Id is not used. (The association to the service is resolved in the message bus.)
        /// </remarks>
        SendRequestMessage = 50,

        /// <summary>
        /// Used by service when sending message to the client.
        /// </summary>
        /// <remarks>
        /// MessageBusMessage Id parameter is client id which shall receive the message.
        /// </remarks>
        SendResponseMessage = 60
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
