
using System.Reflection;
namespace Eneter.MessagingUnitTests
{
    internal static class TestExtension
    {
        public static T GetProperty<T>(this object obj, string propertyName)
        {
            T aResult = (T)obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj, null);
            return aResult;
        }

        public static T GetField<T>(this object obj, string fieldName)
        {
            T aResult = (T)obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(obj);
            return aResult;
        }
    }
}
