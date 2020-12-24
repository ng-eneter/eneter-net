

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