

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.BackupRouter
{
    /// <summary>
    /// Factory creating backup connection router.
    /// </summary>
    public class BackupConnectionRouterFactory : IBackupConnectionRouterFactory
    {
        /// <summary>
        /// Constructs the factory.
        /// </summary>
        /// <param name="outputMessagingFactory">messaging system used to connect services (receivers) via the duplex output channels
        /// </param>
        public BackupConnectionRouterFactory(IMessagingSystemFactory outputMessagingFactory)
        {
            using (EneterTrace.Entering())
            {
                myOutputMessagingFactory = outputMessagingFactory;
            }
        }

        /// <summary>
        /// Creates the backup connection router.
        /// </summary>
        /// <returns></returns>
        public IBackupConnectionRouter CreateBackupConnectionRouter()
        {
            using (EneterTrace.Entering())
            {
                return new BackupConnectionRouter(myOutputMessagingFactory);
            }
        }

        private IMessagingSystemFactory myOutputMessagingFactory;
    }
}