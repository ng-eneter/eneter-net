/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// Lists possibilities how the command will process requests for execution.
    /// </summary>
    public enum EProcessingStrategy
    {
        /// <summary>
        /// All requests to execute the command are queued and processed by one thread.
        /// </summary>
        SingleThread,

        /// <summary>
        /// All requests to execute the command are executed immediately in separate threads.
        /// </summary>
        MultiThread
    }
}
