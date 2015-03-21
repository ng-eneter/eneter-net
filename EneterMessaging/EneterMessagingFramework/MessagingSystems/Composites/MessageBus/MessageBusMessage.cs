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
    /// <summary>
    /// Internal commands for interaction with the message bus.
    /// </summary>
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
        /// MessageBusMessage Id parameter is client id which sent the message to the service.
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


    /// <summary>
    /// Internal message for interaction with the message bus.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class MessageBusMessage
    {
        /// <summary>
        /// Default constructor available for deserialization.
        /// </summary>
        public MessageBusMessage()
        {
        }

        /// <summary>
        /// Constructs the message.
        /// </summary>
        /// <param name="request">Requested from the message bus.</param>
        /// <param name="id">Depending on the request it is client id or service id.</param>
        /// <param name="messageData">If the request is SendRequestMessage or SendResponseMessage it is the serialized message data.
        /// Otherwise it is null.
        /// </param>
        public MessageBusMessage(EMessageBusRequest request, string id, object messageData)
        {
            Request = request;
            Id = id;
            MessageData = messageData;
        }

        /// <summary>
        /// Request for the message bus.
        /// </summary>
        [DataMember]
        public EMessageBusRequest Request { get; set; }

        /// <summary>
        /// Depending on the request it is client id or service id.
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// If the request is SendRequestMessage or SendResponseMessage it is the serialized message data.
        /// Otherwise it is null.
        /// </summary>
        [DataMember]
        public object MessageData { get; set; }
    }
}
