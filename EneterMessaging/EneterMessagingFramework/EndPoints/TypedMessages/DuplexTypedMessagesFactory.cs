/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using System;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Implements the factory to create duplex strongly typed message sender and receiver.
    /// </summary>
    /// <remarks>
    /// 
    /// <example>
    /// Simple service listening to request messages of type 'RequestMessage' and responding the response message of type 'ResponseMessage'.
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
    /// 
    /// <example>
    /// Simple client sending request messages of type 'RequestMessage' and receiving responses of type 'ResponseMessage'.
    /// The client is synchronous. It sends the request message and waits for the response.
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
    /// <example>
    /// Simple client sending request messages of type 'RequestMessage' and receiving responses of type 'ResponseMessage'.
    /// The client receives the response asynchronously via the event.
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
        /// </remarks>
        /// <param name="syncResponseReceiveTimeout">Timeout specifying the maximum time
        /// the ISyncDuplexTypedMessageSender will wait for the response message from the service.
        /// </param>
        public DuplexTypedMessagesFactory(TimeSpan syncResponseReceiveTimeout)
            : this(syncResponseReceiveTimeout, new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory with specified timeout for synchronous message sender and
        /// specified serializer.
        /// </summary>
        /// <param name="syncResponseReceiveTimeout">Timeout specifying the maximum time
        /// the ISyncDuplexTypedMessageSender will wait for the response message from the service.
        /// </param>
        /// <param name="serializer">Serializer used to serialize request and response messages.</param>
        public DuplexTypedMessagesFactory(TimeSpan syncResponseReceiveTimeout, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySyncResponseReceiveTimeout = syncResponseReceiveTimeout;
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Creates duplex typed message sender that can send request messages and receive response
        /// messages of specified type.
        /// </summary>
        /// <typeparam name="_ResponseType">Type of receiving response messages.</typeparam>
        /// <typeparam name="_RequestType">Type of sending messages.</typeparam>
        /// <returns>duplex typed message sender</returns>
        public IDuplexTypedMessageSender<_ResponseType, _RequestType> CreateDuplexTypedMessageSender<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexTypedMessageSender<_ResponseType, _RequestType>(mySerializer);
            }
        }

        /// <summary>
        /// Creates synchronous duplex typed message sender that sends a request message and then
        /// waits until the response message is received.
        /// </summary>
        /// <typeparam name="_ResponseType">Response message type.</typeparam>
        /// <typeparam name="_RequestType">Request message type.</typeparam>
        /// <returns></returns>
        public ISyncDuplexTypedMessageSender<_ResponseType, _RequestType> CreateSyncDuplexTypedMessageSender<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                SyncTypedMessageSender<_ResponseType, _RequestType> aSender = new SyncTypedMessageSender<_ResponseType, _RequestType>(mySyncResponseReceiveTimeout, mySerializer);
                return aSender;
            }
        }

        /// <summary>
        /// Creates duplex typed message receiver that can receive request messages and
        /// send back response messages of specified type.
        /// </summary>
        /// <typeparam name="_ResponseType">Type of response messages.</typeparam>
        /// <typeparam name="_RequestType">Type of receiving messages.</typeparam>
        /// <returns>duplex typed message receiver</returns>
        public IDuplexTypedMessageReceiver<_ResponseType, _RequestType> CreateDuplexTypedMessageReceiver<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexTypedMessageReceiver<_ResponseType, _RequestType>(mySerializer);
            }
        }


        private ISerializer mySerializer;
        private TimeSpan mySyncResponseReceiveTimeout;
    }
}
