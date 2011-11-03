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
    public class Test_FragmentFinalizer
    {
        [SetUp]
        public void Setup()
        {
            FragmentDataFactory aFragmentDataFactory = new FragmentDataFactory();
            myFragmentFinalizer = aFragmentDataFactory.CreateSequenceFinalizer("MySeqId");
        }

        [Test]
        public void ProcessSequence()
        {
            Assert.AreEqual("MySeqId", myFragmentFinalizer.SequenceId);

            for (int i = 0; i < 100; ++i)
            {
                Fragment aFragment1 = new Fragment("MySeqId", i, i == 99);
                IEnumerable<IFragment> aResult = myFragmentFinalizer.ProcessFragment(aFragment1);

                if (i == 99)
                {
                    Assert.AreEqual(100, aResult.Count());

                    IFragment[] aFragments = aResult.ToArray();
                    for (int ii = 0; ii < 100; ++ii)
                    {
                        Assert.AreEqual(ii, aFragments[ii].Index);
                        Assert.AreEqual(ii == 99, aFragments[ii].IsFinal);
                        Assert.AreEqual("MySeqId", aFragments[ii].SequenceId);
                    }
                }
                else
                {
                    Assert.AreEqual(0, aResult.Count());
                }
            }
        }

        [Test]
        public void ProcessSequence_ComesNotOrdered()
        {
            Assert.AreEqual("MySeqId", myFragmentFinalizer.SequenceId);

            Fragment aFragment = new Fragment("MySeqId", 0, false);
            IEnumerable<IFragment> aResult = myFragmentFinalizer.ProcessFragment(aFragment);
            Assert.AreEqual(0, aResult.Count());

            aFragment = new Fragment("MySeqId", 1, false);
            aResult = myFragmentFinalizer.ProcessFragment(aFragment);
            Assert.AreEqual(0, aResult.Count());

            aFragment = new Fragment("MySeqId", 5, true);
            aResult = myFragmentFinalizer.ProcessFragment(aFragment);
            Assert.AreEqual(0, aResult.Count());

            aFragment = new Fragment("MySeqId", 3, false);
            aResult = myFragmentFinalizer.ProcessFragment(aFragment);
            Assert.AreEqual(0, aResult.Count());

            aFragment = new Fragment("MySeqId", 2, false);
            aResult = myFragmentFinalizer.ProcessFragment(aFragment);
            Assert.AreEqual(0, aResult.Count());

            aFragment = new Fragment("MySeqId", 4, false);
            aResult = myFragmentFinalizer.ProcessFragment(aFragment);
            IFragment[] aFragments = aResult.ToArray();
            Assert.AreEqual(6, aFragments.Count());
            for (int ii = 0; ii < 6; ++ii)
            {
                Assert.AreEqual(ii, aFragments[ii].Index);
                Assert.AreEqual(ii == 5, aFragments[ii].IsFinal);
                Assert.AreEqual("MySeqId", aFragments[ii].SequenceId);
            }
        }


        [Test]
        public void ProcessSequence_Multithread()
        {
            Assert.AreEqual("MySeqId", myFragmentFinalizer.SequenceId);

            AutoResetEvent aSequenceCompletedSignal = new AutoResetEvent(false);

            IFragment[] aFragments = null;

            Action<int> aDelegate = x =>
                {
                    for (int i = 100 * x; i < 100 * x + 100; ++i)
                    {
                        Fragment aFragment = new Fragment("MySeqId", i, i == 999);

                        IEnumerable<IFragment> aResult = myFragmentFinalizer.ProcessFragment(aFragment);

                        if (myFragmentFinalizer.IsWholeSequenceProcessed)
                        {
                            aFragments = aResult.ToArray();
                            aSequenceCompletedSignal.Set();
                        }
                    }
                };

            for (int i = 0; i < 10; ++i)
            {
                aDelegate.BeginInvoke(i, null, null);
            }

            aSequenceCompletedSignal.WaitOne();

            Assert.AreEqual(1000, aFragments.Count());
            for (int ii = 0; ii < 1000; ++ii)
            {
                Assert.AreEqual(ii, aFragments[ii].Index);
                Assert.AreEqual(ii == 999, aFragments[ii].IsFinal);
                Assert.AreEqual("MySeqId", aFragments[ii].SequenceId);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ProcessSequence_ArgumentException()
        {
            Assert.AreEqual("MySeqId", myFragmentFinalizer.SequenceId);

            Fragment aFragment = new Fragment("IncorrectSequenceId", 0, false);
            IEnumerable<IFragment> aResult = myFragmentFinalizer.ProcessFragment(aFragment);
        }


        private IFragmentProcessor myFragmentFinalizer;
    }
}
