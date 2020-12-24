


namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Creates multi-typed message senders and receivers.
    /// </summary>
    public interface IMultiTypedMessagesFactory
    {
        /// <summary>
        /// Creates multityped message sender which can send request messages and receive response messages.
        /// </summary>
        /// <returns>multi typed message sender</returns>
        IMultiTypedMessageSender CreateMultiTypedMessageSender();

        /// <summary>
        /// Creates mulityped message sender which sends a request message and then waits for the response.
        /// </summary>
        /// <returns>synchronous multi typed message sender</returns>
        ISyncMultitypedMessageSender CreateSyncMultiTypedMessageSender();

        /// <summary>
        /// Creates multityped message receiver which can receive request messages and send response messages.
        /// </summary>
        /// <returns>multi typed message receiver</returns>
        IMultiTypedMessageReceiver CreateMultiTypedMessageReceiver();
    }
}
