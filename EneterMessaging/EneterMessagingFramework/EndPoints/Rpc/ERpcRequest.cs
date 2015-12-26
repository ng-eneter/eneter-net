/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


namespace Eneter.Messaging.EndPoints.Rpc
{
    /// <summary>
    /// Internal commands for interaction via RPC.
    /// </summary>
    public enum ERpcRequest
    {
        /// <summary>
        /// Client invokes a method.
        /// </summary>
        InvokeMethod = 10,

        /// <summary>
        /// Client subscribes an event.
        /// </summary>
        SubscribeEvent = 20,

        /// <summary>
        /// Client unsubscribes an event.
        /// </summary>
        UnsubscribeEvent = 30,

        /// <summary>
        /// Service raises an event.
        /// </summary>
        RaiseEvent = 40,

        /// <summary>
        /// RPC service sends back a response for 'InvokeMethod', 'SubscribeEvent' or 'UnsubscribeEvent'.
        /// </summary>
        Response = 50
    }
}
