/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright (c) Ondrej Uzovic 2020
*/

using System;
using System.Collections.Generic;

namespace Eneter.Messaging.Utils.Collections
{
    internal static class ListExt
    {
        public static int RemoveWhere<T>(this IList<T> list,  Predicate<T> match)
        {
            int count = 0;
            for (int i = list.Count - 1; i >= 0; --i)
            {
                T item = list[i];
                if (match(item))
                {
                    list.RemoveAt(i);
                    ++count;
                }
            }

            return count;
        }
    }
}
