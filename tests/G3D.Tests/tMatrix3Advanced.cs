using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private static bool nearlyEqual(float a, float b, float tolerance)
        {
            return global::System.Math.Abs(a - b) <= tolerance * global::System.Math.Max(1.0f, global::System.Math.Max(global::System.Math.Abs(a), global::System.Math.Abs(b)));
        }

        private static bool nearlyEqual(Matrix3 a, Matrix3 b, float tolerance)
        {
            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 3; ++c)
                {
                    if (!nearlyEqual(a[r, c], b[r, c], tolerance))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static void testMatrix3SVD()
        {
            printf("G3D::Matrix3 singular value decomposition  ");

            Matrix3 a = new Matrix3(
                1.0f, 2.0f, 3.0f,
                4.0f, 5.0f, 6.0f,
                7.0f, 8.0f, 10.0f);

            Matrix3 l;
            Matrix3 r;
            Vector3 s;
            a.singularValueDecomposition(out l, out s, out r);

            Check(s[0] >= 0.0f && s[1] >= 0.0f && s[2] >= 0.0f, "SVD singular values are nonnegative");

            Matrix3 composed = Matrix3.identity();
            composed.singularValueComposition(l, s, r);
            Check(nearlyEqual(composed, a, 1e-3f), "SVD composition reconstructs the original matrix");

            Matrix3 identity = Matrix3.identity();
            Matrix3 identityL;
            Matrix3 identityR;
            Vector3 identityS;
            identity.singularValueDecomposition(out identityL, out identityS, out identityR);
            Check(nearlyEqual(identityS[0], 1.0f, 1e-4f) && nearlyEqual(identityS[1], 1.0f, 1e-4f) &&
                  nearlyEqual(identityS[2], 1.0f, 1e-4f), "SVD of the identity matrix");

            printf("passed\n");
        }

        private static void testMatrix3SpectralNorm()
        {
            printf("G3D::Matrix3 spectral norm  ");

            Matrix3 a = new Matrix3(
                1.0f, 2.0f, 3.0f,
                4.0f, 5.0f, 6.0f,
                7.0f, 8.0f, 10.0f);

            float norm = a.spectralNorm();

            Matrix3 ata = a.transpose() * a;
            float[] eigenvalues;
            Vector3[] eigenvectors;
            ata.eigenSolveSymmetric(out eigenvalues, out eigenvectors);

            float maxEigenvalue = eigenvalues[0];
            for (int i = 1; i < 3; ++i)
            {
                if (eigenvalues[i] > maxEigenvalue)
                {
                    maxEigenvalue = eigenvalues[i];
                }
            }

            Check(nearlyEqual(norm, (float)global::System.Math.Sqrt(maxEigenvalue), 1e-3f),
                  "spectral norm matches the largest eigenvalue of A transpose times A");

            Check(nearlyEqual(Matrix3.identity().spectralNorm(), 1.0f, 1e-4f),
                  "spectral norm of the identity matrix");

            printf("passed\n");
        }

        private static void testMatrix3QDUDecomposition()
        {
            printf("G3D::Matrix3 QDU decomposition  ");

            Matrix3 a = new Matrix3(
                2.0f, 1.0f, 3.0f,
                1.0f, 3.0f, 2.0f,
                4.0f, 2.0f, 1.0f);

            Matrix3 q;
            Vector3 d;
            Vector3 u;
            a.qDUDecomposition(out q, out d, out u);

            Matrix3 diagonal = Matrix3.zero();
            diagonal[0, 0] = d[0];
            diagonal[1, 1] = d[1];
            diagonal[2, 2] = d[2];

            Matrix3 upper = Matrix3.identity();
            upper[0, 1] = u[0];
            upper[0, 2] = u[1];
            upper[1, 2] = u[2];

            Matrix3 composed = q * diagonal * upper;
            Check(nearlyEqual(composed, a, 1e-4f), "QDU composition reconstructs the original matrix");

            Matrix3 identityQ;
            Vector3 identityD;
            Vector3 identityU;
            Matrix3.identity().qDUDecomposition(out identityQ, out identityD, out identityU);
            Check(identityD[0] == 1.0f && identityD[1] == 1.0f && identityD[2] == 1.0f,
                  "QDU diagonal of the identity matrix");
            Check(identityU[0] == 0.0f && identityU[1] == 0.0f && identityU[2] == 0.0f,
                  "QDU shear of the identity matrix");

            printf("passed\n");
        }

        private static void testMatrix3EigenSolve()
        {
            printf("G3D::Matrix3 eigenSolveSymmetric  ");

            Matrix3 a = new Matrix3(
                2.0f, 1.0f, 0.0f,
                1.0f, 2.0f, 1.0f,
                0.0f, 1.0f, 2.0f);

            float[] eigenvalues;
            Vector3[] eigenvectors;
            a.eigenSolveSymmetric(out eigenvalues, out eigenvectors);

            float expected0 = 2.0f - (float)global::System.Math.Sqrt(2.0);
            float expected1 = 2.0f;
            float expected2 = 2.0f + (float)global::System.Math.Sqrt(2.0);

            float[] sorted = new float[] { eigenvalues[0], eigenvalues[1], eigenvalues[2] };
            for (int i = 0; i < 3; ++i)
            {
                for (int j = i + 1; j < 3; ++j)
                {
                    if (sorted[j] < sorted[i])
                    {
                        float swap = sorted[i];
                        sorted[i] = sorted[j];
                        sorted[j] = swap;
                    }
                }
            }

            float min = sorted[0];
            float middle = sorted[1];
            float max = sorted[2];

            Check(nearlyEqual(min, expected0, 1e-4f), "eigenSolveSymmetric smallest eigenvalue");
            Check(nearlyEqual(middle, expected1, 1e-4f), "eigenSolveSymmetric middle eigenvalue");
            Check(nearlyEqual(max, expected2, 1e-4f), "eigenSolveSymmetric largest eigenvalue");

            for (int i = 0; i < 3; ++i)
            {
                Vector3 v = eigenvectors[i];
                Vector3 product = a * v;
                Vector3 scaled = v * eigenvalues[i];
                Check(product.fuzzyEq(scaled) || nearlyEqual(product.x, scaled.x, 1e-3f) &&
                      nearlyEqual(product.y, scaled.y, 1e-3f) && nearlyEqual(product.z, scaled.z, 1e-3f),
                      "eigenSolveSymmetric eigenvector " + i);
            }

            printf("passed\n");
        }

        private static void testMatrix3Advanced()
        {
            testMatrix3SVD();
            testMatrix3SpectralNorm();
            testMatrix3QDUDecomposition();
            testMatrix3EigenSolve();
        }
    }
}
