


namespace Eneter.Messaging.Nodes.Dispatcher
{
    /// <summary>
    /// Creates the dispatcher.
    /// </summary>
    /// <remarks>
    /// The dispatcher sends messages to all duplex output channels and also can route back response messages.
    /// </remarks>
    public interface IDuplexDispatcherFactory
    {
        /// <summary>
        /// Creates the dispatcher.
        /// </summary>
        /// <returns>duplex dispatcher</returns>
        IDuplexDispatcher CreateDuplexDispatcher();
    }
}
