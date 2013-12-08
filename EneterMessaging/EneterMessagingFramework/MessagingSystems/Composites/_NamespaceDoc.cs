/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System.Runtime.CompilerServices;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;


namespace Eneter.Messaging.MessagingSystems.Composites
{
    /// <summary>
    /// Functionality extending the default behavior of messaging systems.
    /// </summary>
    /// <remarks>
    /// E.g.: Buffering of sent messages, or network connection monitoring.<br/>
    /// The composite is a messaging system derived from
    /// <see cref="IMessagingSystemFactory"/> and implements the extending functionality.
    /// It means, e.g. if you wish to buffer sent messages during the disconnection,
    /// you can create the buffered messaging system.
    /// <example>
    /// Creating of the buffered messaging using the TCP as the underlying messaging system.
    /// <code>
    /// // Create TCP messaging system.
    /// IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();
    /// 
    /// // Create the buffered messaging using TCP as the underlying messaging.
    /// IMessagingSystemFactory aBufferedMessaging = new BufferedMessagingFactory(anUnderlyingMessaging);
    /// </code>
    /// </example>
    /// Creating buffered messaging that internally uses monitored messaging constantly monitoring the connection.
    /// <example>
    /// Creating the TCP based messaging system constantly checking the network connection and providing the buffer
    /// for sent messages in case of the disconnection.
    /// <code>
    /// // Create TCP messaging system.
    /// IMessagingSystemFactory aTcpMessaging = new TcpMessagingSystemFactory();
    /// 
    /// // Create the composite providing the network connection monitor.
    /// IMessagingSystemFactory aMonitoredMessaging = new MonitoredMessagingFactory(aTcpMessaging);
    /// 
    /// // Create the composite providing the buffer used for the sent messages in case of the disconnection.
    /// IMessagingSystemFactory aBufferedMonitoredMessaging = new BufferedMessagingFactory(aMonitoredMessaging);
    /// 
    /// 
    /// ...
    /// 
    /// // Create the duplex output channel, that monitores the network connection and buffers sent messages if disconnected.
    /// IDuplexOutputChannel aDuplexOutputChannel = aBufferedMonitoredMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:6080/");
    /// 
    /// </code>
    /// </example>
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
