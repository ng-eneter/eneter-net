using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eneter.MessagingUnitTests.EndPoints.TypedMessages
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public class Fake_TypedMessage
    {
        public Fake_TypedMessage()
        {
        }

        public Fake_TypedMessage(string firstName, string secondName)
        {
            FirstName = firstName;
            SecondName = secondName;
        }

        public string FirstName { get; set; }
        public string SecondName { get; set; }
    }
}
