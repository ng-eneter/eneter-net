/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.DataProcessing.Sequencing
{
    /// <summary>
    /// The interface declares the fragment.
    /// Fragments are used to sequence data.
    /// The fragment contains the SequenceId identifying the sequence where the fragment blongs, Index specifying the position in the sequence
    /// and IsFinal indicating if it is the last fragment of the sequence.<br/>
    /// </summary>
    public interface IFragment
    {
        /// <summary>
        /// Returns identifier of the sequence where the fragment belongs.
        /// </summary>
        string SequenceId { get; }

        /// <summary>
        /// Returns the position of the fragment in the sequence.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Returns true if it is the last fragment in the sequence.
        /// </summary>
        bool IsFinal { get; }
    }
}
