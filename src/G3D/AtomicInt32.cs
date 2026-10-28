/**
 @file AtomicInt32.h

 @maintainer Morgan McGuire, matrix@graphics3d.com

 @created 2005-09-01
 @edited  2005-09-26
 */
using System.Threading;

namespace G3D
{
    /**
     An integer that may safely be used on different threads without
     external locking.

     On Win32, Linux, and Mac OS X this is implemented without locks.  

     <B>BETA API</B>  This is unsupported and may change
     */
    public struct AtomicInt32
    {
        private int _value;

        /** Atomic set */
        public AtomicInt32(int x)
        {
            // APR does this assignment using InterlockedExchange
            // Morgan believes that volatile should be sufficient, however.
            _value = x;
        }

        /** Atomic set */
        public AtomicInt32(AtomicInt32 x)
        {
            _value = x._value;
        }

        /** Atomic set */
        public static implicit operator AtomicInt32(int x)
        {
            return new AtomicInt32(x);
        }

        /** Returns the current value */
        public int value()
        {
            return Thread.VolatileRead(ref _value);
        }

        /** Returns the old value, before the add. */
        public int add(int x)
        {
            return Interlocked.Add(ref _value, x) - x;
        }

        /** Returns old value. */
        public int sub(int x)
        {
            return add(-x);
        }

        public void increment()
        {
            // Note: returns the newly incremented value
            Interlocked.Increment(ref _value);
        }

        /** Returns zero if the result is zero after decrement, non-zero otherwise.*/
        public uint decrement()
        {
            // Note: returns the newly decremented value
            return Interlocked.Decrement(ref _value) != 0 ? 1u : 0u;
        }

        /**
         Atomic test-and-set:  if <code>*this == comperand</code> then <code>*this := exchange</code> else do nothing.
         In both cases, returns the old value of <code>*this</code>.
     
         Performs an atomic comparison of this with the Comperand value. 
         If this is equal to the Comperand value, the Exchange value is stored in this.
         Otherwise, no operation is performed.
         */
        public int compareAndSet(int comperand, int exchange)
        {
            return Interlocked.CompareExchange(ref _value, exchange, comperand);
        }
    }
}
