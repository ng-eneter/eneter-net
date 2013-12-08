/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem.Sequencing
{
    /// <summary>
    /// The factory class provides methods to create fragment processors.
    /// The fragment processors provides strategies to process incoming fragments of data.
    /// </summary>
    internal class FragmentDataFactory
    {
        /// <summary>
        /// Creates the fragment processor ordering the incoming sequence of fragments.
        /// The sequencer receives data fragments and continuously returns the sequence of ordered fragments.
        /// </summary>
        public IFragmentProcessor CreateFragmentSequencer(string sequenceInstanceId)
        {
            using (EneterTrace.Entering())
            {
                return new FragmentSequencer(sequenceInstanceId);
            }
        }

        /// <summary>
        /// Creates the fragment processor ordering the incoming sequence of fragments.
        /// The sequencer receives data fragments and when the whole sequence is collected it returns it.
        /// </summary>
        public IFragmentProcessor CreateSequenceFinalizer(string sequenceInstanceId)
        {
            using (EneterTrace.Entering())
            {
                return new FragmentFinalizer(sequenceInstanceId);
            }
        }

        /// <summary>
        /// Creates the fragment processor able to process more sequences at once.
        /// </summary>
        /// <param name="fragmentProcessorFactoryMethod">
        /// Factory method used to create processors for particular sequences.
        /// Note: CreateFragmentSequencer() and CreateSequenceFinalizer() from this factory class can be used as factory methods.
        /// </param>
        public IMultiInstanceFragmentProcessor CreateMultiinstanceFragmentProcessor(Func<string, IFragmentProcessor> fragmentProcessorFactoryMethod)
        {
            using (EneterTrace.Entering())
            {
                return new MultiInstanceFragmentProcessor(fragmentProcessorFactoryMethod);
            }
        }
    }
}


#endif