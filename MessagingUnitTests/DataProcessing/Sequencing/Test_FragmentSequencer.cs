using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Sequencing;
using System.Threading;

namespace Eneter.MessagingUnitTests.DataProcessing.Sequencing
{
    [TestFixture]
    public class Test_FragmentSequencer
    {
        [SetUp]
        public void Setup()
        {
            FragmentDataFactory aFragmentDataFactory = new FragmentDataFactory();
            myFragmentSequencer = aFragmentDataFactory.CreateFragmentSequencer("MySeqId");
        }

        [Test]
        public void ProcessSequence()
        {
            Assert.AreEqual("MySeqId", myFragmentSequencer.SequenceId);

            for (int i = 0; i < 100; ++i)
            {
                Fragment aFragment1 = new Fragment("MySeqId", i, i == 99);
                IEnumerable<IFragment> aResult = myFragmentSequencer.ProcessFragment(aFragment1);

                Assert.AreEqual(1, aResult.Count());
                Assert.AreEqual(i, aResult.First().Index);
                Assert.AreEqual(i == 99, aResult.First().IsFinal);
                Assert.AreEqual("MySeqId", aResult.First().SequenceId);
            }
        }

        [Test]
        public void ProcessSequence_ComesNotOrdered()
        {
            Assert.AreEqual("MySeqId", myFragmentSequencer.SequenceId);

            // The fragment with index 0 is the first one therefore the sequencer should return it
            Fragment aFragment = new Fragment("MySeqId", 0, false);
            IEnumerable<IFragment> aResult = myFragmentSequencer.ProcessFragment(aFragment);
            Assert.AreEqual(1, aResult.Count());
            Assert.AreEqual(0, aResult.First().Index);
            Assert.AreEqual(false, aResult.First().IsFinal);
            Assert.AreEqual("MySeqId", aResult.First().SequenceId);

            // The fragment with index 1 is the second (it goeas according the order) therefore the sequencer should return it
            aFragment = new Fragment("MySeqId", 1, false);
            aResult = myFragmentSequencer.ProcessFragment(aFragment);
            Assert.AreEqual(1, aResult.Count());
            Assert.AreEqual(1, aResult.First().Index);
            Assert.AreEqual(false, aResult.First().IsFinal);
            Assert.AreEqual("MySeqId", aResult.First().SequenceId);

            // The fragment with index 5 is not according the order therefore the sequencer returns nothing.
            aFragment = new Fragment("MySeqId", 5, true);
            aResult = myFragmentSequencer.ProcessFragment(aFragment);
            Assert.AreEqual(0, aResult.Count());

            // The fragment with index 3 is not according the order therefore the sequencer returns nothing.
            aFragment = new Fragment("MySeqId", 3, false);
            aResult = myFragmentSequencer.ProcessFragment(aFragment);
            Assert.AreEqual(0, aResult.Count());

            // The fragment with index 2 causes that fragments with index 2, 3 are according to the oreder
            // and the sequencer should return them.
            aFragment = new Fragment("MySeqId", 2, false);
            aResult = myFragmentSequencer.ProcessFragment(aFragment);
            Assert.AreEqual(2, aResult.Count());
            IFragment[] aFragments = aResult.ToArray();
            Assert.AreEqual(2, aFragments[0].Index);
            Assert.AreEqual(false, aFragments[0].IsFinal);
            Assert.AreEqual("MySeqId", aFragments[0].SequenceId);
            Assert.AreEqual(3, aFragments[1].Index);
            Assert.AreEqual(false, aFragments[1].IsFinal);
            Assert.AreEqual("MySeqId", aFragments[1].SequenceId);

            // The fragment with index 4 causes that fragments with index 4, 4 are according to the oreder
            // and the sequencer should return them.
            aFragment = new Fragment("MySeqId", 4, false);
            aResult = myFragmentSequencer.ProcessFragment(aFragment);
            Assert.AreEqual(2, aResult.Count());
            aFragments = aResult.ToArray();
            Assert.AreEqual(4, aFragments[0].Index);
            Assert.AreEqual(false, aFragments[0].IsFinal);
            Assert.AreEqual("MySeqId", aFragments[0].SequenceId);
            Assert.AreEqual(5, aFragments[1].Index);
            Assert.AreEqual(true, aFragments[1].IsFinal);
            Assert.AreEqual("MySeqId", aFragments[1].SequenceId);
        }


        [Test]
        public void ProcessSequence_Multithread()
        {
            Assert.AreEqual("MySeqId", myFragmentSequencer.SequenceId);

            AutoResetEvent aSequenceCompletedSignal = new AutoResetEvent(false);

            List<IFragment> anOrderedSequence = new List<IFragment>();

            Action<int> aDelegate = x =>
            {
                for (int i = 100 * x; i < 100 * x + 100; ++i)
                {
                    Fragment aFragment = new Fragment("MySeqId", i, i == 999);

                    IEnumerable<IFragment> aResult = myFragmentSequencer.ProcessFragment(aFragment);
                    lock (anOrderedSequence)
                    {
                        foreach (IFragment aRetFragment in aResult)
                        {
                            anOrderedSequence.Add(aRetFragment);
                        }
                    }

                    if (myFragmentSequencer.IsWholeSequenceProcessed)
                    {
                        aSequenceCompletedSignal.Set();
                    }
                }
            };

            for (int i = 0; i < 10; ++i)
            {
                aDelegate.BeginInvoke(i, null, null);
            }

            aSequenceCompletedSignal.WaitOne();

            Assert.AreEqual(1000, anOrderedSequence.Count());
            for (int ii = 0; ii < 1000; ++ii)
            {
                Assert.AreEqual(ii, anOrderedSequence[ii].Index);
                Assert.AreEqual(ii == 999, anOrderedSequence[ii].IsFinal);
                Assert.AreEqual("MySeqId", anOrderedSequence[ii].SequenceId);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ProcessSequence_ArgumentException()
        {
            Assert.AreEqual("MySeqId", myFragmentSequencer.SequenceId);

            Fragment aFragment = new Fragment("IncorrectSequenceId", 0, false);
            IEnumerable<IFragment> aResult = myFragmentSequencer.ProcessFragment(aFragment);
        }


        private IFragmentProcessor myFragmentSequencer;
    }
}
