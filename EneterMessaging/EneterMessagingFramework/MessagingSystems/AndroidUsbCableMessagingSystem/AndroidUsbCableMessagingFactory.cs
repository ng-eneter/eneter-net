/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT && !COMPACT_FRAMEWORK

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;

namespace Eneter.Messaging.MessagingSystems.AndroidUsbCableMessagingSystem
{
    /// <summary>
    /// Factory creating duplex output channels for the communication with Android device via the USB cable.
    /// </summary>
    /// <remarks>
    /// When Android device is connected to the computer via the USB cable the process adb (Android Debug Bridge) is started
    /// on the computer and adbd (Android Debug Bridge Daemon) is started on the Android device.
    /// These processes then communicate via the USB cable.<br/>
    /// <br/>
    /// How this messaging works:
    /// <ol>
    /// <li>Your desktop application sends a message via the output channel created by AndroidUsbCableMessagingFactory</li>
    /// <li>The output channel internally sends the message via TCP to the adb service.</li>
    /// <li>adb service receives data and transfers it via USB cable to adbd.</li>
    /// <li>adbd in the Android device receives data and forwards it via TCP to the desired port.</li>
    /// <li>Android application listening on that port receives the message and processes it.</li>
    /// </ol>
    /// Notice there is a restrction for this type of communication:<br/>
    /// The Android application must be a listener (service) and the computer application must be the client.<br/>
    /// </remarks>
    /// <example>
    /// The service on the android side. (implemented in Java)
    /// <code>
    /// package eneter.testing;
    /// 
    /// import eneter.messaging.diagnostic.EneterTrace;
    /// import eneter.messaging.endpoints.typedmessages.*;
    /// import eneter.messaging.messagingsystems.messagingsystembase.*;
    /// import eneter.messaging.messagingsystems.tcpmessagingsystem.TcpMessagingSystemFactory;
    /// import eneter.net.system.EventHandler;
    /// import android.app.Activity;
    /// import android.os.Bundle;
    /// 
    /// public class AndroidUsbCableServiceActivity extends Activity
    /// {
    ///     // Eneter communication.
    ///     private IDuplexTypedMessageReceiver&lt;String, String&gt; myEchoReceiver;
    ///     
    ///     
    ///     /** Called when the activity is first created. */
    ///     @Override
    ///     public void onCreate(Bundle savedInstanceState)
    ///     {
    ///         super.onCreate(savedInstanceState);
    ///         setContentView(R.layout.main);
    ///         
    ///         // Start listening.
    ///         startListening();
    ///     }
    ///     
    ///     @Override
    ///     public void onDestroy()
    ///     {
    ///         stopListening();
    ///         
    ///         super.onDestroy();
    ///     }
    ///     
    ///     private void startListening()
    ///     {
    ///         try
    ///         {
    ///             // Create message receiver.
    ///             IDuplexTypedMessagesFactory aReceiverFactory = new DuplexTypedMessagesFactory();
    ///             myEchoReceiver = aReceiverFactory.createDuplexTypedMessageReceiver(String.class, String.class);
    ///             
    ///             // Subscribe to receive messages.
    ///             myEchoReceiver.messageReceived().subscribe(new EventHandler&lt;TypedRequestReceivedEventArgs&lt;String&gt;&gt;()
    ///                 {
    ///                     @Override
    ///                     public void onEvent(Object sender, TypedRequestReceivedEventArgs&lt;String&gt; e)
    ///                     {
    ///                         // Response back with the same message.
    ///                         try
    ///                         {
    ///                             myEchoReceiver.sendResponseMessage(e.getResponseReceiverId(), e.getRequestMessage());
    ///                         }
    ///                         catch (Exception err)
    ///                         {
    ///                             EneterTrace.error("Sending echo response failed.", err);
    ///                         }
    ///                     }
    ///                 });
    ///             
    ///             // Create TCP messaging.
    ///             // It must listen to IP address 127.0.0.1. You can set desired port e.g. 8090.
    ///             IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    ///             IDuplexInputChannel anInputChannel = aMessaging.createDuplexInputChannel("tcp://127.0.0.1:8090/");
    ///             
    ///             // Attach the input channel to the receiver and start listening.
    ///             myEchoReceiver.attachDuplexInputChannel(anInputChannel);
    ///         }
    ///         catch (Exception err)
    ///         {
    ///             EneterTrace.error("OpenConnection failed.", err);
    ///         }
    ///     }
    ///     
    ///     private void stopListening()
    ///     {
    ///         // Detach input channel and stop listening.
    ///         myEchoReceiver.detachDuplexInputChannel();
    ///     }
    /// }
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// The client application communicating with the Android application via the USB cable.
    /// <code>
    /// using System;
    /// using System.Windows.Forms;
    /// using Eneter.Messaging.EndPoints.TypedMessages;
    /// using Eneter.Messaging.MessagingSystems.AndroidUsbCableMessagingSystem;
    /// using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
    /// 
    /// namespace AndroidEchoClient
    /// {
    ///     public partial class Form1 : Form
    ///     {
    ///         private IDuplexTypedMessageSender&lt;string, string&gt; myEchoSender;
    /// 
    ///         public Form1()
    ///         {
    ///             InitializeComponent();
    /// 
    ///             // Echo sender-receiver
    ///             IDuplexTypedMessagesFactory aSenderFactory = new DuplexTypedMessagesFactory();
    ///             myEchoSender = aSenderFactory.CreateDuplexTypedMessageSender&lt;string, string&gt;();
    /// 
    ///             // Subscribe to get the response.
    ///             myEchoSender.ResponseReceived += OnResponseReceived;
    /// 
    ///             // Create messaging using the USB cable connected to Android device.
    ///             IMessagingSystemFactory aMessaging = new AndroidUsbCableMessagingFactory();
    /// 
    ///             // Create output channel.
    ///             // It sets the Android application listens to port 8090.
    ///             IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("8090");
    ///             myEchoSender.AttachDuplexOutputChannel(anOutputChannel);
    ///         }
    /// 
    ///         private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    ///         {
    ///             myEchoSender.DetachDuplexOutputChannel();
    ///         }
    /// 
    ///         private void SendBtn_Click(object sender, EventArgs e)
    ///         {
    ///             // Send the message.
    ///             myEchoSender.SendRequestMessage(TextMessageTextBox.Text);
    ///         }
    /// 
    ///         private void OnResponseReceived(object sender, TypedResponseReceivedEventArgs&lt;string&gt; e)
    ///         {
    ///             // This is not the UI thread, so
    ///             // route displaying to the main UI thread.
    ///             if (InvokeRequired)
    ///             {
    ///                 Action aUIUpdate = () =&gt; ResponseLabel.Text = e.ResponseMessage;
    ///                 Invoke(aUIUpdate);
    ///             }
    ///             else
    ///             {
    ///                 ResponseLabel.Text = e.ResponseMessage;
    ///             }
    ///         }
    /// 
    ///     }
    /// }
    /// 
    /// </code>
    /// </example>
    public class AndroidUsbCableMessagingFactory : IMessagingSystemFactory
    {
        /// <summary>
        /// Constructs the Android messaging via the USB cable. It expects the adb service listens on default port 5037.
        /// </summary>
        public AndroidUsbCableMessagingFactory()
            : this(5037, new EneterProtocolFormatter())
        {
        }

        /// <summary>
        /// Constructs the Android messaging via the USB cable with specified parameters.
        /// </summary>
        /// <param name="adbHostPort">Port where adb service is listening to commands. Default value is 5037.</param>
        /// <param name="protocolFormatter">Low level formatting used for encoding messages between channels.
        /// EneterProtocolFormatter() can be used by default.
        /// </param>
        public AndroidUsbCableMessagingFactory(int adbHostPort, IProtocolFormatter<byte[]> protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                myAdbHostPort = adbHostPort;
                myUnderlyingTcpMessaging = new TcpMessagingSystemFactory(protocolFormatter);
                myProtocolFormatter = protocolFormatter;
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages via the USB cable to the duplex input channel on Android device.
        /// </summary>
        /// <remarks>
        /// <example>
        /// Using AndroidUsbCableMessagingFactory to create a client on the computer.
        /// <code>
        /// // Create messaging using Android USB cable.
        /// IMessagingSystemFactory aMessaging = new AndroidUsbCableMessagingFactory();
        /// 
        /// // Create duplex output channel that will comunicate via the port 7634.
        /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("7634");
        /// 
        /// // Create message sender that will send messages.
        /// ISyncTypedMessagesFactory aSenderFactory = new SyncTypedMessagesFactory();
        /// ISyncTypedMessageSender aSender = aSenderFactory.CreateSyncMessageSender&lt;string,string&gt;();
        /// 
        /// // Attach the output channel and be able to send messages and receive responses.
        /// // Note: It will configure adb to listen on the port 7634 and forward incoming data via the cable
        /// //       to Android where adbd will forward it to the port 7634.
        /// aSender.AttachDuplexOutputChannel(anOutputChannel);
        /// 
        /// // Send message and wait for the response.
        /// string aResponse = aSender.SendRequestMessage("Hello.");
        /// ...
        /// </code>
        /// Service code on the Android side.
        /// <code>
        /// // Create TCP messaging listening on the same port 7634.
        /// // Note: Use standard TCP messaging, just listen to the specified port.
        /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
        /// IDuplexInputChannel anInputChannel = aMessaging.createDuplexInputChannel("tcp://127.0.0.1:7634/");
        /// 
        /// // Create message receiver.
        /// IDuplexTypedMessagesFactory aReceiverFactory = new DuplexTypedMessagesFactory();
        /// myReceiver = aReceiverFactory.createDuplexTypedMessageReceiver(String.class, String.class);
        /// 
        /// // Subscribe to receive messages.
        /// myReceiver.messageReceived().subscribe(new EventHandler&lt;TypedRequestReceivedEventArgs&lt;String&gt;&gt;()
        /// {
        ///     @Override
        ///     public void onEvent(Object sender, TypedRequestReceivedEventArgs&lt;String&gt; e)
        ///     {
        ///         // Response back with the same message.
        ///         try
        ///         {
        ///             myReceiver.sendResponseMessage(e.getResponseReceiverId(), e.getRequestMessage());
        ///         }
        ///         catch (Exception err)
        ///         {
        ///             EneterTrace.error("Sending echo response failed.", err);
        ///         }
        ///     }
        /// });
        /// 
        /// // Attach the input channel to the receiver and start listening.
        /// myReceiver.attachDuplexInputChannel(anInputChannel);
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="channelId">Port number where the Android application is listening.</param>
        /// <returns></returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                return new AndroidUsbDuplexOutputChannel(int.Parse(channelId), null, myAdbHostPort, myProtocolFormatter, myUnderlyingTcpMessaging);
            }
        }

        /// <summary>
        /// Creates the duplex output channel sending messages via the USB cable to the duplex input channel on Android device.
        /// </summary>
        /// <param name="channelId">Port number where the Android application is listening.</param>
        /// <param name="responseReceiverId">Identifies the response receiver of this duplex output channel.</param>
        /// <returns></returns>
        public IDuplexOutputChannel CreateDuplexOutputChannel(string channelId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                return new AndroidUsbDuplexOutputChannel(int.Parse(channelId), responseReceiverId, myAdbHostPort, myProtocolFormatter, myUnderlyingTcpMessaging);
            }
        }

        /// <summary>
        /// Not supported. The known restriction is that Android cannot be client. Therefore, .NET or Java application
        /// running on PC cannot be a service using the duplex input chanel for listening. :-(
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public IDuplexInputChannel CreateDuplexInputChannel(string channelId)
        {
            throw new NotSupportedException("Duplex input channel is not supported for Android USB cable messaging.");
        }

        /// <summary>
        /// Returns underlying TCP messaging.
        /// </summary>
        /// <remarks>
        /// It allows to set parameters like timeouts and threading mode.
        /// </remarks>
        public IMessagingSystemFactory UnderlyingTcpMessaging
        {
            get
            {
                return myUnderlyingTcpMessaging;
            }
        }

        private int myAdbHostPort;
        private IMessagingSystemFactory myUnderlyingTcpMessaging;
        private IProtocolFormatter<byte[]> myProtocolFormatter;
    }
}

#endif