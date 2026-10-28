/** 
 @file Queue.h
 
 @maintainer Morgan McGuire, graphics3d.com
 
 @created 2002-07-09
 @edited  2005-08-20
 */

using System;
using global::System.Collections.Generic;

namespace G3D
{
    public sealed class Queue<T> : ICloneable
    {
        private T[] data;
        private int head;
        private int num;
        private int numAllocated;

        public Queue()
        {
            data = null;
            head = 0;
            num = 0;
            numAllocated = 0;
        }

        public Queue(Queue<T> other)
        {
            data = null;
            head = 0;
            num = 0;
            numAllocated = 0;
            copy(other);
        }

        public object Clone()
        {
            return new Queue<T>(this);
        }

        public void copyFrom(Queue<T> other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return;
            }

            clear();
            copy(other);
        }

        private void copy(Queue<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            if (other.numAllocated == 0)
            {
                data = null;
                head = 0;
                num = 0;
                numAllocated = 0;
                return;
            }

            data = new T[other.numAllocated];
            head = other.head;
            num = other.num;
            numAllocated = other.numAllocated;
            Array.Copy(other.data, data, other.numAllocated);
        }

        private int index(int i)
        {
            return (head + i + numAllocated) % numAllocated;
        }

        private void repackAndRealloc(int newSize)
        {
            T[] old = data;
            data = new T[newSize];

            int firstEnd = head + num;
            int secondEnd = 0;
            if (firstEnd > numAllocated)
            {
                secondEnd = firstEnd - numAllocated;
                firstEnd = numAllocated;
            }

            int j = 0;
            for (int i = head; i < firstEnd; ++i, ++j)
            {
                data[j] = old[i];
            }

            for (int i = 0; i < secondEnd; ++i, ++j)
            {
                data[j] = old[i];
            }

            head = 0;
            numAllocated = newSize;
        }

        private void reserveSpace()
        {
            if (num == numAllocated)
            {
                repackAndRealloc(numAllocated * 3 + 20);
            }
        }

        public void pushFront(T e)
        {
            reserveSpace();

            int i = index(-1);
            data[i] = e;
            head = i;
            ++num;
        }

        public void pushBack(T e)
        {
            reserveSpace();

            int i = index(num);
            data[i] = e;
            ++num;
        }

        public void enqueue(T e)
        {
            pushBack(e);
        }

        public T popBack()
        {
            if (num == 0)
            {
                throw new InvalidOperationException("Queue is empty.");
            }

            int tail = index(num - 1);
            T result = data[tail];
            data[tail] = default(T);
            --num;
            return result;
        }

        public T popFront()
        {
            if (num == 0)
            {
                throw new InvalidOperationException("Queue is empty.");
            }

            T result = data[head];
            data[head] = default(T);
            head = (head + 1) % numAllocated;
            --num;
            return result;
        }

        public T dequeue()
        {
            return popFront();
        }

        public void clear(bool freeStorage = true)
        {
            if (data != null && num > 0)
            {
                int firstEnd = head + num;
                int secondEnd = 0;
                if (firstEnd > numAllocated)
                {
                    secondEnd = firstEnd - numAllocated;
                    firstEnd = numAllocated;
                }

                for (int i = head; i < firstEnd; ++i)
                {
                    data[i] = default(T);
                }

                for (int i = 0; i < secondEnd; ++i)
                {
                    data[i] = default(T);
                }
            }

            num = 0;
            head = 0;
            if (freeStorage)
            {
                numAllocated = 0;
                data = null;
            }
        }

        public void fastClear()
        {
            clear(false);
        }

        public int size()
        {
            return num;
        }

        public int length()
        {
            return size();
        }

        public T this[int n]
        {
            get
            {
                if ((uint)n >= (uint)num)
                {
                    throw new ArgumentOutOfRangeException("n");
                }
                return data[index(n)];
            }
            set
            {
                if ((uint)n >= (uint)num)
                {
                    throw new ArgumentOutOfRangeException("n");
                }
                data[index(n)] = value;
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

        public void deleteAll()
        {
            for (int i = 0; i < size(); ++i)
            {
                IDisposable disposable = this[i] as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
            clear();
        }
    }
}
