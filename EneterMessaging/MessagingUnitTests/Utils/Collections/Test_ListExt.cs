using Eneter.Messaging.Utils.Collections;
using NUnit.Framework;
using System.Collections.Generic;

namespace Eneter.MessagingUnitTests.Utils.Collections
{
    [TestFixture]
    public class Test_ListExt
    {
        [Test]
        public void RemoveWhere()
        {
            List<int> list = new List<int>() { 1, 2, 3, 4, 5 };
            int count = list.RemoveWhere(x => x < 4);

            Assert.AreEqual(3, count);
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(4, list[0]);
            Assert.AreEqual(5, list[1]);
        }
    }
}
