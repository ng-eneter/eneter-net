/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    /// <summary>
    /// Provides the messaging system which buffers sent messages if the connection is not available.
    /// </summary>
    /// <remarks>
    /// The buffered messaging is intended to temporarily store sent messages while the network connection is not available.
    /// Typical scenarios are:
    /// <br/><br/>
    /// <b>Short disconnections</b><br/>
    /// The network connection is unstable and can be interrupted. In case of the disconnection, the sent messages are stored
    /// in the buffer while the connection tries to be automatically reopen. If the reopen is successful and the connection
    /// is established, the messages are sent from the buffer.
    /// <br/><br/>
    /// <b>Independent startup order</b><br/>
    /// The communicating applications starts in undefined order and initiates the communication. In case the application receiving
    /// messages is not up, the sent messages are stored in the buffer. Then when the receiving application is running, the messages
    /// are automatically sent from the buffer.
    /// 
    /// <example>
    /// Simple client buffering messages in case of a disconnection.
    /// <code>
    /// // Create TCP messaging.
    /// IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();
    /// 
    /// // Create buffered messaging that internally uses TCP.
    /// IMessagingSystemFactory aMessaging = new BufferedMessagingSystemFactory(anUnderlyingMessaging);
    /// 
    /// // Create the duplex output channel.
    /// IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8045/");
    /// 
    /// // Create message sender to send simple string messages.
    /// IDuplexStringMessagesFactory aSenderFactory = new DuplexStringMessagesFactory();
    /// IDuplexStringMessageSender aSender = aSenderFactory.CreateDuplexStringMessageSender();
    /// 
    /// // Subscribe to receive responses.
    /// aSender.ResponseReceived += OnResponseReceived;
    /// 
    /// // Attach output channel an be able to send messages and receive responses.
    /// aSender.AttachDuplexOutputChannel(anOutputChannel);
    /// 
    /// ...
    /// 
    /// // Send a message. If the connection was broken the message will be stored in the buffer.
    /// // Note: The buffered messaging will try to reconnect automatically.
    /// aSender.SendMessage("Hello.");
    /// </code>
    /// </example>
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
