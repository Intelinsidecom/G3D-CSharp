using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void perfSystemMemcpy()
        {
            printf("----------------------------------------------------------\n");

            // Number of memory sizes to test
            const int M = 8;

            //  Repeats per memory size
            const int trials = 200;

            int[] size = new int[M];
            for (int i = 0; i < M; ++i)
            {
                size[i] = (int)(1024 * Math.Pow(i + 1, 4));
            }

            printf("System::memcpy Performance:\n");
            printf("  Measured in cycles/kb at various copy sizes\n\n");
            ulong[] native = new ulong[M];
            ulong[] g3d = new ulong[M];

            for (int m = 0; m < M; ++m)
            {
                int n = size[m];
                byte[] m1 = System.alignedMalloc(n, 1024 * 4);
                byte[] m2 = System.alignedMalloc(n, 1024 * 4);

                // First iteration just primes the system
                System.memcpy(m1, m2, n);
                System.beginCycleCount(ref native[m]);
                for (int j = 0; j < trials; ++j)
                {
                    System.memcpy(m1, m2, n);
                }
                System.endCycleCount(ref native[m]);

                System.memcpy(m1, m2, n);
                System.beginCycleCount(ref g3d[m]);
                for (int j = 0; j < trials; ++j)
                {
                    System.memcpy(m1, m2, n);
                }
                System.endCycleCount(ref g3d[m]);

                System.alignedFree(m1);
                System.alignedFree(m2);
            }

            printf("         Size       ");
            for (int i = 0; i < M; ++i)
            {
                printf("%6uk", size[i] / 1024);
            }
            printf("\n");

            printf("    ::memcpy        ");
            for (int m = 0; m < M; ++m)
            {
                double k = trials * (double)size[m] / 1024;
                printf(" %6d", (int)(native[m] / k));
            }
            printf("\n");

            printf("    System::memcpy* ");
            for (int m = 0; m < M; ++m)
            {
                double k = trials * (double)size[m] / 1024;
                printf(" %6d", (int)(g3d[m] / k));
            }
            printf("\n");

            if (System.hasSSE2() && System.hasMMX())
            {
                printf("      * MMX on this machine\n");
            }
            else if (System.hasSSE() && System.hasMMX())
            {
                printf("      * MMX on this machine\n");
            }
            else
            {
                printf("      * memcpy on this machine\n");
            }
            printf("\n");
        }

        private static void testSystemMemcpy()
        {
            printf("System::memcpy ");
            const int k = 50000;
            byte[] a = new byte[k];
            byte[] b = new byte[k];

            int i;

            for (i = 0; i < k; ++i)
            {
                a[i] = (byte)(i & 255);
            }

            System.memcpy(b, a, k);

            for (i = 0; i < k; ++i)
            {
                Debug.debugAssert(b[i] == (i & 255));
                Debug.debugAssert(a[i] == (i & 255));
            }
            printf("passed\n");
        }
    }
}
