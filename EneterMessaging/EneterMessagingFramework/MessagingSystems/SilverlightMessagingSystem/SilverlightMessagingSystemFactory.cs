/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    /// <summary>
    /// Implements the messaging system delivering messages between Silverlight applications.
    /// </summary>
    /// <remarks>
    /// It creates output and input channels using the Silverlight messaging.
    /// This messaging system improves the Silverlight messaging. It allows to send messages of unlimited size and from any thread.
    /// The input channels receive messages always in the Silverlight thread.
    /// </remarks>
    /// <example>
    /// Desktop application as a service capable to receive messages from a Silverlight client.
    /// <code>
    /// using System;
    /// using System.Windows.Forms;
    /// using Eneter.Messaging.EndPoints.StringMessages;
    /// using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
    /// using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
    /// 
    /// namespace DesktopApplication
    /// {
    ///     public partial class Form1 : Form
    ///     {
    ///         public Form1()
    ///         {
    ///             InitializeComponent();
    /// 
    ///             // Start the policy server to be able to communicate with silverlight.
    ///             myPolicyServer.StartPolicyServer();
    /// 
    ///             // Create duplex message receiver.
    ///             // It can receive messages and also send back response messages.
    ///             IDuplexStringMessagesFactory aStringMessagesFactory = new DuplexStringMessagesFactory();
    ///             myMessageReceiver = aStringMessagesFactory.CreateDuplexStringMessageReceiver();
    ///             myMessageReceiver.ResponseReceiverConnected += ClientConnected;
    ///             myMessageReceiver.ResponseReceiverDisconnected += ClientDisconnected;
    ///             myMessageReceiver.RequestReceived += MessageReceived;
    /// 
    ///             // Create TCP based messaging.
    ///             IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    ///             IDuplexInputChannel aDuplexInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:4502/");
    /// 
    ///             // Attach the duplex input channel to the message receiver and start listening.
    ///             // Note: Duplex input channel can receive messages but also send messages back.
    ///             myMessageReceiver.AttachDuplexInputChannel(aDuplexInputChannel);
    ///         }
    /// 
    ///         private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    ///         {
    ///             // Close listenig.
    ///             // Note: If the listening is not closed, then listening threads are not ended
    ///             //       and the application would not be closed properly.
    ///             myMessageReceiver.DetachDuplexInputChannel();
    /// 
    ///             myPolicyServer.StopPolicyServer();
    ///         }
    /// 
    ///         // The method is called when a message from the client is received.
    ///         private void MessageReceived(object sender, StringRequestReceivedEventArgs e)
    ///         {
    ///             // Display received message.
    ///             InvokeInUIThread(() =&gt;
    ///                 {
    ///                     ReceivedMessageTextBox.Text = e.RequestMessage;
    ///                 });
    ///         }
    /// 
    /// 
    ///         // The method is called when a client is connected.
    ///         // The Silverlight client is connected when the client attaches the output duplex channel.
    ///         private void ClientConnected(object sender, ResponseReceiverEventArgs e)
    ///         {
    ///             // Add the connected client to the listbox.
    ///             InvokeInUIThread(() =&gt;
    ///                 {
    ///                     ConnectedClientsListBox.Items.Add(e.ResponseReceiverId);
    ///                 });
    ///         }
    /// 
    ///         // The method is called when a client is disconnected.
    ///         // The Silverlight client is disconnected if the web page is closed.
    ///         private void ClientDisconnected(object sender, ResponseReceiverEventArgs e)
    ///         {
    ///             // Remove the disconnected client from the listbox.
    ///             InvokeInUIThread(() =&gt;
    ///                 {
    ///                     ConnectedClientsListBox.Items.Remove(e.ResponseReceiverId);
    ///                 });
    ///         }
    /// 
    ///         private void SendButton_Click(object sender, EventArgs e)
    ///         {
    ///             // Send the message to all connected clients.
    ///             foreach (string aClientId in ConnectedClientsListBox.Items)
    ///             {
    ///                 myMessageReceiver.SendResponseMessage(aClientId, MessageTextBox.Text);
    ///             }
    ///         }
    /// 
    ///         // Helper method to invoke some functionality in UI thread.
    ///         private void InvokeInUIThread(Action uiMethod)
    ///         {
    ///             // If we are not in the UI thread then we must synchronize 
    ///             // via the invoke mechanism.
    ///             if (InvokeRequired)
    ///             {
    ///                 Invoke(uiMethod);
    ///             }
    ///             else
    ///             {
    ///                 uiMethod();
    ///             }
    ///         }
    /// 
    ///         private TcpPolicyServer myPolicyServer = new TcpPolicyServer();
    ///         private IDuplexStringMessageReceiver myMessageReceiver;
    ///     }
    /// }
    /// 
    /// </code>
    /// </example>
    public class SilverlightMessagingSystemFactory : IMessagingSystemFactory
    {
        private class ConnectorFactory : IOutputConnectorFactory, IInputConnectorFactory
        {
            public ConnectorFactory(IProtocolFormatter protocolFormatter)
            {
                myProtocolFormatter = protocolFormatter;
            }

            public IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress)
            {
                return new SilverlightOutputConnector(inputConnectorAddress, outputConnectorAddress, myProtocolFormatter);
            }

            public IInputConnector CreateInputConnector(string inputConnecterAddress)
            {
                return new SilverlightInputConnector(inputConnecterAddress, myProtocolFormatter);
            }

            private IProtocolFormatter myProtocolFormatter;
        }


        /// <summary>
        /// Constructs the factory.
        /// </summary>
        public SilverlightMessagingSystemFactory()
        {
            using (EneterTrace.Entering())
            {
                IProtocolFormatter aProtocolFormatter = new EneterStringProtocolFormatter();
                myConnectorFactory = new ConnectorFactory(aProtocolFormatter);
                myDispatcher = new NoDispatching().GetDispatcher();
                myDispatcherAfterMessageDecoded = myDispatcher;
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using Silverlight messaging.
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method generates the unique response receiver id automatically.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
        /// <param name="channelId">id representing the receiving duplex input channel</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new DefaultDuplexOutputChannel(channelId, null, myDispatcher, myDispatcherAfterMessageDecoded, myConnectorFactory);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages to the duplex input channel and receiving response messages by using Silverlight messaging.
        /// </summary>
        /// <remarks>
        /// The duplex output channel is intended for the bidirectional communication.
        /// Therefore, it can send messages to the duplex input channel and receive response messages.
        /// <br/><br/>
        /// The duplex input channel distinguishes duplex output channels according to the response receiver id.
        /// This method allows to specified a desired response receiver id. Please notice, the response receiver
        /// id is supposed to be unique.
        /// <br/><br/>
        /// The duplex output channel can communicate only with the duplex input channel and not with the input channel.
        /// </remarks>
        /// <param name="channelId">id representing the receiving duplex input channel</param>
        /// <param name="responseReceiverId">identifies the response receiver of this duplex output channel</param>
        /// <returns>duplex output channel</returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return new DefaultDuplexOutputChannel(channelId, responseReceiverId, myDispatcher, myDispatcherAfterMessageDecoded, myConnectorFactory);
            }
        }

        /// <summary>
        /// Creates the duplex input channel receiving messages from the duplex output channel and sending back response messages by using Silverlight messaging.
        /// </summary>
        /// <remarks>
        /// The duplex input channel is intended for the bidirectional communication.
        /// It can receive messages from the duplex output channel and send back response messages.
        /// <br/><br/>
        /// The duplex input channel can communicate only with the duplex output channel and not with the output channel.
        /// </remarks>
        /// <param name="channelId">id, the duplex input channel is listening to</param>
        /// <returns>duplex input channel</returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                IInputConnector anInputConnector = myConnectorFactory.CreateInputConnector(channelId);
                return new DefaultDuplexInputChannel(channelId, myDispatcher, myDispatcherAfterMessageDecoded, anInputConnector);
            }
        }

        private IThreadDispatcher myDispatcher;
        private IThreadDispatcher myDispatcherAfterMessageDecoded;
        private ConnectorFactory myConnectorFactory;
    }
}

#endif
