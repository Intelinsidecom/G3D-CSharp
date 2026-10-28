using System;
using global::System.Collections.Generic;
using global::System.Runtime.InteropServices;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        /** Make this structure big */
        [StructLayout(LayoutKind.Sequential, Size = 404)]
        private struct BigE
        {
            public int x;

            public BigE(bool init)
            {
                // Documented adaptation: the C++ default constructor set x = 7
                // and initialized dummy[100]; the C# layout has no dummy array,
                // so only x is assigned. The bool parameter allows constructing
                // with the same default values without conflicting with the
                // parameterless struct constructor.
                x = 7;
            }
        }

        private static void perfQueue()
        {
            printf("Queue Performance:\n");

            ulong g3dStreamSmall = 0, g3dEnquequeFSmall = 0, g3dEnquequeBSmall = 0;
            ulong stdStreamSmall = 0, stdEnquequeFSmall = 0, stdEnquequeBSmall = 0;

            ulong g3dStreamLarge = 0, g3dEnquequeFLarge = 0, g3dEnquequeBLarge = 0;
            ulong stdStreamLarge = 0, stdEnquequeFLarge = 0, stdEnquequeBLarge = 0;

            int iterations = 100000;
            int enqueuesize = 1000;

            // Number of elements in the queue at the beginning for streaming tests
            int qsize = 1000;
            {
                Queue<int> g3dQ = new Queue<int>();
                // Documented adaptation: std::deque -> List<int>.
                List<int> stdQ = new List<int>();

                for (int i = 0; i < qsize; ++i)
                {
                    g3dQ.pushBack(i);
                    stdQ.Add(i);
                }

                // Run many times to filter out startup behavior
                for (int j = 0; j < 3; ++j)
                {
                    System.beginCycleCount(ref g3dStreamSmall);
                    {
                        for (int i = 0; i < iterations; ++i)
                        {
                            g3dQ.pushBack(g3dQ.popFront());
                        }
                    }
                    System.endCycleCount(ref g3dStreamSmall);

                    System.beginCycleCount(ref stdStreamSmall);
                    {
                        for (int i = 0; i < iterations; ++i)
                        {
                            int v = stdQ[0];
                            stdQ.RemoveAt(0);
                            stdQ.Add(v);
                        }
                    }
                    System.endCycleCount(ref stdStreamSmall);
                }
            }
            {
                Queue<int> g3dQ = new Queue<int>();
                System.beginCycleCount(ref g3dEnquequeFSmall);
                {
                    for (int i = 0; i < enqueuesize; ++i)
                    {
                        g3dQ.pushFront(i);
                    }
                }
                System.endCycleCount(ref g3dEnquequeFSmall);

                List<int> stdQ = new List<int>();
                System.beginCycleCount(ref stdEnquequeFSmall);
                {
                    for (int i = 0; i < enqueuesize; ++i)
                    {
                        stdQ.Insert(0, i);
                    }
                }
                System.endCycleCount(ref stdEnquequeFSmall);
            }
            {
                Queue<int> g3dQ = new Queue<int>();
                System.beginCycleCount(ref g3dEnquequeBSmall);
                {
                    for (int i = 0; i < enqueuesize; ++i)
                    {
                        g3dQ.pushBack(i);
                    }
                }
                System.endCycleCount(ref g3dEnquequeBSmall);

                List<int> stdQ = new List<int>();
                System.beginCycleCount(ref stdEnquequeBSmall);
                {
                    for (int i = 0; i < enqueuesize; ++i)
                    {
                        stdQ.Add(i);
                    }
                }
                System.endCycleCount(ref stdEnquequeBSmall);
            }

            /////////////////////////////////
            {
                Queue<BigE> g3dQ = new Queue<BigE>();
                List<BigE> stdQ = new List<BigE>();

                for (int i = 0; i < qsize; ++i)
                {
                    BigE v = new BigE(true);
                    g3dQ.pushBack(v);
                    stdQ.Add(v);
                }

                // Run many times to filter out startup behavior
                for (int j = 0; j < 3; ++j)
                {
                    BigE v = stdQ[0];

                    System.beginCycleCount(ref g3dStreamLarge);
                    {
                        for (int i = 0; i < iterations; ++i)
                        {
                            g3dQ.popFront();
                            g3dQ.pushBack(v);
                        }
                    }
                    System.endCycleCount(ref g3dStreamLarge);

                    System.beginCycleCount(ref stdStreamLarge);
                    {
                        for (int i = 0; i < iterations; ++i)
                        {
                            stdQ.RemoveAt(0);
                            stdQ.Add(v);
                        }
                    }
                    System.endCycleCount(ref stdStreamLarge);
                }
            }
            {
                Queue<BigE> g3dQ = new Queue<BigE>();
                System.beginCycleCount(ref g3dEnquequeFLarge);
                {
                    BigE v = new BigE(true);
                    for (int i = 0; i < enqueuesize; ++i)
                    {
                        g3dQ.pushFront(v);
                    }
                }
                System.endCycleCount(ref g3dEnquequeFLarge);

                List<BigE> stdQ = new List<BigE>();
                System.beginCycleCount(ref stdEnquequeFLarge);
                {
                    BigE v = new BigE(true);
                    for (int i = 0; i < enqueuesize; ++i)
                    {
                        stdQ.Insert(0, v);
                    }
                }
                System.endCycleCount(ref stdEnquequeFLarge);
            }
            {
                Queue<BigE> g3dQ = new Queue<BigE>();
                System.beginCycleCount(ref g3dEnquequeBLarge);
                BigE v = new BigE(true);
                for (int i = 0; i < enqueuesize; ++i)
                {
                    g3dQ.pushBack(v);
                }
                System.endCycleCount(ref g3dEnquequeBLarge);

                List<BigE> stdQ = new List<BigE>();
                System.beginCycleCount(ref stdEnquequeBLarge);
                {
                    BigE v2 = new BigE(true);
                    for (int i = 0; i < enqueuesize; ++i)
                    {
                        stdQ.Add(v2);
                    }
                }
                System.endCycleCount(ref stdEnquequeBLarge);
            }

            printf(" Pile-up push front cycles per elt (max queue size = %d)\n", enqueuesize);
            printf("  G3D::Queue<int>             %5.02f\n", g3dEnquequeFSmall / (float)enqueuesize);
            printf("  std::deque<int>             %5.02f\n", stdEnquequeFSmall / (float)enqueuesize);
            printf("  G3D::Queue<BigE>            %5.02f\n", g3dEnquequeFLarge / (float)enqueuesize);
            printf("  std::deque<BigE>            %5.02f\n", stdEnquequeFLarge / (float)enqueuesize);
            printf("\n");

            printf(" Pile-up push back cycles per elt (max queue size = %d)\n", enqueuesize);
            printf("  G3D::Queue<int>             %5.02f\n", g3dEnquequeBSmall / (float)enqueuesize);
            printf("  std::deque<int>             %5.02f\n", stdEnquequeBSmall / (float)enqueuesize);
            printf("  G3D::Queue<BigE>            %5.02f\n", g3dEnquequeBLarge / (float)enqueuesize);
            printf("  std::deque<BigE>            %5.02f\n", stdEnquequeBLarge / (float)enqueuesize);
            printf("\n");

            printf(" Streaming cycles per iteration (queue size = %d)\n", qsize);
            printf("  G3D::Queue<int>             %5.02f\n", g3dStreamSmall / (float)iterations);
            printf("  std::deque<int>             %5.02f\n", stdStreamSmall / (float)iterations);
            printf("  G3D::Queue<BigE>            %5.02f\n", g3dStreamLarge / (float)iterations);
            printf("  std::deque<BigE>            %5.02f\n", stdStreamLarge / (float)iterations);

            printf("\n\n");
        }

        private static string makeMessage(Queue<int> q)
        {
            string s = "Expected [ ";
            for (int i = 0; i < q.size(); ++i)
            {
                s = s + Format.format("%d ", i);
            }
            s = s + "], got [";
            for (int i = 0; i < q.size(); ++i)
            {
                s = s + Format.format("%d ", q[i]);
            }
            return s + "]";
        }

        private static void checkQueue(Queue<int> q)
        {
            for (int i = 0; i < q.size(); ++i)
            {
                Debug.debugAssertM(q[i] == (i + 1),
                    makeMessage(q));
            }
        }

        private static void testQueueCopy()
        {
            Queue<int> q1 = new Queue<int>();
            Queue<int> q2 = new Queue<int>();
            for (int i = 0; i < 10; ++i)
            {
                q1.pushBack(i);
            }

            q2.copyFrom(q1);

            for (int i = 0; i < 10; ++i)
            {
                Debug.debugAssert(q2[i] == q1[i]);
            }
        }

        private static void testQueue()
        {
            printf("Queue ");

            testQueueCopy();

            {
                Queue<int> q = new Queue<int>();
                q.pushFront(3);
                q.pushFront(2);
                q.pushFront(1);
                checkQueue(q);
            }

            {
                Queue<int> q = new Queue<int>();
                q.pushBack(1);
                q.pushBack(2);
                q.pushBack(3);
                checkQueue(q);
            }

            {
                Queue<int> q = new Queue<int>();
                q.pushFront(2);
                q.pushFront(1);
                q.pushBack(3);
                checkQueue(q);
            }

            {
                Queue<int> q = new Queue<int>();
                q.pushFront(2);
                q.pushBack(3);
                q.pushFront(1);
                checkQueue(q);
            }

            {
                Queue<int> q = new Queue<int>();
                q.pushBack(2);
                q.pushFront(1);
                q.pushBack(3);
                checkQueue(q);
            }

            {
                Queue<int> q = new Queue<int>();
                q.pushBack(-1);
                q.pushBack(1);
                q.pushBack(2);
                q.pushBack(3);
                q.pushBack(-1);

                q.popFront();
                q.popBack();
                checkQueue(q);
            }

            {
                Queue<int> q = new Queue<int>();
                q.pushBack(1);
                q.pushBack(2);
                q.pushBack(3);
                q.pushBack(-1);

                q.popBack();
                checkQueue(q);
            }
            {
                Queue<int> q = new Queue<int>();
                q.pushBack(-1);
                q.pushBack(1);
                q.pushBack(2);
                q.pushBack(3);

                q.popFront();
                checkQueue(q);
            }

            // Sanity check queue copying.
            {
                Queue<int> q = new Queue<int>();
                q.pushBack(1);
                q.pushBack(2);
                q.pushBack(3);

                checkQueue(q);

                Queue<int> r = new Queue<int>(q);
                checkQueue(r);
            }

            printf("succeeded\n");
        }
    }
}
