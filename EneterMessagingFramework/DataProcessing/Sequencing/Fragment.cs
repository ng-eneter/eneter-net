/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.DataProcessing.Sequencing
{
    /// <summary>
    /// Provides the basic implementation for IFragment.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class Fragment : IFragment
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
        [DataMember]
        public string SequenceId { get; set; }

        /// <summary>
        /// Returns the position of the fragment in the sequence.
        /// </summary>
        [DataMember]
        public int Index { get; set; }

        /// <summary>
        /// Returns true if the item is the last in the sequence.
        /// </summary>
        [DataMember]
        public bool IsFinal { get; set; }
    }
}
