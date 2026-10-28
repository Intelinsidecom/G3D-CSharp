using System;
using global::System.Collections.Generic;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private class TableKey
        {
            public int value;

            public override bool Equals(object obj)
            {
                TableKey other = obj as TableKey;
                return (other != null) && (value == other.value);
            }

            public override int GetHashCode()
            {
                return value;
            }
        }

        private class TableKeyB
        {
            public int value;

            public override bool Equals(object obj)
            {
                TableKeyB other = obj as TableKeyB;
                return (other != null) && (value == other.value);
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        private static void testTable()
        {
            printf("G3D::Table  ");
            // Basic get/set
            {
                Table<int, int> table = new Table<int, int>();

                table.set(10, 20);
                table.set(3, 1);
                table.set(1, 4);

                Debug.debugAssert(table[10] == 20);
                Debug.debugAssert(table[3] == 1);
                Debug.debugAssert(table[1] == 4);
                Debug.debugAssert(table.containsKey(10));
                Debug.debugAssert(!table.containsKey(0));

                Debug.debugAssert(table.debugGetDeepestBucketSize() == 1);
            }

            // Test overloaded pointer hashing
            // Documented adaptation: C++ hashed TableKey* pointer identity; C#
            // uses class instance identity via EqualityComparer, with
            // GetHashCode overridden for bucket distribution.
            {
                TableKey[] x = new TableKey[4];
                for (int i = 0; i < 4; ++i)
                {
                    x[i] = new TableKey();
                }
                x[0].value = 1;
                x[1].value = 2;
                x[2].value = 3;
                x[3].value = 4;
                Table<TableKey, int> table = new Table<TableKey, int>();

                table.set(x[0], 10);
                table.set(x[1], 20);
                table.set(x[2], 30);
                table.set(x[3], 40);
                Debug.debugAssert(table[x[0]] == 10);
                Debug.debugAssert(table[x[1]] == 20);
                Debug.debugAssert(table[x[2]] == 30);
                Debug.debugAssert(table[x[3]] == 40);

                // Our current implementation should not have collisions in this case.
                Debug.debugAssert(table.debugGetDeepestBucketSize() == 1);
            }

            {
                TableKeyB[] x = new TableKeyB[6];
                for (int i = 0; i < 6; ++i)
                {
                    x[i] = new TableKeyB();
                }
                Table<TableKeyB, int> table = new Table<TableKeyB, int>();
                for (int i = 0; i < 6; ++i)
                {
                    x[i].value = i;
                    table.set(x[i], i);
                }

                Debug.debugAssert(table.size() == 6);
                Debug.debugAssert(table.debugGetDeepestBucketSize() == 6);
                Debug.debugAssert(table.debugGetNumBuckets() == 10);
            }

            printf("passed\n");
        }

        private static void perfTest<K, V>(string description, K[] keys, V[] vals, int M)
        {
            ulong tableSet = 0, tableGet = 0, tableRemove = 0;
            ulong mapSet = 0, mapGet = 0, mapRemove = 0;
            ulong hashMapSet = 0, hashMapGet = 0, hashMapRemove = 0;

            ulong overhead = 0;

            // Run many times to filter out startup behavior
            for (int j = 0; j < 3; ++j)
            {
                // There's a little overhead just for the loop and reading
                // the values from the arrays.  Take this into account when
                // counting cycles.
                System.beginCycleCount(ref overhead);
                {
                    K k;
                    V v;
                    for (int i = 0; i < M; ++i)
                    {
                        k = keys[i];
                        v = vals[i];
                    }
                }
                System.endCycleCount(ref overhead);

                {
                    Table<K, V> t = new Table<K, V>();
                    System.beginCycleCount(ref tableSet);
                    for (int i = 0; i < M; ++i)
                    {
                        t.set(keys[i], vals[i]);
                    }
                    System.endCycleCount(ref tableSet);

                    System.beginCycleCount(ref tableGet);
                    for (int i = 0; i < M; ++i)
                    {
                        V ignoredGet = t[keys[i]];
                    }
                    System.endCycleCount(ref tableGet);

                    System.beginCycleCount(ref tableRemove);
                    for (int i = 0; i < M; ++i)
                    {
                        t.remove(keys[i]);
                    }
                    System.endCycleCount(ref tableRemove);
                }

                /////////////////////////////////

                {
                    // Documented adaptation: std::map -> SortedDictionary; the
                    // printed label remains "std::map" as in the original.
                    SortedDictionary<K, V> t = new SortedDictionary<K, V>();
                    System.beginCycleCount(ref mapSet);
                    for (int i = 0; i < M; ++i)
                    {
                        t[keys[i]] = vals[i];
                    }
                    System.endCycleCount(ref mapSet);

                    System.beginCycleCount(ref mapGet);
                    for (int i = 0; i < M; ++i)
                    {
                        V ignoredMapGet = t[keys[i]];
                    }
                    System.endCycleCount(ref mapGet);

                    System.beginCycleCount(ref mapRemove);
                    for (int i = 0; i < M; ++i)
                    {
                        t.Remove(keys[i]);
                    }
                    System.endCycleCount(ref mapRemove);
                }

                /////////////////////////////////

                {
                    // Documented adaptation: hash_map -> Dictionary; the
                    // printed label remains "hash_map" as in the original.
                    Dictionary<K, V> t = new Dictionary<K, V>();
                    System.beginCycleCount(ref hashMapSet);
                    for (int i = 0; i < M; ++i)
                    {
                        t[keys[i]] = vals[i];
                    }
                    System.endCycleCount(ref hashMapSet);

                    System.beginCycleCount(ref hashMapGet);
                    for (int i = 0; i < M; ++i)
                    {
                        V ignoredHashGet = t[keys[i]];
                    }
                    System.endCycleCount(ref hashMapGet);

                    System.beginCycleCount(ref hashMapRemove);
                    for (int i = 0; i < M; ++i)
                    {
                        t.Remove(keys[i]);
                    }
                    System.endCycleCount(ref hashMapRemove);
                }
            }

            tableSet -= overhead;
            if (tableGet < overhead)
            {
                tableGet = 0;
            }
            else
            {
                tableGet -= overhead;
            }
            tableRemove -= overhead;

            mapSet -= overhead;
            mapGet -= overhead;
            mapRemove -= overhead;

            float N = M;
            printf("%s\n", description);
            bool G3Dwin =
                (tableSet <= mapSet) &&
                (tableGet <= mapGet) &&
                (tableRemove <= mapRemove);
            printf("Table         %9.1f  %9.1f  %9.1f   %s\n",
                   (float)tableSet / N, (float)tableGet / N, (float)tableRemove / N,
                   G3Dwin ? " ok " : "FAIL");
            printf("hash_map      %9.1f  %9.1f  %9.1f\n", (float)hashMapSet / N, (float)hashMapGet / N, (float)hashMapRemove / N);
            printf("std::map      %9.1f  %9.1f  %9.1f\n", (float)mapSet / N, (float)mapGet / N, (float)mapRemove / N);
            printf("\n");
        }

        private static void perfTable()
        {
            printf("                insert       fetch     remove    outcome\n");

            const int M = 300;
            {
                int[] keys = new int[M];
                int[] vals = new int[M];
                for (int i = 0; i < M; ++i)
                {
                    keys[i] = i * 2;
                    vals[i] = i;
                }
                perfTest<int, int>("int,int", keys, vals, M);
            }

            {
                string[] keys = new string[M];
                int[] vals = new int[M];
                for (int i = 0; i < M; ++i)
                {
                    keys[i] = Format.format("%d", i * 2);
                    vals[i] = i;
                }
                perfTest<string, int>("string, int", keys, vals, M);
            }

            {
                int[] keys = new int[M];
                string[] vals = new string[M];
                for (int i = 0; i < M; ++i)
                {
                    keys[i] = i * 2;
                    vals[i] = Format.format("%d", i);
                }
                perfTest<int, string>("int, string", keys, vals, M);
            }

            {
                string[] keys = new string[M];
                string[] vals = new string[M];
                for (int i = 0; i < M; ++i)
                {
                    keys[i] = Format.format("%d", i * 2);
                    vals[i] = Format.format("%d", i);
                }
                perfTest<string, string>("string, string", keys, vals, M);
            }
        }
    }
}
