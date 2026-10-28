using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testAABoxCulledBy()
        {
            const uint b00000000 = 0;
            const uint b00000001 = 1;
            const uint b00000010 = 2;
            const uint b00000011 = 3;
            const uint b00000100 = 4;
            const uint b00000101 = 5;
            const uint b00000110 = 6;
            const uint b00000111 = 7;
            const uint b00001000 = 8;

            // Avoid unused variable warnings
            // (void)b00000000; etc. - not needed in C#

            printf("AABox::culledBy\n");

            Array<Plane> planes = new Array<Plane>();

            // Planes at +/- 1
            planes.append(new Plane(new Vector3(-1, 0, 0), new Vector3(1, 0, 0)));
            planes.append(new Plane(new Vector3(1, 0, 0), new Vector3(-1, 0, 0)));

            AABox box = new AABox(new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 1, 1));

            uint parentMask, childMask;
            int index = 0;
            bool culled;

            // Contained case
            parentMask = uint.MaxValue; childMask = 0; index = 0;
            culled = box.culledBy(planes, ref index, parentMask, out childMask);
            Debug.debugAssert(index == -1);
            Debug.debugAssert(!culled);
            Debug.debugAssert(childMask == b00000000);

            // Positive straddle
            box = new AABox(new Vector3(0.5f, 0, 0), new Vector3(1.5f, 1, 1));
            parentMask = uint.MaxValue; childMask = 0; index = 0;
            culled = box.culledBy(planes, ref index, parentMask, out childMask);
            Debug.debugAssert(index == -1);
            Debug.debugAssert(!culled);
            Debug.debugAssert(childMask == b00000001);

            // Negative straddle
            box = new AABox(new Vector3(-1.5f, 0, 0), new Vector3(0.5f, 1, 1));
            parentMask = uint.MaxValue; childMask = 0; index = 0;
            culled = box.culledBy(planes, ref index, parentMask, out childMask);
            Debug.debugAssert(index == -1);
            Debug.debugAssert(!culled);
            Debug.debugAssert(childMask == b00000010);

            // Full straddle
            box = new AABox(new Vector3(-1.5f, 0, 0), new Vector3(1.5f, 1, 1));
            parentMask = uint.MaxValue; childMask = 0; index = 0;
            culled = box.culledBy(planes, ref index, parentMask, out childMask);
            Debug.debugAssert(index == -1);
            Debug.debugAssert(!culled);
            Debug.debugAssert(childMask == b00000011);

            // Negative culled 
            box = new AABox(new Vector3(-2.5f, 0, 0), new Vector3(-1.5f, 1, 1));
            parentMask = uint.MaxValue; childMask = 0; index = 0;
            culled = box.culledBy(planes, ref index, parentMask, out childMask);
            Debug.debugAssert(index == 1);
            Debug.debugAssert(culled);

            // Positive culled 
            box = new AABox(new Vector3(1.5f, 0, 0), new Vector3(2.5f, 1, 1));
            parentMask = uint.MaxValue; childMask = 0; index = 0;
            culled = box.culledBy(planes, ref index, parentMask, out childMask);
            Debug.debugAssert(index == 0);
            Debug.debugAssert(culled);

            GCamera camera = new GCamera();
            camera.getClipPlanes(Rect2D.xywh(0, 0, 640, 480), planes);
        }

        private static void testAABox()
        {
            printf("AABox ");

            testAABoxCulledBy();

            printf("passed\n");
        }
    }
}
