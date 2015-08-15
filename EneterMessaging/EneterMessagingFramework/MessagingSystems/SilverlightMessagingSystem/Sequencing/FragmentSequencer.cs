/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using System.Collections.Generic;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem.Sequencing
{
    internal class FragmentSequencer : IFragmentProcessor
    {
        /// <summary>
        /// The constructor initializes the sequencer for a particular instance.
        /// </summary>
        /// <param name="sequenceId"></param>
        public FragmentSequencer(string sequenceId)
        {
            using (EneterTrace.Entering())
            {
                SequenceId = sequenceId;
            }
        }

        public string SequenceId { get; private set; }

        public bool IsWholeSequenceProcessed
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myPendingSequences))
                    {
                        return IsFinalFragmentReceived && myPendingSequences.Count == 0;
                    }
                }
            }
        }

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

                using (ThreadLock.Lock(myPendingSequences))
                {
                    // Insert incoming fragment to pendings ordered according to fragment index
                    bool isInserted = false;
                    for (int i = 0; i < myPendingSequences.Count; ++i)
                    {
                        if (myPendingSequences[i].Index > fragment.Index)
                        {
                            myPendingSequences.Insert(i, fragment);
                            isInserted = true;
                            break;
                        }
                    }
                    if (!isInserted)
                    {
                        myPendingSequences.Add(fragment);
                    }

                    // If the incoming fragment is final then record that.
                    // Note: Because the sequences can come unordered even the final fragment is received
                    //       still some missing sequences can come.
                    if (fragment.IsFinal)
                    {
                        IsFinalFragmentReceived = true;
                    }

                    // Get all sequences following the expected fragment index
                    List<Fragment> aCalculatedSequence = new List<Fragment>();

                    for (int i = 0; i < myPendingSequences.Count; )
                    {
                        // If it is the expected item in the fragment.
                        if (myPendingSequences[i].Index == myExpectedSequenceIndex)
                        {
                            // Put the fragment calculated sequences.
                            aCalculatedSequence.Add(myPendingSequences[i]);

                            // Set the next expected fragment.
                            ++myExpectedSequenceIndex;

                            // The fragment is not pending anymore.
                            myPendingSequences.RemoveAt(i);
                        }
                        else
                        {
                            // This and next items are not following sequences.
                            // Therefore stop and keep them in pendings.
                            break;
                        }
                    }

                    return aCalculatedSequence;
                }
            }
        }

        private readonly string TracedObject = "FragmentSequencer ";

        private bool IsFinalFragmentReceived { get; set; }
        private int myExpectedSequenceIndex = 0;
        private List<Fragment> myPendingSequences = new List<Fragment>();
    }
}

#endif