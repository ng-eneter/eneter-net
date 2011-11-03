/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.Nodes.Router
{
    /// <summary>
    /// Declares the factory creating the one-way router.
    /// </summary>
    public interface IRouterFactory
    {
        /// <summary>
        /// Creates the router.
        /// </summary>
        IRouter CreateRouter();
    }
}
