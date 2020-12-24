


namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Creates typed message senders and receivers.
    /// </summary>
    public interface IDuplexTypedMessagesFactory
    {
        /// <summary>
        /// Creates message sender (client) which can send messages and receive response messages.
        /// </summary>
        /// <typeparam name="TResponse">Type of response messages.</typeparam>
        /// <typeparam name="TRequest">Type of request messages.</typeparam>
        /// <returns>message sender</returns>
        IDuplexTypedMessageSender<TResponse, TRequest> CreateDuplexTypedMessageSender<TResponse, TRequest>();

        /// <summary>
        /// Creates message sender (client) which sends a request message and then waits for the response.
        /// </summary>
        /// <typeparam name="TResponse">Type of response messages.</typeparam>
        /// <typeparam name="TRequest">Type of request messages.</typeparam>
        /// <returns>synchronous message sender</returns>
        ISyncDuplexTypedMessageSender<TResponse, TRequest> CreateSyncDuplexTypedMessageSender<TResponse, TRequest>();
        
        /// <summary>
        /// Creates message receiver (service) which can receive messages and send back response messages.
        /// </summary>
        /// <typeparam name="TResponse">Type of response messages.</typeparam>
        /// <typeparam name="TRequest">Type of request messages.</typeparam>
        /// <returns>typed message receiver</returns>
        IDuplexTypedMessageReceiver<TResponse, TRequest> CreateDuplexTypedMessageReceiver<TResponse, TRequest>();
    }
}
