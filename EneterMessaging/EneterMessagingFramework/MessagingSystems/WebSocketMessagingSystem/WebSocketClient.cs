/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !WINDOWS_PHONE_70

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using System.Runtime.CompilerServices;

#if !SILVERLIGHT
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
using System.Net.Sockets;
#endif

#if SILVERLIGHT
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
#endif

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    /// <summary>
    /// WebSocket client.
    /// </summary>
    /// <remarks>
    /// Represents the client for the websocket communication.
    /// <example>
    /// The following example shows how to communicate with a websocket server.
    /// <code>
    /// WebSocketClient aClient = new WebSocketClient("ws://127.0.0.1:8045/MyService/");
    /// 
    /// // Subscribe to receive messages.
    /// aClient.MessageReceived += OnResponseMessageReceived;
    /// 
    /// // Open the connection.
    /// aClient.OpenConnection();
    /// 
    /// // Send a text message.
    /// aClient.SendMessage("Hello.");
    /// 
    /// ....
    /// 
    /// // Handler of response messages.
    /// void OnResponseMessageReceived(object sender, WebSocketMessage e)
    /// {
    ///     // Read the whole text message.
    ///     if (e.IsText)
    ///     {
    ///         string aMessage = e.GetWholeTextMessage();
    ///         ...
    ///     }
    /// }
    /// 
    /// </code>
    /// </example>
    /// </remarks>
    public class WebSocketClient
    {
        private enum EMessageInSendProgress
        {
            None,
            Binary,
            Text
        }

        // Identifies who is responsible for starting the thread listening to response messages.
        // The point is, if nobody is subscribed to receive response messages, then we can significantly improve
        // the performance if the listening thread is not started.
        private enum EResponseListeningResponsible
        {
            // Open connection method is responsible for starting threads that will loop and receive incoming messages.
            OpenConnection,

            // Subscribing to MessageReceived or ConnectionClosed or PongReceived will start threads looping for incoming messages.
            EventSubscription,

            // Looping threads are already running, so nobody is supposed to start it.
            Nobody
        }

        /// <summary>
        /// Event is invoked when the connection is open.
        /// </summary>
        public event EventHandler ConnectionOpened;

        /// <summary>
        /// Event is invoked when the connection is closed.
        /// </summary>
        public event EventHandler ConnectionClosed
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            add
            {
                lock (myConnectionManipulatorLock)
                {
                    if (myResponsibleForActivatingListening == EResponseListeningResponsible.EventSubscription)
                    {
                        ActivateResponseListening();
                    }
                    myConnectionClosedImpl += value;
                }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            remove { myConnectionClosedImpl -= value; }
        }

        /// <summary>
        /// Event is invoked when the pong is received. E.g. when the server responded ping.
        /// </summary>
        public event EventHandler PongReceived
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            add
            {
                lock (myConnectionManipulatorLock)
                {
                    if (myResponsibleForActivatingListening == EResponseListeningResponsible.EventSubscription)
                    {
                        ActivateResponseListening();
                    }
                    myPongReceivedImpl += value;
                }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            remove { myPongReceivedImpl -= value; }
        }

        /// <summary>
        /// The event is invoked when a data message from server is received.
        /// </summary>
        public event EventHandler<WebSocketMessage> MessageReceived
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            add
            {
                lock (myConnectionManipulatorLock)
                {
                    if (myResponsibleForActivatingListening == EResponseListeningResponsible.EventSubscription)
                    {
                        ActivateResponseListening();
                    }
                    myMessageReceivedImpl += value;
                }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            remove { myMessageReceivedImpl -= value; }
        }

#if !SILVERLIGHT

        /// <summary>
        /// Constructs the websocket client.
        /// </summary>
        /// <param name="uri">websocket uri address. Provide port number too. e.g. ws://127.0.0.1:8055/myservice/<br/>
        /// You can also specify the query that can be used to pass some open connection related parameters.
        /// e.g. ws://127.0.0.1:8055/myservice/?param1=10&amp;param2=20
        /// </param>
        public WebSocketClient(Uri uri)
            : this(uri, new NonSecurityFactory())
        {
        }

        /// <summary>
        /// Constructs the websocket client.
        /// </summary>
        /// <param name="uri">websocket uri address. Provide port number too. e.g. ws://127.0.0.1:8055/myservice/<br/>
        /// You can also specify the query that can be used to pass some open connection related parameters.
        /// e.g. ws://127.0.0.1:8055/myservice/?param1=10&amp;param2=20
        /// </param>
        /// <param name="clientSecurityFactory">
        /// Factory allowing SSL communication. <see cref="ClientSslFactory"/>
        /// </param>
        public WebSocketClient(Uri uri, ISecurityFactory clientSecurityFactory)
        {
            using (EneterTrace.Entering())
            {
                Uri = uri;
                mySecurityFactory = clientSecurityFactory;

                HeaderFields = new Dictionary<string, string>();
                HeaderFields["Host"] = Uri.Authority;
                HeaderFields["Upgrade"] = "websocket";
                HeaderFields["Connection"] = "Upgrade";
                HeaderFields["Sec-WebSocket-Version"] = "13";

                ConnectTimeout = 30000;
                SendTimeout = 30000;
            }
        }
#else
        /// <summary>
        /// Constructs the websocket client.
        /// </summary>
        /// <param name="uri">websocket uri address. Provide port number too. e.g. ws://127.0.0.1:8055/myservice/<br/>
        /// You can also specify the query that can be used to pass some open connection related parameters.
        /// e.g. ws://127.0.0.1:8055/myservice/?param1=10&amp;param2=20
        /// </param>
        public WebSocketClient(Uri uri)
        {
            using (EneterTrace.Entering())
            {
                Uri = uri;

                HeaderFields = new Dictionary<string, string>();
                HeaderFields["Host"] = Uri.Host + ":" + Uri.Port;
                HeaderFields["Upgrade"] = "websocket";
                HeaderFields["Connection"] = "Upgrade";
                HeaderFields["Sec-WebSocket-Version"] = "13";

                ConnectTimeout = 30000;
                SendTimeout = 30000;
            }
        }
#endif

        /// <summary>
        /// Sets or gets the connection timeout in miliseconds. Default value is 30000 miliseconds.
        /// </summary>
        public int ConnectTimeout { get; set; }

        /// <summary>
        /// Sets or gets the send timeout in miliseconds. Default value is 30000 miliseconds.
        /// </summary>
        public int SendTimeout { get; set; }

        /// <summary>
        /// Sets or gets the receive timeout in miliseconds. If exceeded the connection is closed. Default value is -1 infinite time.
        /// </summary>
        public int ReceiveTimeout { get; set; }

        /// <summary>
        /// Returns address of websocket server.
        /// </summary>
        public Uri Uri { get; private set; }

        /// <summary>
        /// Allows to get and set header-fields which shall be sent in open connection request.
        /// </summary>
        /// <remarks>
        /// It allows to add your custom header fields that shell be sent in the open connection request.
        /// The header-field Sec-WebSocket-Key is generated and added when OpenConnection() is called.
        /// </remarks>
        public IDictionary<string, string> HeaderFields { get; private set; }

        /// <summary>
        /// Returns true if the connection to the server is open.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return myTcpClient != null && IsResponseSubscribed == false ||
                               myTcpClient != null && IsResponseSubscribed == true && myIsListeningToResponses == true;
                    }
                }
            }
        }

#if !SILVERLIGHT
        /// <summary>
        /// Returns the IP address of the client used for the communication with the server.
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        if (myTcpClient != null)
                        {
                            return myTcpClient.Client.LocalEndPoint as IPEndPoint;
                        }

                        return null;
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Opens connection to the websocket server.
        /// </summary>
        public void OpenConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    // If it is needed clear after previous connection
                    if (myTcpClient != null)
                    {
                        try
                        {
                            ClearConnection(false);
                        }
                        catch
                        {
                            // We tried to clean after the previous connection. The exception can be ignored.
                        }
                    }

                    try
                    {
                        myStopReceivingRequestedFlag = false;

                        // Generate the key for this connection.
                        byte[] aWebsocketKey = new byte[16];
                        myGenerator.NextBytes(aWebsocketKey);
                        string aKey64baseEncoded = Convert.ToBase64String(aWebsocketKey);
                        HeaderFields["Sec-WebSocket-Key"] = aKey64baseEncoded;

                        // Send HTTP request to open the websocket communication.
                        byte[] anOpenRequest = WebSocketFormatter.EncodeOpenConnectionHttpRequest(Uri.AbsolutePath + Uri.Query, HeaderFields);

#if !SILVERLIGHT

                        // Open TCP connection.
                        AddressFamily anAddressFamily = (Uri.HostNameType == UriHostNameType.IPv6) ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                        myTcpClient = new TcpClient(anAddressFamily);
                        myTcpClient.NoDelay = true;
#if !COMPACT_FRAMEWORK
                        // Note: Compact framework does not support these timeouts. - it throws exception.
                        myTcpClient.SendTimeout = SendTimeout;
                        myTcpClient.ReceiveTimeout = ReceiveTimeout;
#endif

                        // Note: TcpClient and Socket do not have a possibility to set the connection timeout.
                        //       There it must be workerounded a little bit.
                        Exception anException = null;
                        ManualResetEvent aConnectionCompletedEvent = new ManualResetEvent(false);
                        ThreadPool.QueueUserWorkItem(x =>
                            {
                                try
                                {
#if !COMPACT_FRAMEWORK
                                    // This call also resolves the host name.
                                    myTcpClient.Connect(Uri.Host, Uri.Port);
#else
                                    // Compact framework has problems with resolving host names.
                                    // Therefore directly IPAddress is used.
                                    myTcpClient.Connect(IPAddress.Parse(Uri.Host), Uri.Port);
#endif
                                }
                                catch (Exception err)
                                {
                                    anException = err;
                                }
                                aConnectionCompletedEvent.Set();
                            });
                        if (!aConnectionCompletedEvent.WaitOne(ConnectTimeout))
                        {
                            throw new TimeoutException(TracedObject + "failed to open connection within " + ConnectTimeout + " ms.");
                        }
                        if (anException != null)
                        {
                            throw anException;
                        }

                        // If SSL then authentication is performed and security stream is provided.
                        myClientStream = mySecurityFactory.CreateSecurityStreamAndAuthenticate(myTcpClient.GetStream());

                        // Send HTTP request opening the connection.
                        myClientStream.Write(anOpenRequest, 0, anOpenRequest.Length);

                        // Get HTTP response and check if the communication was open.
                        Match anHttpResponseRegEx = WebSocketFormatter.DecodeOpenConnectionHttpResponse(myClientStream);
#else
                        // Opening connection in Silverlight.
                        myTcpClient = new TcpClient(Uri.Host, Uri.Port);
                        myTcpClient.ConnectTimeout = ConnectTimeout;
                        myTcpClient.SendTimeout = SendTimeout;
                        myTcpClient.Connect();
                        myTcpClient.Send(anOpenRequest);

                        // Get HTTP response and check if the communication was open.
                        Match anHttpResponseRegEx = WebSocketFormatter.DecodeOpenConnectionHttpResponse(myTcpClient.GetInputStream());
#endif

                        ValidateOpenConnectionResponse(anHttpResponseRegEx, aWebsocketKey);

                        // If somebody is subscribed to receive some response messages then
                        // the bidirectional communication is needed and the listening thread must be activated.
                        if (IsResponseSubscribed)
                        {
                            ActivateResponseListening();
                        }
                        else
                        {
                            // Nobody is subscribed so delegate the responsibility to start listening threads
                            // to the point when somebody subscribes to receive some response messages like
                            // CloseConnection, Pong, MessageReceived.
                            myResponsibleForActivatingListening = EResponseListeningResponsible.EventSubscription;
                        }

                        // Notify opening the websocket connection.
                        // Note: the notification is executed from a different thread.
                        Notify(ConnectionOpened);
                    }
                    catch (Exception err)
                    {
                        try
                        {
                            ClearConnection(false);
                        }
                        catch
                        {
                        }

                        EneterTrace.Error(TracedObject + ErrorHandler.OpenConnectionFailure, err);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Closes connection with the webscocket server.
        /// </summary>
        /// <remarks>
        /// It sends the close message to the service and closes the underlying tcp connection.
        /// </remarks>
        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                ClearConnection(true);
            }
        }

        /// <summary>
        /// Sends message to the server.
        /// </summary>
        /// <remarks>
        /// The message must be type of string or byte[].
        /// If the type is string then the message is sent as the text message via text frame.
        /// If the type is byte[] the message is sent as the binary message via binary frame.
        /// </remarks>
        /// <param name="data">message to be sent. Must be byte[] or string.</param>
        public void SendMessage(object data)
        {
            using (EneterTrace.Entering())
            {
                SendMessage(data, true);
            }
        }

        /// <summary>
        /// Sends message to the server. Allows to send the message via multiple frames.
        /// </summary>
        /// <remarks>
        /// The message must be type of string or byte[].
        /// If the type is string then the message is sent as the text message via text frame.
        /// If the type is byte[] the message is sent as the binary message via binary frame.<br/>
        /// <br/>
        /// It allows to send the message in multiple frames. The server then can receive all parts separately
        /// using WebSocketMessage.InputStream or as a whole message using WebSocketMessage.GetWholeMessage().
        /// <example>
        /// The following example shows how to send 'Hello world.' in three parts.
        /// <code>
        ///     ...
        ///     
        ///     // Send the first part of the message.
        ///     client.SendMessage("Hello ", false);
        ///     
        ///     // Send the second part.
        ///     client.SendMessage("wo", false);
        ///     
        ///     // Send the third final part.
        ///     client.SendMessage("rld.", true);
        ///     
        ///     ...
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="data"></param>
        /// <param name="isFinal"></param>
        public void SendMessage(object data, bool isFinal)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    // If there is no message that was not finalized yet then send the binary or text data frame.
                    if (myMessageInSendProgress == EMessageInSendProgress.None)
                    {
                        if (data is byte[])
                        {
                            SendFrame(maskingKey => WebSocketFormatter.EncodeBinaryMessageFrame(isFinal, maskingKey, (byte[])data));

                            if (isFinal == false)
                            {
                                myMessageInSendProgress = EMessageInSendProgress.Binary;
                            }
                        }
                        else if (data is string)
                        {
                            SendFrame(maskingKey => WebSocketFormatter.EncodeTextMessageFrame(isFinal, maskingKey, (string)data));

                            if (isFinal == false)
                            {
                                myMessageInSendProgress = EMessageInSendProgress.Text;
                            }
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "failed to send the message because input parameter data is not byte[] or string.";
                            EneterTrace.Error(anErrorMessage);
                            throw new ArgumentException(anErrorMessage);
                        }
                    }
                    // If there is a binary message that was sent only partialy - was not finalized yet.
                    else if (myMessageInSendProgress == EMessageInSendProgress.Binary)
                    {
                        if (data is byte[])
                        {
                            SendFrame(maskingKey => WebSocketFormatter.EncodeContinuationMessageFrame(isFinal, maskingKey, (byte[])data));

                            if (isFinal == true)
                            {
                                myMessageInSendProgress = EMessageInSendProgress.None;
                            }
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "failed to send the continuation binary message because input parameter data was not byte[].";
                            EneterTrace.Error(anErrorMessage);
                            throw new ArgumentException(anErrorMessage);
                        }
                    }
                    // If there is a text message that was sent only partialy - was not finalized yet.
                    else
                    {
                        if (data is string)
                        {
                            SendFrame(maskingKey => WebSocketFormatter.EncodeContinuationMessageFrame(isFinal, maskingKey, (string)data));

                            if (isFinal == true)
                            {
                                myMessageInSendProgress = EMessageInSendProgress.None;
                            }
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "failed to send the continuation text message because input parameter data was not string.";
                            EneterTrace.Error(anErrorMessage);
                            throw new ArgumentException(anErrorMessage);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Pings the service. According to websocket protocol, pong should be responded.
        /// </summary>
        public void SendPing()
        {
            using (EneterTrace.Entering())
            {
                SendFrame(maskingKey => WebSocketFormatter.EncodePingFrame(maskingKey));
            }
        }

        /// <summary>
        /// Sends unsolicited pong to the service.
        /// </summary>
        public void SendPong()
        {
            using (EneterTrace.Entering())
            {
                SendFrame(maskingKey => WebSocketFormatter.EncodePongFrame(maskingKey, null));
            }
        }

        private void ValidateOpenConnectionResponse(Match responseRegEx, byte[] webSocketKey)
        {
            using (EneterTrace.Entering())
            {
                if (!responseRegEx.Success)
                {
                    string anErrorMessage = TracedObject + ErrorHandler.OpenConnectionFailure + " The http response was not recognized.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                // Check required header fields.
                IDictionary<string, string> aHeaderFields = WebSocketFormatter.GetHttpHeaderFields(responseRegEx);

                string aSecurityAccept;
                aHeaderFields.TryGetValue("Sec-WebSocket-Accept", out aSecurityAccept);

                // If some required header field is missing or has incorrect value.
                if (!aHeaderFields.ContainsKey("Upgrade") ||
                    !aHeaderFields.ContainsKey("Connection") ||
                    string.IsNullOrEmpty(aSecurityAccept))
                {
                    string anErrorMessage = TracedObject + ErrorHandler.OpenConnectionFailure + " A required header field was missing.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                // Check the value of websocket accept.
                string aWebSocketKeyBase64 = Convert.ToBase64String(webSocketKey);
                string aCalculatedAcceptance = WebSocketFormatter.EncryptWebSocketKey(aWebSocketKeyBase64);
                if (aCalculatedAcceptance != aSecurityAccept)
                {
                    string anErrorMessage = TracedObject + ErrorHandler.OpenConnectionFailure + " Sec-WebSocket-Accept has incorrect value.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }
            }
        }

        private void SendFrame(Func<byte[], byte[]> formatter)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (!IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.SendMessageNotConnectedFailure;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Encode the message frame.
                        byte[] aMaskingKey = GetMaskingKey();
                        byte[] aFrame = formatter(aMaskingKey);

#if !SILVERLIGHT
                        // Send the message.
                        myClientStream.Write(aFrame, 0, aFrame.Length);
#else
                        myTcpClient.Send(aFrame);
#endif
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                        throw;
                    }
                }
            }
        }

        private void ActivateResponseListening()
        {
            using (EneterTrace.Entering())
            {
                if (!myIsListeningToResponses)
                {
                    // Start thread processing decoded messages from the queue.
                    myMessageProcessingThread.RegisterMessageHandler(MessageHandler);

                    // Start listening to frames responded by websocket server.
                    //ThreadPool.QueueUserWorkItem(x => DoResponseListening());
                    myResponseReceiverThread = new Thread(DoResponseListening);
                    myResponseReceiverThread.Start();

                    // Wait until the listening thread is running.
                    myListeningToResponsesStartedEvent.WaitOne(1000);

                    // Listening to response messages is active. So nobody is responsible to activate it.
                    myResponsibleForActivatingListening = EResponseListeningResponsible.Nobody;
                }
            }
        }

        private void DoResponseListening()
        {
            using (EneterTrace.Entering())
            {
                myIsListeningToResponses = true;
                myListeningToResponsesStartedEvent.Set();
                ushort aCloseCode = 0;

                try
                {
                    DynamicStream aContinuousMessageStream = null;

                    while (!myStopReceivingRequestedFlag)
                    {
#if !SILVERLIGHT
                        // Decode the incoming message.
                        WebSocketFrame aFrame = WebSocketFormatter.DecodeFrame(myClientStream);
#else
                        // Decode the incoming message.
                        WebSocketFrame aFrame = WebSocketFormatter.DecodeFrame(myTcpClient.GetInputStream());
#endif
                        if (!myStopReceivingRequestedFlag && aFrame != null)
                        {
                            // Frames from server must be unmasked.
                            // According the protocol, If the frame was masked, the client must close connection with the server.
                            if (aFrame.MaskFlag)
                            {
                                throw new InvalidOperationException(TracedObject + "received masked frame from the server. Frames from server shall be unmasked.");
                            }

                            // Process the frame.
                            if (aFrame.FrameType == EFrameType.Ping)
                            {
                                // Response 'pong'. The response responses same data as received in the 'ping'.
                                SendFrame(maskingKey => WebSocketFormatter.EncodePongFrame(maskingKey, aFrame.Message));
                            }
                            else if (aFrame.FrameType == EFrameType.Close)
                            {
                                EneterTrace.Debug(TracedObject + "received the close frame.");
                                break;
                            }
                            else if (aFrame.FrameType == EFrameType.Pong)
                            {
                                Notify(myPongReceivedImpl);
                            }
                            // If a new message starts.
                            else if (aFrame.FrameType == EFrameType.Binary || aFrame.FrameType == EFrameType.Text)
                            {
                                // If a previous message is not finished then the new message is not expected -> protocol error.
                                if (aContinuousMessageStream != null)
                                {
                                    EneterTrace.Warning(TracedObject + "detected unexpected new message. (previous message was not finished)");

                                    // Protocol error close code.
                                    aCloseCode = 1002;
                                    break;
                                }

                                WebSocketMessage aReceivedMessage = null;

                                // If the message does not come in multiple frames then optimize the performance
                                // and use MemoryStream instead of DynamicStream.
                                if (aFrame.IsFinal)
                                {
                                    MemoryStream aMessageStream = new MemoryStream(aFrame.Message);
                                    aReceivedMessage = new WebSocketMessage(aFrame.FrameType == EFrameType.Text, aMessageStream);
                                }
                                // if the message is split to several frames then use DynamicStream so that writing of incoming
                                // frames and reading of already received data can run in parallel.
                                else
                                {
                                    // Create stream where the message data will be writen.
                                    aContinuousMessageStream = new DynamicStream();
                                    aContinuousMessageStream.Write(aFrame.Message, 0, aFrame.Message.Length);
                                    aReceivedMessage = new WebSocketMessage(aFrame.FrameType == EFrameType.Text, aContinuousMessageStream);
                                }

                                // Put received message to the queue from where the processing thread will invoke the event MessageReceived.
                                // Note: user will get events always in the same thread.
                                myMessageProcessingThread.EnqueueMessage(aReceivedMessage);
                            }
                            // If a message continues. (I.e. message is split into more fragments.)
                            else if (aFrame.FrameType == EFrameType.Continuation)
                            {
                                // If the message does not exist then continuing frame does not have any sense -> protocol error.
                                if (aContinuousMessageStream == null)
                                {
                                    EneterTrace.Warning(TracedObject + "detected unexpected continuing of a message. (none message was started before)");

                                    // Protocol error close code.
                                    aCloseCode = 1002;
                                    break;
                                }

                                aContinuousMessageStream.Write(aFrame.Message, 0, aFrame.Message.Length);

                                // If this is the final frame.
                                if (aFrame.IsFinal)
                                {
                                    aContinuousMessageStream.IsBlockingMode = false;
                                    aContinuousMessageStream = null;
                                }
                            }
                        }

                        // If disconnected
                        if (aFrame == null)
                        {
                            //EneterTrace.Warning(TracedObject + "detected the TCP connection is not available. The connection will be closed.");
                            break;
                        }
                    }
                }
                catch (IOException)
                {
                    // Ignore this exception. It is often thrown when the connection was closed.
                    // Do not thrace this because the tracing degradates the performance in this case.
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.DoListeningFailure, err);
                }

                // If the connection is being closed due to a protocol error.
                if (aCloseCode > 1000)
                {
                    // Try to send the close message.
                    try
                    {
                        byte[] aMaskingKey = GetMaskingKey();
                        byte[] aCloseMessage = WebSocketFormatter.EncodeCloseFrame(aMaskingKey, aCloseCode);

#if !SILVERLIGHT
                        myClientStream.Write(aCloseMessage, 0, aCloseMessage.Length);
#else
                        myTcpClient.Send(aCloseMessage);
#endif
                    }
                    catch
                    {
                    }
                }

                // Stop the thread processing messages
                try
                {
                    myMessageProcessingThread.UnregisterMessageHandler();
                }
                catch
                {
                    // We need just to close it, therefore we are not interested about exception.
                    // They can be ignored here.
                }

                myIsListeningToResponses = false;
                myListeningToResponsesStartedEvent.Reset();

                // Notify the listening to messages stopped.
                Notify(myConnectionClosedImpl);
            }
        }

        private void ClearConnection(bool sendCloseMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myStopReceivingRequestedFlag = true;
                    myMessageInSendProgress = EMessageInSendProgress.None;

                    if (myTcpClient != null)
                    {
#if !SILVERLIGHT
                        // Try to send the frame closing the communication.
                        if (myClientStream != null)
                        {
                            try
                            {
                                if (sendCloseMessageFlag)
                                {
                                    // Generate the masking key.
                                    byte[] aMaskingKey = GetMaskingKey();
                                    byte[] aCloseFrame = WebSocketFormatter.EncodeCloseFrame(aMaskingKey, 1000);
                                    myClientStream.Write(aCloseFrame, 0, aCloseFrame.Length);
                                }
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                            }

                            myClientStream.Close();
                        }
#else
                        try
                        {
                            // Generate the masking key.
                            byte[] aMaskingKey = GetMaskingKey();
                            byte[] aCloseFrame = WebSocketFormatter.EncodeCloseFrame(aMaskingKey, 1000);
                            myTcpClient.Send(aCloseFrame);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                        }
#endif

                        try
                        {
                            myTcpClient.Close();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to close Tcp connection.", err);
                        }

                        myTcpClient = null;
                    }

#if COMPACT_FRAMEWORK
                    if (myResponseReceiverThread != null)
#else
                    if (myResponseReceiverThread != null && myResponseReceiverThread.ThreadState != ThreadState.Unstarted)
#endif
                    {
                        if (!myResponseReceiverThread.Join(3000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myResponseReceiverThread.ManagedThreadId);

                            try
                            {
                                myResponseReceiverThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                            }
                        }
                    }
                    myResponseReceiverThread = null;

                    try
                    {
                        myMessageProcessingThread.UnregisterMessageHandler();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.UnregisterMessageHandlerThreadFailure, err);
                    }

                    // Reset the responsibility for starting of threads looping for response messages.
                    myResponsibleForActivatingListening = EResponseListeningResponsible.OpenConnection;
                }
            }
        }

        private byte[] GetMaskingKey()
        {
            byte[] aKey = new byte[4];
            myGenerator.NextBytes(aKey);
            return aKey;
        }

        private void MessageHandler(WebSocketMessage message)
        {
            using (EneterTrace.Entering())
            {
                if (myMessageReceivedImpl != null)
                {
                    try
                    {
                        myMessageReceivedImpl(this, message);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }

        private void Notify(EventHandler eventHandler)
        {
            using (EneterTrace.Entering())
            {
                WaitCallback aConnectionOpenedInvoker = x =>
                {
                    using (EneterTrace.Entering())
                    {
                        try
                        {
                            if (eventHandler != null)
                            {
                                eventHandler(this, new EventArgs());
                            }
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                };

                // Invoke the event in a different thread.
                ThreadPool.QueueUserWorkItem(aConnectionOpenedInvoker);
            }
        }

        private bool IsResponseSubscribed
        {
            get
            {
                // If somebody is subscribed for response messages, then we need bidirectional communication.
                // Note: It means, the thread listening to responses and the thread responsible for processing messages from the queue
                //       are supposed to be started.
                return myMessageReceivedImpl != null || myConnectionClosedImpl != null || myPongReceivedImpl != null;
            }
        }

#if !SILVERLIGHT
        private ISecurityFactory mySecurityFactory;
        private TcpClient myTcpClient;
#else
        private TcpClient myTcpClient;
#endif

        private Random myGenerator = new Random();

#if !SILVERLIGHT
        private Stream myClientStream;
#endif

        private object myConnectionManipulatorLock = new object();
        private EResponseListeningResponsible myResponsibleForActivatingListening = EResponseListeningResponsible.OpenConnection;

        private Thread myResponseReceiverThread;
        private bool myStopReceivingRequestedFlag;
        private bool myIsListeningToResponses;
        private ManualResetEvent myListeningToResponsesStartedEvent = new ManualResetEvent(false);

        private EMessageInSendProgress myMessageInSendProgress = EMessageInSendProgress.None;

        private WorkingThread<WebSocketMessage> myMessageProcessingThread = new WorkingThread<WebSocketMessage>();

        private EventHandler myConnectionClosedImpl;
        private EventHandler myPongReceivedImpl;
        private EventHandler<WebSocketMessage> myMessageReceivedImpl;


        private string TracedObject
        {
            get
            {
                string anAddress = (Uri != null) ? Uri.ToString() : "";
                return GetType().Name + " '" + anAddress + "' ";
            }
        }
    }
}


#endif