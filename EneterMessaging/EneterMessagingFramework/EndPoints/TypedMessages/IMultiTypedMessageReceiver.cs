

using System;
using System.Collections.Generic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Receiver for multiple message types.
    /// </summary>
    /// <remarks>
    /// It is a service component which can receive and send messages of multiple types.<br/>
    /// The following example shows how to create a service which can receive messages of various types:
    /// <example>
    /// <code>
    /// // Create multityped receiver
    /// IMultiTypedMessagesFactory aFactory = new MultiTypedMessagesFactory();
    /// IMultiTypedMessageReceiver aReceiver = aFactory.CreateMultiTypedMessageReceiver();
    /// 
    /// // Register handlers for message types which can be received.
    /// aReceiver.RegisterRequestMessageReceiver&lt;Alarm&gt;(OnAlarmMessage);
    /// aReceiver.RegisterRequestMessageReceiver&lt;Image&gt;(OnImageMessage);
    /// 
    /// // Attach input channel and start listening. E.g. using TCP.
    /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:9043/");
    /// aReceiver.AttachDuplexInputChannel(anInputChannel);
    /// 
    /// Console.WriteLine("Service is running. Press ENTER to stop.");
    /// Console.ReadLine();
    /// 
    /// // Detach input channel and stop listening.
    /// aReceiver.DetachInputChannel();
    /// 
    /// 
    /// private void OnAlarmMessage(object sender, TypedRequestReceivedEventArgs&lt;Alarm&gt; e)
    /// {
    ///    // Get alarm message data.
    ///    Alarm anAlarm = e.RequestMessage;
    /// 
    ///    ...
    /// 
    ///    // Send response message.
    ///    aReceiver.SendResponseMessage&lt;ResponseMessage&gt;(e.getResponseReceiverId(), aResponseMessage);
    /// }
    /// 
    /// private void onImageMessage(object sender, TypedRequestReceivedEventArgs&lt;Image&gt; e)
    /// {
    ///    // Get image message data.
    ///    Image anImage = e.RequestMessage;
    /// 
    ///    ...
    /// 
    ///    // Send response message.
    ///    aReceiver.SendResponseMessage&lt;ResponseMessage&gt;(e.getResponseReceiverId(), aResponseMessage);
    /// }
    /// 
    /// </code>
    /// </example>
    /// </remarks>
    public interface IMultiTypedMessageReceiver : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// Raised when a new client is connected.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// Raised when a client closed the connection.
        /// </summary>
        /// <remarks>
        /// The event is raised only if the connection was closed by the client.
        /// It is not raised if the client was disconnected by IDuplexInputChannel.DisconnectResponseReceiver(...). 
        /// </remarks>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Registers message handler for specified message type.
        /// </summary>
        /// <remarks>
        /// If the specified message type is received the handler will be called to process it.
        /// </remarks>
        /// <typeparam name="T">Type of the message the handler shall process.</typeparam>
        /// <param name="handler">The callback method which will be called to process the message of the given type.</param>
        void RegisterRequestMessageReceiver<T>(EventHandler<TypedRequestReceivedEventArgs<T>> handler);

        /// <summary>
        /// Unregisters the message handler for the specified message type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void UnregisterRequestMessageReceiver<T>();

        /// <summary>
        /// Returns the list of registered message types which can be received. 
        /// </summary>
        IEnumerable<Type> RegisteredRequestMessageTypes { get; }

        /// <summary>
        /// Sends the response message.
        /// </summary>
        /// <remarks>
        /// The message of the specified type will be serialized and sent back to the response receiver.
        /// If the response receiver has registered a handler for this message type then the handler will be called to process the message.
        /// </remarks>
        /// <typeparam name="TResponseMessage">Type of the message.</typeparam>
        /// <param name="responseReceiverId">Identifies response receiver which will receive the message.
        /// If responseReceiverId is * then the broadcast message
        /// to all connected clients is sent.
        /// <example>
        /// <code>
        /// // Send broadcast to all connected clients.
        /// aReceiver.SendResponseMessage&lt;YourBroadcast&gt;("*", aBroadcastMessage);
        /// </code>
        /// </example>
        /// </param>
        /// <param name="responseMessage">response message</param>
        void SendResponseMessage<TResponseMessage>(string responseReceiverId, TResponseMessage responseMessage);
    }
}
