/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem.Sequencing
{
    /// <summary>
    /// Internal implementation of fragment. Fragments are used in Silverlight messaging to transfer messages bigger than the Silverlight limit.
    /// </summary>
    public class Fragment
    {
        /// <summary>
        /// Default constructor used for the deserialization.
        /// </summary>
        public Fragment()
        {
        }

        /// <summary>
        /// Constructs the fragment from the input parameters.
        /// </summary>
        /// <param name="sequenceId">Identifies the sequence where the fragment belongs.</param>
        /// <param name="index">Position in the sequence.</param>
        /// <param name="isFinal">Indicates whether it is the last fragment.</param>
        public Fragment(string sequenceId, int index, bool isFinal)
        {
            SequenceId = sequenceId;
            Index = index;
            IsFinal = isFinal;
        }

        /// <summary>
        /// Returns the sequence identifier.
        /// </summary>
        public string SequenceId { get; set; }

        /// <summary>
        /// Returns the position of the fragment in the sequence.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Returns true if the item is the last in the sequence.
        /// </summary>
        public bool IsFinal { get; set; }
    }
}

#endif