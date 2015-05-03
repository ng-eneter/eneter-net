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
    /// Extensions for messaging systems.
    /// </summary>
    /// <remarks>
    /// The composites are extensions which can be composed on top of each other in order to add additional features
    /// into the communication.
    /// E.g. connection monitoring, connection recovery, authentication or communication via the message bus.<br/>
    /// <br/>
    /// The following example shows how to add the connection monitoring and the authentication into the communication via TCP.
    /// <example>
    /// Example shows hot composite messaging systems can be chained to create the desired behavior.
    /// <code>
    /// // Create TCP messaging system.
    /// IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();
    /// 
    /// // Create monitored messaging which takes TCP as underlying messaging.
    /// IMessagingSystemFactory aMonitoredMessaging = new MonitoredMessagingFactory(anUnderlyingMessaging);
    /// 
    /// // Create messaging with authenticated connection.
    /// // It takes monitored messaging as the underlying messaging.
    /// IMessagingSystemFactory aMessaging = new AuthenticatedMessagingFactory(aMonitoredMessaging, ...);
    /// 
    /// // Creating channels.
    /// IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8095/");
    /// IDuplexInputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8095/");
    /// </code>
    /// </example>
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
