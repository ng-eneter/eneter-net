/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/


using System;
using System.Collections.Generic;
using System.Net;

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    /// <summary>
    /// Represents the client on the server side.
    /// </summary>
    /// <remarks>
    /// The client context is obtained when a client opened the connection with the server.
    /// It provides functionality to receive messages from the client and send back response messages.
    /// To see the example refer to  <see cref="WebSocketListener"/>.
    /// </remarks>
    public interface IWebSocketClientContext
    {
        /// <summary>
        /// The event is invoked when the connection with the client was closed.
        /// </summary>
        event EventHandler ConnectionClosed;

        /// <summary>
        /// The event is invoked when the pong message was received.
        /// </summary>
        /// <remarks>
        /// The pong message is sent as a response to ping. According to websocket protocol
        /// unsolicit pong can be sent too. (i.e. it does not have to be a response to a ping)<br/>
        /// </remarks>
        event EventHandler PongReceived;

        /// <summary>
        /// Returns the IP address of the connected client.
        /// </summary>
        IPEndPoint ClientEndPoint { get; }

        /// <summary>
        /// Returns true if the client is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Returns URI of this connection including query parameters sent by the client.
        /// </summary>
        Uri Uri { get; }

        /// <summary>
        /// Returns the readonly dictionary containing header HTTP header fields.
        /// </summary>
        IDictionary<string, string> HeaderFields { get; }

        /// <summary>
        /// Sets or gets the send timeout in miliseconds. Default value is 0 what is infinite time.
        /// </summary>
        int SendTimeout { get; set; }

        /// <summary>
        /// Sets or gets the receive timeout in miliseconds. Default value is 0 what is infinite time.
        /// </summary>
        int ReceiveTimeout { get; set; }

        /// <summary>
        /// Sends message to the client.
        /// </summary>
        /// <remarks>
        /// The message must be type of string or byte[]. If the type is string then the message is sent as the text message via text frame.
        /// If the type is byte[] the message is sent as the binary message via binary frame.
        /// </remarks>
        /// <param name="data">message to be sent to the client. Must be byte[] or string.</param>
        /// <exception cref="ArgumentException">input parameter is not string or byte[].</exception>
        void SendMessage(object data);

        /// <summary>
        /// Sends message to the client. Allows to send the message via multiple frames.
        /// </summary>
        /// <remarks>
        /// The message must be type of string or byte[]. If the type is string then the message is sent as the text message via text frame.
        /// If the type is byte[] the message is sent as the binary message via binary frame.<br/>
        /// <br/>
        /// It allows to send the message in multiple frames. The client then can receive all parts separately
        ///     
        /// <example>
        /// The following example shows how to send 'Hello world.' in three parts.
        /// <code>
        /// void ProcessConnection(IWebSocketClientContext clientContext)
        /// {
        ///     ...
        ///     
        ///     // Send the first part of the message.
        ///     clientContext.SendMessage("Hello ", false);
        ///     
        ///     // Send the second part.
        ///     clientContext.SendMessage("wo", false);
        ///     
        ///     // Send the third final part.
        ///     clientContext.SendMessage("rld.", true);
        ///     
        ///     ...
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="data">message to be sent to the client. The message can be byte[] or string.</param>
        /// <param name="isFinal">true if this is the last part of the message.</param>
        void SendMessage(object data, bool isFinal);

        /// <summary>
        /// Waits until a message is received from the client.
        /// </summary>
        /// <remarks>
        /// <example>
        /// Example shows how to implement a loop receiving the text messages from the client.
        /// <code>
        /// void ProcessConnection(IWebSocketClientContext clientContext)
        /// {
        ///     // The loop waiting for incoming messages.
        ///     // Note: The waiting thread is released when the connection is closed.
        ///     WebSocketMessage aWebSocketMessage;
        ///     while ((aWebSocketMessage = clientContext.ReceiveMessage()) != null)
        ///     {
        ///         if (aWebSocketMessage.IsText)
        ///         {
        ///             // Wait until all data frames are collected
        ///             // and return the message.
        ///             string aMessage = aWebSocketMessage.GetWholeTextMessage();
        ///             ...
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <returns>message</returns>
        WebSocketMessage ReceiveMessage();

        /// <summary>
        /// Pings the client. According to websocket protocol, pong should be responded.
        /// </summary>
        void SendPing();

        /// <summary>
        /// Sends unsolicited pong to the client.
        /// </summary>
        void SendPong();

        /// <summary>
        /// Closes connection with the client.
        /// </summary>
        /// <remarks>
        /// It sends the close message to the client and closes the underlying tcp connection.
        /// </remarks>
        void CloseConnection();
    }
}