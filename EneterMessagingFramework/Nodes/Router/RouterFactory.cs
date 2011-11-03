/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Nodes.Router
{
    /// <summary>
    /// Implements the factory creating the router.
    /// </summary>
    public class RouterFactory : IRouterFactory
    {
        /// <summary>
        /// Creates the router.
        /// </summary>
        public IRouter CreateRouter()
        {
            using (EneterTrace.Entering())
            {
                return new Router();
            }
        }
    }
}
