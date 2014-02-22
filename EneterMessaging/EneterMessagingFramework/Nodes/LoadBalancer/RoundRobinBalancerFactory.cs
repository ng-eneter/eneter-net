/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.LoadBalancer
{
    /// <summary>
    /// Implements factory to create the load balancer based on Round-Robin algorithm.
    /// </summary>
    /// <remarks>
    /// The Round-Robin balancer distributes the incoming requests equally to all maintained receivers.
    /// It means, the balancer maintains which receiver was used the last time. Then, when a new request comes,
    /// the balancer picks the next receiver in the list up. If it is at the end, then it starts from the beginning.
    /// </remarks>
    public class RoundRobinBalancerFactory : ILoadBalancerFactory
    {
        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="duplexOutputChannelsFactory">
        /// messaging system used to create duplex output channels that will be used for the communication with
        /// services from the farm.
        /// </param>
        public RoundRobinBalancerFactory(IMessagingSystemFactory duplexOutputChannelsFactory)
        {
            using (EneterTrace.Entering())
            {
                myDuplexOutputChannelsFactory = duplexOutputChannelsFactory;
            }
        }

        /// <summary>
        /// Creates the load balancer using the Round-Robin algorithm.
        /// </summary>
        /// <returns>load balancer</returns>
        public ILoadBalancer CreateLoadBalancer()
        {
            using (EneterTrace.Entering())
            {
                return new RoundRobinBalancer(myDuplexOutputChannelsFactory);
            }
        }

        private IMessagingSystemFactory myDuplexOutputChannelsFactory;
    }
}

#endif