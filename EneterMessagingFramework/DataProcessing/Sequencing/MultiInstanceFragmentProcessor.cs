/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Sequencing
{
    /// <summary>
    /// Implements the fragment processor able to process more sequences of fragments in parallel.
    /// It creates the fragment processor (IFragmentProcessor) for each sequence.
    /// </summary>
    internal class MultiInstanceFragmentProcessor : IMultiInstanceFragmentProcessor
    {
        public MultiInstanceFragmentProcessor(Func<string, IFragmentProcessor> factoryMethod)
        {
            using (EneterTrace.Entering())
            {
                FragmentProcessorFactoryMethod = factoryMethod;
            }
        }

        public IEnumerable<IFragment> ProcessFragment(IFragment fragment)
        {
            using (EneterTrace.Entering())
            {
                lock (myProcessedFragmentInstances)
                {
                    // Get the processer for the incoming instance.
                    IFragmentProcessor aFragmentProcessor;
                    myProcessedFragmentInstances.TryGetValue(fragment.SequenceId, out aFragmentProcessor);

                    // If the processor is not created yet then create a new one.
                    if (aFragmentProcessor == null)
                    {
                        aFragmentProcessor = FragmentProcessorFactoryMethod(fragment.SequenceId);
                        myProcessedFragmentInstances[fragment.SequenceId] = aFragmentProcessor;
                    }

                    // Process the fragment and get results.
                    // Note: Results depends on the particular FragmentProcessor.
                    IEnumerable<IFragment> aFragments = aFragmentProcessor.ProcessFragment(fragment);

                    // If all fragments are processed for the given instance id then remove it from processed fragments.
                    if (aFragmentProcessor.IsWholeSequenceProcessed)
                    {
                        myProcessedFragmentInstances.Remove(fragment.SequenceId);
                    }

                    return aFragments;
                }
            }
        }

        private Func<string, IFragmentProcessor> FragmentProcessorFactoryMethod { get; set; }

        private Dictionary<string, IFragmentProcessor> myProcessedFragmentInstances = new Dictionary<string, IFragmentProcessor>();
    }
}
