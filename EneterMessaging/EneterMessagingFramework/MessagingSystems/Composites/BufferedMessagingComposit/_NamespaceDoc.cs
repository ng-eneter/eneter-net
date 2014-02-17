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
    /// Buffering of sent messages if the connection is not available.
    /// </summary>
    /// <remarks>
    /// The buffered messaging is intended to temporarily store sent messages until the network connection is established.<br/>
    /// Typical scenarios are:
    /// <br/><br/>
    /// <b>Short disconnections</b><br/>
    /// In case of unstable the network the connection can broken. Buffered messaging will try to reconnect the broken connection
    /// and meanwhile it will store sent messages in the buffer. Then when the connection is repaired it will send messages from
    /// the buffer.
    /// <br/><br/>
    /// <b>Independent startup order</b><br/>
    /// It can be tricky to start communicating application in a defined order. Buffered messaging allows to start
    /// applications in undefined order. If messages are sent to an application which is not started yet they will be stored
    /// in the buffer until the application is started.
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
