

// Silverlight3 does not support HashSet<_T> !!!!
// So I have found this implementation on the silverlight forum:
// http://forums.silverlight.net/forums/t/124538.aspx

#if SILVERLIGHT3 || WINDOWS_PHONE

namespace System.Collections.Generic
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a set of values.
    /// </summary>
    /// <remarks>
    /// (This is a stripped down Silverlight port of the full .NET CLR's <see cref="T:System.Collections.Generic.HashSet`1" /> class.
    /// The API is feature complete and compatible
    /// except for serialization implementations which are not supported by Silverlight.
    /// Some private methods called in operations with other <see cref="T:System.Collections.Generic.IEnumerable`1" />s
    /// are not implemented by taking advantage of some unsafe bit-level optimization code
    /// in certain constellations, but otherwise should work the same.
    /// This implementation is copied using the Reflector and thus the copyright is fully at Microsoft.
    /// It will remain here until the HashSet is included in a future version of the Silverlight base class library.)
    /// </remarks>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    [DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(HashSetDebugView<>))]
    internal class HashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable
    {
        private const string CapacityName = "Capacity";
        private const string ComparerName = "Comparer";
        private const string ElementsName = "Elements";
        private const int GrowthFactor = 2;
        private const int Lower31BitMask = 0x7fffffff;
        private int[] m_buckets;
        private IEqualityComparer<T> m_comparer;
        private int m_count;
        private int m_freeList;
        private int m_lastIndex;
        private Slot[] m_slots;
        private int m_version;
        private const int ShrinkThreshold = 3;
        private const int StackAllocThreshold = 100;
        private const string VersionName = "Version";

#if NEVER
        private SerializationInfo m_siInfo;
#endif

        /// <summary>
        /// Gets the <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> object that is used to determine equality for the values in the set.
        /// </summary>
        public IEqualityComparer<T> Comparer
        {
            get { return this.m_comparer; }
        }

        /// <summary>
        /// Gets the number of elements that are contained in a set.
        /// </summary>
        public int Count
        {
            get { return this.m_count; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class that is empty and uses the default equality comparer for the set type.
        /// </summary>
        public HashSet() : this(EqualityComparer<T>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class that uses the default equality comparer for the set type,
        /// contains elements copied from the specified collection,
        /// and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new set.</param>
        public HashSet(IEnumerable<T> collection) : this(collection, EqualityComparer<T>.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class that is empty and uses the specified equality comparer for the set type.
        /// </summary>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> implementation to use when comparing values in the set, or null to use the default <see cref="T:System.Collections.Generic.EqualityComparer`1" /> implementation for the set type.</param>
        public HashSet(IEqualityComparer<T> comparer)
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<T>.Default;
            }

            this.m_comparer = comparer;
            this.m_lastIndex = 0;
            this.m_count = 0;
            this.m_freeList = -1;
            this.m_version = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.HashSet`1" /> class that uses the specified equality comparer for the set type,
        /// contains elements copied from the specified collection,
        /// and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new set.</param>
        /// <param name="comparer">The <see cref="T:System.Collections.Generic.IEqualityComparer`1" /> implementation to use when comparing values in the set, or null to use the default <see cref="T:System.Collections.Generic.EqualityComparer`1" /> implementation for the set type.</param>
        /// <exception cref="T:System.ArgumentNullException">collection is null.</exception>
        public HashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
            : this(comparer)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            int capacity = 0;
            ICollection<T> is2 = collection as ICollection<T>;
            if (is2 != null)
            {
                capacity = is2.Count;
            }
            this.Initialize(capacity);
            this.UnionWith(collection);
            if (((this.m_count == 0) && (this.m_slots.Length > HashHelpers.GetMinPrime())) || ((this.m_count > 0) && ((this.m_slots.Length / this.m_count) > 3)))
            {
                this.TrimExcess();
            }
        }

#if NEVER
        protected HashSet(SerializationInfo info, StreamingContext context)
        {
            this.m_siInfo = info;
        }
#endif

        /// <summary>
        /// Adds the specified element to a set.
        /// </summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="T:System.Collections.Generic.HashSet`1" /> object; false if the element is already present.</returns>
        public bool Add(T item)
        {
            return this.AddIfNotPresent(item);
        }

        private bool AddIfNotPresent(T value)
        {
            int freeList;
            if (this.m_buckets == null)
            {
                this.Initialize(0);
            }
            int hashCode = this.InternalGetHashCode(value);
            int index = hashCode % this.m_buckets.Length;
            for (int i = this.m_buckets[hashCode % this.m_buckets.Length] - 1; i >= 0; i = this.m_slots[i].next)
            {
                if ((this.m_slots[i].hashCode == hashCode) && this.m_comparer.Equals(this.m_slots[i].value, value))
                {
                    return false;
                }
            }
            if (this.m_freeList >= 0)
            {
                freeList = this.m_freeList;
                this.m_freeList = this.m_slots[freeList].next;
            }
            else
            {
                if (this.m_lastIndex == this.m_slots.Length)
                {
                    this.IncreaseCapacity();
                    index = hashCode % this.m_buckets.Length;
                }
                freeList = this.m_lastIndex;
                this.m_lastIndex++;
            }
            this.m_slots[freeList].hashCode = hashCode;
            this.m_slots[freeList].value = value;
            this.m_slots[freeList].next = this.m_buckets[index] - 1;
            this.m_buckets[index] = freeList + 1;
            this.m_count++;
            this.m_version++;
            return true;
        }

        private bool AddOrGetLocation(T value, out int location)
        {
            int freeList;
            int hashCode = this.InternalGetHashCode(value);
            int index = hashCode % this.m_buckets.Length;
            for (int i = this.m_buckets[hashCode % this.m_buckets.Length] - 1; i >= 0; i = this.m_slots[i].next)
            {
                if ((this.m_slots[i].hashCode == hashCode) && this.m_comparer.Equals(this.m_slots[i].value, value))
                {
                    location = i;
                    return false;
                }
            }
            if (this.m_freeList >= 0)
            {
                freeList = this.m_freeList;
                this.m_freeList = this.m_slots[freeList].next;
            }
            else
            {
                if (this.m_lastIndex == this.m_slots.Length)
                {
                    this.IncreaseCapacity();
                    index = hashCode % this.m_buckets.Length;
                }
                freeList = this.m_lastIndex;
                this.m_lastIndex++;
            }
            this.m_slots[freeList].hashCode = hashCode;
            this.m_slots[freeList].value = value;
            this.m_slots[freeList].next = this.m_buckets[index] - 1;
            this.m_buckets[index] = freeList + 1;
            this.m_count++;
            this.m_version++;
            location = freeList;
            return true;
        }

        private static bool AreEqualityComparersEqual(HashSet<T> set1, HashSet<T> set2)
        {
            return set1.Comparer.Equals(set2.Comparer);
        }

        private ElementCount CheckUniqueAndUnfoundElements(IEnumerable<T> other, bool returnIfUnfound)
        {
            ElementCount count;
            if (this.m_count != 0)
            {
                BitHelper helper;
                int length = BitHelper.ToIntArrayLength(this.m_lastIndex);

#if NEVER
                if (length <= 100)
                {
                    int* bitArrayPtr = stackalloc int[length];
                    helper = new BitHelper(bitArrayPtr, length);
                }
                else
#endif
                {
                    int[] bitArray = new int[length];
                    helper = new BitHelper(bitArray, length);
                }
                int num4 = 0;
                int num5 = 0;
                foreach (T local in other)
                {
                    int bitPosition = this.InternalIndexOf(local);
                    if (bitPosition >= 0)
                    {
                        if (!helper.IsMarked(bitPosition))
                        {
                            helper.MarkBit(bitPosition);
                            num5++;
                        }
                    }
                    else
                    {
                        num4++;
                        if (returnIfUnfound)
                        {
                            break;
                        }
                    }
                }
                count.uniqueCount = num5;
                count.unfoundCount = num4;
                return count;
            }
            int num = 0;
            using (IEnumerator<T> enumerator = other.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    T current = enumerator.Current;
                    num++;
                    goto Label_0039;
                }
            }
        Label_0039:
            count.uniqueCount = 0;
            count.unfoundCount = num;
            return count;
        }

        /// <summary>
        /// Removes all elements from a <see cref="T:System.Collections.Generic.HashSet`1" /> object.
        /// </summary>
        public void Clear()
        {
            if (this.m_lastIndex > 0)
            {
                Array.Clear(this.m_slots, 0, this.m_lastIndex);
                Array.Clear(this.m_buckets, 0, this.m_buckets.Length);
                this.m_lastIndex = 0;
                this.m_count = 0;
                this.m_freeList = -1;
            }
            this.m_version++;
        }

        /// <summary>
        /// Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object contains the specified element.
        /// </summary>
        /// <param name="item">The element to locate in the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object contains the specified element; otherwise, false.</returns>
        public bool Contains(T item)
        {
            if (this.m_buckets != null)
            {
                int hashCode = this.InternalGetHashCode(item);
                for (int i = this.m_buckets[hashCode % this.m_buckets.Length] - 1; i >= 0; i = this.m_slots[i].next)
                {
                    if ((this.m_slots[i].hashCode == hashCode) && this.m_comparer.Equals(this.m_slots[i].value, item))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool ContainsAllElements(IEnumerable<T> other)
        {
            foreach (T local in other)
            {
                if (!this.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Copies the elements of a <see cref="T:System.Collections.Generic.HashSet`1" /> object to an array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the <see cref="T:System.Collections.Generic.HashSet`1" /> object. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="count">The number of elements to copy to array.</param>
        /// <exception cref="T:System.ArgumentNullException">array is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">arrayIndex is less than 0 -or- count is less than 0.</exception>
        /// <exception cref="T:System.ArgumentException">arrayIndex is greater than the length of the destination array -or- count is greater than the available space from the index to the end of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException("arrayIndex", SR.GetString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", SR.GetString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if ((arrayIndex > array.Length) || (count > (array.Length - arrayIndex)))
            {
                throw new ArgumentException(SR.GetString("Arg_ArrayPlusOffTooSmall"));
            }
            int num = 0;
            for (int i = 0; (i < this.m_lastIndex) && (num < count); i++)
            {
                if (this.m_slots[i].hashCode >= 0)
                {
                    array[arrayIndex + num] = this.m_slots[i].value;
                    num++;
                }
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.CopyTo(array, arrayIndex, this.m_count);
        }

        public void CopyTo(T[] array)
        {
            this.CopyTo(array, 0, this.m_count);
        }

        public static IEqualityComparer<HashSet<T>> CreateSetComparer()
        {
            return new HashSetEqualityComparer<T>();
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current <see cref="T:System.Collections.Generic.HashSet`1" /> object.
        /// </summary>
        /// <param name="other">The collection of items to remove from the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <exception cref="T:System.ArgumentNullException">other is null.</exception>
        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.m_count != 0)
            {
                if (other == this)
                {
                    this.Clear();
                }
                else
                {
                    foreach (T local in other)
                    {
                        this.Remove(local);
                    }
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a <see cref="T:System.Collections.Generic.HashSet`1" /> object.
        /// </summary>
        /// <returns>A Enumerator object for the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator((HashSet<T>)this);
        }

#if NEVER
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            info.AddValue("Version", this.m_version);
            info.AddValue("Comparer", this.m_comparer, typeof(IEqualityComparer<T>));
            info.AddValue("Capacity", (this.m_buckets == null) ? 0 : this.m_buckets.Length);
            if (this.m_buckets != null)
            {
                T[] array = new T[this.m_count];
                this.CopyTo(array);
                info.AddValue("Elements", array, typeof(T[]));
            }
        }
#endif

        internal static bool HashSetEquals(HashSet<T> set1, HashSet<T> set2, IEqualityComparer<T> comparer)
        {
            if (set1 == null)
            {
                return (set2 == null);
            }
            if (set2 == null)
            {
                return false;
            }
            if (HashSet<T>.AreEqualityComparersEqual(set1, set2))
            {
                if (set1.Count != set2.Count)
                {
                    return false;
                }
                foreach (T local in set2)
                {
                    if (!set1.Contains(local))
                    {
                        return false;
                    }
                }
                return true;
            }
            foreach (T local2 in set2)
            {
                bool flag = false;
                foreach (T local3 in set1)
                {
                    if (comparer.Equals(local2, local3))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    return false;
                }
            }
            return true;
        }

        private void IncreaseCapacity()
        {
            int min = this.m_count * 2;
            if (min < 0)
            {
                min = this.m_count;
            }
            int prime = HashHelpers.GetPrime(min);
            if (prime <= this.m_count)
            {
                throw new ArgumentException(SR.GetString("Arg_HSCapacityOverflow"));
            }
            Slot[] destinationArray = new Slot[prime];
            if (this.m_slots != null)
            {
                Array.Copy(this.m_slots, 0, destinationArray, 0, this.m_lastIndex);
            }
            int[] numArray = new int[prime];
            for (int i = 0; i < this.m_lastIndex; i++)
            {
                int index = destinationArray[i].hashCode % prime;
                destinationArray[i].next = numArray[index] - 1;
                numArray[index] = i + 1;
            }
            this.m_slots = destinationArray;
            this.m_buckets = numArray;
        }

        private void Initialize(int capacity)
        {
            int prime = HashHelpers.GetPrime(capacity);
            this.m_buckets = new int[prime];
            this.m_slots = new Slot[prime];
        }

        private int InternalGetHashCode(T item)
        {
            if (item == null)
            {
                return 0;
            }
            return (this.m_comparer.GetHashCode(item) & 0x7fffffff);
        }

        private int InternalIndexOf(T item)
        {
            int hashCode = this.InternalGetHashCode(item);
            for (int i = this.m_buckets[hashCode % this.m_buckets.Length] - 1; i >= 0; i = this.m_slots[i].next)
            {
                if ((this.m_slots[i].hashCode == hashCode) && this.m_comparer.Equals(this.m_slots[i].value, item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Modifies the current <see cref="T:System.Collections.Generic.HashSet`1" /> object to contain only elements that are present in that object and in the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <exception cref="T:System.ArgumentNullException">other is null.</exception>
        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.m_count != 0)
            {
                ICollection<T> is2 = other as ICollection<T>;
                if (is2 != null)
                {
                    if (is2.Count == 0)
                    {
                        this.Clear();
                        return;
                    }
                    HashSet<T> set = other as HashSet<T>;
                    if ((set != null) && HashSet<T>.AreEqualityComparersEqual((HashSet<T>)this, set))
                    {
                        this.IntersectWithHashSetWithSameEC(set);
                        return;
                    }
                }
                this.IntersectWithEnumerable(other);
            }
        }

        private void IntersectWithEnumerable(IEnumerable<T> other)
        {
            BitHelper helper;
            int lastIndex = this.m_lastIndex;
            int length = BitHelper.ToIntArrayLength(lastIndex);
#if NEVER
            if (length <= 100)
            {
                int* bitArrayPtr = stackalloc int[length];
                helper = new BitHelper(bitArrayPtr, length);
            }
            else
#endif
            {
                int[] bitArray = new int[length];
                helper = new BitHelper(bitArray, length);
            }
            foreach (T local in other)
            {
                int bitPosition = this.InternalIndexOf(local);
                if (bitPosition >= 0)
                {
                    helper.MarkBit(bitPosition);
                }
            }
            for (int i = 0; i < lastIndex; i++)
            {
                if ((this.m_slots[i].hashCode >= 0) && !helper.IsMarked(i))
                {
                    this.Remove(this.m_slots[i].value);
                }
            }
        }

        private void IntersectWithHashSetWithSameEC(HashSet<T> other)
        {
            for (int i = 0; i < this.m_lastIndex; i++)
            {
                if (this.m_slots[i].hashCode >= 0)
                {
                    T item = this.m_slots[i].value;
                    if (!other.Contains(item))
                    {
                        this.Remove(item);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object is a proper subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object is a proper subset of other; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException">other is null.</exception>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            ICollection<T> is2 = other as ICollection<T>;
            if (is2 != null)
            {
                if (this.m_count == 0)
                {
                    return (is2.Count > 0);
                }
                HashSet<T> set = other as HashSet<T>;
                if ((set != null) && HashSet<T>.AreEqualityComparersEqual((HashSet<T>)this, set))
                {
                    if (this.m_count >= set.Count)
                    {
                        return false;
                    }
                    return this.IsSubsetOfHashSetWithSameEC(set);
                }
            }
            ElementCount count = this.CheckUniqueAndUnfoundElements(other, false);
            return ((count.uniqueCount == this.m_count) && (count.unfoundCount > 0));
        }

        /// <summary>
        /// Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object is a proper superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object is a proper superset of other; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException">other is null.</exception>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.m_count == 0)
            {
                return false;
            }
            ICollection<T> is2 = other as ICollection<T>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                HashSet<T> set = other as HashSet<T>;
                if ((set != null) && HashSet<T>.AreEqualityComparersEqual((HashSet<T>)this, set))
                {
                    if (set.Count >= this.m_count)
                    {
                        return false;
                    }
                    return this.ContainsAllElements(set);
                }
            }
            ElementCount count = this.CheckUniqueAndUnfoundElements(other, true);
            return ((count.uniqueCount < this.m_count) && (count.unfoundCount == 0));
        }

        /// <summary>
        /// Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object is a subset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object is a subset of other; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException">other is null.</exception>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.m_count == 0)
            {
                return true;
            }
            HashSet<T> set = other as HashSet<T>;
            if ((set != null) && HashSet<T>.AreEqualityComparersEqual((HashSet<T>)this, set))
            {
                if (this.m_count > set.Count)
                {
                    return false;
                }
                return this.IsSubsetOfHashSetWithSameEC(set);
            }
            ElementCount count = this.CheckUniqueAndUnfoundElements(other, false);
            return ((count.uniqueCount == this.m_count) && (count.unfoundCount >= 0));
        }

        private bool IsSubsetOfHashSetWithSameEC(HashSet<T> other)
        {
            foreach (T local in this)
            {
                if (!other.Contains(local))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether a <see cref="T:System.Collections.Generic.HashSet`1" /> object is a superset of the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object is a superset of other; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException">other is null.</exception>

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            ICollection<T> is2 = other as ICollection<T>;
            if (is2 != null)
            {
                if (is2.Count == 0)
                {
                    return true;
                }
                HashSet<T> set = other as HashSet<T>;
                if (((set != null) && HashSet<T>.AreEqualityComparersEqual((HashSet<T>)this, set)) && (set.Count > this.m_count))
                {
                    return false;
                }
            }
            return this.ContainsAllElements(other);
        }

#if NEVER
        public virtual void OnDeserialization(object sender)
        {
            if (this.m_siInfo != null)
            {
                int num = this.m_siInfo.GetInt32("Capacity");
                this.m_comparer = (IEqualityComparer<T>) this.m_siInfo.GetValue("Comparer", typeof(IEqualityComparer<T>));
                this.m_freeList = -1;
                if (num != 0)
                {
                    this.m_buckets = new int[num];
                    this.m_slots = new Slot<T>[num];
                    T[] localArray = (T[]) this.m_siInfo.GetValue("Elements", typeof(T[]));
                    if (localArray == null)
                    {
                        throw new SerializationException(SR.GetString("Serialization_MissingKeys"));
                    }
                    for (int i = 0; i < localArray.Length; i++)
                    {
                        this.AddIfNotPresent(localArray[ i ]);
                    }
                }
                else
                {
                    this.m_buckets = null;
                }
                this.m_version = this.m_siInfo.GetInt32("Version");
                this.m_siInfo = null;
            }
        }
#endif

        /// <summary>
        /// Determines whether the current <see cref="T:System.Collections.Generic.HashSet`1" /> object overlaps the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <returns>true if the <see cref="T:System.Collections.Generic.HashSet`1" /> object and other share at least one common element; otherwise, false.</returns>
        /// <exception cref="T:System.ArgumentNullException">other is null.</exception>
        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.m_count != 0)
            {
                foreach (T local in other)
                {
                    if (this.Contains(local))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Removes the specified element from a <see cref="T:System.Collections.Generic.HashSet`1" /> object.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>true if the element is successfully found and removed; otherwise, false. This method returns false if item is not found in the <see cref="T:System.Collections.Generic.HashSet`1" /> object.</returns>
        public bool Remove(T item)
        {
            if (this.m_buckets != null)
            {
                int hashCode = this.InternalGetHashCode(item);
                int index = hashCode % this.m_buckets.Length;
                int num3 = -1;
                for (int i = this.m_buckets[index] - 1; i >= 0; i = this.m_slots[i].next)
                {
                    if ((this.m_slots[i].hashCode == hashCode) && this.m_comparer.Equals(this.m_slots[i].value, item))
                    {
                        if (num3 < 0)
                        {
                            this.m_buckets[index] = this.m_slots[i].next + 1;
                        }
                        else
                        {
                            this.m_slots[num3].next = this.m_slots[i].next;
                        }
                        this.m_slots[i].hashCode = -1;
                        this.m_slots[i].value = default(T);
                        this.m_slots[i].next = this.m_freeList;
                        this.m_freeList = i;
                        this.m_count--;
                        this.m_version++;
                        return true;
                    }
                    num3 = i;
                }
            }
            return false;
        }

        /// <summary>
        /// Removes all elements that match the conditions defined by the specified predicate from a <see cref="T:System.Collections.Generic.HashSet`1" /> collection.
        /// </summary>
        /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the elements to remove.</param>
        /// <returns>The number of elements that were removed from the <see cref="T:System.Collections.Generic.HashSet`1" /> collection.</returns>
        /// <exception cref="T:System.ArgumentNullException">match is null.</exception>
        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            int num = 0;
            for (int i = 0; i < this.m_lastIndex; i++)
            {
                if (this.m_slots[i].hashCode >= 0)
                {
                    T local = this.m_slots[i].value;
                    if (match(local) && this.Remove(local))
                    {
                        num++;
                    }
                }
            }
            return num;
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            HashSet<T> set = other as HashSet<T>;
            if ((set != null) && HashSet<T>.AreEqualityComparersEqual((HashSet<T>)this, set))
            {
                if (this.m_count != set.Count)
                {
                    return false;
                }
                return this.ContainsAllElements(set);
            }
            ICollection<T> is2 = other as ICollection<T>;
            if (((is2 != null) && (this.m_count == 0)) && (is2.Count > 0))
            {
                return false;
            }
            ElementCount count = this.CheckUniqueAndUnfoundElements(other, true);
            return ((count.uniqueCount == this.m_count) && (count.unfoundCount == 0));
        }

        /// <summary>
        /// Modifies the current <see cref="T:System.Collections.Generic.HashSet`1" /> object to contain only elements that are present either in that object or in the specified collection, but not both.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <exception cref="T:System.ArgumentNullException">other is null.</exception>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            if (this.m_count == 0)
            {
                this.UnionWith(other);
            }
            else if (other == this)
            {
                this.Clear();
            }
            else
            {
                HashSet<T> set = other as HashSet<T>;
                if ((set != null) && HashSet<T>.AreEqualityComparersEqual((HashSet<T>)this, set))
                {
                    this.SymmetricExceptWithUniqueHashSet(set);
                }
                else
                {
                    this.SymmetricExceptWithEnumerable(other);
                }
            }
        }

        private void SymmetricExceptWithEnumerable(IEnumerable<T> other)
        {
            BitHelper helper;
            BitHelper helper2;
            int lastIndex = this.m_lastIndex;
            int length = BitHelper.ToIntArrayLength(lastIndex);
#if NEVER
            if (length <= 50)
            {
                int* bitArrayPtr = stackalloc int[length];
                helper = new BitHelper(bitArrayPtr, length);
                int* numPtr2 = stackalloc int[length];
                helper2 = new BitHelper(numPtr2, length);
            }
            else
#endif
            {
                int[] bitArray = new int[length];
                helper = new BitHelper(bitArray, length);
                int[] numArray2 = new int[length];
                helper2 = new BitHelper(numArray2, length);
            }
            foreach (T local in other)
            {
                int location = 0;
                if (this.AddOrGetLocation(local, out location))
                {
                    helper2.MarkBit(location);
                }
                else if ((location < lastIndex) && !helper2.IsMarked(location))
                {
                    helper.MarkBit(location);
                }
            }
            for (int i = 0; i < lastIndex; i++)
            {
                if (helper.IsMarked(i))
                {
                    this.Remove(this.m_slots[i].value);
                }
            }
        }

        private void SymmetricExceptWithUniqueHashSet(HashSet<T> other)
        {
            foreach (T local in other)
            {
                if (!this.Remove(local))
                {
                    this.AddIfNotPresent(local);
                }
            }
        }

        void ICollection<T>.Add(T item)
        {
            this.AddIfNotPresent(item);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator((HashSet<T>)this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator((HashSet<T>)this);
        }

        internal T[] ToArray()
        {
            T[] array = new T[this.Count];
            this.CopyTo(array);
            return array;
        }

        public void TrimExcess()
        {
            if (this.m_count == 0)
            {
                this.m_buckets = null;
                this.m_slots = null;
                this.m_version++;
            }
            else
            {
                int prime = HashHelpers.GetPrime(this.m_count);
                Slot[] slotArray = new Slot[prime];
                int[] numArray = new int[prime];
                int index = 0;
                for (int i = 0; i < this.m_lastIndex; i++)
                {
                    if (this.m_slots[i].hashCode >= 0)
                    {
                        slotArray[index] = this.m_slots[i];
                        int num4 = slotArray[index].hashCode % prime;
                        slotArray[index].next = numArray[num4] - 1;
                        numArray[num4] = index + 1;
                        index++;
                    }
                }
                this.m_lastIndex = index;
                this.m_slots = slotArray;
                this.m_buckets = numArray;
                this.m_freeList = -1;
            }
        }

        /// <summary>
        /// Modifies the current <see cref="T:System.Collections.Generic.HashSet`1" /> object to contain all elements that are present in both itself and in the specified collection.
        /// </summary>
        /// <param name="other">The collection to compare to the current <see cref="T:System.Collections.Generic.HashSet`1" /> object.</param>
        /// <exception cref="T:System.ArgumentNullException">other is null.</exception>
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }
            foreach (T local in other)
            {
                this.AddIfNotPresent(local);
            }
        }

        // Nested Types
        [StructLayout(LayoutKind.Sequential)]
        internal struct ElementCount
        {
            internal int uniqueCount;
            internal int unfoundCount;
        }

        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private HashSet<T> set;
            private int index;
            private int version;
            private T current;
            internal Enumerator(HashSet<T> set)
            {
                this.set = set;
                this.index = 0;
                this.version = set.m_version;
                this.current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (this.version != this.set.m_version)
                {
                    throw new InvalidOperationException(SR.GetString("InvalidOperation_EnumFailedVersion"));
                }
                while (this.index < this.set.m_lastIndex)
                {
                    if (this.set.m_slots[this.index].hashCode >= 0)
                    {
                        this.current = this.set.m_slots[this.index].value;
                        this.index++;
                        return true;
                    }
                    this.index++;
                }
                this.index = this.set.m_lastIndex + 1;
                this.current = default(T);
                return false;
            }

            public T Current
            {
                get
                {
                    return this.current;
                }
            }
            object IEnumerator.Current
            {
                get
                {
                    if ((this.index == 0) || (this.index == (this.set.m_lastIndex + 1)))
                    {
                        throw new InvalidOperationException(SR.GetString("InvalidOperation_EnumOpCantHappen"));
                    }
                    return this.Current;
                }
            }
            void IEnumerator.Reset()
            {
                if (this.version != this.set.m_version)
                {
                    throw new InvalidOperationException(SR.GetString("InvalidOperation_EnumFailedVersion"));
                }
                this.index = 0;
                this.current = default(T);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Slot
        {
            internal int hashCode;
            internal T value;
            internal int next;
        }

        internal static class SR
        {
            static internal string GetString(string s)
            {
                return s;
            }
        }
    }

    internal class BitHelper
    {
        // Fields
        private const byte IntSize = 0x20;
        private int[] m_array;
        private int m_length;
        private const byte MarkedBitFlag = 1;

#if NEVER
        private BitArray m_arrayPtr;
        private bool useStackAlloc;

        internal BitHelper(BitArray bitArrayPtr, int length)
        {
            this.m_arrayPtr = bitArrayPtr;
            this.m_length = length;
            this.useStackAlloc = true;
        }
#endif

        internal BitHelper(int[] bitArray, int length)
        {
            this.m_array = bitArray;
            this.m_length = length;
        }

        internal bool IsMarked(int bitPosition)
        {
#if NEVER
            if (this.useStackAlloc)
            {
                int num = bitPosition / 0x20;
                return (((num < this.m_length) && (num >= 0)) && (((this.m_arrayPtr[num] ? 1 : 0) & (((int)1) << (bitPosition % 0x20))) != 0));
            }
#endif
            int index = bitPosition / 0x20;
            return (((index < this.m_length) && (index >= 0)) && ((this.m_array[index] & (((int)1) << (bitPosition % 0x20))) != 0));
        }

        internal void MarkBit(int bitPosition)
        {
#if NEVER
            if (this.useStackAlloc)
            {
                int num = bitPosition / 0x20;
                if ((num < this.m_length) && (num >= 0))
                {
                    int* numPtr1 = this.m_arrayPtr + num;
                    numPtr1[0] |= ((int)1) << (bitPosition % 0x20);
                }
            }
            else
#endif
            {
                int index = bitPosition / 0x20;
                if ((index < this.m_length) && (index >= 0))
                {
                    this.m_array[index] |= ((int)1) << (bitPosition % 0x20);
                }
            }
        }

        internal static int ToIntArrayLength(int n)
        {
            if (n <= 0)
            {
                return 0;
            }
            return (((n - 1) / 0x20) + 1);
        }
    }

    internal static class HashHelpers
    {
        // Fields
        internal static readonly int[] primes = new int[] { 
            3, 7, 11, 0x11, 0x17, 0x1d, 0x25, 0x2f, 0x3b, 0x47, 0x59, 0x6b, 0x83, 0xa3, 0xc5, 0xef, 
            0x125, 0x161, 0x1af, 0x209, 0x277, 0x2f9, 0x397, 0x44f, 0x52f, 0x63d, 0x78b, 0x91d, 0xaf1, 0xd2b, 0xfd1, 0x12fd, 
            0x16cf, 0x1b65, 0x20e3, 0x2777, 0x2f6f, 0x38ff, 0x446f, 0x521f, 0x628d, 0x7655, 0x8e01, 0xaa6b, 0xcc89, 0xf583, 0x126a7, 0x1619b, 
            0x1a857, 0x1fd3b, 0x26315, 0x2dd67, 0x3701b, 0x42023, 0x4f361, 0x5f0ed, 0x72125, 0x88e31, 0xa443b, 0xc51eb, 0xec8c1, 0x11bdbf, 0x154a3f, 0x198c4f, 
            0x1ea867, 0x24ca19, 0x2c25c1, 0x34fa1b, 0x3f928f, 0x4c4987, 0x5b8b6f, 0x6dda89
         };

        // Methods
        internal static int GetMinPrime()
        {
            return primes[0];
        }

        internal static int GetPrime(int min)
        {
            for (int i = 0; i < primes.Length; i++)
            {
                int num2 = primes[i];
                if (num2 >= min)
                {
                    return num2;
                }
            }
            for (int j = min | 1; j < 0x7fffffff; j += 2)
            {
                if (IsPrime(j))
                {
                    return j;
                }
            }
            return min;
        }

        internal static bool IsPrime(int candidate)
        {
            if ((candidate & 1) == 0)
            {
                return (candidate == 2);
            }
            int num = (int)Math.Sqrt((double)candidate);
            for (int i = 3; i <= num; i += 2)
            {
                if ((candidate % i) == 0)
                {
                    return false;
                }
            }
            return true;
        }
    }

    internal class HashSetDebugView<T>
    {
        // Fields
        private HashSet<T> set;

        // Methods
        public HashSetDebugView(HashSet<T> set)
        {
            if (set == null)
            {
                throw new ArgumentNullException("set");
            }
            this.set = set;
        }

        // Properties
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                return this.set.ToArray();
            }
        }
    }

    internal class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
    {
        // Fields
        private IEqualityComparer<T> m_comparer;

        // Methods
        public HashSetEqualityComparer()
        {
            this.m_comparer = EqualityComparer<T>.Default;
        }

        public HashSetEqualityComparer(IEqualityComparer<T> comparer)
        {
            if (this.m_comparer == null)
            {
                this.m_comparer = EqualityComparer<T>.Default;
            }
            else
            {
                this.m_comparer = comparer;
            }
        }

        public override bool Equals(object obj)
        {
            HashSetEqualityComparer<T> comparer = obj as HashSetEqualityComparer<T>;
            if (comparer == null)
            {
                return false;
            }
            return (this.m_comparer == comparer.m_comparer);
        }

        public bool Equals(HashSet<T> x, HashSet<T> y)
        {
            return HashSet<T>.HashSetEquals(x, y, this.m_comparer);
        }

        public override int GetHashCode()
        {
            return this.m_comparer.GetHashCode();
        }

        public int GetHashCode(HashSet<T> obj)
        {
            int num = 0;
            if (obj != null)
            {
                foreach (T local in obj)
                {
                    num ^= this.m_comparer.GetHashCode(local) & 0x7fffffff;
                }
            }
            return num;
        }
    }
}

#endif