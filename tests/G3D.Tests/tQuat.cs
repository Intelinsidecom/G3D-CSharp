using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testQuatMatrixConversion()
        {
            {
                // This is a known corner case
                Matrix3 M = Matrix3.fromAxisAngle(Vector3.UNIT_Y, (float)g3dmath.toRadians(180));
                Quat q = new Quat(M);
                Matrix3 M2 = q.toRotationMatrix();

                for (int r = 0; r < 3; ++r)
                {
                    for (int c = 0; c < 3; ++c)
                    {
                        Debug.debugAssert(Math.Abs(M[r, c] - M2[r, c]) < 0.0005);
                    }
                }
            }

            {
                // This is a known corner case (near the one above)
                Matrix3 M = new Matrix3(-0.99999988f, 0, 0,
                                         0, 1, 0,
                                         0, 0, -0.99999988f);
                Quat q = new Quat(M);
                Matrix3 M2 = q.toRotationMatrix();

                for (int r = 0; r < 3; ++r)
                {
                    for (int c = 0; c < 3; ++c)
                    {
                        Debug.debugAssert(Math.Abs(M[r, c] - M2[r, c]) < 0.0005);
                    }
                }
            }

            {

            }

            // Round trip M->q->M
            for (int i = 0; i < 100; ++i)
            {
                Matrix3 M = Matrix3.fromAxisAngle(Vector3.random(), (float)g3dmath.random(0, g3dmath.G3D_TWO_PI));
                if (i == 0)
                {
                    // Corner case, make sure we test it first.
                    M = Matrix3.identity();
                }

                Quat q = new Quat(M);
                Matrix3 M2 = q.toRotationMatrix();

                for (int r = 0; r < 3; ++r)
                {
                    for (int c = 0; c < 3; ++c)
                    {
                        Debug.debugAssert(Math.Abs(M[r, c] - M2[r, c]) < 0.0005);
                    }
                }
            }

            // Round trip q->M->q
            for (int i = 0; i < 100; ++i)
            {
                Quat q1 = Quat.fromAxisAngleRotation(Vector3.random(), (float)g3dmath.random(0, g3dmath.G3D_TWO_PI));
                Matrix3 M = q1.toRotationMatrix();
                Quat q2 = new Quat(M);

                Debug.debugAssert(q1.fuzzyEq(q2) || q1.fuzzyEq(-q2));
            }
        }

        private static void testQuatSlerp()
        {
            // Test that we take the shortest path
            {
                Vector3 axis = Vector3.UNIT_Y;

                // Start
                float a0 = 0;

                // End
                float a1 = (float)(g3dmath.G3D_PI * 3 / 2);

                float a2 = (float)((g3dmath.G3D_PI * 3 / 2 + g3dmath.G3D_PI * 2) / 2);

                Quat q0 = Quat.fromAxisAngleRotation(axis, a0);
                Quat q1 = Quat.fromAxisAngleRotation(axis, a1);
                Quat q2 = Quat.fromAxisAngleRotation(axis, a2);

                // slerp result
                Quat rq2 = q0.slerp(q1, 0.5f);

                float ra2;
                Vector3 raxis;
                rq2.toAxisAngleRotation(out raxis, out ra2);

                Debug.debugAssert(rq2.sameRotation(q2));
            }

            // Test general slerp
            for (int i = 0; i < 100; ++i)
            {
                Vector3 axis = Vector3.random();

                // We test 0->PI because that way we know the shortest path is
                // always between them (and not wrapping around the other way).

                // Start
                float a0 = (float)g3dmath.random(0, g3dmath.G3D_PI);

                // End
                float a1 = (float)g3dmath.random(0, g3dmath.G3D_PI);

                float a2 = (a0 + a1) / 2;
                Quat q0 = Quat.fromAxisAngleRotation(axis, a0);
                Quat q1 = Quat.fromAxisAngleRotation(axis, a1);
                Quat q2 = Quat.fromAxisAngleRotation(axis, a2);

                Quat rq2 = q0.slerp(q1, 0.5f);

                float ra2;
                Vector3 raxis;
                rq2.toAxisAngleRotation(out raxis, out ra2);

                Debug.debugAssert(g3dmath.fuzzyEq(ra2, a2));
                Debug.debugAssert(raxis.fuzzyEq(axis));
                Debug.debugAssert(rq2.fuzzyEq(q2));
            }
        }

        private static void testQuat()
        {
            printf("Quat ");

            testQuatSlerp();
            testQuatMatrixConversion();

            printf("passed\n");
        }
    }
}
