/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using System;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Factory to create typed message senders and receivers.
    /// </summary>
    /// <remarks>
    /// The following example shows how to send a receive messages:
    /// <example>
    /// Implementation of receiver:
    /// <code>
    /// using System;
    /// using Eneter.Messaging.EndPoints.TypedMessages;
    /// using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
    /// using Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem;
    /// 
    /// namespace CalculatorService
    /// {
    ///     // Request message.
    ///     public class RequestMessage
    ///     {
    ///         public int Number1 { get; set; }
    ///         public int Number2 { get; set; }
    ///     }
    /// 
    ///     // Response message.
    ///     public class ResponseMessage
    ///     {
    ///         public int Result { get; set; }
    ///     }
    /// 
    ///     class Program
    ///     {
    ///         static void Main(string[] args)
    ///         {
    ///             // Create message receiver.
    ///             IDuplexTypedMessagesFactory aReceiverFactory = new DuplexTypedMessagesFactory();
    ///             IDuplexTypedMessageReceiver&lt;ResponseMessage, RequestMessage&gt; aReceiver =
    ///                 aReceiverFactory.CreateDuplexTypedMessageReceiver&lt;ResponseMessage, RequestMessage&gt;();
    /// 
    ///             // Subscribe to process request messages.
    ///             aReceiver.MessageReceived += OnMessageReceived;
    /// 
    ///             // Use WebSocket for the communication.
    ///             // Note: You can also other messagings. E.g. TcpMessagingSystemFactory
    ///             IMessagingSystemFactory aMessaging = new WebSocketMessagingSystemFactory();
    ///             IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("ws://192.168.1.102:8099/aaa/");
    /// 
    ///             // Attach the input channel to the receiver and start listening.
    ///             aReceiver.AttachDuplexInputChannel(anInputChannel);
    /// 
    ///             Console.WriteLine("The calculator service is running. Press ENTER to stop.");
    ///             Console.ReadLine();
    /// 
    ///             // Detach the input channel to stop listening.
    ///             aReceiver.DetachDuplexInputChannel();
    ///         }
    /// 
    ///         private static void OnMessageReceived(object sender, TypedRequestReceivedEventArgs&lt;RequestMessage&gt; e)
    ///         {
    ///             // Calculate numbers.
    ///             ResponseMessage aResponseMessage = new ResponseMessage();
    ///             aResponseMessage.Result = e.RequestMessage.Number1 + e.RequestMessage.Number2;
    /// 
    ///             Console.WriteLine("{0} + {1} = {2}", e.RequestMessage.Number1, e.RequestMessage.Number2, aResponseMessage.Result);
    /// 
    ///             // Send back the response message.
    ///             var aReceiver = (IDuplexTypedMessageReceiver&lt;ResponseMessage, RequestMessage&gt;)sender;
    ///             aReceiver.SendResponseMessage(e.ResponseReceiverId, aResponseMessage);
    ///         }
    ///     }
    /// }
    /// 
    /// </code>
    /// </example>
    /// <example>
    /// Implementation of sender:
    /// <code>
    /// using System;
    /// using System.Windows.Forms;
    /// using Eneter.Messaging.EndPoints.TypedMessages;
    /// using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
    /// using Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem;
    /// 
    /// namespace CalculatorClient
    /// {
    ///     public partial class Form1 : Form
    ///     {
    ///         // Request message.
    ///         public class RequestMessage
    ///         {
    ///             public int Number1 { get; set; }
    ///             public int Number2 { get; set; }
    ///         }
    /// 
    ///         // Response message.
    ///         public class ResponseMessage
    ///         {
    ///             public int Result { get; set; }
    ///         }
    /// 
    ///         private IDuplexTypedMessageSender&lt;ResponseMessage, RequestMessage&gt; mySender;
    /// 
    ///         public Form1()
    ///         {
    ///             InitializeComponent();
    /// 
    ///             OpenConnection();
    ///         }
    /// 
    ///         private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    ///         {
    ///             CloseConnection();
    ///         }
    /// 
    /// 
    ///         private void OpenConnection()
    ///         {
    ///             // Create the message sender.
    ///             IDuplexTypedMessagesFactory aSenderFactory = new DuplexTypedMessagesFactory();
    ///             mySender = aSenderFactory.CreateDuplexTypedMessageSender&lt;ResponseMessage, RequestMessage&gt;();
    /// 
    ///             // Subscribe to receive response messages.
    ///             mySender.ResponseReceived += OnResponseReceived;
    /// 
    ///             // Use Websocket for the communication.
    ///             // If you want to use TCP then use TcpMessagingSystemFactory().
    ///             IMessagingSystemFactory aMessaging = new WebSocketMessagingSystemFactory();
    ///             IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("ws://192.168.1.102:8099/aaa/");
    /// 
    ///             // Attach the output channel and be able to send messages
    ///             // and receive response messages.
    ///             mySender.AttachDuplexOutputChannel(anOutputChannel);
    ///         }
    /// 
    ///         private void CloseConnection()
    ///         {
    ///             // Detach input channel and stop listening to response messages.
    ///             mySender.DetachDuplexOutputChannel();
    ///         }
    /// 
    ///         private void CalculateBtn_Click(object sender, EventArgs e)
    ///         {
    ///             // Create the request message.
    ///             RequestMessage aRequest = new RequestMessage();
    ///             aRequest.Number1 = int.Parse(Number1TextBox.Text);
    ///             aRequest.Number2 = int.Parse(Number2TextBox.Text);
    /// 
    ///             // Send request to the service to calculate 2 numbers.
    ///             mySender.SendRequestMessage(aRequest);
    ///         }
    /// 
    ///         private void OnResponseReceived(object sender, TypedResponseReceivedEventArgs&lt;ResponseMessage&gt; e)
    ///         {
    ///             // Display the result using the UI thread.
    ///             UI(() =&gt; ResultTextBox.Text = e.ResponseMessage.Result.ToString());
    ///         }
    /// 
    ///         // Helper method to invoke a delegate in the UI thread.
    ///         // Note: You can manipulate UI controls only from the UI tread.
    ///         private void UI(Action action)
    ///         {
    ///             if (InvokeRequired)
    ///             {
    ///                 Invoke(action);
    ///             }
    ///             else
    ///             {
    ///                 action();
    ///             }
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <example>
    /// Implementation of synchronous sender (after sending it waits for the response):
    /// <code>
    /// using System;
    /// using System.Windows.Forms;
    /// using Eneter.Messaging.EndPoints.TypedMessages;
    /// using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
    /// using Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem;
    /// 
    /// namespace CalculatorClientSync
    /// {
    ///     public partial class Form1 : Form
    ///     {
    ///         // Request message.
    ///         public class RequestMessage
    ///         {
    ///             public int Number1 { get; set; }
    ///             public int Number2 { get; set; }
    ///         }
    /// 
    ///         // Response message.
    ///         public class ResponseMessage
    ///         {
    ///             public int Result { get; set; }
    ///         }
    /// 
    ///         private ISyncDuplexTypedMessageSender&lt;ResponseMessage, RequestMessage&gt; mySender;
    /// 
    ///         public Form1()
    ///         {
    ///             InitializeComponent();
    /// 
    ///             OpenConnection();
    ///         }
    /// 
    ///         private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    ///         {
    ///             CloseConnection();
    ///         }
    /// 
    ///         private void OpenConnection()
    ///         {
    ///             // Create the message sender.
    ///             IDuplexTypedMessagesFactory aSenderFactory = new DuplexTypedMessagesFactory();
    ///             mySender = aSenderFactory.CreateSyncDuplexTypedMessageSender&lt;ResponseMessage, RequestMessage&gt;();
    /// 
    ///             // Use Websocket for the communication.
    ///             // If you want to use TCP then use TcpMessagingSystemFactory().
    ///             IMessagingSystemFactory aMessaging = new WebSocketMessagingSystemFactory();
    ///             IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("ws://192.168.1.102:8099/aaa/");
    /// 
    ///             // Attach the output channel and be able to send messages
    ///             // and receive response messages.
    ///             mySender.AttachDuplexOutputChannel(anOutputChannel);
    ///         }
    /// 
    ///         private void CloseConnection()
    ///         {
    ///             // Detach input channel and stop listening to response messages.
    ///             mySender.DetachDuplexOutputChannel();
    ///         }
    /// 
    ///         private void CalculateBtn_Click(object sender, EventArgs e)
    ///         {
    ///             // Create the request message.
    ///             RequestMessage aRequest = new RequestMessage();
    ///             aRequest.Number1 = int.Parse(Number1TextBox.Text);
    ///             aRequest.Number2 = int.Parse(Number2TextBox.Text);
    /// 
    ///             // Send request to the service to calculate 2 numbers.
    ///             ResponseMessage aResponse = mySender.SendRequestMessage(aRequest);
    /// 
    ///             // Display the result.
    ///             ResultTextBox.Text = aResponse.Result.ToString();
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// 
    /// 
    /// </remarks>
    public class DuplexTypedMessagesFactory : IDuplexTypedMessagesFactory
    {
        /// <summary>
        /// Constructs the factory with xml serializer. <br/>
        /// </summary>
        /// <remarks>
        /// The factory will create senders and receivers with the default XmlStringSerializer<br/>
        /// and the factory will create ISyncDuplexTypedMessageSender that can wait infinite
        /// time for the response message from the service.
        /// </remarks>
        public DuplexTypedMessagesFactory()
            : this(TimeSpan.FromMilliseconds(-1), new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory with specified serializer.
        /// </summary>
        /// <remarks>
        /// The factory will create senders and receivers with the specified serializer
        /// and the factory will create ISyncDuplexTypedMessageSender that can wait infinite
        /// time for the response message from the service.<br/>
        /// <br/>
        /// For possible serializers you can refer to <see cref="Eneter.Messaging.DataProcessing.Serializing"/>
        /// </remarks>
        /// <param name="serializer">Serializer used to serialize request and response messages.</param>
        public DuplexTypedMessagesFactory(ISerializer serializer)
            : this(TimeSpan.FromMilliseconds(-1), serializer)
        {
        }

        /// <summary>
        /// Constructs the factory with specified timeout for ISyncDuplexTypedMessageSender.
        /// </summary>
        /// <remarks>
        /// The factory will create senders and receivers using the default XmlStringSerializer
        /// and the factory will create ISyncDuplexTypedMessageSender with specified timeout
        /// indicating how long it can wait for a response message from the service.
        /// The timeout value TimeSpan.FromMilliseconds(-1) means infinite time.
        /// </remarks>
        /// <param name="syncResponseReceiveTimeout">maximum waiting time when synchronous message sender is used.</param>
        public DuplexTypedMessagesFactory(TimeSpan syncResponseReceiveTimeout)
            : this(syncResponseReceiveTimeout, new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory with specified timeout for synchronous message sender and specified serializer.
        /// </summary>
        /// <param name="syncResponseReceiveTimeout">maximum waiting time when synchronous message sender is used.</param>
        /// <param name="serializer">serializer that will be used to serialize/deserialize messages.</param>
        public DuplexTypedMessagesFactory(TimeSpan syncResponseReceiveTimeout, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                SyncResponseReceiveTimeout = syncResponseReceiveTimeout;
                Serializer = serializer;
                SerializerProvider = null;
                SyncDuplexTypedSenderThreadMode = new SyncDispatching();
            }
        }

        /// <summary>
        /// Creates duplex typed message sender that can send request messages and receive response
        /// messages of specified type.
        /// </summary>
        /// <typeparam name="TResponse">Type of response messages.</typeparam>
        /// <typeparam name="TRequest">Type of request messages.</typeparam>
        /// <returns>duplex typed message sender</returns>
        public IDuplexTypedMessageSender<TResponse, TRequest> CreateDuplexTypedMessageSender<TResponse, TRequest>()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexTypedMessageSender<TResponse, TRequest>(Serializer);
            }
        }

        /// <summary>
        /// Creates synchronous duplex typed message sender that sends a request message and then
        /// waits until the response message is received.
        /// </summary>
        /// <typeparam name="TResponse">Response message type.</typeparam>
        /// <typeparam name="TRequest">Request message type.</typeparam>
        /// <returns></returns>
        public ISyncDuplexTypedMessageSender<TResponse, TRequest> CreateSyncDuplexTypedMessageSender<TResponse, TRequest>()
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aThreadDispatcher = SyncDuplexTypedSenderThreadMode.GetDispatcher();
                SyncTypedMessageSender<TResponse, TRequest> aSender = new SyncTypedMessageSender<TResponse, TRequest>(SyncResponseReceiveTimeout, Serializer, aThreadDispatcher);
                return aSender;
            }
        }

        /// <summary>
        /// Creates duplex typed message receiver that can receive request messages and
        /// send back response messages of specified type.
        /// </summary>
        /// <typeparam name="TResponse">Type of response messages.</typeparam>
        /// <typeparam name="TRequest">Type of receiving messages.</typeparam>
        /// <returns>duplex typed message receiver</returns>
        public IDuplexTypedMessageReceiver<TResponse, TRequest> CreateDuplexTypedMessageReceiver<TResponse, TRequest>()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexTypedMessageReceiver<TResponse, TRequest>(Serializer);
            }
        }

        /// <summary>
        /// Gets or sets the threading mode for receiving ConnectionOpened and ConnectionClosed events for SyncDuplexTypedMessageSender.
        /// </summary>
        /// <remarks>
        /// E.g. you use SyncDuplexTypedMessageSender and you want to route ConnectionOpened and ConnectionClosed events
        /// to the main UI thread of your WPF based application. Therefore you specify WindowsDispatching when you create your
        /// TCP duplex output channel which you then attach to the SyncDuplexTypedMessageSender.<br/>
        /// Later when the application is running you call SyncDuplexTypedMessageSender.SendRequestMessage(..).<br/>
        /// However if you call it from the main UI thread the deadlock occurs.
        /// Because this component is synchronous the SendRequestMessage(..) will stop the calling main UI thread and will wait
        /// for the response. But the problem is when the response comes the underlying TCP messaging will try to route it to
        /// the main UI thread (as was specified during creating TCP duplex output channel).<br/>
        /// But because the main UI thread is suspending and waiting the message will never arrive.<br/>
        /// <br/>
        /// Solution:<br/>
        /// Do not specify the threading mode when you create yur duplex output channel but specify it using the
        /// SyncDuplexTypedSenderThreadMode property when you create SyncDuplexTypedMessageSender.
        /// </remarks>
        public IThreadDispatcherProvider SyncDuplexTypedSenderThreadMode { get; set; }

        /// <summary>
        /// Serializer for messages.
        /// </summary>
        public ISerializer Serializer { get; set; }

        /// <summary>
        /// Gets/sets callback for retrieving serializer based on response receiver id.
        /// </summary>
        /// <remarks>
        /// This callback is used by DuplexTypedMessageReceiver when it needs to serialize/deserialize the communication with DuplexTypedMessageSender.
        /// Providing this callback allows to use a different serializer for each connected client.
        /// This can be used e.g. if the communication with each client needs to be encrypted using a different password.<br/>
        /// <br/>
        /// The default value is null and it means SerializerProvider callback is not used and one serializer which specified in the Serializer property is used for all serialization/deserialization.<br/>
        /// If SerializerProvider is not null then the setting in the Serializer property is ignored.
        /// </remarks>
        public GetSerializerCallback SerializerProvider
        {
            get
            {
                if (Serializer is CallbackSerializer)
                {
                    return ((CallbackSerializer)Serializer).GetSerializerCallback;
                }

                return null;
            }
            set
            {
                Serializer = new CallbackSerializer(value);
            }
        }

        /// <summary>
        /// Timeout which is used for SyncDuplexTypedMessageSender.
        /// </summary>
        /// <remarks>
        /// When SyncDuplexTypedMessageSender calls SendRequestMessage(..) then it waits until the response is received.
        /// This timeout specifies the maximum wating time. The default value is -1 and it means infinite time.
        /// </remarks>
        public TimeSpan SyncResponseReceiveTimeout { get; set; }
    }
}
