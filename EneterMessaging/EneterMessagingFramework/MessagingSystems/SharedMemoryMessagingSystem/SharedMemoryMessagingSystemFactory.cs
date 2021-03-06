﻿

#if !NET35 && NETFRAMEWORK

using System;
using System.IO.MemoryMappedFiles;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    /// <summary>
    /// Messaging system delivering messages via the shared memory.
    /// </summary>
    /// <remarks>
    /// It creates communication channels for sending and receiving messages via shared memory.<br/>
    /// Communication via the shared memory can transfer messages between applications running on the same machine
    /// and is significantly faster than using named pipes.<br/>
    /// Messaging via the shared memeory is supported only in .Net 4.0 or higher.
    /// <example>
    /// General using of shared memory.
    /// <code>
    /// IMessagingSystemFactory aMessaging = new SharedMemoryMessagingSystemFactory();
    /// 
    /// // Duplex output channel that can be attached to message senders.
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("MyChannelName");
    /// 
    /// // Duplex input channel that can be attached to a mesage receiver.
    /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("MyChannelName");
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// Windows service listening to messages via shared memory.
    /// <code>
    /// public partial class Service1 : ServiceBase
    /// {
    ///    // Request message coming from the cient.
    ///    public class RequestMessage
    ///    {
    ///       public int Number1;
    ///       public int Number2;
    ///    }
    ///    
    ///    // Response message.
    ///    public class ResponseMessage
    ///    {
    ///       public int Result;
    ///    }
    ///    
    ///    private IDuplexTypedMessageReceiver&lt;ResponseMessage, RequestMessage&gt; myReceiver;
    ///    
    ///    public Service1()
    ///    {
    ///       InitializeComponent();
    ///    }
    ///    
    ///    protected override void OnStart(string[] args)
    ///    {
    ///       //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
    ///       //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
    ///    
    ///       // Create message receiver.
    ///       // It receives 'RequestMessage' and responses result in 'int'.
    ///       IDuplexTypedMessagesFactory aReceiverFactory = new DuplexTypedMessagesFactory();
    ///       myReceiver = aReceiverFactory.CreateDuplexTypedMessageReceiver&lt;ResponseMessage, RequestMessage&gt;();
    ///    
    ///       // Subscribe to receive response messages.
    ///       myReceiver.MessageReceived += OnMessageReceived;
    ///    
    ///       // Set the security for the shared memory allowing the communication
    ///       // with desktop applications.
    ///       MemoryMappedFileSecurity aSharedMemmorySecurity = new MemoryMappedFileSecurity();
    ///       aSharedMemmorySecurity.SetSecurityDescriptorSddlForm("S:(ML;;NW;;;LW)");
    ///       SecurityIdentifier aSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
    ///       AccessRule&lt;MemoryMappedFileRights&gt; aRules = new AccessRule&lt;MemoryMappedFileRights&gt;(aSid, MemoryMappedFileRights.ReadWrite, AccessControlType.Allow);
    ///       aSharedMemmorySecurity.SetAccessRule(aRules);
    ///    
    ///       // Use Shared messaging for the communication.
    ///       IMessagingSystemFactory aMessaging = new SharedMemoryMessagingSystemFactory(10485760,
    ///           new EneterProtocolFormatter(), aSharedMemmorySecurity);
    ///       IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("MyAddress");
    ///    
    ///       // Attach the input channel to the message receiver and start listening.
    ///       myReceiver.AttachDuplexInputChannel(anInputChannel);
    ///    }
    ///    
    ///    protected override void OnStop()
    ///    {
    ///       if (myReceiver != null)
    ///       {
    ///           // Stop listening.
    ///           // Note: it releases the listening thread.
    ///           myReceiver.DetachDuplexInputChannel();
    ///       }
    ///    }
    ///    
    ///    private void OnMessageReceived(object eventSender, TypedRequestReceivedEventArgs&lt;RequestMessage&gt; e)
    ///    {
    ///       // Calculate received numbers and sends back the response.
    ///       int aResult = e.RequestMessage.Number1 + e.RequestMessage.Number2;
    ///    
    ///       string s = string.Format("{0} + {1} = {2}", e.RequestMessage.Number1, e.RequestMessage.Number2, aResult);
    ///       EneterTrace.Info(s);
    ///    
    ///       // Send back the response message.
    ///       ResponseMessage aResponseMessage = new ResponseMessage();
    ///       aResponseMessage.Result = aResult;
    ///    
    ///       myReceiver.SendResponseMessage(e.ResponseReceiverId, aResponseMessage);
    ///    }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// Desktop client application using shared memory to communicate with Windows service.
    /// It must use Global prefix in channel name.
    /// <code>
    /// public partial class Form1 : Form
    /// {
    ///     // Request message coming from the cient.
    ///     public class RequestMessage
    ///     {
    ///         public int Number1;
    ///         public int Number2;
    ///     }
    /// 
    ///     // Response message.
    ///     public class ResponseMessage
    ///     {
    ///         public int Result;
    ///     }
    /// 
    ///     private IDuplexTypedMessageSender&lt;ResponseMessage, RequestMessage&gt; mySender;
    /// 
    ///     public Form1()
    ///     {
    ///         InitializeComponent();
    /// 
    ///         // Create message sender.
    ///         // It sends 'RequestMessage' and  receives 'int'.
    ///         IDuplexTypedMessagesFactory aSenderFactory = new DuplexTypedMessagesFactory();
    ///         mySender = aSenderFactory.CreateDuplexTypedMessageSender&lt;ResponseMessage, RequestMessage&gt;();
    /// 
    ///         // Subscribe to receive responses.
    ///         mySender.ResponseReceived += OnResponseReceived;
    /// 
    ///         // Use shared messaging for the communication.
    ///         // Note: Client must use Global if the sevice is Windows Service.
    ///         IMessagingSystemFactory aMessaging = new SharedMemoryMessagingSystemFactory();
    ///         IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("Global\\MyAddress");
    /// 
    ///         // Attach the output channel and be able to send messages
    ///         // and receive response messages.
    ///         mySender.AttachDuplexOutputChannel(anOutputChannel);
    ///     }
    /// 
    ///     private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    ///     {
    ///         // Detach the output channel to stop the listening thread.
    ///         mySender.DetachDuplexOutputChannel();
    ///     }
    /// 
    ///     private void SendBtn_Click(object sender, EventArgs e)
    ///     {
    ///         // Create the request message.
    ///         RequestMessage aRequestMessage = new RequestMessage();
    ///         aRequestMessage.Number1 = int.Parse(Number1TextBox.Text);
    ///         aRequestMessage.Number2 = int.Parse(Number2TextBox.Text);
    /// 
    ///         // Send the request message to the service
    ///         // to calculate numbers.
    ///         mySender.SendRequestMessage(aRequestMessage);
    ///     }
    /// 
    /// 
    ///     private void OnResponseReceived(object sender, TypedResponseReceivedEventArgs&lt;ResponseMessage&gt; e)
    ///     {
    ///         // Display the received result.
    ///         // But invoke the displaying in the UI thread.
    ///         UI(() =&gt; ResultTextBox.Text = e.ResponseMessage.Result.ToString());
    ///     }
    /// 
    /// 
    ///     // Helper method to invoke some functionality in UI thread.
    ///     private void UI(Action uiMethod)
    ///     {
    ///         // If we are not in the UI thread then we must synchronize via the invoke mechanism.
    ///         if (InvokeRequired)
    ///         {
    ///             Invoke(uiMethod);
    ///         }
    ///         else
    ///         {
    ///             uiMethod();
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// </remarks>
    public class SharedMemoryMessagingSystemFactory : IMessagingSystemFactory
    {
        private class SharedMemoryConnectorFactory : IOutputConnectorFactory, IInputConnectorFactory
        {
            public SharedMemoryConnectorFactory(IProtocolFormatter protocolFormatter, TimeSpan connectTimeout, TimeSpan sendTimeout, int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
            {
                using (EneterTrace.Entering())
                {
                    myProtocolFormatter = protocolFormatter;
                    myConnectTimeout = connectTimeout;
                    mySendTimeout = sendTimeout;
                    myMaxMessageSize = maxMessageSize;
                    mySecurity = memoryMappedFileSecurity;
                }
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new SharedMemoryOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter, myConnectTimeout, mySendTimeout, myMaxMessageSize, mySecurity);
                }
            }

            public IInputConnector CreateInputConnector(string inputConnectorAddress)
            {
                using (EneterTrace.Entering())
                {
                    return new SharedMemoryInputConnector(inputConnectorAddress, myProtocolFormatter, mySendTimeout, myMaxMessageSize, mySecurity);
                }
            }

            private IProtocolFormatter myProtocolFormatter;
            private TimeSpan myConnectTimeout;
            private TimeSpan mySendTimeout;
            private int myMaxMessageSize;
            private MemoryMappedFileSecurity mySecurity;
        }



        /// <summary>
        /// Constructs the messaging factory with the default settings.
        /// </summary>
        /// <remarks>
        /// The default constructor creates the factory that will create input and output channels
        /// using the shared memory. <br/>
        /// The maximum message size will be 10Mb.
        /// </remarks>
        public SharedMemoryMessagingSystemFactory()
            : this(new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the messaging system.
        /// </summary>
        /// <param name="protocolFormatter">formats low-level communication between channels.
        /// The default formatter is EneterProtocolFormatter.
        /// </param>
        public SharedMemoryMessagingSystemFactory(IProtocolFormatter protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myProtocolFormatter = protocolFormatter;

                MaxMessageSize = 10485760;
                SharedMemorySecurity = null;

                ConnectTimeout = TimeSpan.FromMilliseconds(30000);

                // Infinite time for sending and receiving.
                SendTimeout = TimeSpan.FromMilliseconds(0);

                InputChannelThreading = new SyncDispatching();
                OutputChannelThreading = InputChannelThreading;
            }
        }

        /// <summary>
        /// Creates duplex output channel which can send and receive messages from the duplex input channel using shared memory.
        /// </summary>
        /// <param name="channelId">Identifies the input channel which shall be connected.
        /// The channel id is the name of the memory-mapped file that
        /// is used to send and receive messages.
        /// </param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                IOutputConnectorFactory aConnectorFactory = new SharedMemoryConnectorFactory(myProtocolFormatter, ConnectTimeout, SendTimeout, MaxMessageSize, SharedMemorySecurity);
                return new DefaultDuplexOutputChannel(channelId, null, aDispatcher, aDispatcherAfterMessageDecoded, aConnectorFactory);
            }
        }

        /// <summary>
        /// Creates duplex output channel which can send and receive messages from the duplex input channel using shared memory.
        /// </summary>
        /// <param name="channelId">Identifies the input channel which shall be connected.
        /// The channel id is the name of the memory-mapped file that
        /// is used to send and receive messages.
        /// </param>
        /// <param name="responseReceiverId">Identifies the input channel which shall be connected.
        /// The channel id is the name of the memory-mapped file that
        /// is used to send and receive messages.
        /// </param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                IThreadDispatcher aDispatcher = OutputChannelThreading.GetDispatcher();
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                IOutputConnectorFactory aConnectorFactory = new SharedMemoryConnectorFactory(myProtocolFormatter, ConnectTimeout, SendTimeout, MaxMessageSize, SharedMemorySecurity);
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, aDispatcher, aDispatcherAfterMessageDecoded, aConnectorFactory);
            }
        }

        /// <summary>
        ///  Creates the duplex input channel which can receive and send messages to the duplex output channel using shared memory.
        /// </summary>
        /// <param name="channelId">Address which shell be used for listening.
        /// It is the name of the memory-mapped file that will be used to send and receive messages.
        /// </param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IInputConnectorFactory aConnectorFactory = new SharedMemoryConnectorFactory(myProtocolFormatter, ConnectTimeout, SendTimeout, MaxMessageSize, SharedMemorySecurity);
                IThreadDispatcher aThreadDispatcher = InputChannelThreading.GetDispatcher();
                IInputConnector anInputConnector = aConnectorFactory.CreateInputConnector(channelId);
                IThreadDispatcher aDispatcherAfterMessageDecoded = myDispatchingAfterMessageDecoded.GetDispatcher();
                return new DefaultDuplexInputChannel(channelId, aThreadDispatcher, aDispatcherAfterMessageDecoded, anInputConnector);
            }
        }

        /// <summary>
        /// Sets ot gets timeout to open the connection. Default is 30000 miliseconds. Value 0 is infinite time.
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; }

        /// <summary>
        /// Sets or gets timeout to send a message. Default is 0 what is infinite time.
        /// </summary>
        /// <remarks>
        /// It is the maximum time to put the message into the shared memory.
        /// </remarks>
        public TimeSpan SendTimeout { get; set; }

        /// <summary>
        /// Sets or gets the threading mode for input channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that all messages from all connected clients are routed by one working thread.
        /// </remarks>
        public IThreadDispatcherProvider InputChannelThreading { get; set; }

        /// <summary>
        /// Sets or gets the threading mode for output channels.
        /// </summary>
        /// <remarks>
        /// Default setting is that received response messages are routed via one working thread.
        /// </remarks>
        public IThreadDispatcherProvider OutputChannelThreading { get; set; }

        /// <summary>
        /// Maximum size of one message in bytes.
        /// </summary>
        /// <remarks>
        /// The default value is 10 MB (10485760 bytes).
        /// </remarks>
        public int MaxMessageSize { get; set; }

        /// <summary>
        /// Security settings for communication via the shared memory.
        /// </summary>
        /// <remarks>
        /// The default value is null. It means security settings are not applied by default.
        /// </remarks>
        public MemoryMappedFileSecurity SharedMemorySecurity { get; set; }

        private IProtocolFormatter myProtocolFormatter;
        private IThreadDispatcherProvider myDispatchingAfterMessageDecoded = new SyncDispatching();
    }
}

#endif