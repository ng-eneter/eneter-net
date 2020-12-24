

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Provides dispatcher that shall be used for raising events and delivering messages in a correct thread.
    /// </summary>
    public interface IThreadDispatcherProvider
    {
        /// <summary>
        /// Returns dispatcher that will invoke methods according to its threading model.
        /// </summary>
        /// <returns></returns>
        IThreadDispatcher GetDispatcher();
    }
}
