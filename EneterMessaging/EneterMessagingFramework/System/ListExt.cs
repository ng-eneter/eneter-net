// Silverlight3, 4 and WindowsPhone7, 7.1 do not support List extension RemoveAll.
// I have found this implementation on the silverlight forum:
// http://stackoverflow.com/questions/1541777/can-you-remove-an-item-from-a-list-whilst-iterating-through-it-in-c-sharp


#if SILVERLIGHT3 || SILVERLIGHT4 || WINDOWS_PHONE

namespace System.Collections.Generic
{
    internal static class ListExt
    {
        public static int RemoveAll<T>(this List<T> list, Predicate<T> match)
        {
            if (list == null)
                throw new NullReferenceException();

            if (match == null)
                throw new ArgumentNullException("match");

            int i = 0;
            int j = 0;

            for (i = 0; i < list.Count; i++)
            {
                if (!match(list[i]))
                {
                    if (i != j)
                        list[j] = list[i];

                    j++;
                }
            }

            int removed = i - j;
            if (removed > 0)
                list.RemoveRange(list.Count - removed, removed);

            return removed;
        }
    }
}


#endif