using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void perfTextOutput()
        {
            printf("TextOutput\n");

            // printf
            {
                TextOutput t = new TextOutput(new TextOutput.Options());

                ulong tt = 0, tf = 0, tp = 0;
                const int N = 5000;
                string s = "";

                // Documented adaptation: sprintf baseline uses String.Format,
                // which is the closest managed equivalent to C sprintf into a
                // fixed buffer.
                System.beginCycleCount(ref tp);
                for (int i = 0; i < N; ++i)
                {
                    s = string.Format("{0}, {1}, {2}\n", i, i + 1, i + 2);
                }
                System.endCycleCount(ref tp);

                System.beginCycleCount(ref tf);
                for (int i = 0; i < N; ++i)
                {
                    s = Format.format("%d, %d, %d\n", i, i + 1, i + 2);
                }
                System.endCycleCount(ref tf);

                System.beginCycleCount(ref tt);
                for (int i = 0; i < N; ++i)
                {
                    t.printf("%d, %d, %d\n", i, i + 1, i + 2);
                }
                System.endCycleCount(ref tt);
                t.commitString(ref s);

                int k = 3;
                printf(" Cycles to print int32\n");
                printf("   sprintf                    %g\n", (double)tp / (k * N));
                printf("   format                     %g\n", (double)tf / (k * N));
                printf("   TextOutput::printf         %g\n", (double)tt / (k * N));
                printf("\n");
            }
            printf("\n\n");
        }
    }
}
