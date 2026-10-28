using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        static void testSerialize()
        {
            AABSPTree<Vector3> tree = new AABSPTree<Vector3>();
            int N = 1000;

            for (int i = 0; i < N; ++i)
            {
                tree.insert(Vector3.random());
            }
            tree.balance();

            // Save the structure
            string tempFile = "test-bsp.dat";
            BinaryOutput b = new BinaryOutput(tempFile, G3DEndian.G3D_LITTLE_ENDIAN);
            tree.serializeStructure(b);
            b.commit(false);
        }

        static void testBoxIntersect()
        {
            AABSPTree<Vector3> tree = new AABSPTree<Vector3>();

            // Make a tree containing a regular grid of points
            for (int x = -5; x <= 5; ++x)
            {
                for (int y = -5; y <= 5; ++y)
                {
                    for (int z = -5; z <= 5; ++z)
                    {
                        tree.insert(new Vector3(x, y, z));
                    }
                }
            }
            tree.balance();

            AABox box = new AABox(new Vector3(-1.5f, -1.5f, -1.5f), new Vector3(1.5f, 1.5f, 1.5f));

            AABSPTree<Vector3>.BoxIntersectionIterator it = tree.beginBoxIntersection(box);
            AABSPTree<Vector3>.BoxIntersectionIterator end = tree.endBoxIntersection();

            int hits = 0;
            while (!it.isEndValue())
            {
                Vector3 v = it.Current;

                Debug.debugAssert(box.contains(v));

                ++hits;
                it.MoveNext();
            }

            Debug.debugAssertM(
                hits == 3 * 3 * 3,
                "Wrong number of intersections found in testBoxIntersect for AABSPTree"
            );
        }

        private static void testAABSPTree()
        {
            testBoxIntersect();
            testSerialize();
            printf("AABSPTree passed\n");
        }

        private static void perfAABSPTree()
        {
            Array<AABox> array = new Array<AABox>();
            AABSPTree<AABox> tree = new AABSPTree<AABox>();

            const int NUM_POINTS = 100000;

            for (int i = 0; i < NUM_POINTS; ++i)
            {
                Vector3 pt = new Vector3(
                    (float)g3dmath.random(-10.0f, 10.0f),
                    (float)g3dmath.random(-10.0f, 10.0f),
                    (float)g3dmath.random(-10.0f, 10.0f)
                );
                AABox box = new AABox(pt, pt + new Vector3(.1f, .1f, .1f));
                array.append(box);
                tree.insert(box);
            }

            tree.balance();

            UInt64 bspcount = 0,
                arraycount = 0,
                boxcount = 0;

            // Run twice to get cache issues out of the way
            for (int it = 0; it < 2; ++it)
            {
                Array<Plane> plane = new Array<Plane>();
                plane.append(new Plane(new Vector3(-1, 0, 0), new Vector3(3, 1, 1)));
                plane.append(new Plane(new Vector3(1, 0, 0), new Vector3(1, 1, 1)));
                plane.append(new Plane(new Vector3(0, 0, -1), new Vector3(1, 1, 3)));
                plane.append(new Plane(new Vector3(0, 0, 1), new Vector3(1, 1, 1)));
                plane.append(new Plane(new Vector3(0, -1, 0), new Vector3(1, 3, 1)));
                plane.append(new Plane(new Vector3(0, 1, 0), new Vector3(1, -3, 1)));

                AABox box = new AABox(new Vector3(1, 1, 1), new Vector3(3, 3, 3));

                Array<AABox> point = new Array<AABox>();

                System.beginCycleCount(ref bspcount);
                tree.getIntersectingMembers(plane, point);
                System.endCycleCount(ref bspcount);

                point.clear();

                System.beginCycleCount(ref boxcount);
                tree.getIntersectingMembers(box, point);
                System.endCycleCount(ref boxcount);

                point.clear();

                System.beginCycleCount(ref arraycount);
                for (int i = 0; i < array.size(); ++i)
                {
                    int dummy = 0;
                    if (!array[i].culledBy(plane, dummy, uint.MaxValue))
                    {
                        point.append(array[i]);
                    }
                }
                System.endCycleCount(ref arraycount);
            }

            printf(
                "AABSPTree<AABox>::getIntersectingMembers(plane) %g Mcycles\n"
                    + "AABSPTree<AABox>::getIntersectingMembers(box)   %g Mcycles\n"
                    + "Culled by on Array<AABox>                       %g Mcycles\n\n",
                bspcount / 1e6,
                boxcount / 1e6,
                arraycount / 1e6
            );
        }
    }
}
