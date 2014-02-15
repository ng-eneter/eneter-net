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
    /// Functionality extending behavior of messaging systems.
    /// </summary>
    /// <remarks>
    /// E.g. extending behavior by connection monitoring, buffering, authentication or communication via the message bus.
    /// 
    /// The composite implements IMessagingSystemFactory so it looks like any other messaging but it provides
    /// some additional behavior which is then applied on the underlying messaging.
    /// Multiple composite messaging systems can be applied in a "chain". 
    /// E.g. if you want to have TCP communication with monitored connection and authentication you can
    /// compose it like in the following example.
    /// <example>
    /// Example shows hot composite messaging systems can be chained to create the desired behavior.
    /// <code>
    /// // Create TCP messaging system.
    /// IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();
    /// 
    /// // Create monitored messaging which takes TCP as underlying messaging.
    /// IMessagingSystemFactory aMonitoredMessaging = new MonitoredMessagingFactory(aTcpMessaging);
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
