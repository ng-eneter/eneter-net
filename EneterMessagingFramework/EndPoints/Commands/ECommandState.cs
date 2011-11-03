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
    /// Enumerates possible commands states.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    public enum ECommandState
    {
        /// <summary>
        /// The state is not applicable.
        /// </summary>
        NotApplicable,

        /// <summary>
        /// The command was not executed yet.
        /// </summary>
        NotStarted,

        /// <summary>
        /// The execute request is in the progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// The execute request is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// The execute request is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// The execute request is canceled.
        /// </summary>
        Canceled,

        /// <summary>
        /// The execute request failed.
        /// </summary>
        Failed
    }
}
