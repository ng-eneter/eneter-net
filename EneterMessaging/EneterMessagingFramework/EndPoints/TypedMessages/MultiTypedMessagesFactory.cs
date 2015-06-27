/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Factory to create multi-typed message senders and receivers.
    /// </summary>
    /// <remarks>
    /// The following example shows how to send and receive messages:
    /// <example>
    /// <code>
    /// public class MyRequestMessage
    /// {
    ///     public double Number1 { get; set; }
    ///     public double Number2 { get; set; }
    /// }
    /// 
    /// class Program
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         try
    ///         {
    ///             // Create multi-typed receiver.
    ///             IMultiTypedMessagesFactory aFactory = new MultiTypedMessagesFactory();
    ///             IMultiTypedMessageReceiver aReceiver = aFactory.CreateMultiTypedMessageReceiver();
    ///         
    ///             // Register message types which can be processed.
    ///             aReceiver.RegisterRequestMessageReceiver&lt;int&gt;(OnIntMessage);
    ///             aReceiver.RegisterRequestMessageReceiver&lt;MyRequestMessage&gt;(OnMyReqestMessage);
    ///         
    ///             // Attach input channel and start listening.
    ///             IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    ///             IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8033/");
    ///             aReceiver.AttachDuplexInputChannel(anInputChannel);
    ///         
    ///             Console.WriteLine("Service is running. Press ENTER to stop.");
    ///             Console.ReadLine();
    ///         
    ///             // Detach input channel to stop the listening thread.
    ///             aReceiver.DetachDuplexInputChannel();
    ///         }
    ///         catch (Exception err)
    ///         {
    ///             EneterTrace.Error("Service failed.", err);
    ///         }
    ///     }
    /// 
    ///     private static void OnIntMessage(Object eventSender, TypedRequestReceivedEventArgs&lt;int&gt; e)
    ///     {
    ///         int aNumber = e.RequestMessage;
    ///         
    ///         // Calculate factorial.
    ///         int aResult = 1;
    ///         for (int i = 1; i &lt;= aNumber; ++i)
    ///         {
    ///             aResult *= i;
    ///         }
    /// 
    ///         Console.WriteLine(aNumber + "! =" + aResult);
    ///     
    ///         // Send back the result.
    ///         IMultiTypedMessageReceiver aReceiver = (IMultiTypedMessageReceiver)eventSender;
    ///         try
    ///         {
    ///             aReceiver.SendResponseMessage&lt;int&gt;(e.ResponseReceiverId, aResult);
    ///         }
    ///         catch (Exception err)
    ///         {
    ///             EneterTrace.Error("Failed to send the response message.", err);
    ///         }
    ///     }
    /// 
    ///     private static void OnMyReqestMessage(Object eventSender, TypedRequestReceivedEventArgs&lt;MyRequestMessage&gt; e)
    ///     {
    ///         MyRequestMessage aRequestMessage = e.RequestMessage;
    ///     
    ///         double aResult = aRequestMessage.Number1 + aRequestMessage.Number2;
    ///     
    ///         Console.WriteLine(aRequestMessage.Number1 + " + " + aRequestMessage.Number2 + " = " + aResult);
    ///     
    ///         // Send back the message.
    ///         IMultiTypedMessageReceiver aReceiver = (IMultiTypedMessageReceiver)eventSender;
    ///         try
    ///         {
    ///             aReceiver.SendResponseMessage&lt;double&gt;(e.ResponseReceiverId, aResult);
    ///         }
    ///         catch (Exception err)
    ///         {
    ///             EneterTrace.Error("Failed to send the response message.", err);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// Implementation of sender (client):
    /// <example>
    /// <code>
    /// public partial class Form1 : Form
    /// {
    ///     public class MyRequestMessage
    ///     {
    ///         public double Number1 { get; set; }
    ///         public double Number2 { get; set; }
    ///     }
    /// 
    ///     public class ggg
    ///     {
    ///         public int a;
    ///     }
    /// 
    ///     private IMultiTypedMessageSender mySender;
    /// 
    ///     public Form1()
    ///     {
    ///         InitializeComponent();
    /// 
    ///         OpenConnection();
    ///     }
    /// 
    ///     private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    ///     {
    ///         CloseConnection();
    ///     }
    /// 
    ///     private void OpenConnection()
    ///     {
    ///         IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory()
    ///         {
    ///             // Receive response messages in the main UI thread.
    ///             // Note: UI controls can be accessed only from the UI thread.
    ///             //       So if this is not set then your message handling method would have to
    ///             //       route it manually.
    ///             OutputChannelThreading = new WinFormsDispatching(this)
    ///         };
    /// 
    ///         IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8033/");
    /// 
    ///         IMultiTypedMessagesFactory aFactory = new MultiTypedMessagesFactory();
    ///         mySender = aFactory.CreateMultiTypedMessageSender();
    /// 
    ///         // Register handlers for particular types of response messages.
    ///         mySender.RegisterResponseMessageReceiver&lt;int&gt;(OnIntMessage);
    ///         mySender.RegisterResponseMessageReceiver&lt;double&gt;(OnDoubleMessage);
    /// 
    ///         // Attach output channel and be able to send messages and receive responses.
    ///         mySender.AttachDuplexOutputChannel(anOutputChannel);
    ///     }
    /// 
    ///     private void CloseConnection()
    ///     {
    ///         // Detach output channel and release the thread listening to responses.
    ///         mySender.DetachDuplexOutputChannel();
    ///     }
    /// 
    ///     private void CalculateBtn_Click(object sender, EventArgs e)
    ///     {
    ///         // Create message.
    ///         MyRequestMessage aRequest = new MyRequestMessage();
    ///         aRequest.Number1 = double.Parse(Number1TextBox.Text);
    ///         aRequest.Number2 = double.Parse(Number2TextBox.Text);
    /// 
    ///         // Send message.
    ///         mySender.SendRequestMessage&lt;MyRequestMessage&gt;(aRequest);
    ///     }
    /// 
    ///     private void CalculateFactorialBtn_Click(object sender, EventArgs e)
    ///     {
    ///         // Create message.
    ///         int aNumber = int.Parse(FactorialNumberTextBox.Text);
    /// 
    ///         // Send Message.
    ///         mySender.SendRequestMessage&lt;int&gt;(aNumber);
    ///     }
    /// 
    ///     private void OnIntMessage(object sender, TypedResponseReceivedEventArgs&lt;int&gt; e)
    ///     {
    ///         FactorialResultTextBox.Text = e.ResponseMessage.ToString();
    ///     }
    /// 
    ///     private void OnDoubleMessage(object sender, TypedResponseReceivedEventArgs&lt;double&gt; e)
    ///     {
    ///         ResultTextBox.Text = e.ResponseMessage.ToString();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// </remarks>
    public class MultiTypedMessagesFactory : IMultiTypedMessagesFactory
    {
        /// <summary>
        /// Constructs the factory with default parameters.
        /// </summary>
        /// <remarks>
        /// It instantiates the factory which will create multi typed senders and receivers which will use
        /// default XmlStringSerializer and with infinite timeout for SyncMultiTypedMessageSender.
        /// </remarks>
        public MultiTypedMessagesFactory()
            : this(new XmlStringSerializer())
        {
        }

        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <remarks>
        /// It instantiates the factory with infinite timeout for SyncMultiTypedMessageSender.
        /// </remarks>
        /// <param name="serializer">serializer which will be used to serializer/deserialize messages</param>
        public MultiTypedMessagesFactory(ISerializer serializer)
        {
            SyncResponseReceiveTimeout = TimeSpan.FromMilliseconds(-1);
            Serializer = serializer;
            SerializerProvider = null;
            SyncDuplexTypedSenderThreadMode = new SyncDispatching();
        }

        /// <summary>
        /// Creates multi typed message sender.
        /// </summary>
        /// <remarks>
        /// The sender is able to send messages of various types and receive response messages of various types.
        /// </remarks>
        /// <returns>multi typed sender</returns>
        public IMultiTypedMessageSender CreateMultiTypedMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new MultiTypedMessageSender(Serializer);
            }
        }

        /// <summary>
        /// Creates multi typed message sender which waits for the response message.
        /// </summary>
        /// <returns>synchronous multi typed sender</returns>
        public ISyncMultitypedMessageSender CreateSyncMultiTypedMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new SyncMultiTypedMessageSender(SyncResponseReceiveTimeout, Serializer, SyncDuplexTypedSenderThreadMode);
            }
        }

        /// <summary>
        /// Creates multi typed message receiver.
        /// </summary>
        /// <remarks>
        /// The receiver is able to receive messages of various types and send response messages
        /// of various types.
        /// </remarks>
        /// <returns></returns>
        public IMultiTypedMessageReceiver CreateMultiTypedMessageReceiver()
        {
            using (EneterTrace.Entering())
            {
                return new MultiTypedMessageReceiver(Serializer, SerializerProvider);
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
        /// This callback is used by MultiTypedMessageReceiver when it needs to serialize/deserialize the communication with MultiTypedMessageSender.
        /// Providing this callback allows to use a different serializer for each connected client.
        /// This can be used e.g. if the communication with each client needs to be encrypted differently.<br/>
        /// <br/>
        /// The default value is null and it means the serializer specified in the Serializer property is used for all serialization/deserialization.
        /// </remarks>
        public GetSerializerCallback SerializerProvider { get; set; }

        /// <summary>
        /// Timeout which is used for SyncMultitypedMessageSender.
        /// </summary>
        /// <remarks>
        /// When SyncMultitypedMessageSender calls SendRequestMessage(..) then it waits until the response is received.
        /// This timeout specifies the maximum wating time. The default value is -1 and it means infinite time.
        /// </remarks>
        public TimeSpan SyncResponseReceiveTimeout { get; set; }
    }
}
