/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.Nodes.Router
{
    /// <summary>
    /// Declares the factory creating duplex router.
    /// </summary>
    public interface IDuplexRouterFactory
    {
        /// <summary>
        /// Creates the duplex router.
        /// </summary>
        /// <returns>duplex router</returns>
        IDuplexRouter CreateDuplexRouter();
    }
}
