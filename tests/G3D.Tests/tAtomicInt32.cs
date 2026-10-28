using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testAtomicInt32()
        {
            Console.Write("G3D::AtomicInt32 ");

            // Test first in the absence of threads.
            {
                AtomicInt32 a = new AtomicInt32(1);
                Debug.debugAssert(a.value() == 1);

                a = 1;
                a.increment();
                Debug.debugAssert(a.value() == 2);

                a = 2;
                a.decrement();
                Debug.debugAssert(a.value() == 1);
                Debug.debugAssert(a.decrement() == 0);

                a = 10;
                Debug.debugAssert(a.value() == 10);

                a = 10;
                a.compareAndSet(10, 1);
                Debug.debugAssert(a.value() == 1);

                a = 1;
                a.compareAndSet(10, 15);
                Debug.debugAssert(a.value() == 1);

                a = 1;
                a.add(5);
                Debug.debugAssert(a.value() == 6);

                a = 6;
                a.sub(3);
                Debug.debugAssert(a.value() == 3);

                a = 6;
                a.sub(-3);
                Debug.debugAssert(a.value() == 9);
            }

            Console.Write("passed\n");
        }
    }
}
