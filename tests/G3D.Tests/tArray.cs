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
        private struct Big
        {
            public int x;
        }

        private static void testSort()
        {
            printf("Array::Sort\n");

            {
                Array<int> array = new Array<int>();
                array.append(12, 7, 1);
                array.append(2, 3, 10);

                array.sort();

                Debug.debugAssert(array[0] == 1);
                Debug.debugAssert(array[1] == 2);
                Debug.debugAssert(array[2] == 3);
                Debug.debugAssert(array[3] == 7);
                Debug.debugAssert(array[4] == 10);
                Debug.debugAssert(array[5] == 12);
            }

            {
                Array<int> array = new Array<int>();
                array.append(12, 7, 1);
                array.append(2, 3, 10);

                array.sortSubArray(0, 2);

                Debug.debugAssert(array[0] == 1);
                Debug.debugAssert(array[1] == 7);
                Debug.debugAssert(array[2] == 12);
                Debug.debugAssert(array[3] == 2);
                Debug.debugAssert(array[4] == 3);
                Debug.debugAssert(array[5] == 10);
            }
        }

        private static void perfArray()
        {
            printf("Array Performance:\n");

            // Note:
            //
            // std::vector calls the copy constructor for new elements and always calls the
            // constructor even when it doesn't exist (e.g., for int).  This makes its alloc
            // time much worse than other methods, but gives it a slight boost on the first
            // memory access because everything is in cache.  These tests work on huge arrays
            // to amortize that effect down.

            {
                // Measure time for short array allocation
                ulong vectorAllocBig = 0, vectorAllocSmall = 0;
                ulong arrayAllocBig = 0, arrayAllocSmall = 0;

                const int M = 3000;

                // Run many times to filter out startup behavior
                for (int j = 0; j < 3; ++j)
                {
                    System.beginCycleCount(ref vectorAllocBig);
                    for (int i = 0; i < M; ++i)
                    {
                        List<Big> v = new List<Big>(4);
                    }
                    System.endCycleCount(ref vectorAllocBig);

                    System.beginCycleCount(ref vectorAllocSmall);
                    for (int i = 0; i < M; ++i)
                    {
                        List<int> v = new List<int>(4);
                    }
                    System.endCycleCount(ref vectorAllocSmall);

                    System.beginCycleCount(ref arrayAllocBig);
                    for (int i = 0; i < M; ++i)
                    {
                        Array<Big> v = new Array<Big>(4);
                    }
                    System.endCycleCount(ref arrayAllocBig);

                    System.beginCycleCount(ref arrayAllocSmall);
                    for (int i = 0; i < M; ++i)
                    {
                        Array<int> v = new Array<int>(4);
                    }
                    System.endCycleCount(ref arrayAllocSmall);
                }

                printf(" Array cycles/alloc for short arrays\n\n");
                printf("                           Big class           int      outcome\n");
                bool G3Dwin =
                    (arrayAllocBig <= vectorAllocBig * 1.1) &&
                    (arrayAllocSmall <= vectorAllocSmall * 1.1);

                printf("  G3D::Array               %9.02f     %9.02f     %s\n",
                       (double)arrayAllocBig / M, (double)arrayAllocSmall / M,
                       G3Dwin ? " ok " : "FAIL");
                printf("  std::vector              %9.02f     %9.02f\n",
                       (double)vectorAllocBig / M, (double)vectorAllocSmall / M);
                printf("\n\n");
            }

            ////////////////////////////////////////////////////
            // Measure time for array resize
            ulong vectorResizeBig = 0, vectorResizeSmall = 0;
            ulong arrayResizeBig = 0, arrayResizeSmall = 0;
            ulong mallocResizeBig = 0, mallocResizeSmall = 0;

            const int M2 = 2000;

            //  Low and high bounds for resize tests
            const int L = 1;
            const int H = M2 + L;

            // Run many times to filter out startup behavior
            for (int j = 0; j < 3; ++j)
            {
                System.beginCycleCount(ref vectorResizeBig);
                {
                    List<Big> array = new List<Big>();
                    for (int i = L; i < H; ++i)
                    {
                        while (array.Count < i)
                        {
                            array.Add(default(Big));
                        }
                    }
                }
                System.endCycleCount(ref vectorResizeBig);

                System.beginCycleCount(ref vectorResizeSmall);
                {
                    List<int> array = new List<int>();
                    for (int i = L; i < H; ++i)
                    {
                        while (array.Count < i)
                        {
                            array.Add(0);
                        }
                    }
                }
                System.endCycleCount(ref vectorResizeSmall);

                System.beginCycleCount(ref arrayResizeSmall);
                {
                    Array<int> array = new Array<int>();
                    for (int i = L; i < H; ++i)
                    {
                        array.resize(i, false);
                    }
                }
                System.endCycleCount(ref arrayResizeSmall);

                System.beginCycleCount(ref arrayResizeBig);
                {
                    Array<Big> array = new Array<Big>();
                    for (int i = L; i < H; ++i)
                    {
                        array.resize(i, false);
                    }
                }
                System.endCycleCount(ref arrayResizeBig);

                System.beginCycleCount(ref mallocResizeBig);
                {
                    byte[] array = null;
                    for (int i = L; i < H; ++i)
                    {
                        array = System.realloc(array, 404 * i);
                    }
                    System.free(array);
                }
                System.endCycleCount(ref mallocResizeBig);

                System.beginCycleCount(ref mallocResizeSmall);
                {
                    byte[] array = null;
                    for (int i = L; i < H; ++i)
                    {
                        array = System.realloc(array, 4 * i);
                    }
                    System.free(array);
                }
                System.endCycleCount(ref mallocResizeSmall);
            }

            {
                printf(" Array cycles/resize (%d resizes)\n\n", M2);
                printf("                           Big class           int     outcome\n");
                bool G3Dwin =
                    (arrayResizeBig <= vectorResizeBig * 1.1) &&
                    (arrayResizeSmall <= vectorResizeSmall * 1.1);

                printf("  G3D::Array               %9.02f     %9.02f     %s\n",
                       (double)arrayResizeBig / M2, (double)arrayResizeSmall / M2,
                       G3Dwin ? " ok " : "FAIL");
                printf("  std::vector              %9.02f     %9.02f\n", (double)vectorResizeBig / M2, (double)vectorResizeSmall / M2);
                printf("  realloc(*)               %9.02f     %9.02f\n", (double)mallocResizeBig / M2, (double)mallocResizeSmall / M2);
                printf("    * does not call constructor or destructor!\n\n");
            }

            // Measure times for various operations on large arrays of small elements
            ulong newAllocInt = 0, newFreeInt = 0, newAccessInt = 0;
            ulong arrayAllocInt = 0, arrayFreeInt = 0, arrayAccessInt = 0;
            ulong vectorAllocInt = 0, vectorFreeInt = 0, vectorAccessInt = 0;
            ulong mallocAllocInt = 0, mallocFreeInt = 0, mallocAccessInt = 0;
            ulong sysmallocAllocInt = 0, sysmallocFreeInt = 0, sysmallocAccessInt = 0;

            // 1 million
            int size = 1000000;

            // Run many times to filter out startup behavior
            for (int j = 0; j < 3; ++j)
            {
                System.beginCycleCount(ref mallocAllocInt);
                {
                    byte[] array = System.malloc(4 * size);
                    System.endCycleCount(ref mallocAllocInt);
                    System.beginCycleCount(ref mallocAccessInt);
                    // Documented adaptation: safe C# cannot index a raw
                    // malloc buffer as int*; the access phase runs on the
                    // byte view with one 4-byte store per element.
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, i);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, readInt32(array, i * 4) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, readInt32(array, i * 4) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, readInt32(array, i * 4) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, readInt32(array, i * 4) + 1);
                        }
                    }
                    System.endCycleCount(ref mallocAccessInt);
                    System.beginCycleCount(ref mallocFreeInt);
                    System.free(array);
                }
                System.endCycleCount(ref mallocFreeInt);

                System.beginCycleCount(ref sysmallocAllocInt);
                {
                    byte[] array = System.alignedMalloc(4 * size, 4096);
                    System.endCycleCount(ref sysmallocAllocInt);
                    System.beginCycleCount(ref sysmallocAccessInt);
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, i);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, readInt32(array, i * 4) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, readInt32(array, i * 4) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, readInt32(array, i * 4) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 4, readInt32(array, i * 4) + 1);
                        }
                    }
                    System.endCycleCount(ref sysmallocAccessInt);
                    System.beginCycleCount(ref sysmallocFreeInt);
                    System.alignedFree(array);
                }
                System.endCycleCount(ref sysmallocFreeInt);

                System.beginCycleCount(ref arrayAllocInt);
                {
                    Array<int> array = new Array<int>(size);
                    System.endCycleCount(ref arrayAllocInt);
                    System.beginCycleCount(ref arrayAccessInt);
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = i;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                    }
                    System.endCycleCount(ref arrayAccessInt);
                    System.beginCycleCount(ref arrayFreeInt);
                }
                System.endCycleCount(ref arrayFreeInt);

                {
                    System.beginCycleCount(ref newAllocInt);
                    int[] array = new int[size];
                    System.endCycleCount(ref newAllocInt);
                    System.beginCycleCount(ref newAccessInt);
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = i;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                    }
                    System.endCycleCount(ref newAccessInt);

                    System.beginCycleCount(ref newFreeInt);
                    array = null;
                }
                System.endCycleCount(ref newFreeInt);

                System.beginCycleCount(ref vectorAllocInt);
                {
                    List<int> array = new List<int>(size);
                    for (int k = 0; k < size; ++k)
                    {
                        array.Add(0);
                    }
                    System.endCycleCount(ref vectorAllocInt);
                    System.beginCycleCount(ref vectorAccessInt);
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = i;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            array[i] = array[i] + 1;
                        }
                    }
                    System.endCycleCount(ref vectorAccessInt);
                    System.beginCycleCount(ref vectorFreeInt);
                }
                System.endCycleCount(ref vectorFreeInt);
            }

            {
                // Number of memory ops per element that LOOPS performed
                float N = 9 * 3;

                printf(" Int array cycles/elt\n");
                printf("                             Alloc    Access   Free\n");
                printf("  G3D::Array                 %5.02f    %5.02f   %5.02f\n", (arrayAllocInt / (float)size), (arrayAccessInt / (float)(N * size)), (arrayFreeInt / (float)size));
                printf("  std::vector                %5.02f    %5.02f   %5.02f\n", (vectorAllocInt / (float)size), (vectorAccessInt / (float)(N * size)), (vectorFreeInt / (float)size));
                printf("  new/delete                 %5.02f    %5.02f   %5.02f\n", (newAllocInt / (float)size), (newAccessInt / (float)(N * size)), (newFreeInt / (float)size));
                printf("  malloc/free                %5.02f    %5.02f   %5.02f\n", (mallocAllocInt / (float)size), (mallocAccessInt / (float)(N * size)), (mallocFreeInt / (float)size));
                printf("  System::alignedMalloc      %5.02f    %5.02f   %5.02f\n", (sysmallocAllocInt / (float)size), (sysmallocAccessInt / (float)(N * size)), (sysmallocFreeInt / (float)size));
                printf("\n");
            }

            ///////////////////////////////////////////////////////
            // 100 thousand
            size = 100000;

            // Measure times for various operations on large arrays of small elements
            ulong newAllocBig2 = 0, newFreeBig2 = 0, newAccessBig2 = 0;
            ulong arrayAllocBig2 = 0, arrayFreeBig2 = 0, arrayAccessBig2 = 0;
            ulong vectorAllocBig2 = 0, vectorFreeBig2 = 0, vectorAccessBig2 = 0;
            ulong mallocAllocBig2 = 0, mallocFreeBig2 = 0, mallocAccessBig2 = 0;
            ulong sysmallocAllocBig2 = 0, sysmallocFreeBig2 = 0, sysmallocAccessBig2 = 0;
            // Run many times to filter out startup behavior
            for (int j = 0; j < 3; ++j)
            {
                System.beginCycleCount(ref mallocAllocBig2);
                {
                    byte[] array = System.malloc(404 * size);
                    System.endCycleCount(ref mallocAllocBig2);
                    System.beginCycleCount(ref mallocAccessBig2);
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, i);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, readInt32(array, i * 404) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, readInt32(array, i * 404) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, readInt32(array, i * 404) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, readInt32(array, i * 404) + 1);
                        }
                    }
                    System.endCycleCount(ref mallocAccessBig2);
                    System.beginCycleCount(ref mallocFreeBig2);
                    System.free(array);
                }
                System.endCycleCount(ref mallocFreeBig2);

                System.beginCycleCount(ref sysmallocAllocBig2);
                {
                    byte[] array = System.alignedMalloc(404 * size, 4096);
                    System.endCycleCount(ref sysmallocAllocBig2);
                    System.beginCycleCount(ref sysmallocAccessBig2);
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, i);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, readInt32(array, i * 404) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, readInt32(array, i * 404) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, readInt32(array, i * 404) + 1);
                        }
                        for (i = 0; i < size; ++i)
                        {
                            writeInt32(array, i * 404, readInt32(array, i * 404) + 1);
                        }
                    }
                    System.endCycleCount(ref sysmallocAccessBig2);
                    System.beginCycleCount(ref sysmallocFreeBig2);
                    System.alignedFree(array);
                }
                System.endCycleCount(ref sysmallocFreeBig2);

                System.beginCycleCount(ref arrayAllocBig2);
                {
                    Array<Big> array = new Array<Big>(size);
                    System.endCycleCount(ref arrayAllocBig2);
                    System.beginCycleCount(ref arrayAccessBig2);
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = i;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                    }
                    System.endCycleCount(ref arrayAccessBig2);
                    System.beginCycleCount(ref arrayFreeBig2);
                }
                System.endCycleCount(ref arrayFreeBig2);

                {
                    System.beginCycleCount(ref newAllocBig2);
                    Big[] array = new Big[size];
                    System.endCycleCount(ref newAllocBig2);
                    System.beginCycleCount(ref newAccessBig2);
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = i;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                    }
                    System.endCycleCount(ref newAccessBig2);

                    System.beginCycleCount(ref newFreeBig2);
                    array = null;
                }
                System.endCycleCount(ref newFreeBig2);

                System.beginCycleCount(ref vectorAllocBig2);
                {
                    List<Big> array = new List<Big>(size);
                    while (array.Count < size)
                    {
                        array.Add(default(Big));
                    }
                    System.endCycleCount(ref vectorAllocBig2);
                    System.beginCycleCount(ref vectorAccessBig2);
                    for (int k = 0; k < 3; ++k)
                    {
                        int i;
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = i;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                        for (i = 0; i < size; ++i)
                        {
                            Big e = array[i];
                            e.x = e.x + 1;
                            array[i] = e;
                        }
                    }
                    System.endCycleCount(ref vectorAccessBig2);
                    System.beginCycleCount(ref vectorFreeBig2);
                }
                System.endCycleCount(ref vectorFreeBig2);
            }

            {
                // Number of memory ops per element that LOOPS performed (3 loops * (5 writes + 4 reads))
                float N = 9 * 3;

                printf(" Big class array cycles/elt\n");
                printf("                             Alloc    Access   Free\n");
                printf("  G3D::Array               %7.02f    %5.02f   %5.02f\n", (arrayAllocBig2 / (float)size), (arrayAccessBig2 / (float)(N * size)), (arrayFreeBig2 / (float)size));
                printf("  std::vector              %7.02f    %5.02f   %5.02f\n", (vectorAllocBig2 / (float)size), (vectorAccessBig2 / (float)(N * size)), (vectorFreeBig2 / (float)size));
                printf("  new/delete               %7.02f    %5.02f   %5.02f\n", (newAllocBig2 / (float)size), (newAccessBig2 / (float)(N * size)), (newFreeBig2 / (float)size));
                printf("  malloc/free(*)           %7.02f    %5.02f   %5.02f\n", (mallocAllocBig2 / (float)size), (mallocAccessBig2 / (float)(N * size)), (mallocFreeBig2 / (float)size));
                printf("  System::alignedMalloc(*) %7.02f    %5.02f   %5.02f\n", (sysmallocAllocBig2 / (float)size), (sysmallocAccessBig2 / (float)(N * size)), (sysmallocFreeBig2 / (float)size));
                printf("    * does not call constructor or destructor!\n\n");
            }

            printf("\n");
        }

        private static void writeInt32(byte[] b, int offset, int v)
        {
            b[offset] = (byte)v;
            b[offset + 1] = (byte)(v >> 8);
            b[offset + 2] = (byte)(v >> 16);
            b[offset + 3] = (byte)(v >> 24);
        }

        private static int readInt32(byte[] b, int offset)
        {
            return b[offset] | (b[offset + 1] << 8) | (b[offset + 2] << 16) | (b[offset + 3] << 24);
        }

        private static void testArray()
        {
            printf("G3D::Array  ");
            testSort();
            printf("passed\n");
        }
    }
}
