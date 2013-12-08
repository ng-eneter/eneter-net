/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System.Collections.Generic;
using Eneter.Messaging.Diagnostic;
using System;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem.Sequencing
{
    /// <summary>
    /// The class collects and sorts incoming fragments.
    /// When all fragments are collected the ProcessFragment method returns the fragment of sorted fragments.
    /// </summary>
    internal class FragmentFinalizer : IFragmentProcessor
    {
        /// <summary>
        /// The constructor initializes the finalizer for the given instance id.
        /// </summary>
        /// <param name="sequenceId">sequence that the finalizer processes</param>
        public FragmentFinalizer(string sequenceId)
        {
            using (EneterTrace.Entering())
            {
                FragmentSequencer = new FragmentSequencer(sequenceId);
            }
        }

        /// <summary>
        /// Returns the instance id.
        /// The instance id specifies which fragments belong together.
        /// </summary>
        public string SequenceId { get { return FragmentSequencer.SequenceId; } }

        /// <summary>
        /// Returns true if all fragments have been processed.
        /// </summary>
        public bool IsWholeSequenceProcessed { get { return FragmentSequencer.IsWholeSequenceProcessed; } }

        /// <summary>
        /// The method puts incoming fragment to the sequenece.
        /// It returns the collection when all fragments are collected and sorted.
        /// </summary>
        /// <param name="fragment"></param>
        /// <returns></returns>
        public IEnumerable<Fragment> ProcessFragment(Fragment fragment)
        {
            using (EneterTrace.Entering())
            {
                if (fragment.SequenceId != SequenceId)
                {
                    string anError = TracedObject + "processes sequence id '" + SequenceId + "' but the incoming fragment has the sequence id '" + fragment.SequenceId + "'.";
                    EneterTrace.Error(anError);
                    throw new ArgumentException(anError);
                }

                // Ask sequencer to get sequences following each other.
                IEnumerable<Fragment> aSequencedSequences = FragmentSequencer.ProcessFragment(fragment);

                // Add the fragment of fragments among finalized sorted sequences.
                mySortedFragments.AddRange(aSequencedSequences);

                return (IsWholeSequenceProcessed) ? mySortedFragments : new List<Fragment>();
            }
        }


        /// <summary>
        /// The sequencer for sequencing of fragments.
        /// </summary>
        private FragmentSequencer FragmentSequencer { get; set; }

        /// <summary>
        /// The collection collects ordered fragments.
        /// </summary>
        private List<Fragment> mySortedFragments = new List<Fragment>();


        private readonly string TracedObject = "FragmentFinalizer ";
    }
}


#endif