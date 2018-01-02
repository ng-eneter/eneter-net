/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

namespace Eneter.Messaging.Nodes.BackupRouter
{
    /// <summary>
    /// Declares the factory for creating the backup connection router.
    /// </summary>
    public interface IBackupConnectionRouterFactory
    {
        /// <summary>
        /// Creates the backup connection router.
        /// </summary>
        /// <returns></returns>
        IBackupConnectionRouter CreateBackupConnectionRouter();
    }
}