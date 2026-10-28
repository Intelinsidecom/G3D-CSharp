using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testRandom()
        {
            printf("Random number generators ");

            int num0 = 0;
            int num1 = 0;
            for (int i = 0; i < 10000; ++i)
            {
                switch (g3dmath.iRandom(0, 1))
                {
                    case 0:
                        ++num0;
                        break;

                    case 1:
                        ++num1;
                        break;

                    default:
                        Debug.debugAssertM(
                            false,
                            "Random number outside the range [0, 1] from iRandom(0,1)"
                        );
                        break;
                }
            }
            int difference = g3dmath.iAbs(num0 - num1);
            Debug.debugAssertM(difference < 300, "Integer random number generator appears skewed.");

            for (int i = 0; i < 100; ++i)
            {
                double r = g3dmath.random(0, 1);
                Debug.debugAssert(r >= 0.0);
                Debug.debugAssert(r <= 1.0);
            }

            // Triangle.randomPoint
            Triangle tri = new Triangle(
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0)
            );
            for (int i = 0; i < 1000; ++i)
            {
                Vector3 p = tri.randomPoint();
                Debug.debugAssert(p.z == 0);
                Debug.debugAssert(p.x <= 1.0 - p.y);
                Debug.debugAssert(p.x >= 0);
                Debug.debugAssert(p.y >= 0);
                Debug.debugAssert(p.x <= 1.0);
                Debug.debugAssert(p.y <= 1.0);
            }

            for (int i = 0; i < 100; i++)
            {
                Vector3 point = tri.randomPoint();
                Debug.debugAssert(
                    CollisionDetection.isPointInsideTriangle(
                        tri.vertex(0),
                        tri.vertex(1),
                        tri.vertex(2),
                        tri.normal(),
                        point
                    )
                );
            }

            printf("passed\n");
        }
    }
}
