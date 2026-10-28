/**
 @file Set.h

 Hash set

 @maintainer Morgan McGuire, matrix@graphics3d.com

 @created 2001-12-09
 @edited  2006-02-20
 */

using System;

namespace G3D
{
    public class Set<T>
    {
        private Table<T, bool> memberTable;

        public Set()
        {
            memberTable = new Table<T, bool>();
        }

        public Set(Set<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            memberTable = new Table<T, bool>(other.memberTable);
        }

        public void copyFrom(Set<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            if (object.ReferenceEquals(this, other))
            {
                return;
            }

            memberTable.copyFrom(other.memberTable);
        }

        public int size()
        {
            return memberTable.size();
        }

        public bool contains(T member)
        {
            return memberTable.containsKey(member);
        }

        public void insert(T member)
        {
            memberTable.set(member, true);
        }

        public void remove(T member)
        {
            memberTable.remove(member);
        }

        public Array<T> getMembers()
        {
            return memberTable.getKeys();
        }

        public void getMembers(Array<T> keyArray)
        {
            memberTable.getKeys(keyArray);
        }

        public void clear()
        {
            memberTable.clear();
        }

        public void deleteAll()
        {
            getMembers().deleteAll();
            clear();
        }

        public class Iterator
        {
            private Table<T, bool>.Iterator it;

            internal Iterator(Table<T, bool>.Iterator it)
            {
                this.it = it;
            }

            public bool MoveNext()
            {
                return it.MoveNext();
            }

            public T Current
            {
                get
                {
                    return it.Current.key;
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
                return left.it == right.it;
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
                return it == null ? 0 : it.GetHashCode();
            }
        }

        public Iterator begin()
        {
            return new Iterator(memberTable.begin());
        }

        public Iterator end()
        {
            return new Iterator(memberTable.end());
        }
    }
}
