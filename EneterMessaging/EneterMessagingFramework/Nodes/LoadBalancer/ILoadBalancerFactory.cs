/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT

namespace Eneter.Messaging.Nodes.LoadBalancer
{
    /// <summary>
    /// Declares the factory for load balancers.
    /// </summary>
    /// <remarks>
    /// The load balancer distributes the workload across a farm of receivers that can run on different machines (or threads).
    /// </remarks>
    public interface ILoadBalancerFactory
    {
        /// <summary>
        /// Creates the load balancer.
        /// </summary>
        /// <returns>load balancer</returns>
        ILoadBalancer CreateLoadBalancer();
    }
}

#endif