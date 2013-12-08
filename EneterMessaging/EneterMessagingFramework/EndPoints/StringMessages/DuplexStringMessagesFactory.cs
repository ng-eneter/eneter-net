/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


using Eneter.Messaging.Diagnostic;
namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// Implements the factory to create duplex string message sender and receiver.
    /// </summary>
    /// <remarks>
    /// <example>
    /// Client sending and receiving text messages.
    /// <code>
    /// // Create string message sender.
    /// IDuplexStringMessagesFactory aSenderFactory = new DuplexStringMessagesFactory();
    /// IDuplexStringMessageSender aSender = aSenderFactory.CreateDuplexStringMessageSender();
    /// 
    /// // Subscribe to receive responses.
    /// aSender.ResponseReceived += OnResponseReceived;
    /// 
    /// // Attach duplex output channel and be able to send messages and receive responses.
    /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:9876/");
    /// aSender.AttachDuplexOutputChannel(anOutputChannel);
    /// 
    /// // Send a message.
    /// aSender.SendMessage("Hello.");
    /// 
    /// ...
    /// 
    /// // Do not forget to detach the output channel e.g. before application stops.
    /// // It will release the thread listening to response messages.
    /// aSender.DetachDuplexOutputChannnel();
    /// 
    /// </code>
    /// </example>
    /// <example>
    /// Service sending and receiving text messages.
    /// <code>
    /// // Create string message receiver.
    /// IDuplexStringMessagesFactory aReceiverFactory = new DuplexStringMessagesFactory();
    /// IDuplexStringMessageReceiver aReceiver = aReceiverFactory.CreateDuplexStringMessageReciever();
    /// 
    /// // Subscribe to receive responses.
    /// aReceiver.RequestReceived += OnRequestReceived;
    /// 
    /// // Attach duplex input channel and start listening.
    /// IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory();
    /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:9876/");
    /// aReceiver.AttachDuplexInputChannel(anInputChannel);
    /// 
    /// ...
    /// 
    /// // Stop listening.
    /// aReceiver.DetachDuplexInputChannel();
    /// 
    /// ...
    /// 
    /// void OnRequestReceived(object sender, StringRequestReceivedEventArgs e)
    /// {
    ///     IDuplexStringMessageReceiver aReceiver = (IDuplexStringMessageReceiver) sender;
    ///     
    ///     // Send back the response message.
    ///     aReceiver.SendResponseMessage(e.ResponseReceiverId, "Hi, I am here.");
    /// }
    /// 
    /// </code>
    /// </example>
    /// </remarks>
    public class DuplexStringMessagesFactory : IDuplexStringMessagesFactory
    {
        /// <summary>
        /// Creates the duplex string message sender.
        /// </summary>
        /// <returns>duplex string message sender</returns>
        public IDuplexStringMessageSender CreateDuplexStringMessageSender()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexStringMessageSender();
            }
        }

        /// <summary>
        /// Creates the duplex string message receiver.
        /// </summary>
        /// <returns>duplex string message receiver</returns>
        public IDuplexStringMessageReceiver CreateDuplexStringMessageReceiver()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexStringMessageReceiver();
            }
        }
    }
}
