

using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    /// <summary>
    /// Duplex input channel which can work offline.
    /// </summary>
    public interface IBufferedDuplexInputChannel : IDuplexInputChannel
    {
        /// <summary>
        /// The event is raised when a response receiver gets into the online state.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverOnline;

        /// <summary>
        /// The event is raised when a response receiver gets into the offline state.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverOffline;
    }
}
