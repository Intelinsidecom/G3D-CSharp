/** 
 @file Array.h
 
 @maintainer Morgan McGuire, graphics3d.com
 @cite Portions written by Aaron Orenstein, a@orenstein.name
 
 @created 2001-03-11
 @edited  2005-09-12

 Copyright 2000-2005, Morgan McGuire.
 All rights reserved.
 */

using System;
using global::System.Collections.Generic;
using global::System.Runtime.InteropServices;

namespace G3D
{
    public sealed class Array<T> : ICloneable
    {
        public const bool DONT_SHRINK_UNDERLYING_ARRAY = false;
        public const int SORT_INCREASING = 1;
        public const int SORT_DECREASING = -1;

        private T[] data;
        private int num;
        private int numAllocated;

        private T[] inlineBuffer;
        private const int InlineBufferSize = 4;

        private static readonly int elementSize = computeElementSize();
        private static readonly int minimumSize = Math.Max(10, 32 / elementSize);

        private static int computeElementSize()
        {
            int size;
            try
            {
                size = Marshal.SizeOf(typeof(T));
            }
            catch (ArgumentException)
            {
                size = Environment.Is64BitProcess ? 8 : 4;
            }

            if (size < 1)
            {
                size = 1;
            }

            return size;
        }

        private void init(int n, int a)
        {
            if (n < 0 || a < 0 || n > a)
            {
                throw new ArgumentOutOfRangeException("n");
            }

            num = 0;
            numAllocated = 0;
            data = null;
            inlineBuffer = null;
if (a > 0)
            {
                if (a <= InlineBufferSize)
                {
                    inlineBuffer = new T[InlineBufferSize];
                    data = inlineBuffer;
                    numAllocated = InlineBufferSize;
                    num = n;
                }
                else
                {
                    resize(n);
                }
            }
        }

        private void copy(Array<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            init(other.num, other.num);
            for (int i = 0; i < num; ++i)
            {
                data[i] = other.data[i];
            }
        }

        private void realloc(int oldNum)
        {
            T[] oldData = data;
            bool wasInline = (oldData == inlineBuffer);
            data = new T[numAllocated];

            int count = Math.Min(oldNum, numAllocated);
            if (oldData != null)
            {
                global::System.Array.Copy(oldData, data, count);
            }

            for (int i = count; i < numAllocated; ++i)
            {
                data[i] = default(T);
            }

            if (wasInline)
            {
                inlineBuffer = null;
            }
        }

        private static int compareIncreasing(T a, T b)
        {
            return Comparer<T>.Default.Compare(a, b);
        }

        private static int compareDecreasing(T a, T b)
        {
            return Comparer<T>.Default.Compare(b, a);
        }

        private sealed class ComparisonComparer : IComparer<T>
        {
            private readonly Comparison<T> comparison;

            public ComparisonComparer(Comparison<T> comparison)
            {
                this.comparison = comparison;
            }

            public int Compare(T x, T y)
            {
                return comparison(x, y);
            }
        }

        public Array()
        {
            init(0, 0);
        }

        public Array(int size)
        {
            init(size, size);
        }

        public Array(Array<T> other)
        {
            copy(other);
        }

        public object Clone()
        {
            return new Array<T>(this);
        }

        public void copyFrom(Array<T> other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return;
            }

            resize(other.num);
            for (int i = 0; i < num; ++i)
            {
                data[i] = other[i];
            }
        }

        public void clear()
        {
            resize(0);
        }

        public void fastClear()
        {
            resize(0, false);
        }

        public int size()
        {
            return num;
        }

        public int length()
        {
            return size();
        }

        public int capacity()
        {
            return numAllocated;
        }

public void fastRemove(int index)
        {
            if (index < 0 || index >= num)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            data[index] = data[num - 1];
            resize(size() - 1);
        }

        public void resize(int n)
        {
            resize(n, true);
        }

public void insert(int n, T value)
        {
            if (n < 0 || n > num)
            {
                throw new ArgumentOutOfRangeException("n");
            }

            resize(num + 1, false);

            for (int i = num - 1; i > n; --i)
            {
                data[i] = data[i - 1];
            }
            data[n] = value;
        }

public void resize(int n, bool shrinkIfNecessary)
        {
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException("n");
            }

            int oldNum = num;
            num = n;

            for (int i = num; i < oldNum; ++i)
            {
                data[i] = default(T);
            }

            if (num > numAllocated)
            {
                if (numAllocated == 0)
                {
                    numAllocated = n;
                }
else
                    {
                        if (num < minimumSize)
                        {
                            numAllocated = minimumSize;
                        }
                        else
                        {
                            double growFactor = 2.0;
                            long oldSizeBytes = (long)numAllocated * elementSize;
                            if (oldSizeBytes > 400000)
                            {
                                growFactor = 1.5;
                            }

                            long grown = (num - (long)numAllocated) + (long)(numAllocated * growFactor);
                            if (grown > int.MaxValue)
                            {
                                throw new OutOfMemoryException("Array capacity exceeds Int32.MaxValue.");
                            }

                            numAllocated = (int)grown;
                            if (numAllocated < minimumSize)
                            {
                                numAllocated = minimumSize;
                            }
                        }

                        if (numAllocated < num)
                        {
                            numAllocated = num;
                        }
                    }

                realloc(oldNum);
            }
            else if ((num <= numAllocated / 3) && shrinkIfNecessary && (num > minimumSize))
            {
                numAllocated = Math.Max(num, minimumSize);
                realloc(Math.Min(num, oldNum));
            }

            for (int i = oldNum; i < num; ++i)
            {
                data[i] = default(T);
            }
        }

        public void append(T value)
        {
            if (num < numAllocated)
            {
                data[num] = value;
                ++num;
            }
            else
            {
                resize(num + 1, DONT_SHRINK_UNDERLYING_ARRAY);
                data[num - 1] = value;
            }
        }

        public void append(T v1, T v2)
        {
            if (num + 2 <= numAllocated)
            {
                data[num] = v1;
                data[num + 1] = v2;
                num += 2;
            }
            else
            {
                resize(num + 2, DONT_SHRINK_UNDERLYING_ARRAY);
                data[num - 2] = v1;
                data[num - 1] = v2;
            }
        }

        public void append(T v1, T v2, T v3)
        {
            if (num + 3 <= numAllocated)
            {
                data[num] = v1;
                data[num + 1] = v2;
                data[num + 2] = v3;
                num += 3;
            }
            else
            {
                resize(num + 3, DONT_SHRINK_UNDERLYING_ARRAY);
                data[num - 3] = v1;
                data[num - 2] = v2;
                data[num - 1] = v3;
            }
        }

        public void append(T v1, T v2, T v3, T v4)
        {
            if (num + 4 <= numAllocated)
            {
                data[num] = v1;
                data[num + 1] = v2;
                data[num + 2] = v3;
                data[num + 3] = v4;
                num += 4;
            }
            else
            {
                resize(num + 4, DONT_SHRINK_UNDERLYING_ARRAY);
                data[num - 4] = v1;
                data[num - 3] = v2;
                data[num - 2] = v3;
                data[num - 1] = v4;
            }
        }

        public bool contains(T e)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < size(); ++i)
            {
                if (comparer.Equals(this[i], e))
                {
                    return true;
                }
            }

            return false;
        }

        public void append(Array<T> array)
        {
            if (object.ReferenceEquals(this, array))
            {
                throw new ArgumentException("Cannot append an Array to itself.", "array");
            }

            int oldNum = num;
            int arrayLength = array.length();

            resize(num + arrayLength, false);

            for (int i = 0; i < arrayLength; ++i)
            {
                data[oldNum + i] = array.data[i];
            }
        }

        public T next()
        {
            resize(num + 1, false);
            return last();
        }

        public void push(T value)
        {
            append(value);
        }

        public void push(Array<T> array)
        {
            append(array);
        }

        public void push_back(T value)
        {
            push(value);
        }

        public void pop_back()
        {
            pop();
        }

        public T front()
        {
            return this[0];
        }

        public T pop(bool shrinkUnderlyingArrayIfNecessary = false)
        {
            if (num <= 0)
            {
                throw new InvalidOperationException("Array is empty.");
            }

            T temp = data[num - 1];
            resize(num - 1, shrinkUnderlyingArrayIfNecessary);
            return temp;
        }

        public void popDiscard(bool shrinkUnderlyingArrayIfNecessary = false)
        {
            if (num <= 0)
            {
                throw new InvalidOperationException("Array is empty.");
            }

            resize(num - 1, shrinkUnderlyingArrayIfNecessary);
        }

        public void swap(Array<T> str)
        {
            Array<T> temp = new Array<T>(str);
            str.copyFrom(this);
            copyFrom(temp);
        }

public T this[int n]
        {
            get
            {
                if ((uint)n >= (uint)num)
                {
                    throw new ArgumentOutOfRangeException("n");
                }
                return data[n];
            }
            set
            {
                if ((uint)n >= (uint)num)
                {
                    throw new ArgumentOutOfRangeException("n");
                }
                data[n] = value;
            }
        }

        public T randomElement()
        {
            if (num <= 0)
            {
                throw new InvalidOperationException("Array is empty.");
            }

            return data[g3dmath.iRandom(0, num - 1)];
        }

        public T last()
        {
            if (num <= 0)
            {
                throw new InvalidOperationException("Array is empty.");
            }

            return data[num - 1];
        }

        public void deleteAll()
        {
            for (int i = 0; i < num; ++i)
            {
                IDisposable disposable = data[i] as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }

            resize(0);
        }

        public int findIndex(T value)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < num; ++i)
            {
                if (comparer.Equals(data[i], value))
                {
                    return i;
                }
            }
            return -1;
        }

        public void remove(int index, int count = 1)
        {
            if (index < 0 || index >= num)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (count <= 0 || index + count > num)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            global::System.Array.Copy(data, index + count, data, index, num - index - count);

            for (int i = num - count; i < num; ++i)
            {
                data[i] = default(T);
            }

            resize(num - count);
        }

        public void reverse()
        {
            T temp;

            int n2 = num / 2;
            for (int i = 0; i < n2; ++i)
            {
                temp = data[num - 1 - i];
                data[num - 1 - i] = data[i];
                data[i] = temp;
            }
        }

        public void sort(Comparison<T> lessThan)
        {
            if (num > 1)
            {
                global::System.Array.Sort(data, 0, num, new ComparisonComparer(lessThan));
            }
        }

        public void sort(int direction = SORT_INCREASING)
        {
            if (num > 1)
            {
                global::System.Array.Sort(data, 0, num, new ComparisonComparer(direction == SORT_INCREASING ? (Comparison<T>)compareIncreasing : compareDecreasing));
            }
        }

        public void sortSubArray(int beginIndex, int endIndex, int direction = SORT_INCREASING)
        {
            if (beginIndex < 0 || endIndex < beginIndex || endIndex >= num)
            {
                throw new ArgumentOutOfRangeException("beginIndex");
            }

            global::System.Array.Sort(data, beginIndex, endIndex - beginIndex + 1, new ComparisonComparer(direction == SORT_INCREASING ? (Comparison<T>)compareIncreasing : compareDecreasing));
        }

        public void sortSubArray(int beginIndex, int endIndex, Comparison<T> lessThan)
        {
            if (beginIndex < 0 || endIndex < beginIndex || endIndex >= num)
            {
                throw new ArgumentOutOfRangeException("beginIndex");
            }

            global::System.Array.Sort(data, beginIndex, endIndex - beginIndex + 1, new ComparisonComparer(lessThan));
        }

        public void randomize()
        {
            T temp;

            for (int i = size() - 1; i >= 0; --i)
            {
                int x = g3dmath.iRandom(0, i);

                temp = data[i];
                data[i] = data[x];
                data[x] = temp;
            }
        }

        public T[] ToArray()
        {
            T[] result = new T[num];
            if (num > 0)
            {
                global::System.Array.Copy(data, result, num);
            }
            return result;
        }

        public void CopyTo(T[] destination, int destinationIndex)
        {
            if (destination == null)
            {
                throw new ArgumentNullException("destination");
            }
            global::System.Array.Copy(data, 0, destination, destinationIndex, num);
        }

        public T[] getCArray()
        {
            return data;
        }
    }
}
