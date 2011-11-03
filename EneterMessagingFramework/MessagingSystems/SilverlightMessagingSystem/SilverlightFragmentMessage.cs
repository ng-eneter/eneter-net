/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using Eneter.Messaging.DataProcessing.Sequencing;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    /// <summary>
    /// The maximum size in Silverlight messaging is restricted to 40 kilobytes. To send longer messages, the eneter messaging framework
    /// split messages into fragments.
    /// </summary>
    public class SilverlightFragmentMessage : Fragment
    {
        /// <summary>
        /// Default constructor for deserialization.
        /// </summary>
        public SilverlightFragmentMessage()
        {
        }

        /// <summary>
        /// Constructs the fragment from input parameters.
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="sequenceId">sequence identifier</param>
        /// <param name="index">number of the fragment</param>
        /// <param name="isFinal">indicates whether it is the last segment</param>
        public SilverlightFragmentMessage(string message, string sequenceId, int index, bool isFinal)
            : base(sequenceId, index, isFinal)
        {
            Message = message;
        }

        /// <summary>
        /// Message fragment content.
        /// </summary>
        public string Message { get; set; }
    }
}

#endif
