/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.AuthenticatedConnection
{
    /// <summary>
    /// Callback providing the login message.
    /// </summary>
    /// <remarks>
    /// Returned login message must be String or byte[].
    /// </remarks>
    /// <param name="channelId">service address</param>
    /// <param name="responseReceiverId">unique id representing the connection with the client</param>
    /// <returns>login message (must be String or byte[])</returns>
    public delegate object GetLoginMessage(string channelId, string responseReceiverId);

    /// <summary>
    /// Callback method to get the handshake message.
    /// </summary>
    /// <remarks>
    /// When AuthenticatedDuplexInputChannel receives the login message this callback is called to get
    /// the handshake message.
    /// The handshake message is then sent to the connecting AuthenticatedDuplexOutputChannel which will process it
    /// and send back the handshake response message.<br/>
    /// <br/>
    /// Returned handshake message must be String or byte[].
    /// If it returns null it means the connection will be closed. (e.g. if the login message was not accepted.)
    /// </remarks>
    /// <param name="channelId">service address</param>
    /// <param name="responseReceiverId">unique id representing the connection with the client</param>
    /// <param name="loginMessage">login message received from the client</param>
    /// <returns>handshake message (must be String or byte[])</returns>
    public delegate object GetHanshakeMessage(string channelId, string responseReceiverId, object loginMessage);

    /// <summary>
    /// Callback method to get the response message for the handshake message.
    /// </summary>
    /// <remarks>
    /// When AuthenticatedDuplexOutputChannel receives the handshake message it calls this callback to get
    /// the response message for the handshake message.
    /// The response handshake message is then sent to AuthenticatedDuplexInputChannel which will
    /// then authenticate the connection.<br/>
    /// <br/>
    /// Returned response message must be String or byte[].
    /// If it returns null then it means the handshake message is not accepted and the connection will be closed.
    /// </remarks>
    /// <param name="channelId">service address</param>
    /// <param name="responseReceiverId">unique id representing the connection with the client</param>
    /// <param name="handshakeMessage">handshake message received from the service</param>
    /// <returns>response message (must be String or byte[])</returns>
    public delegate object GetHandshakeResponseMessage(string channelId, string responseReceiverId, object handshakeMessage);

    /// <summary>
    /// Callback method to authenticate the connection.
    /// </summary>
    /// <remarks>
    /// If it returns true the connection will be established.
    /// If it returns false the connection will be closed.
    /// </remarks>
    /// <param name="channelId">service address</param>
    /// <param name="responseReceiverId">unique id representing the connection with the client</param>
    /// <param name="loginMessage">login message that was sent from the client</param>
    /// <param name="handshakeMessage">verification message (question) that service sent to the client</param>
    /// <param name="handshakeResponseMessage">client's response to the handshake message</param>
    /// <returns>true if the authentication passed and the connection can be established.
    /// If false the authentication failed and the connection will be closed.</returns>
    public delegate bool Authenticate(string channelId, string responseReceiverId, object loginMessage, object handshakeMessage, object handshakeResponseMessage);

    /// <summary>
    /// Callback method to handle when the authentication is cancelled.
    /// </summary>
    /// <remarks>
    /// The callback method is used on the service side when the authentication sequence with the client is canceled.
    /// </remarks>
    /// <param name="channelId">service address</param>
    /// <param name="responseReceiverId">unique id representing the connection with the client</param>
    /// <param name="loginMessage">login message that was sent from the client</param>
    public delegate void HandleAuthenticationCancelled(string channelId, string responseReceiverId, object loginMessage);

    /// <summary>
    /// Extension for authentication during connecting.
    /// </summary>
    /// <remarks>
    /// Here is how the authentication procedure works:
    /// <ol>
    /// <li>AuthenticatedDuplexOutputChannel calls getLoginMessage callback and gets the login message. Then
    ///     sends it to AuthenticatedDuplexInputChannel.</li>
    /// <li>AuthenticatedDuplexInputChannel receives the login message and calls getHandshakeMessage callback.
    ///     The returned handshake message is sent to AuthenticatedDuplexOutputChannel.</li>
    /// <li>AuthenticatedDuplexOutputChannel receives the handshake message and calls getHandshakeResponseMessage.
    ///     The returned handshake response message is then sent to AuthenticatedDuplexInputChannel.</li>
    /// <li>AuthenticatedDuplexInputChannel receives the handshake response message and calls authenticate callback.
    ///     if it returns true the connection is established.</li>
    /// </ol>
    /// 
    /// <example>
    /// Service side using the authentication:
    /// <code>
    /// class Program
    /// {
    ///     private static Dictionary&lt;string, string&gt; myUsers = new Dictionary&lt;string, string&gt;();
    ///     private static IDuplexStringMessageReceiver myReceiver;
    /// 
    ///     static void Main(string[] args)
    ///     {
    ///         //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
    /// 
    ///         // Simulate users.
    ///         myUsers["John"] = "password1";
    ///         myUsers["Steve"] = "password2";
    /// 
    ///         // Create TCP based messaging.
    ///         IMessagingSystemFactory aTcpMessaging = new TcpMessagingSystemFactory();
    /// 
    ///         // Connecting clients will be authenticated.
    ///         IMessagingSystemFactory aMessaging = new AuthenticatedMessagingFactory(aTcpMessaging, GetHandshakeMessage, Authenticate);
    ///         IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8092/");
    /// 
    ///         // Use text messages.
    ///         IDuplexStringMessagesFactory aStringMessagesFactory = new DuplexStringMessagesFactory();
    ///         myReceiver = aStringMessagesFactory.CreateDuplexStringMessageReceiver();
    ///         myReceiver.RequestReceived += OnRequestReceived;
    /// 
    ///         // Attach input channel and start listening.
    ///         // Note: using AuthenticatedMessaging will ensure the connection will be established only
    ///         //       if the authentication procedure passes.
    ///         myReceiver.AttachDuplexInputChannel(anInputChannel);
    /// 
    ///         Console.WriteLine("Service is running. Press Enter to stop.");
    ///         Console.ReadLine();
    /// 
    ///         // Detach input channel and stop listening.
    ///         // Note: tis will release the listening thread.
    ///         myReceiver.DetachDuplexInputChannel();
    ///     }
    /// 
    ///     private static void OnRequestReceived(object sender, StringRequestReceivedEventArgs e)
    ///     {
    ///         // Handle received messages here.
    ///         Console.WriteLine(e.RequestMessage);
    /// 
    ///         // Send back the response.
    ///         myReceiver.SendResponseMessage(e.ResponseReceiverId, "Hello");
    ///     }
    /// 
    ///     private static object GetHandshakeMessage(string channelId, string responseReceiverId, object loginMessage)
    ///     {
    ///         // Check if login is ok.
    ///         if (loginMessage is string)
    ///         {
    ///             string aLoginName = (string)loginMessage;
    ///             if (myUsers.ContainsKey(aLoginName))
    ///             {
    ///                 // Login is OK so generate the handshake message.
    ///                 // e.g. generate GUI.
    ///                 return Guid.NewGuid().ToString();
    ///             }
    ///         }
    ///         
    ///         // Login was not ok so there is not handshake message
    ///         // and the connection will be closed.
    ///         EneterTrace.Warning("Login was not ok. The connection will be closed.");
    ///         return null;
    ///     }
    /// 
    ///     private static bool Authenticate(string channelId, string responseReceiverId, object loginMessage,
    ///         object handshakeMessage, object handshakeResponseMessage)
    ///     {
    ///         if (loginMessage is string)
    ///         {
    ///             // Get the password associated with the user.
    ///             string aLoginName = (string) loginMessage;
    ///             string aPassword;
    ///             myUsers.TryGetValue(aLoginName, out aPassword);
    ///             
    ///             // E.g. handshake response may be encrypted original handshake message.
    ///             //      So decrypt incoming handshake response and check if it is equal to original handshake message.
    ///             try
    ///             {
    ///                 ISerializer aSerializer = new AesSerializer(aPassword);
    ///                 string aDecodedHandshakeResponse = aSerializer.Deserialize&lt;string&gt;(handshakeResponseMessage);
    ///                 string anOriginalHandshake = (string) handshakeMessage;
    ///                 if (anOriginalHandshake == aDecodedHandshakeResponse)
    ///                 {
    ///                     // The handshake response is correct so the connection can be established.
    ///                     return true;
    ///                 }
    ///             }
    ///             catch (Exception err)
    ///             {
    ///                 // Decoding of the response message failed.
    ///                 // The authentication will not pass.
    ///                 EneterTrace.Warning("Decoding handshake message failed.", err);
    ///             }
    ///         }
    ///         
    ///         // Authentication did not pass.
    ///         EneterTrace.Warning("Authentication did not pass. The connection will be closed.");
    ///         return false;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// Client using the authentication:
    /// <code>
    /// class Program
    /// {
    ///     private static IDuplexStringMessageSender mySender;
    /// 
    ///     static void Main(string[] args)
    ///     {
    ///         // TCP messaging.
    ///         IMessagingSystemFactory aTcpMessaging = new TcpMessagingSystemFactory();
    ///     
    ///         // Authenticated messaging uses TCP as the underlying messaging.
    ///         IMessagingSystemFactory aMessaging = new AuthenticatedMessagingFactory(aTcpMessaging, GetLoginMessage, GetHandshakeResponseMessage);
    ///         IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8092/");
    ///     
    ///         // Use text messages.
    ///         mySender = new DuplexStringMessagesFactory().CreateDuplexStringMessageSender();
    ///     
    ///         // Subscribe to receive response messages.
    ///         mySender.ResponseReceived += OnResponseMessageReceived;
    ///     
    ///         // Attach output channel and connect the service.
    ///         mySender.AttachDuplexOutputChannel(anOutputChannel);
    ///     
    ///         // Send a message.
    ///         mySender.SendMessage("Hello");
    ///     
    ///         Console.WriteLine("Client sent the message. Press ENTER to stop.");
    ///         Console.ReadLine();
    ///     
    ///         // Detach output channel and stop listening.
    ///         // Note: it releases the tread listening to responses.
    ///         mySender.DetachDuplexOutputChannel();
    ///     }
    /// 
    ///     private static void OnResponseMessageReceived(object sender, StringResponseReceivedEventArgs e)
    ///     {
    ///         // Process the incoming response here.
    ///         Console.WriteLine(e.ResponseMessage);
    ///     }
    /// 
    ///     public static object GetLoginMessage(string channelId, string responseReceiverId)
    ///     {
    ///         return "John";
    ///     }
    /// 
    ///     public static object GetHandshakeResponseMessage(string channelId, string responseReceiverId, object handshakeMessage)
    ///     {
    ///         try
    ///         {
    ///             // Handshake response is encoded handshake message.
    ///             ISerializer aSerializer = new AesSerializer("password1");
    ///             object aHandshakeResponse = aSerializer.Serialize&lt;string&gt;((string)handshakeMessage);
    ///             
    ///             return aHandshakeResponse;
    ///         }
    ///         catch (Exception err)
    ///         {
    ///             EneterTrace.Warning("Processing handshake message failed. The connection will be closed.", err);
    ///         }
    ///         
    ///         return null;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// </remarks>
    public class AuthenticatedMessagingFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs factory that will be used only by a client.
        /// </summary>
        /// <remarks>
        /// The constructor takes only callbacks which are used by the client. Therefore if you use this constructor
        /// you can create only duplex output channels.
        /// </remarks>
        /// <param name="underlyingMessagingSystem">underlying messaging upon which the authentication will work.</param>
        /// <param name="getLoginMessageCallback">callback called by the output channel to get the login message which shall be used to open the connection</param>
        /// <param name="getHandshakeResponseMessageCallback">callback called by the output channel to get the response for the handshake message which was received from the input channel.</param>
        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            GetLoginMessage getLoginMessageCallback,
            GetHandshakeResponseMessage getHandshakeResponseMessageCallback)
            : this(underlyingMessagingSystem, getLoginMessageCallback, getHandshakeResponseMessageCallback, null, null, null)
        {
        }

        /// <summary>
        /// Constructs factory that will be used only by a service.
        /// </summary>
        /// <remarks>
        /// The constructor takes only callbacks which are used by the service. Therefore if you use this constructor
        /// you can create only duplex input channels.
        /// </remarks>
        /// <param name="underlyingMessagingSystem">underlying messaging upon which the authentication will work.</param>
        /// <param name="getHandshakeMessageCallback">callback returning the handshake message.</param>
        /// <param name="authenticateCallback">callback performing the authentication.</param>
        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            GetHanshakeMessage getHandshakeMessageCallback,
            Authenticate authenticateCallback)
            : this(underlyingMessagingSystem, null, null, getHandshakeMessageCallback, authenticateCallback, null)
        {
        }

        /// <summary>
        /// Constructs factory that will be used only by a service.
        /// </summary>
        /// <remarks>
        /// The constructor takes only callbacks which are used by the service. Therefore if you use this constructor
        /// you can create only duplex input channels.
        /// </remarks>
        /// <param name="underlyingMessagingSystem">underlying messaging upon which the authentication will work.</param>
        /// <param name="getHandshakeMessageCallback">callback called by the input chanel to get the handshake message for the login message which was received from the output channel.</param>
        /// <param name="authenticateCallback">callback called by the input channe to perform the authentication.</param>
        /// <param name="handleAuthenticationCancelledCallback">callback called by the input channel to indicate the output channel closed the connection during the authentication procedure.
        /// Can be null if your authentication code does not need to handle it.
        /// (E.g. if the authentication logic needs to clean if the authentication fails.)</param>
        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            GetHanshakeMessage getHandshakeMessageCallback,
            Authenticate authenticateCallback,
            HandleAuthenticationCancelled handleAuthenticationCancelledCallback)
            : this(underlyingMessagingSystem, null, null, getHandshakeMessageCallback, authenticateCallback, handleAuthenticationCancelledCallback)
        {
        }

        /// <summary>
        /// Constructs factory that can be used by client and service simultaneously.
        /// </summary>
        /// <remarks>
        /// If you construct the factory with this constructor you can create both duplex output channels and
        /// duplex input channels.
        /// </remarks>
        /// <param name="underlyingMessagingSystem">underlying messaging upon which the authentication will work.</param>
        /// <param name="getLoginMessageCallback">callback called by the output channel to get the login message which shall be used to open the connection</param>
        /// <param name="getHandshakeResponseMessageCallback">callback called by the output channel to get the response for the handshake message which was received from the input channel.</param>
        /// <param name="getHandshakeMessageCallback">callback called by the input chanel to get the handshake message for the login message which was received from the output channel.</param>
        /// <param name="authenticateCallback">callback called by the input channe to perform the authentication.</param>
        /// <param name="handleAuthenticationCancelledCallback">callback called by the input channel to indicate the output channel closed the connection during the authentication procedure.
        /// Can be null if your authentication code does not need to handle it.
        /// (E.g. if the authentication logic needs to clean if the authentication fails.)</param>
        public AuthenticatedMessagingFactory(IMessagingSystemFactory underlyingMessagingSystem,
            GetLoginMessage getLoginMessageCallback,
            GetHandshakeResponseMessage getHandshakeResponseMessageCallback,
            GetHanshakeMessage getHandshakeMessageCallback,
            Authenticate authenticateCallback,
            HandleAuthenticationCancelled handleAuthenticationCancelledCallback)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingMessaging = underlyingMessagingSystem;
                AuthenticationTimeout = TimeSpan.FromMilliseconds(30000);

                myGetLoginMessageCallback = getLoginMessageCallback;
                myGetHandShakeMessageCallback = getHandshakeMessageCallback;
                myGetHandshakeResponseMessageCallback = getHandshakeResponseMessageCallback;
                myAuthenticateCallback = authenticateCallback;
                myHandleAuthenticationCancelled = handleAuthenticationCancelledCallback;

                OutputChannelThreading = new SyncDispatching();
            }
        }

        /// <summary>
        /// Creates duplex output channel which performs authentication procedure during opening the connection.
        /// </summary>
        /// <remarks>
        /// The created authenticated output channel can throw following exceptions when opening the connection:
        /// <ul>
        /// <li>TimeOutException - if the authentication sequence exceeds the specified AuthenticationTimeout.</li>
        /// <li>InvalidOperationException - if the authentication fails and the conneciton is not granted.</li>
        /// <li>Other underlying messaging specific exception - if it fails to open the connection and start the authentication.</li>
        /// </ul>
        /// </remarks>
        /// <param name="channelId">service address</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                if (myGetLoginMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback to get the login message is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                if (myGetHandshakeResponseMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback to get the response message for handshake is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }


                IDuplexOutputChannel anUnderlyingOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId);
                IThreadDispatcher aThreadDispatcher = OutputChannelThreading.GetDispatcher();
                return new AuthenticatedDuplexOutputChannel(anUnderlyingOutputChannel, myGetLoginMessageCallback, myGetHandshakeResponseMessageCallback, AuthenticationTimeout, aThreadDispatcher);
            }
        }
        
        /// <summary>
        /// Creates duplex output channel which performs authentication procedure during opening the connection.
        /// </summary>
        /// <remarks>
        /// The created authenticated output channel can throw following exceptions when opening the connection:
        /// <ul>
        /// <li>TimeOutException - if the authentication sequence exceeds the specified AuthenticationTimeout.</li>
        /// <li>AuthenticationException - if the authentication fails and the conneciton is not granted.</li>
        /// <li>Other underlying messaging specific exception - if it fails to open the connection and start the authentication.</li>
        /// </ul>
        /// </remarks>
        /// <param name="channelId">service address</param>
        /// <param name="responseReceiverId">unique identifier of the connection with the service.</param>
        /// <returns></returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (myGetLoginMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback to get the login message is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                if (myGetHandshakeResponseMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex output channel because the callback to get the response message for handshake is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                IDuplexOutputChannel anUnderlyingOutputChannel = myUnderlyingMessaging.CreateDuplexOutputChannel(channelId, responseReceiverId);
                IThreadDispatcher aThreadDispatcher = OutputChannelThreading.GetDispatcher();
                return new AuthenticatedDuplexOutputChannel(anUnderlyingOutputChannel, myGetLoginMessageCallback, myGetHandshakeResponseMessageCallback, AuthenticationTimeout, aThreadDispatcher);
            }
        }

        /// <summary>
        /// Creates duplex input channel which performs the authentication procedure.
        /// </summary>
        /// <param name="channelId">service address</param>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                if (myGetHandShakeMessageCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex input channel because the callback to get the handshake message is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                if (myAuthenticateCallback == null)
                {
                    string anErrorMessage = TracedObject + "failed to create duplex input channel because the callback to verify the handshake response message is null.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                IDuplexInputChannel anUnderlyingInputChannel = myUnderlyingMessaging.CreateDuplexInputChannel(channelId);
                return new AuthenticatedDuplexInputChannel(anUnderlyingInputChannel, myGetHandShakeMessageCallback, myAuthenticateCallback, myHandleAuthenticationCancelled);
            }
        }


        /// <summary>
        /// Gets/sets maximum time until the authentication procedure must be performed.
        /// </summary>
        /// <remarks>
        /// The timeout is applied in duplex output channel. If the authentication is not completed within the specified time
        /// TimeoutException is thrown.<br/>
        /// Timeout is set to 30 seconds by default.
        /// </remarks>
        public TimeSpan AuthenticationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the threading mode for the authenticated output channel.
        /// </summary>
        /// <remarks>
        /// When opening connection the authenticated output channel communicates with the authenticated input channel.
        /// During this communication the openConnection() is blocked until the whole authentication communication is performed.
        /// It means if openConnection() is called from the same thread into which the underlying duplex output channel
        /// routes events the openConneciton() would get into the deadusing (ThreadLock.Lock(because the underlying output channel would
        /// route authentication messages into the same thread).<br/>
        /// <br/>
        /// Therefore it is possible to set the threading mode of the authenticated output channel independently. 
        /// </remarks>
        public IThreadDispatcherProvider OutputChannelThreading { get; set; }


        private IMessagingSystemFactory myUnderlyingMessaging;

        private GetLoginMessage myGetLoginMessageCallback;
        private GetHanshakeMessage myGetHandShakeMessageCallback;
        private GetHandshakeResponseMessage myGetHandshakeResponseMessageCallback;
        private Authenticate myAuthenticateCallback;
        private HandleAuthenticationCancelled myHandleAuthenticationCancelled;

        private string TracedObject
        {
            get
            {
                return GetType().Name;
            }
        }
    }
}
