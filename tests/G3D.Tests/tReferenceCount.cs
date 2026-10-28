using System;
using System.Runtime.CompilerServices;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private sealed class WKFoo : ReferenceCountedObject
        {
            public string name;

            public WKFoo(string x)
            {
                name = x;
                //printf("Foo(\"%s\") = 0x%x\n", name.c_str(), this);
            }

            protected override void dispose()
            {
                //printf("~Foo(\"%s\") = 0x%x\n", name.c_str(), this);
            }
        }

        private sealed class CircleB : ReferenceCountedObject
        {
            public WeakReferenceCountedPointer<CircleA> weakRefToA =
                new WeakReferenceCountedPointer<CircleA>();
        }

        private sealed class CircleA : ReferenceCountedObject
        {
            public ReferenceCountedPointer<CircleB> refToB =
                new ReferenceCountedPointer<CircleB>();

            protected override void dispose()
            {
                refToB.Dispose();
            }
        }

        private static void testCycle()
        {
            ReferenceCountedPointer<CircleA> a =
                new ReferenceCountedPointer<CircleA>(new CircleA());
            a.pointer().refToB.set(new CircleB());
            a.pointer().refToB.pointer().weakRefToA.set(a);
            a.set((CircleA)null);
        }

        private static void testWeakPointer()
        {
            Console.Write("WeakReferenceCountedPointer ");

            testCycle();

            WeakReferenceCountedPointer<WKFoo> wB =
                new WeakReferenceCountedPointer<WKFoo>();
            {
                ReferenceCountedPointer<WKFoo> A =
                    new ReferenceCountedPointer<WKFoo>(new WKFoo("A"));

                WeakReferenceCountedPointer<WKFoo> wA =
                    new WeakReferenceCountedPointer<WKFoo>(A);

                ReferenceCountedPointer<WKFoo> strongA = wA.createStrongPtr();
                Debug.debugAssert(strongA.isNull() == false);
                strongA.Dispose();

                A.set((WKFoo)null);

                strongA = wA.createStrongPtr();
                Debug.debugAssert(strongA.isNull() == true);
                strongA.Dispose();


                ReferenceCountedPointer<WKFoo> B =
                    new ReferenceCountedPointer<WKFoo>(new WKFoo("B"));

                A.set(B);
                strongA = wA.createStrongPtr();
                Debug.debugAssert(strongA.isNull() == true);
                strongA.Dispose();

                ReferenceCountedPointer<WKFoo> strongB = wB.createStrongPtr();
                Debug.debugAssert(strongB.isNull() == true);
                strongB.Dispose();

                wA.set(A);

                strongA = wA.createStrongPtr();
                Debug.debugAssert(strongA.isNull() == false);
                strongA.Dispose();

                wB.set(B);

                Debug.debugAssert(A == B);
                Debug.debugAssert(wA == wB);

                wA.set((WKFoo)null);
                strongA = wA.createStrongPtr();
                Debug.debugAssert(strongA.isNull() == true);
                strongA.Dispose();

                {
                    ReferenceCountedPointer<WKFoo> C =
                        new ReferenceCountedPointer<WKFoo>(new WKFoo("C"));
                    C.Dispose();
                }

                strongB = wB.createStrongPtr();
                Debug.debugAssert(strongB.isNull() == false);
                strongB.Dispose();

                B.Dispose();
                A.Dispose();
            }
            ReferenceCountedPointer<WKFoo> collected = wB.createStrongPtr();
            Debug.debugAssert(collected.isNull() == true);
            collected.Dispose();

            Console.Write("passed\n");
        }


        private static int numRCPFoo = 0;
        private class RCPFoo : ReferenceCountedObject
        {
            public int x;
            public RCPFoo()
            {
                ++numRCPFoo;
            }
            protected override void dispose()
            {
                --numRCPFoo;
            }
        }


        private sealed class RefSubclass : RCPFoo
        {
        }


        private class Reftest : ReferenceCountedObject
        {
            public static Array<string> sequence = new Array<string>();
            public string s;

            public Reftest(string s)
            {
                this.s = s;
                Debug.debugPrintf("alloc 0x%s (%s)\n",
                    RuntimeHelpers.GetHashCode(this).ToString("x"), s);
                sequence.append(s);
            }

            protected override void dispose()
            {
                Debug.debugPrintf("free 0x%s (~%s)\n",
                    RuntimeHelpers.GetHashCode(this).ToString("x"), s);
                sequence.append("~" + s);
            }
        }

        private sealed class Reftest2 : Reftest
        {
            public Reftest2()
                : base("2")
            {
            }
        }


        /* Called from testRCP to test automatic down-casting */
        private static void subclasstest(ReferenceCountedPointer<RCPFoo> b)
        {
        }

        private static void testRCP()
        {
            Console.Write("ReferenceCountedPointer");

            Debug.debugAssert(numRCPFoo == 0);
            ReferenceCountedPointer<RCPFoo> a =
                new ReferenceCountedPointer<RCPFoo>(new RCPFoo());
            Debug.debugAssert(numRCPFoo == 1);
            Debug.debugAssert(a.isLastReference());

            {
                ReferenceCountedPointer<RCPFoo> b =
                    new ReferenceCountedPointer<RCPFoo>(new RCPFoo());
                Debug.debugAssert(numRCPFoo == 2);
                b.set(a);
                Debug.debugAssert(numRCPFoo == 1);
                Debug.debugAssert(!a.isLastReference());
                Debug.debugAssert(!b.isLastReference());
                b.Dispose();
            }

            Debug.debugAssert(a.isLastReference());
            Debug.debugAssert(numRCPFoo == 1);


            // Test allocation and deallocation of 
            // reference counted values.
            {
                ReferenceCountedPointer<Reftest> a1 =
                    new ReferenceCountedPointer<Reftest>(new Reftest("a"));
                ReferenceCountedPointer<Reftest> b =
                    new ReferenceCountedPointer<Reftest>(new Reftest("b"));

                a1.set(b);
                Reftest.sequence.append("--");
                Debug.debugPrintf("---------\n");
                b.set((Reftest)null);
                Reftest.sequence.append("--");
                Debug.debugPrintf("---------\n");

                a1.Dispose();
            }

            Debug.debugAssert(Reftest.sequence[0] == "a");
            Debug.debugAssert(Reftest.sequence[1] == "b");
            Debug.debugAssert(Reftest.sequence[2] == "~a");
            Debug.debugAssert(Reftest.sequence[3] == "--");
            Debug.debugAssert(Reftest.sequence[4] == "--");
            Debug.debugAssert(Reftest.sequence[5] == "~b");

            Reftest.sequence.clear();

            // Test type hierarchies with reference counted values.

            {
                ReferenceCountedPointer<Reftest> one =
                    new ReferenceCountedPointer<Reftest>(new Reftest("1"));
                ReferenceCountedPointer<Reftest2> two =
                    new ReferenceCountedPointer<Reftest2>(new Reftest2());

                one.set(two.pointer());
                two.Dispose();
                one.Dispose();
            }
            Debug.debugAssert(Reftest.sequence[0] == "1");
            Debug.debugAssert(Reftest.sequence[1] == "2");
            Debug.debugAssert(Reftest.sequence[2] == "~1");
            Debug.debugAssert(Reftest.sequence[3] == "~2");
            Reftest.sequence.clear();

            {
                ReferenceCountedPointer<Reftest> one =
                    new ReferenceCountedPointer<Reftest>(new Reftest2());
                one.Dispose();
            }
            Debug.debugAssert(Reftest.sequence[0] == "2");
            Debug.debugAssert(Reftest.sequence[1] == "~2");
            Reftest.sequence.clear();

            {
                // Should not compile
//                ARef2 one = new Reftest("1");
            }


            // Test subclassing

            {
                ReferenceCountedPointer<RefSubclass> s =
                    new ReferenceCountedPointer<RefSubclass>(new RefSubclass());
                ReferenceCountedPointer<RCPFoo> b =
                    new ReferenceCountedPointer<RCPFoo>();

                // s is a subclass, so this should call the templated
                // constructor and succeed.
                b.set(s.pointer());

                // Likewise
                subclasstest(b);

                b.Dispose();
                s.Dispose();
            }

            a.Dispose();
        }

        private static void testReferenceCount()
        {
            testWeakPointer();
            testRCP();
        }
    }
}
