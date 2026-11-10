/**
  @file tMatrix.cs

  Port of test/tMatrix.cpp.

  The original test is compiled against contrib/Matrix/Matrix.cpp directly;
  this port calls the ported G3D.Matrix class instead, which is why the
  original's include of the implementation file has no counterpart here.
*/

using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static void testMatrix()
        {
            printf("Matrix ");

            // Zeros
            {
                Matrix M = new Matrix(3, 4);
                Debug.debugAssert(M.rows() == 3);
                Debug.debugAssert(M.cols() == 4);
                Debug.debugAssert(M.get(0, 0) == 0);
                Debug.debugAssert(M.get(1, 1) == 0);
            }

            // Identity
            {
                Matrix M = Matrix.identity(4);
                Debug.debugAssert(M.rows() == 4);
                Debug.debugAssert(M.cols() == 4);
                Debug.debugAssert(M.get(0, 0) == 1);
                Debug.debugAssert(M.get(0, 1) == 0);
            }

            // Add
            {
                Matrix A = Matrix.random(2, 3);
                Matrix B = Matrix.random(2, 3);
                Matrix C = A + B;

                for (int r = 0; r < 2; ++r)
                {
                    for (int c = 0; c < 3; ++c)
                    {
                        Debug.debugAssert(g3dmath.fuzzyEq(C.get(r, c), A.get(r, c) + B.get(r, c)));
                    }
                }
            }

            // Matrix multiply
            {
                Matrix A = new Matrix(2, 2);
                Matrix B = new Matrix(2, 2);

                A.set(0, 0, 1); A.set(0, 1, 3);
                A.set(1, 0, 4); A.set(1, 1, 2);

                B.set(0, 0, -6); B.set(0, 1, 9);
                B.set(1, 0, 1); B.set(1, 1, 7);

                Matrix C = A * B;

                Debug.debugAssert(g3dmath.fuzzyEq(C.get(0, 0), -3));
                Debug.debugAssert(g3dmath.fuzzyEq(C.get(0, 1), 30));
                Debug.debugAssert(g3dmath.fuzzyEq(C.get(1, 0), -22));
                Debug.debugAssert(g3dmath.fuzzyEq(C.get(1, 1), 50));
            }

            // Transpose
            {
                Matrix A = new Matrix(2, 2);

                A.set(0, 0, 1);
                A.set(0, 1, 3);
                A.set(1, 0, 4);
                A.set(1, 1, 2);

                Matrix C = A.transpose();

                Debug.debugAssert(g3dmath.fuzzyEq(C.get(0, 0), 1));
                Debug.debugAssert(g3dmath.fuzzyEq(C.get(0, 1), 4));
                Debug.debugAssert(g3dmath.fuzzyEq(C.get(1, 0), 3));
                Debug.debugAssert(g3dmath.fuzzyEq(C.get(1, 1), 2));

                A = Matrix.random(3, 4);
                A = A.transpose();

                Debug.debugAssert(A.rows() == 4);
                Debug.debugAssert(A.cols() == 3);
            }

            // Copy-on-mutate
            {
                Matrix.debugNumCopyOps = Matrix.debugNumAllocOps = 0;

                Matrix A = Matrix.identity(2);

                Debug.debugAssert(Matrix.debugNumAllocOps == 1);
                Debug.debugAssert(Matrix.debugNumCopyOps == 0);

                Matrix B = A;
                Debug.debugAssert(Matrix.debugNumAllocOps == 1);
                Debug.debugAssert(Matrix.debugNumCopyOps == 0);

                B.set(0, 0, 4);
                Debug.debugAssert(B.get(0, 0) == 4);
                Debug.debugAssert(A.get(0, 0) == 1);
                Debug.debugAssert(Matrix.debugNumAllocOps == 2);
                Debug.debugAssert(Matrix.debugNumCopyOps == 1);
            }

            // Inverse
            {
                Matrix A = new Matrix(2, 2);

                A.set(0, 0, 1);
                A.set(0, 1, 3);
                A.set(1, 0, 4);
                A.set(1, 1, 2);

                Matrix C = A.inverse();

                Debug.debugAssert(g3dmath.fuzzyEq(C.get(0, 0), -0.2));
                Debug.debugAssert(g3dmath.fuzzyEq(C.get(0, 1), 0.3));
                Debug.debugAssert(g3dmath.fuzzyEq(C.get(1, 0), 0.4));
                Debug.debugAssert(g3dmath.fuzzyEq(C.get(1, 1), -0.1));
            }

            {
                Matrix A = Matrix.random(10, 10);
                Matrix B = A.inverse();

                B = B * A;

                for (int r = 0; r < B.rows(); ++r)
                {
                    for (int c = 0; c < B.cols(); ++c)
                    {

                        float v = B.get(r, c);
                        // The precision isn't great on our inverse, so be tolerant
                        if (r == c)
                        {
                            Debug.debugAssert(Math.Abs(v - 1) < 1e-4);
                        }
                        else
                        {
                            Debug.debugAssert(Math.Abs(v) < 1e-4);
                        }
                    }
                }
            }

            // Negate
            {
                Matrix A = Matrix.random(2, 2);
                Matrix B = -A;

                for (int r = 0; r < A.rows(); ++r)
                {
                    for (int c = 0; c < A.cols(); ++c)
                    {
                        Debug.debugAssert(B.get(r, c) == -A.get(r, c));
                    }
                }
            }

            // Transpose
            {
                Matrix A = Matrix.random(3, 2);
                Matrix B = A.transpose();
                Debug.debugAssert(B.rows() == A.cols());
                Debug.debugAssert(B.cols() == A.rows());

                for (int r = 0; r < A.rows(); ++r)
                {
                    for (int c = 0; c < A.cols(); ++c)
                    {
                        Debug.debugAssert(B.get(c, r) == A.get(r, c));
                    }
                }
            }

            // SVD
            {
                Matrix A = new Matrix(3, 3);
                A.set(0, 0, 1.0f);
                A.set(0, 1, 2.0f);
                A.set(0, 2, 1.0f);
                A.set(1, 0, -3.0f);
                A.set(1, 1, 7.0f);
                A.set(1, 2, -6.0f);
                A.set(2, 0, 4.0f);
                A.set(2, 1, -4.0f);
                A.set(2, 2, 10.0f);
                A = Matrix.random(27, 15);

                Array<float> D = new Array<float>();
                Matrix U = new Matrix(0, 0),
                    V = new Matrix(0, 0);

                A.svd(U, D, V);

                // Verify that we can reconstruct
                Matrix B = U * Matrix.fromDiagonal(D) * V.transpose();

                Matrix test = Matrix.abs(A - B) < 0.1f;

                //        A.debugPrint("A");
                //        U.debugPrint("U");
                //        D.debugPrint("D");
                //        V.debugPrint("V");
                //        (U * D * V.transpose()).debugPrint("UDV'");

                Debug.debugAssert(test.allNonZero());

                // norm() returns a double in both the original and the port.
                double m = (A - B).norm() / A.norm();
                Debug.debugAssert(m < 0.01);
            }

            /*
            Matrix a(3, 5);
            a.set(0,0, 1);  a.set(0,1, 2); a.set(0,2,  3); a.set(0,3, 4);  a.set(0,4,  5);
            a.set(1,0, 3);  a.set(1,1, 5); a.set(1,2,  3); a.set(1,3, 1);  a.set(1,4,  2);
            a.set(2,0, 1);  a.set(2,1, 1); a.set(2,2,  1); a.set(2,3, 1);  a.set(2,4,  1);

            Matrix b = a;
            b.set(0,0, 1.8124); b.set(0,1,    0.5341); b.set(0,2,    2.8930); b.set(0,3,    5.2519); b.set(0,4,    4.8829);
            b.set(1,0, 2.5930); b.set(1,1,   2.6022); b.set(1,2,    4.2760); b.set(1,3,    5.9497); b.set(1,4,    6.3751);

            a.debugPrint("a");
            a.debugPrint("b");

            Matrix H = b * a.pseudoInverse();
            H.debugPrint("H");
            */

            printf("passed\n");

        }
    }
}
