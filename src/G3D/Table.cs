/**
 @file Table.h

 Templated hash table class.

 @maintainer Morgan McGuire, matrix@graphics3d.com
 @cite Bug fix by Darius Jazayeri, jazayeri@MIT.EDU

 @created 2001-04-22
 @edited  2005-09-12
 Copyright 2000-2005, Morgan McGuire.
 All rights reserved.
 */

using System;
using global::System.Collections.Generic;

namespace G3D
{
    public class Table<TKey, TValue>
    {
        public class Entry
        {
            public TKey key;
            public TValue value;
        }

        internal class Node
        {
            public uint hashCode;
            public Entry entry;
            public Node next;

            public Node(TKey key, TValue value, uint hashCode, Node next)
            {
                this.entry = new Entry();
                this.entry.key = key;
                this.entry.value = value;
                this.hashCode = hashCode;
                this.next = next;
            }

            public Node Clone()
            {
                return new Node(entry.key, entry.value, hashCode, next == null ? null : next.Clone());
            }
        }

        private int _size;
        private Node[] bucket;
        private int numBuckets;

        private static uint HashCode(TKey key)
        {
            if (object.ReferenceEquals(key, null))
            {
                return 0;
            }

            uint code = (uint)EqualityComparer<TKey>.Default.GetHashCode(key);
            return code * 0x9e3779b9U;
        }

        private static bool KeysEqual(TKey left, TKey right)
        {
            return EqualityComparer<TKey>.Default.Equals(left, right);
        }

        private void FreeMemory()
        {
            if (bucket != null)
            {
                for (int b = 0; b < bucket.Length; ++b)
                {
                    Node node = bucket[b];
                    while (node != null)
                    {
                        Node next = node.next;
                        node.next = null;
                        node = next;
                    }
                }
            }

            bucket = null;
            numBuckets = 0;
            _size = 0;
        }

        private void CopyFrom(Table<TKey, TValue> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            _size = other._size;
            numBuckets = other.numBuckets;
            bucket = new Node[numBuckets];

            for (int b = 0; b < numBuckets; ++b)
            {
                if (other.bucket[b] != null)
                {
                    bucket[b] = other.bucket[b].Clone();
                }
            }
        }

        private void Resize(int newNumBuckets)
        {
            Node[] oldBucket = bucket;
            bucket = new Node[newNumBuckets];

            for (int b = 0; b < numBuckets; ++b)
            {
                Node node = oldBucket[b];
                while (node != null)
                {
                    Node nextNode = node.next;
                    int i = (int)(node.hashCode % (uint)newNumBuckets);
                    node.next = bucket[i];
                    bucket[i] = node;
                    node = nextNode;
                }
            }

            numBuckets = newNumBuckets;
        }

        public Table()
        {
            numBuckets = 10;
            _size = 0;
            bucket = new Node[numBuckets];
        }

        public Table(Table<TKey, TValue> other)
        {
            CopyFrom(other);
        }

        public void copyFrom(Table<TKey, TValue> other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return;
            }

            FreeMemory();
            CopyFrom(other);
        }

        public int debugGetDeepestBucketSize()
        {
            int deepest = 0;

            for (int b = 0; b < numBuckets; ++b)
            {
                int count = 0;
                Node node = bucket[b];
                while (node != null)
                {
                    node = node.next;
                    ++count;
                }

                if (count > deepest)
                {
                    deepest = count;
                }
            }

            return deepest;
        }

        public double debugGetLoad()
        {
            if (size() == 0)
            {
                return 0.0;
            }

            return debugGetDeepestBucketSize() / (double)size();
        }

        public int debugGetNumBuckets()
        {
            return numBuckets;
        }

        public class Iterator
        {
            private int index;
            private Node node;
            private readonly Table<TKey, TValue> table;
            private readonly int tableNumBuckets;
            private readonly Node[] tableBucket;
            private bool isDone;

            internal Iterator(Table<TKey, TValue> table, int numBuckets, Node[] bucket)
            {
                this.table = table;
                this.tableNumBuckets = numBuckets;
                this.tableBucket = bucket;

                if (numBuckets == 0)
                {
                    isDone = true;
                    return;
                }

                index = 0;
                node = tableBucket[index];
                isDone = false;
                FindNext();
            }

            internal Iterator(Table<TKey, TValue> table)
            {
                this.table = table;
                isDone = true;
            }

            private void FindNext()
            {
                while (node == null)
                {
                    ++index;
                    if (index >= tableNumBuckets)
                    {
                        isDone = true;
                        break;
                    }
                    else
                    {
                        node = tableBucket[index];
                    }
                }
            }

            public bool isDoneValue()
            {
                return isDone;
            }

            public bool MoveNext()
            {
                if (isDone)
                {
                    return false;
                }

                node = node.next;
                FindNext();
                return !isDone;
            }

            public Entry Current
            {
                get
                {
                    if (isDone || node == null)
                    {
                        throw new InvalidOperationException("Iterator is not positioned on an element.");
                    }
                    return node.entry;
                }
            }

            public static bool operator ==(Iterator left, Iterator right)
            {
                if (object.ReferenceEquals(left, right))
                {
                    return true;
                }
                if (object.ReferenceEquals(left, null) || object.ReferenceEquals(right, null))
                {
                    return false;
                }

                if (right.isDone || left.isDone)
                {
                    return (left.isDone == right.isDone) && (left.table == right.table);
                }

                return (left.table == right.table) && (left.node == right.node) && (left.index == right.index);
            }

            public static bool operator !=(Iterator left, Iterator right)
            {
                return !(left == right);
            }

            public override bool Equals(object obj)
            {
                return this == obj as Iterator;
            }

            public override int GetHashCode()
            {
                return table == null ? 0 : table.GetHashCode();
            }
        }

        public Iterator begin()
        {
            return new Iterator(this, numBuckets, bucket);
        }

        public Iterator end()
        {
            return new Iterator(this);
        }

        public void clear()
        {
            FreeMemory();
            numBuckets = 20;
            _size = 0;
            bucket = new Node[numBuckets];
        }

        public int size()
        {
            return _size;
        }

        public void set(TKey key, TValue value)
        {
            uint code = HashCode(key);
            uint b = code % (uint)numBuckets;

            Node node = bucket[b];

            if (node == null)
            {
                bucket[b] = new Node(key, value, code, null);
                ++_size;
                return;
            }

            int bucketLength = 1;
            bool allSameCode = true;

            Node current = node;
            while (current != null)
            {
                allSameCode = allSameCode && (code == current.hashCode);

                if (code == current.hashCode && KeysEqual(current.entry.key, key))
                {
                    current.entry.value = value;
                    return;
                }

                current = current.next;
                ++bucketLength;
            }

            const int maxBucketLength = 5;
            if (bucketLength > maxBucketLength && !allSameCode && numBuckets < _size * 20)
            {
                Resize(numBuckets * 2 + 1);
                b = code % (uint)numBuckets;
            }

            bucket[b] = new Node(key, value, code, bucket[b]);
            ++_size;
        }

        public void remove(TKey key)
        {
            uint code = HashCode(key);
            uint b = code % (uint)numBuckets;

            Node node = bucket[b];
            Node previous = null;

            while (node != null)
            {
                if (code == node.hashCode && KeysEqual(node.entry.key, key))
                {
                    if (previous == null)
                    {
                        bucket[b] = node.next;
                    }
                    else
                    {
                        previous.next = node.next;
                    }

                    node.next = null;
                    --_size;
                    return;
                }

                previous = node;
                node = node.next;
            }

            Debug.alwaysAssertM(false, "Tried to remove a key that was not in the table.");
            throw new InvalidOperationException("Tried to remove a key that was not in the table.");
        }

        public TValue get(TKey key)
        {
            uint code = HashCode(key);
            uint b = code % (uint)numBuckets;

            Node node = bucket[b];

            while (node != null)
            {
                if (node.hashCode == code && KeysEqual(node.entry.key, key))
                {
                    return node.entry.value;
                }
                node = node.next;
            }

            Debug.debugAssertM(false, "Key not found");
            throw new InvalidOperationException("Key not found.");
        }

        public bool get(TKey key, out TValue value)
        {
            uint code = HashCode(key);
            uint b = code % (uint)numBuckets;

            Node node = bucket[b];

            while (node != null)
            {
                if (node.hashCode == code && KeysEqual(node.entry.key, key))
                {
                    value = node.entry.value;
                    return true;
                }
                node = node.next;
            }

            value = default(TValue);
            return false;
        }

        public bool containsKey(TKey key)
        {
            uint code = HashCode(key);
            uint b = code % (uint)numBuckets;

            Node node = bucket[b];

            while (node != null)
            {
                if (node.hashCode == code && KeysEqual(node.entry.key, key))
                {
                    return true;
                }
                node = node.next;
            }

            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                return get(key);
            }
            set
            {
                set(key, value);
            }
        }

        public Array<TKey> getKeys()
        {
            Array<TKey> keyArray = new Array<TKey>();
            getKeys(keyArray);
            return keyArray;
        }

        public void getKeys(Array<TKey> keyArray)
        {
            if (keyArray == null)
            {
                throw new ArgumentNullException("keyArray");
            }

            keyArray.resize(0, Array<TKey>.DONT_SHRINK_UNDERLYING_ARRAY);

            for (int i = 0; i < numBuckets; ++i)
            {
                Node node = bucket[i];
                while (node != null)
                {
                    keyArray.append(node.entry.key);
                    node = node.next;
                }
            }
        }

        public void deleteKeys()
        {
            getKeys().deleteAll();
        }

        public void deleteValues()
        {
            for (int i = 0; i < numBuckets; ++i)
            {
                Node node = bucket[i];
                while (node != null)
                {
                    IDisposable disposable = node.entry.value as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                    node = node.next;
                }
            }
        }
    }
}
