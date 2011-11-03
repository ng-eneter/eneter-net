using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencedMessages
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public class Fake_Data
    {
        public Fake_Data()
        {
        }

        public Fake_Data(int number)
        {
            Number = number;
        }

        public int Number { get; set; }
    }
}
