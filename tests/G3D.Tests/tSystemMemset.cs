using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testSystemMemset()
        {
            printf("System::memset");
            {
                const int k = 100;
                byte[] a = new byte[k];

                int i;

                for (i = 0; i < k; ++i)
                {
                    a[i] = (byte)(i & 255);
                }

                System.memset(a, 4, k);

                for (i = 0; i < k; ++i)
                {
                    Debug.debugAssert(a[i] == 4);
                }
            }

            {
                // Test the internal debugAssertions
                for (int N = 100; N < 10000; N += 137)
                {
                    byte[] x = System.malloc(N);
                    System.memset(x, 0, N);
                    x = System.realloc(x, N * 2);
                    System.memset(x, 0, N * 2);
                    System.free(x);
                }
            }

            printf(" passed\n");
        }
    }
}
