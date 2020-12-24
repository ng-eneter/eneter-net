

using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    /// <summary>
    /// Interface for components which want to attach one IDuplexInputChannel.
    /// </summary>
    /// <remarks>
    /// Communication components implementing this interface can attach the duplex input channel and
    /// receive messages and sends response messages.
    /// </remarks>
    public interface IAttachableDuplexInputChannel
    {
        /// <summary>
        /// Attaches the duplex input channel and starts listening to messages.
        /// </summary>
        void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel);

        /// <summary>
        /// Detaches the duplex input channel and stops listening to messages.
        /// </summary>
        void DetachDuplexInputChannel();

        /// <summary>
        /// Returns true if the duplex input channel is attached.
        /// </summary>
        bool IsDuplexInputChannelAttached { get; }

        /// <summary>
        /// Retutns attached duplex input channel.
        /// </summary>
        IDuplexInputChannel AttachedDuplexInputChannel { get; }
    }
}
