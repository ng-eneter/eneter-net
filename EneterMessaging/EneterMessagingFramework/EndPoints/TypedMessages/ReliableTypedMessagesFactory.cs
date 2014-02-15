/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Implements factory to create reliable sender and receiver.
    /// </summary>
    /// <remarks>
    /// The reliable messaging means that the sender of a message is notified whether the message was delivered or not.
    /// <example>
    /// Service using the reliable communication. When it sends the response message it is notified whether the message
    /// was delivered or not.
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
    ///             // Create reliable message receiver.
    ///             IReliableTypedMessagesFactory aReceiverFactory = new ReliableTypedMessagesFactory();
    ///             IReliableTypedMessageReceiver&lt;ResponseMessage, RequestMessage&gt; aReceiver =
    ///                 aReceiverFactory.CreateReliableDuplexTypedMessageReceiver&lt;ResponseMessage, RequestMessage&gt;();
    /// 
    ///             // Subscribe to be notified whether sent response messages
    ///             // were received.
    ///             aReceiver.ResponseMessageDelivered += OnResponseMessageDelivered;
    ///             aReceiver.ResponseMessageNotDelivered += OnResponseMessageNotDelivered;
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
    ///             var aReceiver = (IReliableTypedMessageReceiver&lt;ResponseMessage, RequestMessage&gt;)sender;
    ///             string aResponseId = aReceiver.SendResponseMessage(e.ResponseReceiverId, aResponseMessage);
    /// 
    ///             Console.WriteLine("Sent response has Id: {0}", aResponseId);
    ///         }
    /// 
    ///         private static void OnResponseMessageDelivered(object sender, ReliableMessageIdEventArgs e)
    ///         {
    ///             Console.WriteLine("Response Id: {0} was delivered.", e.MessageId);
    ///         }
    /// 
    ///         private static void OnResponseMessageNotDelivered(object sender, ReliableMessageIdEventArgs e)
    ///         {
    ///             Console.WriteLine("Response Id: {0} was NOT delivered.", e.MessageId);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// Client using the reliable communication. When it sends the request message it is notified whether
    /// it was delivered or not.
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
    ///         private IReliableTypedMessageSender&lt;ResponseMessage, RequestMessage&gt; mySender;
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
    ///             IReliableTypedMessagesFactory aSenderFactory = new ReliableTypedMessagesFactory();
    ///             mySender = aSenderFactory.CreateReliableDuplexTypedMessageSender&lt;ResponseMessage, RequestMessage&gt;();
    /// 
    ///             // Subscribe to be notified whether the request message was delivered or not.
    ///             mySender.MessageDelivered += OnMessageDelivered;
    ///             mySender.MessageNotDelivered += OnMessageNotDelivered;
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
    ///             string aRequestId = mySender.SendRequestMessage(aRequest);
    /// 
    ///             SentMessageIdlabel.Text = "Request Id: " + aRequestId;
    ///         }
    /// 
    ///         private void OnResponseReceived(object sender, TypedResponseReceivedEventArgs&lt;ResponseMessage&gt; e)
    ///         {
    ///             // Display the result using the UI thread.
    ///             UI(() =&gt; ResultTextBox.Text = e.ResponseMessage.Result.ToString());
    ///         }
    /// 
    ///         private void OnMessageDelivered(object sender, ReliableMessageIdEventArgs e)
    ///         {
    ///             // Display the message was delivered.
    ///             UI(() =&gt; DeliveryResultLabel.Text = "Delivered: " + e.MessageId);
    ///         }
    /// 
    ///         private void OnMessageNotDelivered(object sender, ReliableMessageIdEventArgs e)
    ///         {
    ///             // Display the message was NOT delivered.
    ///             UI(() =&gt; DeliveryResultLabel.Text = "NOT delivered: " + e.MessageId);
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
    /// 
    /// </code>
    /// </example>
    /// </remarks>
    public class ReliableTypedMessagesFactory : IReliableTypedMessagesFactory
    {
        /// <summary>
        /// Constructs the factory with default settings.
        /// </summary>
        /// <remarks>
        /// For the serialization of reliable messages is used <see cref="XmlStringSerializer"/>.
        /// The maximum time, the acknowledge message must be received is set to 12 seconds.
        /// </remarks>
        public ReliableTypedMessagesFactory()
            : this(TimeSpan.FromMilliseconds(12000), new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <remarks>
        /// For the serialization of reliable messages is used <see cref="XmlStringSerializer"/>.
        /// </remarks>
        /// <param name="acknowledgeTimeout">The maximum time until the delivery of the message must be acknowledged.</param>
        public ReliableTypedMessagesFactory(TimeSpan acknowledgeTimeout)
            : this(acknowledgeTimeout, new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="acknowledgeTimeout">The maximum time until the delivery of the message must be acknowledged.</param>
        /// <param name="serializer">Serializer used to serialize messages.</param>
        public ReliableTypedMessagesFactory(TimeSpan acknowledgeTimeout, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myAcknowledgeTimeout = acknowledgeTimeout;
                mySerializer = serializer;
            }
        }

        /// <summary>
        /// Creates the reliable message sender.
        /// </summary>
        /// <typeparam name="_ResponseType">type of response message</typeparam>
        /// <typeparam name="_RequestType">type of request message</typeparam>
        /// <returns>reliable typed message sender</returns>
        public IReliableTypedMessageSender<_ResponseType, _RequestType> CreateReliableDuplexTypedMessageSender<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new ReliableDuplexTypedMessageSender<_ResponseType, _RequestType>(myAcknowledgeTimeout, mySerializer);
            }
        }

        /// <summary>
        /// Creates the reliable message receiver.
        /// </summary>
        /// <typeparam name="_ResponseType">type of response message</typeparam>
        /// <typeparam name="_RequestType">type of request message</typeparam>
        /// <returns>reliable typed message receiver</returns>
        public IReliableTypedMessageReceiver<_ResponseType, _RequestType> CreateReliableDuplexTypedMessageReceiver<_ResponseType, _RequestType>()
        {
            using (EneterTrace.Entering())
            {
                return new ReliableDuplexTypedMessageReceiver<_ResponseType, _RequestType>(myAcknowledgeTimeout, mySerializer);
            }
        }

        private TimeSpan myAcknowledgeTimeout;
        private ISerializer mySerializer;
    }
}
