using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void mul(float[,] A, float[,] B, float[,] C)
        {
            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 3; ++c)
                {
                    float sum = 0;
                    for (int i = 0; i < 3; ++i)
                    {
                        sum += A[r, i] * B[i, c];
                    }
                    C[r, c] = sum;
                }
            }
        }

        private static void testEuler()
        {
            float x = 1;
            float y = 2;
            float z = -3;

            float x2, y2, z2;

            Matrix3 rX = Matrix3.fromAxisAngle(Vector3.UNIT_X, x);
            Matrix3 rY = Matrix3.fromAxisAngle(Vector3.UNIT_Y, y);
            Matrix3 rZ = Matrix3.fromAxisAngle(Vector3.UNIT_Z, z);
            Matrix3 rot = rZ * rX * rY;
            rot.toEulerAnglesZXY(out x2, out y2, out z2);
            Debug.debugAssert(g3dmath.fuzzyEq(x, x2));
            Debug.debugAssert(g3dmath.fuzzyEq(y, y2));
            Debug.debugAssert(g3dmath.fuzzyEq(z, z2));
        }

        private static void testMatrix3()
        {
            printf("G3D::Matrix3  ");

            testEuler();

            {
                Matrix3 M = Matrix3.identity();
                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        M[i, j] = (float)g3dmath.random(0, 1);
                    }
                }

                Vector3 v = Vector3.random();

                Vector3 x1 = v * M;
                Vector3 x2 = M.transpose() * v;

                Debug.debugAssert(x1 == x2);
            }

            printf("passed\n");
        }

        private static void perfMatrix3()
        {
            printf("Matrix3:\n");
            ulong raw = 0, opt = 0, overhead = 0, naive = 0;

            // 0.5 million operations
            int n = (int)(0.5 * 1024 * 1024);

            // Use two copies to avoid nice cache behavior
            Matrix3 A = Matrix3.fromAxisAngle(new Vector3(1, 2, 1), 1.2f);
            Matrix3 B = Matrix3.fromAxisAngle(new Vector3(0, 1, -1), .2f);
            Matrix3 C = Matrix3.zero();

            Matrix3 D = Matrix3.fromAxisAngle(new Vector3(1, 2, 1), 1.2f);
            Matrix3 E = Matrix3.fromAxisAngle(new Vector3(0, 1, -1), .2f);
            Matrix3 F = Matrix3.zero();

            int i;
            System.beginCycleCount(ref overhead);
            for (i = n - 1; i >= 0; --i)
            {
            }
            System.endCycleCount(ref overhead);

            System.beginCycleCount(ref raw);
            for (i = n - 1; i >= 0; --i)
            {
                C = A.transpose();
                F = D.transpose();
                C = B.transpose();
            }
            System.endCycleCount(ref raw);

            System.beginCycleCount(ref opt);
            for (i = n - 1; i >= 0; --i)
            {
                Matrix3.transpose(A, ref C);
                Matrix3.transpose(D, ref F);
                Matrix3.transpose(B, ref C);
            }
            System.endCycleCount(ref opt);

            raw -= overhead;
            opt -= overhead;

            printf(" Transpose Performance                       outcome\n");
            printf("     transpose(A, C): %g cycles/mul       %s\n\n",
                (double)opt / (3 * n), ((double)opt / (3 * n) < 400) ? " ok " : "FAIL");
            printf("   C = A.transpose(): %g cycles/mul       %s\n",
                (double)raw / (3 * n), ((double)raw / (3 * n) < 150) ? " ok " : "FAIL");
            printf("\n");
            /////////////////////////////////

            printf(" Matrix-Matrix Multiplication\n");
            System.beginCycleCount(ref raw);
            for (i = n - 1; i >= 0; --i)
            {
                C = A * B;
                F = D * E;
                C = A * D;
            }
            System.endCycleCount(ref raw);

            System.beginCycleCount(ref opt);
            for (i = n - 1; i >= 0; --i)
            {
                Matrix3.mul(A, B, ref C);
                Matrix3.mul(D, E, ref F);
                Matrix3.mul(A, D, ref C);
            }
            System.endCycleCount(ref opt);

            {
                float[,] A2 = new float[3, 3], B2 = new float[3, 3], C2 = new float[3, 3];
                float[,] D2 = new float[3, 3], E2 = new float[3, 3], F2 = new float[3, 3];

                System.beginCycleCount(ref naive);
                for (i = n - 1; i >= 0; --i)
                {
                    mul(A2, B2, C2);
                    mul(D2, E2, F2);
                    mul(A2, D2, C2);
                }
                System.endCycleCount(ref naive);
            }

            raw -= overhead;
            opt -= overhead;

            printf("  mul(A, B, C)          %g cycles/mul     %s\n", (double)opt / (3 * n), ((double)opt / (3 * n) < 250) ? " ok " : "FAIL");
            printf("     C = A * B          %g cycles/mul     %s\n", (double)raw / (3 * n), ((double)raw / (3 * n) < 500) ? " ok " : "FAIL");
            printf("  naive for-loops       %g cycles/mul\n", (double)naive / (3 * n));

            printf("\n\n");
        }
    }
}
