/**
  @file ReferenceCount.h

  Reference Counting Garbage Collector for C++

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @cite Adapted and extended from Justin Miller's "RGC" class that appeared in BYTE magazine.
  @cite See also http://www.jelovic.com/articles/cpp_without_memory_errors_slides.htm

  @created 2001-10-23
  @edited  2006-02-24
*/
using System;

namespace G3D
{
    /** Base class for WeakReferenceCountedPointer */
    public abstract class _WeakPtr
    {
        /** Called by ReferenceCountedObject to tell a weak pointer that its underlying object was collected. */
        internal abstract void objectCollected();
    }

    /** Used internally by ReferenceCountedObject */
    public sealed class _WeakPtrLinkedList
    {
        public _WeakPtr weakPtr;
        public _WeakPtrLinkedList next;

        public _WeakPtrLinkedList()
        {
            weakPtr = null;
            next = null;
        }

        /** Inserts this node into the head of the list that previously had n as its head. */
        public _WeakPtrLinkedList(_WeakPtr p, _WeakPtrLinkedList n)
        {
            weakPtr = p;
            next = n;
        }
    }

    /**
     Objects that are reference counted inherit from this.  Subclasses 
     <B>must</B> have a public destructor (the default destructor is fine)
     and <B>publicly</B> inherit ReferenceCountedObject.

     Multiple inheritance from a reference counted object is dangerous-- use 
     at your own risk.

     ReferenceCountedPointer and ReferenceCountedObject are threadsafe.
     You can create and drop references on multiple threads without
     violating integrity.  WeakReferenceCountedPointer is <i>not</i>
     threadsafe.  Introducing a weak pointer destroys all thread safety,
     even for strong pointers to the same object (this is inherent in the
     design of the class; we cannot fix it without slowing down the
     performance of reference counted objects.)
     */
    public class ReferenceCountedObject : IDisposable
    {
        /**
         The long name is to keep this from accidentally conflicting with
         a subclass's variable name.  Do not use or explicitly manipulate
         this value--its type may change in the future and is not part
         of the supported API.
         */
        public AtomicInt32 ReferenceCountedObject_refCount;

        /**
         Linked list of all weak pointers that reference this (some may be
         on the stack!).  Do not use or explicitly manipulate this value.
         */
        internal _WeakPtrLinkedList ReferenceCountedObject_weakPointer;

        protected ReferenceCountedObject()
        {
            ReferenceCountedObject_refCount = new AtomicInt32(0);
            ReferenceCountedObject_weakPointer = null;
        }

        /** Automatically called immediately before the object is deleted. */
        public void ReferenceCountedObject_zeroWeakPointers()
        {
            // Tell all of my weak pointers that I'm gone.
            _WeakPtrLinkedList node = ReferenceCountedObject_weakPointer;

            while (node != null)
            {
                // Notify the weak pointer that it is going away
                node.weakPtr.objectCollected();

                // Free the node and advance
                _WeakPtrLinkedList tmp = node;
                node = node.next;
                tmp.weakPtr = null;
                tmp.next = null;
            }

            ReferenceCountedObject_weakPointer = null;
        }

        ~ReferenceCountedObject()
        {
            Dispose(false);
        }

        /**
          Named to match the C++ destructor.  Reference counting calls this
          when the last strong reference is dropped.
         */
        protected virtual void dispose()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                dispose();
            }
        }

        /**
          Note: copies will initially start out with 0 
          references and 0 weak references like any other object.
         */
        protected ReferenceCountedObject(ReferenceCountedObject notUsed)
        {
            ReferenceCountedObject_refCount = new AtomicInt32(0);
            ReferenceCountedObject_weakPointer = null;
        }
    }

    /**
     Use ReferenceCountedPointer<T> in place of T* in your program.
     T must subclass ReferenceCountedObject.
     */
    public sealed class ReferenceCountedPointer<T> : IDisposable where T : ReferenceCountedObject
    {
        private T m_pointer;

        public T pointer()
        {
            return m_pointer;
        }

        /** @deprecated use pointer() */
        public T getPointer()
        {
            return m_pointer;
        }

        /** Nulls out the pointer and drops a reference. If the reference count hits zero. */
        private void zeroPointer()
        {
            if (m_pointer != null)
            {
                // Only delete if this instance caused the count to hit
                // exactly zero.  If there is a race condition, the value
                // may be zero after decrement returns, but only one of
                // the instances will get a zero return value.
                if (m_pointer.ReferenceCountedObject_refCount.decrement() == 0)
                {
                    // We held the last reference, so delete the object.
                    // We must zero the weak pointers *before* deletion in case there
                    // are cycles of weak references.
                    m_pointer.ReferenceCountedObject_zeroWeakPointers();
                    m_pointer.Dispose();
                }

                m_pointer = null;
            }
        }

        /**
         Non-atomic (except for the reference increment).  Can only be
         called in contexts like the copy constructor or initial
         constructor where it is known that the reference count will
         not hit zero on some other thread.
         */
        public void setPointer(T x)
        {
            if (!object.ReferenceEquals(x, m_pointer))
            {
                zeroPointer();

                if (x != null)
                {
                    m_pointer = x;
                    m_pointer.ReferenceCountedObject_refCount.increment();
                }
            }
        }

        public ReferenceCountedPointer()
        {
            m_pointer = null;
        }

        public ReferenceCountedPointer(ReferenceCountedPointer<T> p)
        {
            m_pointer = null;
            setPointer(p.m_pointer);
        }

        public ReferenceCountedPointer(T p)
        {
            m_pointer = null;
            setPointer(p);
        }

        public ReferenceCountedPointer<S> downcast<S>() where S : ReferenceCountedObject
        {
            return new ReferenceCountedPointer<S>(m_pointer as S);
        }

        /** C# replacement for the C++ assignment operators. */
        public void set(ReferenceCountedPointer<T> p)
        {
            setPointer(p == null ? null : p.m_pointer);
        }

        /** C# replacement for the C++ assignment operators. */
        public void set(T p)
        {
            setPointer(p);
        }

        public static bool operator ==(ReferenceCountedPointer<T> x, ReferenceCountedPointer<T> y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }
            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return false;
            }
            return object.ReferenceEquals(x.m_pointer, y.m_pointer);
        }

        public static bool operator !=(ReferenceCountedPointer<T> x, ReferenceCountedPointer<T> y)
        {
            return !(x == y);
        }

        public override bool Equals(object obj)
        {
            return obj is ReferenceCountedPointer<T> && this == (ReferenceCountedPointer<T>)obj;
        }

        public override int GetHashCode()
        {
            return m_pointer == null ? 0 : m_pointer.GetHashCode();
        }

        public T value
        {
            get
            {
                return m_pointer;
            }
        }

        public bool isNull()
        {
            return m_pointer == null;
        }

        public bool notNull()
        {
            return m_pointer != null;
        }

        /**
         Returns true if this is the last reference to an object.
         Useful for flushing memoization caches-- a cache that holds the last
         reference is unnecessarily keeping an object alive.

         <b>Not threadsafe.</b>
         */
        public bool isLastReference()
        {
            return m_pointer != null && m_pointer.ReferenceCountedObject_refCount.value() == 1;
        }

        public void Dispose()
        {
            zeroPointer();
            GC.SuppressFinalize(this);
        }

        ~ReferenceCountedPointer()
        {
            zeroPointer();
        }
    }

    /**
     A weak pointer allows the object it references to be garbage collected.
     Weak pointers are commonly used in caches, where it is important to hold
     a pointer to an object without keeping that object alive solely for the
     cache's benefit (i.e., the object can be collected as soon as all
     pointers to it <B>outside</B> the cache are gone).  They are also convenient
     for adding back-pointers in tree and list structures.

     Weak pointers may become NULL at any point (when their target is collected).
     Therefore the only way to reference the target is to convert to a strong
     pointer and then check that it is not NULL.
     */
    public sealed class WeakReferenceCountedPointer<T> : _WeakPtr, IDisposable where T : ReferenceCountedObject
    {
        /** NULL if the object has been collected. */
        private T pointer;

        /**
         Creates a strong pointer, which prevents the object from being
         garbage collected.  The strong pointer may be NULL, which means
         that the underlying.
         */
        public ReferenceCountedPointer<T> createStrongPtr()
        {
            return new ReferenceCountedPointer<T>(pointer);
        }

        /**
         Thread issues: safe because this is only called when another
         object is guaranteed to keep p alive for the duration of this
         call.
         */
        private void setPointer(T p)
        {
            zeroPointer();
            pointer = p;

            if (pointer != null)
            {
                // Add myself to the head of my target's list of weak pointers
                _WeakPtrLinkedList head = new _WeakPtrLinkedList(this, pointer.ReferenceCountedObject_weakPointer);
                pointer.ReferenceCountedObject_weakPointer = head;
            }
        }

        /**
         Removes this from its target's list of weak pointers.  Called
         when the weak pointer goes out of scope.
         */
        private void zeroPointer()
        {
            // Grab a strong reference to prevent the object from being collected while we
            // are traversing its list.
            ReferenceCountedPointer<T> strong = createStrongPtr();

            // If the following test fails then the object was collected before we
            // reached it.
            if (strong.notNull())
            {
                _WeakPtrLinkedList node = pointer.ReferenceCountedObject_weakPointer;
                _WeakPtrLinkedList previous = null;
                while (node != null && !object.ReferenceEquals(node.weakPtr, this))
                {
                    previous = node;
                    node = node.next;
                }

                if (node != null)
                {
                    // Node must now point at the node for me.  Remove node and
                    // close the linked list behind it.
                    if (previous == null)
                    {
                        pointer.ReferenceCountedObject_weakPointer = node.next;
                    }
                    else
                    {
                        previous.next = node.next;
                    }

                    node.weakPtr = null;
                    node.next = null;
                }
            }

            pointer = null;
            strong.Dispose();
        }

        public WeakReferenceCountedPointer()
        {
            pointer = null;
        }

        public WeakReferenceCountedPointer(WeakReferenceCountedPointer<T> weakPtr)
        {
            pointer = null;
            setPointer(weakPtr.pointer);
        }

        public WeakReferenceCountedPointer(ReferenceCountedPointer<T> strongPtr)
        {
            pointer = null;
            setPointer(strongPtr.getPointer());
        }

        /** C# replacement for the C++ assignment operators. */
        public void set(WeakReferenceCountedPointer<T> other)
        {
            setPointer(other.pointer);
        }

        /** C# replacement for the C++ assignment operators. */
        public void set(ReferenceCountedPointer<T> other)
        {
            setPointer(other.getPointer());
        }

        /** C# replacement for the C++ assignment operators. */
        public void set(T other)
        {
            setPointer(other);
        }

        public bool isNull()
        {
            return pointer == null;
        }

        public bool notNull()
        {
            return pointer != null;
        }

        public T pointerValue()
        {
            return pointer;
        }

        public static bool operator ==(WeakReferenceCountedPointer<T> x, WeakReferenceCountedPointer<T> y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }
            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null))
            {
                return false;
            }
            return object.ReferenceEquals(x.pointer, y.pointer);
        }

        public static bool operator !=(WeakReferenceCountedPointer<T> x, WeakReferenceCountedPointer<T> y)
        {
            return !(x == y);
        }

        public override bool Equals(object obj)
        {
            return obj is WeakReferenceCountedPointer<T> && this == (WeakReferenceCountedPointer<T>)obj;
        }

        public override int GetHashCode()
        {
            return pointer == null ? 0 : pointer.GetHashCode();
        }

        internal override void objectCollected()
        {
            pointer = null;
        }

        public void Dispose()
        {
            zeroPointer();
            GC.SuppressFinalize(this);
        }

        ~WeakReferenceCountedPointer()
        {
            zeroPointer();
        }
    }
}
