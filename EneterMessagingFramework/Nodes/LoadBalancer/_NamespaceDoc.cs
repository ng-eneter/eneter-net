/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.Nodes.LoadBalancer
{
    /// <summary>
    /// Functionality to distribute the workload across a farm of receivers.
    /// </summary>
    /// <remarks>
    /// The load balancer maintains a list of receivers processing a certain request.
    /// When the balancer receives the request, it chooses which receiver shall process it,
    /// so that all receivers are loaded optimally.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}

#endif