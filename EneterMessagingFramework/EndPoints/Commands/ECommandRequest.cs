/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// Enumerates requests the command can receive.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    public enum ECommandRequest
    {
        /// <summary>
        /// The command is asked to execute the activity.
        /// </summary>
        Execute,

        /// <summary>
        /// The command is asked to pause.
        /// </summary>
        Pause,

        /// <summary>
        /// The command is asked to resume.
        /// </summary>
        Resume,

        /// <summary>
        /// The command is asked to cancel.
        /// </summary>
        Cancel
    }
}
