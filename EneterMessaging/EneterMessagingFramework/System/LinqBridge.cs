//
// LINQBridge
// Copyright (c) 2007 Atif Aziz, Joseph Albahari. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the New BSD License, a copy of which should have 
// been delivered along with this distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

// The following is the subset of LinqBridge necessary for Eneter Framework.
// The full functionality of LinqBridge can be downloaded here:
// http://www.albahari.com/nutshell/linqbridge.aspx

#if COMPACT_FRAMEWORK

namespace System.Linq
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    
    static partial class Enumerable
    {
        public static TSource Aggregate<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, TSource, TSource> func)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (func == null) throw new ArgumentNullException("func");

            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    throw new InvalidOperationException();

                return e.Renumerable().Skip(1).Aggregate(e.Current, func);
            }
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(
            this IEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func)
        {
            return Aggregate(source, seed, func, r => r);
        }

        public static TResult Aggregate<TSource, TAccumulate, TResult>(
            this IEnumerable<TSource> source,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            Func<TAccumulate, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (func == null) throw new ArgumentNullException("func");
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");

            var result = seed;

            foreach (var item in source)
                result = func(result, item);

            return resultSelector(result);
        }
        
        private static IEnumerable<T> Renumerable<T>(this IEnumerator<T> e)
        {
            Debug.Assert(e != null);

            do { yield return e.Current; } while (e.MoveNext());
        }
        
        
        public static bool Any<TSource>(
            this IEnumerable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            using (var e = source.GetEnumerator())
                return e.MoveNext();
        }
        
        public static bool Any<TSource>(
            this IEnumerable<TSource> source, 
            Func<TSource, bool> predicate)
        {
            return source.Where(predicate).Any();
        }
        
        
        public static IEnumerable<TSource> Concat<TSource>(
            this IEnumerable<TSource> first,
            IEnumerable<TSource> second)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (second == null) throw new ArgumentNullException("second");

            return ConcatYield(first, second);
        }

        private static IEnumerable<TSource> ConcatYield<TSource>(
            IEnumerable<TSource> first, 
            IEnumerable<TSource> second)
        {
            foreach (var item in first)
                yield return item;

            foreach (var item in second)
                yield return item;
        }
        
        
        public static int Count<TSource>(
            this IEnumerable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            var collection = source as ICollection;
            return collection != null 
                 ? collection.Count 
                 : source.Aggregate(0, (count, item) => checked(count + 1));
        }
        
        public static int Count<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            return Count(source.Where(predicate));
        }
        
        
        public static TSource FirstOrDefault<TSource>(
            this IEnumerable<TSource> source)
        {
            return source.FirstImpl(Futures<TSource>.Default);
        }
        
        public static TSource FirstOrDefault<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            return FirstOrDefault(source.Where(predicate));
        }
        
        private static class Futures<T>
        {
            public static readonly Func<T> Default = () => default(T);
            public static readonly Func<T> Undefined = () => { throw new InvalidOperationException(); };
        }
        
        private static TSource FirstImpl<TSource>(
            this IEnumerable<TSource> source, 
            Func<TSource> empty)
        {
            if (source == null) throw new ArgumentNullException("source");
            Debug.Assert(empty != null);

            var list = source as IList<TSource>;    // optimized case for lists
            if (list != null)
                return list.Count > 0 ? list[0] : empty();

            using (var e = source.GetEnumerator())  // fallback for enumeration
                return e.MoveNext() ? e.Current : empty();
        }
        
        
        public static IEnumerable<TSource> Skip<TSource>(
            this IEnumerable<TSource> source,
            int count)
        {
            return source.SkipWhile((item, i) => i < count);
        }
        
        public static IEnumerable<TSource> SkipWhile<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");

            return SkipWhileYield(source, predicate);
        }
        
        private static IEnumerable<TSource> SkipWhileYield<TSource>(
            IEnumerable<TSource> source, 
            Func<TSource, int, bool> predicate)
        {
            using (var e = source.GetEnumerator())
            {
                for (var i = 0; ; i++) 
                { 
                    if (!e.MoveNext())
                        yield break;
                    
                    if (!predicate(e.Current, i))
                        break;
                }

                do { yield return e.Current; } while (e.MoveNext());
            }
        }
        
        
        
        public static List<TSource> ToList<TSource>(
            this IEnumerable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException("source");

            return new List<TSource>(source);
        }
        
        
        
        public static IEnumerable<TSource> Where<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("predicate");

            return source.Where((item, i) => predicate(item));
        }

        /// <summary>
        /// Filters a sequence of values based on a predicate. 
        /// Each element's index is used in the logic of the predicate function.
        /// </summary>

        public static IEnumerable<TSource> Where<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, int, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (predicate == null) throw new ArgumentNullException("predicate");

            return WhereYield(source, predicate);
        }

        private static IEnumerable<TSource> WhereYield<TSource>(
            IEnumerable<TSource> source, 
            Func<TSource, int, bool> predicate)
        {
            var i = 0;
            foreach (var item in source)
                if (predicate(item, i++))
                    yield return item;
        }
        
        
        
    }
}



namespace System
{
    delegate TResult Func<TResult>();
    delegate TResult Func<T, TResult>(T a);
    delegate TResult Func<T1, T2, TResult>(T1 arg1, T2 arg2);
    //delegate TResult Func<T1, T2, T3, TResult>(T1 arg1, T2 arg2, T3 arg3);
    //delegate TResult Func<T1, T2, T3, T4, TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    
    delegate void Action();
    delegate void Action<T1, T2>(T1 arg1, T2 arg2);
    delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
    //delegate void Action<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
}

namespace System.Runtime.CompilerServices
{
    /// <remarks>
    /// This attribute allows us to define extension methods without 
    /// requiring .NET Framework 3.5. For more information, see the section,
    /// <a href="http://msdn.microsoft.com/en-us/magazine/cc163317.aspx#S7">Extension Methods in .NET Framework 2.0 Apps</a>,
    /// of <a href="http://msdn.microsoft.com/en-us/magazine/cc163317.aspx">Basic Instincts: Extension Methods</a>
    /// column in <a href="http://msdn.microsoft.com/msdnmag/">MSDN Magazine</a>, 
    /// issue <a href="http://msdn.microsoft.com/en-us/magazine/cc135410.aspx">Nov 2007</a>.
    /// </remarks>

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
    sealed partial class ExtensionAttribute : Attribute { }
}

#endif