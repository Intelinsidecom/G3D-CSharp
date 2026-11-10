/**
  @file Matrix3.h

  3x3 matrix class

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @cite Portions based on Dave Eberly's Magic Software Library at <A HREF="http://www.magic-software.com">http://www.magic-software.com</A>

  @created 2001-06-02
  @edited  2006-04-05
 */

using System;

namespace G3D
{
    public struct Matrix3
    {
        public float m00, m01, m02;
        public float m10, m11, m12;
        public float m20, m21, m22;

        public const float EPSILON = 1e-06f;

        public Matrix3(BinaryInput b)
        {
            m00 = b.readFloat32();
            m10 = b.readFloat32();
            m20 = b.readFloat32();
            m01 = b.readFloat32();
            m11 = b.readFloat32();
            m21 = b.readFloat32();
            m02 = b.readFloat32();
            m12 = b.readFloat32();
            m22 = b.readFloat32();
        }

        public Matrix3(float fEntry00, float fEntry01, float fEntry02,
                       float fEntry10, float fEntry11, float fEntry12,
                       float fEntry20, float fEntry21, float fEntry22)
        {
            m00 = fEntry00;
            m01 = fEntry01;
            m02 = fEntry02;
            m10 = fEntry10;
            m11 = fEntry11;
            m12 = fEntry12;
            m20 = fEntry20;
            m21 = fEntry21;
            m22 = fEntry22;
        }

        /** Constructs a matrix from a quaternion.
         */
        public Matrix3(Quat q)
        {
            // Implementation from Watt and Watt, pg 362
            // See also http://www.flipcode.com/documents/matrfaq.html#Q54
            Quat unit = q.unitize();
            float xx = 2.0f * unit.x * unit.x;
            float xy = 2.0f * unit.x * unit.y;
            float xz = 2.0f * unit.x * unit.z;
            float xw = 2.0f * unit.x * unit.w;

            float yy = 2.0f * unit.y * unit.y;
            float yz = 2.0f * unit.y * unit.z;
            float yw = 2.0f * unit.y * unit.w;

            float zz = 2.0f * unit.z * unit.z;
            float zw = 2.0f * unit.z * unit.w;

            m00 = 1.0f - yy - zz;
            m01 = xy - zw;
            m02 = xz + yw;
            m10 = xy + zw;
            m11 = 1.0f - xx - zz;
            m12 = yz - xw;
            m20 = xz - yw;
            m21 = yz + xw;
            m22 = 1.0f - xx - yy;
        }

        public bool fuzzyEq(Matrix3 b)
        {
            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 3; ++c)
                {
                    if (!g3dmath.fuzzyEq(elt(r, c), b.elt(r, c)))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void serialize(BinaryOutput b)
        {
            for (int c = 0; c < 3; ++c)
            {
                for (int r = 0; r < 3; ++r)
                {
                    b.writeFloat32(elt(r, c));
                }
            }
        }

        public void deserialize(BinaryInput b)
        {
            for (int c = 0; c < 3; ++c)
            {
                for (int r = 0; r < 3; ++r)
                {
                    setElt(r, c, b.readFloat32());
                }
            }
        }

        public void set(float fEntry00, float fEntry01, float fEntry02,
                        float fEntry10, float fEntry11, float fEntry12,
                        float fEntry20, float fEntry21, float fEntry22)
        {
            m00 = fEntry00;
            m01 = fEntry01;
            m02 = fEntry02;
            m10 = fEntry10;
            m11 = fEntry11;
            m12 = fEntry12;
            m20 = fEntry20;
            m21 = fEntry21;
            m22 = fEntry22;
        }

        public float elt(int row, int column)
        {
            if (row == 0)
            {
                if (column == 0) return m00;
                if (column == 1) return m01;
                return m02;
            }
            if (row == 1)
            {
                if (column == 0) return m10;
                if (column == 1) return m11;
                return m12;
            }
            if (column == 0) return m20;
            if (column == 1) return m21;
            return m22;
        }

        public void setElt(int row, int column, float value)
        {
            if (row == 0)
            {
                if (column == 0) m00 = value;
                else if (column == 1) m01 = value;
                else m02 = value;
            }
            else if (row == 1)
            {
                if (column == 0) m10 = value;
                else if (column == 1) m11 = value;
                else m12 = value;
            }
            else
            {
                if (column == 0) m20 = value;
                else if (column == 1) m21 = value;
                else m22 = value;
            }
        }

        public float this[int row, int column]
        {
            get
            {
                Debug.debugAssert(row >= 0);
                Debug.debugAssert(row < 3);
                Debug.debugAssert(column >= 0);
                Debug.debugAssert(column < 3);
                return elt(row, column);
            }
            set
            {
                Debug.debugAssert(row >= 0);
                Debug.debugAssert(row < 3);
                Debug.debugAssert(column >= 0);
                Debug.debugAssert(column < 3);
                setElt(row, column, value);
            }
        }

        public float[] getRow(int iRow)
        {
            return new float[] { elt(iRow, 0), elt(iRow, 1), elt(iRow, 2) };
        }

        public float[] getColumn(int iCol)
        {
            return new float[] { elt(0, iCol), elt(1, iCol), elt(2, iCol) };
        }

        public void setRow(int iRow, Vector3 vector)
        {
            Debug.debugAssert((iRow >= 0) && (iRow < 3));
            setElt(iRow, 0, vector.x);
            setElt(iRow, 1, vector.y);
            setElt(iRow, 2, vector.z);
        }

        public void setColumn(int iCol, Vector3 vector)
        {
            Debug.debugAssert((iCol >= 0) && (iCol < 3));
            setElt(0, iCol, vector.x);
            setElt(1, iCol, vector.y);
            setElt(2, iCol, vector.z);
        }

        public Vector3 getRowVector(int iRow)
        {
            return new Vector3(elt(iRow, 0), elt(iRow, 1), elt(iRow, 2));
        }

        public Vector3 getColumnVector(int iCol)
        {
            return new Vector3(elt(0, iCol), elt(1, iCol), elt(2, iCol));
        }

        public static bool operator ==(Matrix3 left, Matrix3 right)
        {
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    if (left.elt(iRow, iCol) != right.elt(iRow, iCol))
                        return false;
                }
            }
            return true;
        }

        public static bool operator !=(Matrix3 left, Matrix3 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is Matrix3 && this == (Matrix3)obj;
        }

        public override int GetHashCode()
        {
            return m00.GetHashCode() ^ m11.GetHashCode() ^ m22.GetHashCode();
        }

        public static Matrix3 operator +(Matrix3 left, Matrix3 right)
        {
            Matrix3 sum = new Matrix3();
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    sum.setElt(iRow, iCol, left.elt(iRow, iCol) + right.elt(iRow, iCol));
                }
            }
            return sum;
        }

        public static Matrix3 operator -(Matrix3 left, Matrix3 right)
        {
            Matrix3 diff = new Matrix3();
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    diff.setElt(iRow, iCol, left.elt(iRow, iCol) - right.elt(iRow, iCol));
                }
            }
            return diff;
        }

        public static Matrix3 operator *(Matrix3 left, Matrix3 right)
        {
            Matrix3 prod = new Matrix3();
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    prod.setElt(iRow, iCol,
                        left.elt(iRow, 0) * right.elt(0, iCol) +
                        left.elt(iRow, 1) * right.elt(1, iCol) +
                        left.elt(iRow, 2) * right.elt(2, iCol));
                }
            }
            return prod;
        }

        public static Matrix3 operator -(Matrix3 m)
        {
            Matrix3 neg = new Matrix3();
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    neg.setElt(iRow, iCol, -m.elt(iRow, iCol));
                }
            }
            return neg;
        }

        public static Vector3 operator *(Matrix3 m, Vector3 v)
        {
            Vector3 kProd = new Vector3();
            for (int r = 0; r < 3; ++r)
            {
                kProd[r] =
                    m.elt(r, 0) * v[0] +
                    m.elt(r, 1) * v[1] +
                    m.elt(r, 2) * v[2];
            }
            return kProd;
        }

        public static Vector3 operator *(Vector3 v, Matrix3 m)
        {
            Vector3 kProd = new Vector3();
            for (int r = 0; r < 3; ++r)
            {
                kProd[r] =
                    v[0] * m.elt(0, r) +
                    v[1] * m.elt(1, r) +
                    v[2] * m.elt(2, r);
            }
            return kProd;
        }

        public static Matrix3 operator *(Matrix3 m, float fScalar)
        {
            Matrix3 kProd = new Matrix3();
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    kProd.setElt(iRow, iCol, fScalar * m.elt(iRow, iCol));
                }
            }
            return kProd;
        }

        public static Matrix3 operator *(float fScalar, Matrix3 m)
        {
            // The C++ operator* (float, Matrix3) forwards to the double overload.
            return (double)fScalar * m;
        }

        public static Matrix3 operator *(double fScalar, Matrix3 m)
        {
            Matrix3 kProd = new Matrix3();
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    // The C++ operator* (double, Matrix3) evaluates the product
                    // in double precision and assigns to the float element.
                    kProd.setElt(iRow, iCol, (float)(fScalar * m.elt(iRow, iCol)));
                }
            }
            return kProd;
        }

        public static Matrix3 operator *(int fScalar, Matrix3 m)
        {
            // The C++ operator* (int, Matrix3) forwards to the double overload.
            return (double)fScalar * m;
        }

        public Matrix3 transpose()
        {
            Matrix3 kTranspose = new Matrix3();
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    kTranspose.setElt(iRow, iCol, elt(iCol, iRow));
                }
            }
            return kTranspose;
        }

        public static void transpose(Matrix3 A, ref Matrix3 outM)
        {
            outM = A.transpose();
        }

        /**
          Optimized implementation of out = A * B.  It is safe (but slow) to call
          with A, B, and out possibly pointer equal to one another.
         */
        // This is a static method so that it is not ambiguous whether "this"
        // is an input or output argument.
        public static void mul(Matrix3 A, Matrix3 B, ref Matrix3 outM)
        {
            outM = A * B;
        }

        public bool inverse(out Matrix3 rkInverse, float fTolerance)
        {
            rkInverse = new Matrix3();
            rkInverse.setElt(0, 0, elt(1, 1) * elt(2, 2) - elt(1, 2) * elt(2, 1));
            rkInverse.setElt(0, 1, elt(0, 2) * elt(2, 1) - elt(0, 1) * elt(2, 2));
            rkInverse.setElt(0, 2, elt(0, 1) * elt(1, 2) - elt(0, 2) * elt(1, 1));
            rkInverse.setElt(1, 0, elt(1, 2) * elt(2, 0) - elt(1, 0) * elt(2, 2));
            rkInverse.setElt(1, 1, elt(0, 0) * elt(2, 2) - elt(0, 2) * elt(2, 0));
            rkInverse.setElt(1, 2, elt(0, 2) * elt(1, 0) - elt(0, 0) * elt(1, 2));
            rkInverse.setElt(2, 0, elt(1, 0) * elt(2, 1) - elt(1, 1) * elt(2, 0));
            rkInverse.setElt(2, 1, elt(0, 1) * elt(2, 0) - elt(0, 0) * elt(2, 1));
            rkInverse.setElt(2, 2, elt(0, 0) * elt(1, 1) - elt(0, 1) * elt(1, 0));

            float fDet =
                elt(0, 0) * rkInverse.elt(0, 0) +
                elt(0, 1) * rkInverse.elt(1, 0) +
                elt(0, 2) * rkInverse.elt(2, 0);

            if (g3dmath.abs(fDet) <= fTolerance)
                return false;

            float fInvDet = 1.0f / fDet;

            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    rkInverse.setElt(iRow, iCol, rkInverse.elt(iRow, iCol) * fInvDet);
                }
            }

            return true;
        }

        public bool inverse(out Matrix3 rkInverse)
        {
            return inverse(out rkInverse, 1e-06f);
        }

        public Matrix3 inverse(float fTolerance)
        {
            Matrix3 kInverse = zero();
            inverse(out kInverse, fTolerance);
            return kInverse;
        }

        public Matrix3 inverse()
        {
            return inverse(1e-06f);
        }

        public float determinant()
        {
            float fCofactor00 = elt(1, 1) * elt(2, 2) - elt(1, 2) * elt(2, 1);
            float fCofactor10 = elt(1, 2) * elt(2, 0) - elt(1, 0) * elt(2, 2);
            float fCofactor20 = elt(1, 0) * elt(2, 1) - elt(1, 1) * elt(2, 0);

            float fDet =
                elt(0, 0) * fCofactor00 +
                elt(0, 1) * fCofactor10 +
                elt(0, 2) * fCofactor20;

            return fDet;
        }

        public bool isOrthonormal()
        {
            Vector3 X = getColumnVector(0);
            Vector3 Y = getColumnVector(1);
            Vector3 Z = getColumnVector(2);

            return
                (g3dmath.fuzzyEq(X.dot(Y), 0.0f) &&
                 g3dmath.fuzzyEq(Y.dot(Z), 0.0f) &&
                 g3dmath.fuzzyEq(X.dot(Z), 0.0f) &&
                 g3dmath.fuzzyEq(X.squaredMagnitude(), 1.0f) &&
                 g3dmath.fuzzyEq(Y.squaredMagnitude(), 1.0f) &&
                 g3dmath.fuzzyEq(Z.squaredMagnitude(), 1.0f));
        }

        public void orthonormalize()
        {
            float fInvLength = 1.0f / (float)global::System.Math.Sqrt(elt(0, 0) * elt(0, 0)
                                       + elt(1, 0) * elt(1, 0) +
                                       elt(2, 0) * elt(2, 0));

            setElt(0, 0, elt(0, 0) * fInvLength);
            setElt(1, 0, elt(1, 0) * fInvLength);
            setElt(2, 0, elt(2, 0) * fInvLength);

            float fDot0 =
                elt(0, 0) * elt(0, 1) +
                elt(1, 0) * elt(1, 1) +
                elt(2, 0) * elt(2, 1);

            setElt(0, 1, elt(0, 1) - fDot0 * elt(0, 0));
            setElt(1, 1, elt(1, 1) - fDot0 * elt(1, 0));
            setElt(2, 1, elt(2, 1) - fDot0 * elt(2, 0));

            fInvLength = 1.0f / (float)global::System.Math.Sqrt(elt(0, 1) * elt(0, 1) +
                                      elt(1, 1) * elt(1, 1) +
                                      elt(2, 1) * elt(2, 1));

            setElt(0, 1, elt(0, 1) * fInvLength);
            setElt(1, 1, elt(1, 1) * fInvLength);
            setElt(2, 1, elt(2, 1) * fInvLength);

            float fDot1 =
                elt(0, 1) * elt(0, 2) +
                elt(1, 1) * elt(1, 2) +
                elt(2, 1) * elt(2, 2);

            fDot0 =
                elt(0, 0) * elt(0, 2) +
                elt(1, 0) * elt(1, 2) +
                elt(2, 0) * elt(2, 2);

            setElt(0, 2, elt(0, 2) - fDot0 * elt(0, 0) - fDot1 * elt(0, 1));
            setElt(1, 2, elt(1, 2) - fDot0 * elt(1, 0) - fDot1 * elt(1, 1));
            setElt(2, 2, elt(2, 2) - fDot0 * elt(2, 0) - fDot1 * elt(2, 1));

            fInvLength = 1.0f / (float)global::System.Math.Sqrt(elt(0, 2) * elt(0, 2) +
                                      elt(1, 2) * elt(1, 2) +
                                      elt(2, 2) * elt(2, 2));

            setElt(0, 2, elt(0, 2) * fInvLength);
            setElt(1, 2, elt(1, 2) * fInvLength);
            setElt(2, 2, elt(2, 2) * fInvLength);
        }

        /** singular value decomposition */
        public void singularValueDecomposition(out Matrix3 rkL, out Vector3 rkS, out Matrix3 rkR)
        {
            Matrix3 kA = this;
            Matrix3 kL = Matrix3.zero();
            Matrix3 kR = Matrix3.zero();
            bidiagonalize(ref kA, ref kL, ref kR);

            Vector3 kS = Vector3.zero();

            for (int i = 0; i < ms_iSvdMaxIterations; i++)
            {
                float fTmp, fTmp0, fTmp1;
                float fSin0, fCos0, fTan0;
                float fSin1, fCos1, fTan1;

                bool bTest1 = (g3dmath.abs(kA[0, 1]) <=
                               ms_fSvdEpsilon * (g3dmath.abs(kA[0, 0]) + g3dmath.abs(kA[1, 1])));
                bool bTest2 = (g3dmath.abs(kA[1, 2]) <=
                               ms_fSvdEpsilon * (g3dmath.abs(kA[1, 1]) + g3dmath.abs(kA[2, 2])));

                if (bTest1)
                {
                    if (bTest2)
                    {
                        kS[0] = kA[0, 0];
                        kS[1] = kA[1, 1];
                        kS[2] = kA[2, 2];
                        break;
                    }
                    else
                    {
                        // 2x2 closed form factorization
                        fTmp = (kA[1, 1] * kA[1, 1] - kA[2, 2] * kA[2, 2] +
                                kA[1, 2] * kA[1, 2]) / (kA[1, 2] * kA[2, 2]);
                        fTan0 = 0.5f * (fTmp + (float)global::System.Math.Sqrt(fTmp * fTmp + 4.0f));
                        fCos0 = 1.0f / (float)global::System.Math.Sqrt(1.0f + fTan0 * fTan0);
                        fSin0 = fTan0 * fCos0;

                        for (int iCol = 0; iCol < 3; iCol++)
                        {
                            fTmp0 = kL[iCol, 1];
                            fTmp1 = kL[iCol, 2];
                            kL[iCol, 1] = fCos0 * fTmp0 - fSin0 * fTmp1;
                            kL[iCol, 2] = fSin0 * fTmp0 + fCos0 * fTmp1;
                        }

                        fTan1 = (kA[1, 2] - kA[2, 2] * fTan0) / kA[1, 1];
                        fCos1 = 1.0f / (float)global::System.Math.Sqrt(1.0f + fTan1 * fTan1);
                        fSin1 = -fTan1 * fCos1;

                        for (int iRow = 0; iRow < 3; iRow++)
                        {
                            fTmp0 = kR[1, iRow];
                            fTmp1 = kR[2, iRow];
                            kR[1, iRow] = fCos1 * fTmp0 - fSin1 * fTmp1;
                            kR[2, iRow] = fSin1 * fTmp0 + fCos1 * fTmp1;
                        }

                        kS[0] = kA[0, 0];
                        kS[1] = fCos0 * fCos1 * kA[1, 1] -
                                fSin1 * (fCos0 * kA[1, 2] - fSin0 * kA[2, 2]);
                        kS[2] = fSin0 * fSin1 * kA[1, 1] +
                                fCos1 * (fSin0 * kA[1, 2] + fCos0 * kA[2, 2]);
                        break;
                    }
                }
                else
                {
                    if (bTest2)
                    {
                        // 2x2 closed form factorization
                        fTmp = (kA[0, 0] * kA[0, 0] + kA[1, 1] * kA[1, 1] -
                                kA[0, 1] * kA[0, 1]) / (kA[0, 1] * kA[1, 1]);
                        fTan0 = 0.5f * (-fTmp + (float)global::System.Math.Sqrt(fTmp * fTmp + 4.0f));
                        fCos0 = 1.0f / (float)global::System.Math.Sqrt(1.0f + fTan0 * fTan0);
                        fSin0 = fTan0 * fCos0;

                        for (int iCol = 0; iCol < 3; iCol++)
                        {
                            fTmp0 = kL[iCol, 0];
                            fTmp1 = kL[iCol, 1];
                            kL[iCol, 0] = fCos0 * fTmp0 - fSin0 * fTmp1;
                            kL[iCol, 1] = fSin0 * fTmp0 + fCos0 * fTmp1;
                        }

                        fTan1 = (kA[0, 1] - kA[1, 1] * fTan0) / kA[0, 0];
                        fCos1 = 1.0f / (float)global::System.Math.Sqrt(1.0f + fTan1 * fTan1);
                        fSin1 = -fTan1 * fCos1;

                        for (int iRow = 0; iRow < 3; iRow++)
                        {
                            fTmp0 = kR[0, iRow];
                            fTmp1 = kR[1, iRow];
                            kR[0, iRow] = fCos1 * fTmp0 - fSin1 * fTmp1;
                            kR[1, iRow] = fSin1 * fTmp0 + fCos1 * fTmp1;
                        }

                        kS[0] = fCos0 * fCos1 * kA[0, 0] -
                                fSin1 * (fCos0 * kA[0, 1] - fSin0 * kA[1, 1]);
                        kS[1] = fSin0 * fSin1 * kA[0, 0] +
                                fCos1 * (fSin0 * kA[0, 1] + fCos0 * kA[1, 1]);
                        kS[2] = kA[2, 2];
                        break;
                    }
                    else
                    {
                        golubKahanStep(ref kA, ref kL, ref kR);
                    }
                }
            }

            // positize diagonal
            for (int iRow = 0; iRow < 3; iRow++)
            {
                if (kS[iRow] < 0.0f)
                {
                    kS[iRow] = -kS[iRow];

                    for (int iCol = 0; iCol < 3; iCol++)
                    {
                        kR[iRow, iCol] = -kR[iRow, iCol];
                    }
                }
            }

            rkL = kL;
            rkS = kS;
            rkR = kR;
        }

        /** singular value decomposition */
        public void singularValueComposition(Matrix3 rkL, Vector3 rkS, Matrix3 rkR)
        {
            Matrix3 kTmp = Matrix3.zero();

            // product S*R
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    kTmp[iRow, iCol] = rkS[iRow] * rkR[iRow, iCol];
                }
            }

            // product L*S*R
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    setElt(iRow, iCol, 0.0f);

                    for (int iMid = 0; iMid < 3; iMid++)
                    {
                        setElt(iRow, iCol, elt(iRow, iCol) + rkL[iRow, iMid] * kTmp[iMid, iCol]);
                    }
                }
            }
        }

        private const float ms_fSvdEpsilon = 1e-04f;
        private const int ms_iSvdMaxIterations = 32;

        private static void bidiagonalize(ref Matrix3 kA, ref Matrix3 kL, ref Matrix3 kR)
        {
            float[] afV = new float[3];
            float[] afW = new float[3];
            float fLength, fSign, fT1, fInvT1, fT2;
            bool bIdentity;

            // map first column to (*,0,0)
            fLength = (float)global::System.Math.Sqrt(kA[0, 0] * kA[0, 0] + kA[1, 0] * kA[1, 0] +
                                                     kA[2, 0] * kA[2, 0]);

            if (fLength > 0.0f)
            {
                fSign = (kA[0, 0] > 0.0f ? 1.0f : -1.0f);
                fT1 = kA[0, 0] + fSign * fLength;
                fInvT1 = 1.0f / fT1;
                afV[1] = kA[1, 0] * fInvT1;
                afV[2] = kA[2, 0] * fInvT1;

                fT2 = -2.0f / (1.0f + afV[1] * afV[1] + afV[2] * afV[2]);
                afW[0] = fT2 * (kA[0, 0] + kA[1, 0] * afV[1] + kA[2, 0] * afV[2]);
                afW[1] = fT2 * (kA[0, 1] + kA[1, 1] * afV[1] + kA[2, 1] * afV[2]);
                afW[2] = fT2 * (kA[0, 2] + kA[1, 2] * afV[1] + kA[2, 2] * afV[2]);
                kA[0, 0] += afW[0];
                kA[0, 1] += afW[1];
                kA[0, 2] += afW[2];
                kA[1, 1] += afV[1] * afW[1];
                kA[1, 2] += afV[1] * afW[2];
                kA[2, 1] += afV[2] * afW[1];
                kA[2, 2] += afV[2] * afW[2];

                kL[0, 0] = 1.0f + fT2;
                kL[0, 1] = fT2 * afV[1];
                kL[1, 0] = fT2 * afV[1];
                kL[0, 2] = fT2 * afV[2];
                kL[2, 0] = fT2 * afV[2];
                kL[1, 1] = 1.0f + fT2 * afV[1] * afV[1];
                kL[1, 2] = fT2 * afV[1] * afV[2];
                kL[2, 1] = fT2 * afV[1] * afV[2];
                kL[2, 2] = 1.0f + fT2 * afV[2] * afV[2];
                bIdentity = false;
            }
            else
            {
                kL = Matrix3.identity();
                bIdentity = true;
            }

            // map first row to (*,*,0)
            fLength = (float)global::System.Math.Sqrt(kA[0, 1] * kA[0, 1] + kA[0, 2] * kA[0, 2]);

            if (fLength > 0.0f)
            {
                fSign = (kA[0, 1] > 0.0f ? 1.0f : -1.0f);
                fT1 = kA[0, 1] + fSign * fLength;
                afV[2] = kA[0, 2] / fT1;

                fT2 = -2.0f / (1.0f + afV[2] * afV[2]);
                afW[0] = fT2 * (kA[0, 1] + kA[0, 2] * afV[2]);
                afW[1] = fT2 * (kA[1, 1] + kA[1, 2] * afV[2]);
                afW[2] = fT2 * (kA[2, 1] + kA[2, 2] * afV[2]);
                kA[0, 1] += afW[0];
                kA[1, 1] += afW[1];
                kA[1, 2] += afW[1] * afV[2];
                kA[2, 1] += afW[2];
                kA[2, 2] += afW[2] * afV[2];

                kR[0, 0] = 1.0f;
                kR[0, 1] = 0.0f;
                kR[1, 0] = 0.0f;
                kR[0, 2] = 0.0f;
                kR[2, 0] = 0.0f;
                kR[1, 1] = 1.0f + fT2;
                kR[1, 2] = fT2 * afV[2];
                kR[2, 1] = fT2 * afV[2];
                kR[2, 2] = 1.0f + fT2 * afV[2] * afV[2];
            }
            else
            {
                kR = Matrix3.identity();
            }

            // map second column to (*,*,0)
            fLength = (float)global::System.Math.Sqrt(kA[1, 1] * kA[1, 1] + kA[2, 1] * kA[2, 1]);

            if (fLength > 0.0f)
            {
                fSign = (kA[1, 1] > 0.0f ? 1.0f : -1.0f);
                fT1 = kA[1, 1] + fSign * fLength;
                afV[2] = kA[2, 1] / fT1;

                fT2 = -2.0f / (1.0f + afV[2] * afV[2]);
                afW[1] = fT2 * (kA[1, 1] + kA[2, 1] * afV[2]);
                afW[2] = fT2 * (kA[1, 2] + kA[2, 2] * afV[2]);
                kA[1, 1] += afW[1];
                kA[1, 2] += afW[2];
                kA[2, 2] += afV[2] * afW[2];

                float fA = 1.0f + fT2;
                float fB = fT2 * afV[2];
                float fC = 1.0f + fB * afV[2];

                if (bIdentity)
                {
                    kL[0, 0] = 1.0f;
                    kL[0, 1] = 0.0f;
                    kL[1, 0] = 0.0f;
                    kL[0, 2] = 0.0f;
                    kL[2, 0] = 0.0f;
                    kL[1, 1] = fA;
                    kL[1, 2] = fB;
                    kL[2, 1] = fB;
                    kL[2, 2] = fC;
                }
                else
                {
                    for (int iRow = 0; iRow < 3; iRow++)
                    {
                        float fTmp0 = kL[iRow, 1];
                        float fTmp1 = kL[iRow, 2];
                        kL[iRow, 1] = fA * fTmp0 + fB * fTmp1;
                        kL[iRow, 2] = fB * fTmp0 + fC * fTmp1;
                    }
                }
            }
        }

        private static void golubKahanStep(ref Matrix3 kA, ref Matrix3 kL, ref Matrix3 kR)
        {
            float fT11 = kA[0, 1] * kA[0, 1] + kA[1, 1] * kA[1, 1];
            float fT22 = kA[1, 2] * kA[1, 2] + kA[2, 2] * kA[2, 2];
            float fT12 = kA[1, 1] * kA[1, 2];
            float fTrace = fT11 + fT22;
            float fDiff = fT11 - fT22;
            float fDiscr = (float)global::System.Math.Sqrt(fDiff * fDiff + 4.0f * fT12 * fT12);
            float fRoot1 = 0.5f * (fTrace + fDiscr);
            float fRoot2 = 0.5f * (fTrace - fDiscr);

            // adjust right
            float fY = kA[0, 0] - (g3dmath.abs(fRoot1 - fT22) <=
                                  g3dmath.abs(fRoot2 - fT22) ? fRoot1 : fRoot2);
            float fZ = kA[0, 1];
            float fInvLength = 1.0f / (float)global::System.Math.Sqrt(fY * fY + fZ * fZ);
            float fSin = fZ * fInvLength;
            float fCos = -fY * fInvLength;

            float fTmp0 = kA[0, 0];
            float fTmp1 = kA[0, 1];
            kA[0, 0] = fCos * fTmp0 - fSin * fTmp1;
            kA[0, 1] = fSin * fTmp0 + fCos * fTmp1;
            kA[1, 0] = -fSin * kA[1, 1];
            kA[1, 1] *= fCos;

            int iRow;

            for (iRow = 0; iRow < 3; iRow++)
            {
                fTmp0 = kR[0, iRow];
                fTmp1 = kR[1, iRow];
                kR[0, iRow] = fCos * fTmp0 - fSin * fTmp1;
                kR[1, iRow] = fSin * fTmp0 + fCos * fTmp1;
            }

            // adjust left
            fY = kA[0, 0];

            fZ = kA[1, 0];

            fInvLength = 1.0f / (float)global::System.Math.Sqrt(fY * fY + fZ * fZ);

            fSin = fZ * fInvLength;

            fCos = -fY * fInvLength;

            kA[0, 0] = fCos * kA[0, 0] - fSin * kA[1, 0];

            fTmp0 = kA[0, 1];

            fTmp1 = kA[1, 1];

            kA[0, 1] = fCos * fTmp0 - fSin * fTmp1;

            kA[1, 1] = fSin * fTmp0 + fCos * fTmp1;

            kA[0, 2] = -fSin * kA[1, 2];

            kA[1, 2] *= fCos;

            int iCol;

            for (iCol = 0; iCol < 3; iCol++)
            {
                fTmp0 = kL[iCol, 0];
                fTmp1 = kL[iCol, 1];
                kL[iCol, 0] = fCos * fTmp0 - fSin * fTmp1;
                kL[iCol, 1] = fSin * fTmp0 + fCos * fTmp1;
            }

            // adjust right
            fY = kA[0, 1];

            fZ = kA[0, 2];

            fInvLength = 1.0f / (float)global::System.Math.Sqrt(fY * fY + fZ * fZ);

            fSin = fZ * fInvLength;

            fCos = -fY * fInvLength;

            kA[0, 1] = fCos * kA[0, 1] - fSin * kA[0, 2];

            fTmp0 = kA[1, 1];

            fTmp1 = kA[1, 2];

            kA[1, 1] = fCos * fTmp0 - fSin * fTmp1;

            kA[1, 2] = fSin * fTmp0 + fCos * fTmp1;

            kA[2, 1] = -fSin * kA[2, 2];

            kA[2, 2] *= fCos;

            for (iRow = 0; iRow < 3; iRow++)
            {
                fTmp0 = kR[1, iRow];
                fTmp1 = kR[2, iRow];
                kR[1, iRow] = fCos * fTmp0 - fSin * fTmp1;
                kR[2, iRow] = fSin * fTmp0 + fCos * fTmp1;
            }

            // adjust left
            fY = kA[1, 1];

            fZ = kA[2, 1];

            fInvLength = 1.0f / (float)global::System.Math.Sqrt(fY * fY + fZ * fZ);

            fSin = fZ * fInvLength;

            fCos = -fY * fInvLength;

            kA[1, 1] = fCos * kA[1, 1] - fSin * kA[2, 1];

            fTmp0 = kA[1, 2];

            fTmp1 = kA[2, 2];

            kA[1, 2] = fCos * fTmp0 - fSin * fTmp1;

            kA[2, 2] = fSin * fTmp0 + fCos * fTmp1;

            for (iCol = 0; iCol < 3; iCol++)
            {
                fTmp0 = kL[iCol, 1];
                fTmp1 = kL[iCol, 2];
                kL[iCol, 1] = fCos * fTmp0 - fSin * fTmp1;
                kL[iCol, 2] = fSin * fTmp0 + fCos * fTmp1;
            }
        }

        /** orthogonal Q, diagonal D, upper triangular U stored as (u01,u02,u12) */
        public void qDUDecomposition(out Matrix3 kQ, out Vector3 kD, out Vector3 kU)
        {
            // Factor M = QR = QDU where Q is orthogonal, D is diagonal,
            // and U is upper triangular with ones on its diagonal.  Algorithm uses
            // Gram-Schmidt orthogonalization (the QR algorithm).
            //
            // If M = [ m0 | m1 | m2 ] and Q = [ q0 | q1 | q2 ], then
            //
            //   q0 = m0/|m0|
            //   q1 = (m1-(q0*m1)q0)/|m1-(q0*m1)q0|
            //   q2 = (m2-(q0*m2)q0-(q1*m2)q1)/|m2-(q0*m2)q0-(q1*m2)q1|
            //
            // where |V| indicates length of vector V and A*B indicates dot
            // product of vectors A and B.  The matrix R has entries
            //
            //   r00 = q0*m0  r01 = q0*m1  r02 = q0*m2
            //   r10 = 0      r11 = q1*m1  r12 = q1*m2
            //   r20 = 0      r21 = 0      r22 = q2*m2
            //
            // so D = diag(r00,r11,r22) and U has entries u01 = r01/r00,
            // u02 = r02/r00, and u12 = r12/r11.

            // Q = rotation
            // D = scaling
            // U = shear

            // D stores the three diagonal entries r00, r11, r22
            // U stores the entries U[0] = u01, U[1] = u02, U[2] = u12

            // build orthogonal matrix Q
            float fInvLength = 1.0f / (float)global::System.Math.Sqrt(elt(0, 0) * elt(0, 0) +
                                                   elt(1, 0) * elt(1, 0) +
                                                   elt(2, 0) * elt(2, 0));
            kQ = Matrix3.zero();
            kQ[0, 0] = elt(0, 0) * fInvLength;
            kQ[1, 0] = elt(1, 0) * fInvLength;
            kQ[2, 0] = elt(2, 0) * fInvLength;

            float fDot = kQ[0, 0] * elt(0, 1) + kQ[1, 0] * elt(1, 1) +
                         kQ[2, 0] * elt(2, 1);
            kQ[0, 1] = elt(0, 1) - fDot * kQ[0, 0];
            kQ[1, 1] = elt(1, 1) - fDot * kQ[1, 0];
            kQ[2, 1] = elt(2, 1) - fDot * kQ[2, 0];
            fInvLength = 1.0f / (float)global::System.Math.Sqrt(kQ[0, 1] * kQ[0, 1] + kQ[1, 1] * kQ[1, 1] +
                                                     kQ[2, 1] * kQ[2, 1]);
            kQ[0, 1] *= fInvLength;
            kQ[1, 1] *= fInvLength;
            kQ[2, 1] *= fInvLength;

            fDot = kQ[0, 0] * elt(0, 2) + kQ[1, 0] * elt(1, 2) +
                   kQ[2, 0] * elt(2, 2);
            kQ[0, 2] = elt(0, 2) - fDot * kQ[0, 0];
            kQ[1, 2] = elt(1, 2) - fDot * kQ[1, 0];
            kQ[2, 2] = elt(2, 2) - fDot * kQ[2, 0];
            fDot = kQ[0, 1] * elt(0, 2) + kQ[1, 1] * elt(1, 2) +
                   kQ[2, 1] * elt(2, 2);
            kQ[0, 2] -= fDot * kQ[0, 1];
            kQ[1, 2] -= fDot * kQ[1, 1];
            kQ[2, 2] -= fDot * kQ[2, 1];
            fInvLength = 1.0f / (float)global::System.Math.Sqrt(kQ[0, 2] * kQ[0, 2] + kQ[1, 2] * kQ[1, 2] +
                                                     kQ[2, 2] * kQ[2, 2]);
            kQ[0, 2] *= fInvLength;
            kQ[1, 2] *= fInvLength;
            kQ[2, 2] *= fInvLength;

            // guarantee that orthogonal matrix has determinant 1 (no reflections)
            float fDet = kQ[0, 0] * kQ[1, 1] * kQ[2, 2] + kQ[0, 1] * kQ[1, 2] * kQ[2, 0] +
                         kQ[0, 2] * kQ[1, 0] * kQ[2, 1] - kQ[0, 2] * kQ[1, 1] * kQ[2, 0] -
                         kQ[0, 1] * kQ[1, 0] * kQ[2, 2] - kQ[0, 0] * kQ[1, 2] * kQ[2, 1];

            if (fDet < 0.0f)
            {
                for (int iRow = 0; iRow < 3; iRow++)
                {
                    for (int iCol = 0; iCol < 3; iCol++)
                    {
                        kQ[iRow, iCol] = -kQ[iRow, iCol];
                    }
                }
            }

            // build "right" matrix R
            Matrix3 kR = Matrix3.zero();

            kR[0, 0] = kQ[0, 0] * elt(0, 0) + kQ[1, 0] * elt(1, 0) +
                       kQ[2, 0] * elt(2, 0);

            kR[0, 1] = kQ[0, 0] * elt(0, 1) + kQ[1, 0] * elt(1, 1) +
                       kQ[2, 0] * elt(2, 1);

            kR[1, 1] = kQ[0, 1] * elt(0, 1) + kQ[1, 1] * elt(1, 1) +
                       kQ[2, 1] * elt(2, 1);

            kR[0, 2] = kQ[0, 0] * elt(0, 2) + kQ[1, 0] * elt(1, 2) +
                       kQ[2, 0] * elt(2, 2);

            kR[1, 2] = kQ[0, 1] * elt(0, 2) + kQ[1, 1] * elt(1, 2) +
                       kQ[2, 1] * elt(2, 2);

            kR[2, 2] = kQ[0, 2] * elt(0, 2) + kQ[1, 2] * elt(1, 2) +
                       kQ[2, 2] * elt(2, 2);

            // the scaling component
            kD = Vector3.zero();
            kD[0] = kR[0, 0];

            kD[1] = kR[1, 1];

            kD[2] = kR[2, 2];

            // the shear component
            float fInvD0 = 1.0f / kD[0];

            kU = Vector3.zero();
            kU[0] = kR[0, 1] * fInvD0;

            kU[1] = kR[0, 2] * fInvD0;

            kU[2] = kR[1, 2] / kD[1];
        }

        private static float maxCubicRoot(float[] afCoeff)
        {
            // Spectral norm is for A^T*A, so characteristic polynomial
            // P(x) = c[0]+c[1]*x+c[2]*x^2+x^3 has three positive float roots.
            // This yields the assertions c[0] < 0 and c[2]*c[2] >= 3*c[1].

            // quick out for uniform scale (triple root)
            const float fOneThird = 1.0f / 3.0f;
            const float fEpsilon = 1e-06f;
            float fDiscr = afCoeff[2] * afCoeff[2] - 3.0f * afCoeff[1];

            if (fDiscr <= fEpsilon)
            {
                return -fOneThird * afCoeff[2];
            }

            // Compute an upper bound on roots of P(x).  This assumes that A^T*A
            // has been scaled by its largest entry.
            float fX = 1.0f;

            float fPoly = afCoeff[0] + fX * (afCoeff[1] + fX * (afCoeff[2] + fX));

            if (fPoly < 0.0f)
            {
                // uses a matrix norm to find an upper bound on maximum root
                fX = (float)g3dmath.abs(afCoeff[0]);
                float fTmp = 1.0f + (float)g3dmath.abs(afCoeff[1]);

                if (fTmp > fX)
                {
                    fX = fTmp;
                }

                fTmp = 1.0f + (float)g3dmath.abs(afCoeff[2]);

                if (fTmp > fX)
                {
                    fX = fTmp;
                }
            }

            // Newton's method to find root
            float fTwoC2 = 2.0f * afCoeff[2];

            for (int i = 0; i < 16; i++)
            {
                fPoly = afCoeff[0] + fX * (afCoeff[1] + fX * (afCoeff[2] + fX));

                if (g3dmath.abs(fPoly) <= fEpsilon)
                {
                    return fX;
                }

                float fDeriv = afCoeff[1] + fX * (fTwoC2 + 3.0f * fX);

                fX -= fPoly / fDeriv;
            }

            return fX;
        }

        public float spectralNorm()
        {
            Matrix3 kP = Matrix3.zero();
            int iRow, iCol;
            float fPmax = 0.0f;

            for (iRow = 0; iRow < 3; iRow++)
            {
                for (iCol = 0; iCol < 3; iCol++)
                {
                    kP[iRow, iCol] = 0.0f;

                    for (int iMid = 0; iMid < 3; iMid++)
                    {
                        kP[iRow, iCol] +=
                            elt(iMid, iRow) * elt(iMid, iCol);
                    }

                    if (kP[iRow, iCol] > fPmax)
                    {
                        fPmax = kP[iRow, iCol];
                    }
                }
            }

            float fInvPmax = 1.0f / fPmax;

            for (iRow = 0; iRow < 3; iRow++)
            {
                for (iCol = 0; iCol < 3; iCol++)
                {
                    kP[iRow, iCol] *= fInvPmax;
                }
            }

            float[] afCoeff = new float[3];
            afCoeff[0] = -(kP[0, 0] * (kP[1, 1] * kP[2, 2] - kP[1, 2] * kP[2, 1]) +
                           kP[0, 1] * (kP[2, 0] * kP[1, 2] - kP[1, 0] * kP[2, 2]) +
                           kP[0, 2] * (kP[1, 0] * kP[2, 1] - kP[2, 0] * kP[1, 1]));
            afCoeff[1] = kP[0, 0] * kP[1, 1] - kP[0, 1] * kP[1, 0] +
                         kP[0, 0] * kP[2, 2] - kP[0, 2] * kP[2, 0] +
                         kP[1, 1] * kP[2, 2] - kP[1, 2] * kP[2, 1];
            afCoeff[2] = -(kP[0, 0] + kP[1, 1] + kP[2, 2]);

            float fRoot = maxCubicRoot(afCoeff);
            float fNorm = (float)global::System.Math.Sqrt(fPmax * fRoot);
            return fNorm;
        }

        private void tridiagonal(float[] afDiag, float[] afSubDiag)
        {
            // Householder reduction T = Q^t M Q
            //   Input:
            //     mat, symmetric 3x3 matrix M
            //   Output:
            //     mat, orthogonal matrix Q
            //     diag, diagonal entries of T
            //     subd, subdiagonal entries of T (T is symmetric)

            float fA = elt(0, 0);
            float fB = elt(0, 1);
            float fC = elt(0, 2);
            float fD = elt(1, 1);
            float fE = elt(1, 2);
            float fF = elt(2, 2);

            afDiag[0] = fA;
            afSubDiag[2] = 0.0f;

            if (g3dmath.abs(fC) >= EPSILON)
            {
                float fLength = (float)global::System.Math.Sqrt(fB * fB + fC * fC);
                float fInvLength = 1.0f / fLength;
                fB *= fInvLength;
                fC *= fInvLength;
                float fQ = 2.0f * fB * fE + fC * (fF - fD);
                afDiag[1] = fD + fC * fQ;
                afDiag[2] = fF - fC * fQ;
                afSubDiag[0] = fLength;
                afSubDiag[1] = fE - fB * fQ;
                setElt(0, 0, 1.0f);
                setElt(0, 1, 0.0f);
                setElt(0, 2, 0.0f);
                setElt(1, 0, 0.0f);
                setElt(1, 1, fB);
                setElt(1, 2, fC);
                setElt(2, 0, 0.0f);
                setElt(2, 1, fC);
                setElt(2, 2, -fB);
            }
            else
            {
                afDiag[1] = fD;
                afDiag[2] = fF;
                afSubDiag[0] = fB;
                afSubDiag[1] = fE;
                setElt(0, 0, 1.0f);
                setElt(0, 1, 0.0f);
                setElt(0, 2, 0.0f);
                setElt(1, 0, 0.0f);
                setElt(1, 1, 1.0f);
                setElt(1, 2, 0.0f);
                setElt(2, 0, 0.0f);
                setElt(2, 1, 0.0f);
                setElt(2, 2, 1.0f);
            }
        }

        private bool qLAlgorithm(float[] afDiag, float[] afSubDiag)
        {
            // QL iteration with implicit shifting to reduce matrix from tridiagonal
            // to diagonal

            for (int i0 = 0; i0 < 3; i0++)
            {
                const int iMaxIter = 32;
                int iIter;
                int i1;

                for (iIter = 0; iIter < iMaxIter; iIter++)
                {
                    for (i1 = i0; i1 <= 1; i1++)
                    {
                        float fSum = (float)g3dmath.abs(afDiag[i1]) +
                                    (float)g3dmath.abs(afDiag[i1 + 1]);

                        if ((float)g3dmath.abs(afSubDiag[i1]) + fSum == fSum)
                        {
                            break;
                        }
                    }

                    if (i1 == i0)
                    {
                        break;
                    }

                    float fTmp0 = (afDiag[i0 + 1] - afDiag[i0]) / (2.0f * afSubDiag[i0]);

                    float fTmp1 = (float)global::System.Math.Sqrt(fTmp0 * fTmp0 + 1.0f);

                    if (fTmp0 < 0.0f)
                    {
                        fTmp0 = afDiag[i1] - afDiag[i0] + afSubDiag[i0] / (fTmp0 - fTmp1);
                    }
                    else
                    {
                        fTmp0 = afDiag[i1] - afDiag[i0] + afSubDiag[i0] / (fTmp0 + fTmp1);
                    }

                    float fSin = 1.0f;

                    float fCos = 1.0f;

                    float fTmp2 = 0.0f;

                    for (int i2 = i1 - 1; i2 >= i0; i2--)
                    {
                        float fTmp3 = fSin * afSubDiag[i2];
                        float fTmp4 = fCos * afSubDiag[i2];

                        if (g3dmath.abs(fTmp3) >= g3dmath.abs(fTmp0))
                        {
                            fCos = fTmp0 / fTmp3;
                            fTmp1 = (float)global::System.Math.Sqrt(fCos * fCos + 1.0f);
                            afSubDiag[i2 + 1] = fTmp3 * fTmp1;
                            fSin = 1.0f / fTmp1;
                            fCos *= fSin;
                        }
                        else
                        {
                            fSin = fTmp3 / fTmp0;
                            fTmp1 = (float)global::System.Math.Sqrt(fSin * fSin + 1.0f);
                            afSubDiag[i2 + 1] = fTmp0 * fTmp1;
                            fCos = 1.0f / fTmp1;
                            fSin *= fCos;
                        }

                        fTmp0 = afDiag[i2 + 1] - fTmp2;
                        fTmp1 = (afDiag[i2] - fTmp0) * fSin + 2.0f * fTmp4 * fCos;
                        fTmp2 = fSin * fTmp1;
                        afDiag[i2 + 1] = fTmp0 + fTmp2;
                        fTmp0 = fCos * fTmp1 - fTmp4;

                        for (int iRow = 0; iRow < 3; iRow++)
                        {
                            fTmp3 = elt(iRow, i2 + 1);
                            setElt(iRow, i2 + 1, fSin * elt(iRow, i2) +
                                                  fCos * fTmp3);
                            setElt(iRow, i2, fCos * elt(iRow, i2) -
                                            fSin * fTmp3);
                        }
                    }

                    afDiag[i0] -= fTmp2;
                    afSubDiag[i0] = fTmp0;
                    afSubDiag[i1] = 0.0f;
                }

                if (iIter == iMaxIter)
                {
                    // should not get here under normal circumstances
                    return false;
                }
            }

            return true;
        }

        /** eigensolver, matrix must be symmetric */
        public void eigenSolveSymmetric(out float[] afEigenvalue, out Vector3[] akEigenvector)
        {
            Matrix3 kMatrix = this;
            float[] afSubDiag = new float[3];
            afEigenvalue = new float[3];
            kMatrix.tridiagonal(afEigenvalue, afSubDiag);
            kMatrix.qLAlgorithm(afEigenvalue, afSubDiag);

            akEigenvector = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                akEigenvector[i] = new Vector3(kMatrix[0, i], kMatrix[1, i], kMatrix[2, i]);
            }

            // make eigenvectors form a right--handed system
            Vector3 kCross = akEigenvector[1].cross(akEigenvector[2]);

            float fDet = akEigenvector[0].dot(kCross);

            if (fDet < 0.0f)
            {
                akEigenvector[2] = -akEigenvector[2];
            }
        }

        public void toAxisAngle(out Vector3 rkAxis, out float rfRadians)
        {
            float fTrace = elt(0, 0) + elt(1, 1) + elt(2, 2);
            float fCos = 0.5f * (fTrace - 1.0f);
            rfRadians = (float)g3dmath.aCos(fCos);

            if (rfRadians > 0.0f)
            {
                if (rfRadians < g3dmath.G3D_PI)
                {
                    rkAxis.x = elt(2, 1) - elt(1, 2);
                    rkAxis.y = elt(0, 2) - elt(2, 0);
                    rkAxis.z = elt(1, 0) - elt(0, 1);
                    rkAxis.unitize(1e-06f);
                }
                else
                {
                    float fHalfInverse;

                    if (elt(0, 0) >= elt(1, 1))
                    {
                        if (elt(0, 0) >= elt(2, 2))
                        {
                            rkAxis.x = 0.5f * (float)global::System.Math.Sqrt(elt(0, 0) -
                                                        elt(1, 1) - elt(2, 2) + 1.0);
                            fHalfInverse = 0.5f / rkAxis.x;
                            rkAxis.y = fHalfInverse * elt(0, 1);
                            rkAxis.z = fHalfInverse * elt(0, 2);
                        }
                        else
                        {
                            rkAxis.z = 0.5f * (float)global::System.Math.Sqrt(elt(2, 2) -
                                                        elt(0, 0) - elt(1, 1) + 1.0);
                            fHalfInverse = 0.5f / rkAxis.z;
                            rkAxis.x = fHalfInverse * elt(0, 2);
                            rkAxis.y = fHalfInverse * elt(1, 2);
                        }
                    }
                    else
                    {
                        if (elt(1, 1) >= elt(2, 2))
                        {
                            rkAxis.y = 0.5f * (float)global::System.Math.Sqrt(elt(1, 1) -
                                                        elt(0, 0) - elt(2, 2) + 1.0);
                            fHalfInverse = 0.5f / rkAxis.y;
                            rkAxis.x = fHalfInverse * elt(0, 1);
                            rkAxis.z = fHalfInverse * elt(1, 2);
                        }
                        else
                        {
                            rkAxis.z = 0.5f * (float)global::System.Math.Sqrt(elt(2, 2) -
                                                        elt(0, 0) - elt(1, 1) + 1.0);
                            fHalfInverse = 0.5f / rkAxis.z;
                            rkAxis.x = fHalfInverse * elt(0, 2);
                            rkAxis.y = fHalfInverse * elt(1, 2);
                        }
                    }
                }
            }
            else
            {
                rkAxis.x = 1.0f;
                rkAxis.y = 0.0f;
                rkAxis.z = 0.0f;
            }
        }

        public static Matrix3 fromAxisAngle(Vector3 rkAxis, float fRadians)
        {
            Matrix3 m = new Matrix3();

            float fCos = (float)global::System.Math.Cos(fRadians);
            float fSin = (float)global::System.Math.Sin(fRadians);
            float fOneMinusCos = 1.0f - fCos;
            float fX2 = rkAxis.x * rkAxis.x;
            float fY2 = rkAxis.y * rkAxis.y;
            float fZ2 = rkAxis.z * rkAxis.z;
            float fXYM = rkAxis.x * rkAxis.y * fOneMinusCos;
            float fXZM = rkAxis.x * rkAxis.z * fOneMinusCos;
            float fYZM = rkAxis.y * rkAxis.z * fOneMinusCos;
            float fXSin = rkAxis.x * fSin;
            float fYSin = rkAxis.y * fSin;
            float fZSin = rkAxis.z * fSin;

            m.setElt(0, 0, fX2 * fOneMinusCos + fCos);
            m.setElt(0, 1, fXYM - fZSin);
            m.setElt(0, 2, fXZM + fYSin);
            m.setElt(1, 0, fXYM + fZSin);
            m.setElt(1, 1, fY2 * fOneMinusCos + fCos);
            m.setElt(1, 2, fYZM - fXSin);
            m.setElt(2, 0, fXZM - fYSin);
            m.setElt(2, 1, fYZM + fXSin);
            m.setElt(2, 2, fZ2 * fOneMinusCos + fCos);

            return m;
        }

        public bool toEulerAnglesXYZ(out float rfXAngle, out float rfYAngle, out float rfZAngle)
        {
            if (elt(0, 2) < 1.0f)
            {
                if (elt(0, 2) > -1.0f)
                {
                    rfXAngle = (float)g3dmath.aTan2(-elt(1, 2), elt(2, 2));
                    rfYAngle = (float)g3dmath.aSin(elt(0, 2));
                    rfZAngle = (float)g3dmath.aTan2(-elt(0, 1), elt(0, 0));
                    return true;
                }
                else
                {
                    rfXAngle = -(float)g3dmath.aTan2(elt(1, 0), elt(1, 1));
                    rfYAngle = -(float)g3dmath.G3D_HALF_PI;
                    rfZAngle = 0.0f;
                    return false;
                }
            }
            else
            {
                rfXAngle = (float)g3dmath.aTan2(elt(1, 0), elt(1, 1));
                rfYAngle = (float)g3dmath.G3D_HALF_PI;
                rfZAngle = 0.0f;
                return false;
            }
        }

        public bool toEulerAnglesXZY(out float rfXAngle, out float rfZAngle, out float rfYAngle)
        {
            if (elt(0, 1) < 1.0f)
            {
                if (elt(0, 1) > -1.0f)
                {
                    rfXAngle = (float)g3dmath.aTan2(elt(2, 1), elt(1, 1));
                    rfZAngle = (float)g3dmath.aSin(-elt(0, 1));
                    rfYAngle = (float)g3dmath.aTan2(elt(0, 2), elt(0, 0));
                    return true;
                }
                else
                {
                    rfXAngle = (float)g3dmath.aTan2(elt(2, 0), elt(2, 2));
                    rfZAngle = (float)g3dmath.G3D_HALF_PI;
                    rfYAngle = 0.0f;
                    return false;
                }
            }
            else
            {
                rfXAngle = (float)g3dmath.aTan2(-elt(2, 0), elt(2, 2));
                rfZAngle = -(float)g3dmath.G3D_HALF_PI;
                rfYAngle = 0.0f;
                return false;
            }
        }

        public bool toEulerAnglesYXZ(out float rfYAngle, out float rfXAngle, out float rfZAngle)
        {
            if (elt(1, 2) < 1.0)
            {
                if (elt(1, 2) > -1.0)
                {
                    rfYAngle = (float)g3dmath.aTan2(elt(0, 2), elt(2, 2));
                    rfXAngle = (float)g3dmath.aSin(-elt(1, 2));
                    rfZAngle = (float)g3dmath.aTan2(elt(1, 0), elt(1, 1));
                    return true;
                }
                else
                {
                    rfYAngle = (float)g3dmath.aTan2(elt(0, 1), elt(0, 0));
                    rfXAngle = (float)g3dmath.G3D_HALF_PI;
                    rfZAngle = 0.0f;
                    return false;
                }
            }
            else
            {
                rfYAngle = (float)g3dmath.aTan2(-elt(0, 1), elt(0, 0));
                rfXAngle = -(float)g3dmath.G3D_HALF_PI;
                rfZAngle = 0.0f;
                return false;
            }
        }

        public bool toEulerAnglesYZX(out float rfYAngle, out float rfZAngle, out float rfXAngle)
        {
            if (elt(1, 0) < 1.0)
            {
                if (elt(1, 0) > -1.0)
                {
                    rfYAngle = (float)g3dmath.aTan2(-elt(2, 0), elt(0, 0));
                    rfZAngle = (float)g3dmath.aSin(elt(1, 0));
                    rfXAngle = (float)g3dmath.aTan2(-elt(1, 2), elt(1, 1));
                    return true;
                }
                else
                {
                    rfYAngle = -(float)g3dmath.aTan2(elt(2, 1), elt(2, 2));
                    rfZAngle = -(float)g3dmath.G3D_HALF_PI;
                    rfXAngle = 0.0f;
                    return false;
                }
            }
            else
            {
                rfYAngle = (float)g3dmath.aTan2(elt(2, 1), elt(2, 2));
                rfZAngle = (float)g3dmath.G3D_HALF_PI;
                rfXAngle = 0.0f;
                return false;
            }
        }

        public bool toEulerAnglesZXY(out float rfZAngle, out float rfXAngle, out float rfYAngle)
        {
            if (elt(2, 1) < 1.0f)
            {
                if (elt(2, 1) > -1.0f)
                {
                    rfZAngle = (float)g3dmath.aTan2(-elt(0, 1), elt(1, 1));
                    rfXAngle = (float)g3dmath.aSin(elt(2, 1));
                    rfYAngle = (float)g3dmath.aTan2(-elt(2, 0), elt(2, 2));
                    return true;
                }
                else
                {
                    // WARNING.  Not unique.  ZA - YA = -atan(r02,r00)
                    rfZAngle = -(float)g3dmath.aTan2(elt(0, 2), elt(0, 0));
                    rfXAngle = -(float)g3dmath.G3D_HALF_PI;
                    rfYAngle = 0.0f;
                    return false;
                }
            }
            else
            {
                // WARNING.  Not unique.  ZA + YA = atan2(r02,r00)
                rfZAngle = (float)g3dmath.aTan2(elt(0, 2), elt(0, 0));
                rfXAngle = (float)g3dmath.G3D_HALF_PI;
                rfYAngle = 0.0f;
                return false;
            }
        }

        public bool toEulerAnglesZYX(out float rfZAngle, out float rfYAngle, out float rfXAngle)
        {
            if (elt(2, 0) < 1.0f)
            {
                if (elt(2, 0) > -1.0f)
                {
                    rfZAngle = (float)g3dmath.aTan2(elt(1, 0), elt(0, 0));
                    rfYAngle = -(float)g3dmath.aSin(elt(2, 1));
                    rfXAngle = (float)g3dmath.aTan2(elt(2, 1), elt(2, 2));
                    return true;
                }
                else
                {
                    rfZAngle = -(float)g3dmath.aTan2(elt(0, 1), elt(0, 2));
                    rfYAngle = (float)g3dmath.G3D_HALF_PI;
                    rfXAngle = 0.0f;
                    return false;
                }
            }
            else
            {
                rfZAngle = (float)g3dmath.aTan2(-elt(0, 1), -elt(0, 2));
                rfYAngle = -(float)g3dmath.G3D_HALF_PI;
                rfXAngle = 0.0f;
                return false;
            }
        }

        public static Matrix3 fromEulerAnglesXYZ(float fYAngle, float fPAngle, float fRAngle)
        {
            float fCos, fSin;

            fCos = (float)global::System.Math.Cos(fYAngle);
            fSin = (float)global::System.Math.Sin(fYAngle);
            Matrix3 kXMat = new Matrix3(1.0f, 0.0f, 0.0f, 0.0f, fCos, -fSin, 0.0f, fSin, fCos);

            fCos = (float)global::System.Math.Cos(fPAngle);
            fSin = (float)global::System.Math.Sin(fPAngle);
            Matrix3 kYMat = new Matrix3(fCos, 0.0f, fSin, 0.0f, 1.0f, 0.0f, -fSin, 0.0f, fCos);

            fCos = (float)global::System.Math.Cos(fRAngle);
            fSin = (float)global::System.Math.Sin(fRAngle);
            Matrix3 kZMat = new Matrix3(fCos, -fSin, 0.0f, fSin, fCos, 0.0f, 0.0f, 0.0f, 1.0f);

            return kXMat * (kYMat * kZMat);
        }

        public static Matrix3 fromEulerAnglesXZY(float fYAngle, float fPAngle, float fRAngle)
        {
            float fCos, fSin;

            fCos = (float)global::System.Math.Cos(fYAngle);
            fSin = (float)global::System.Math.Sin(fYAngle);
            Matrix3 kXMat = new Matrix3(1.0f, 0.0f, 0.0f, 0.0f, fCos, -fSin, 0.0f, fSin, fCos);

            fCos = (float)global::System.Math.Cos(fPAngle);
            fSin = (float)global::System.Math.Sin(fPAngle);
            Matrix3 kZMat = new Matrix3(fCos, -fSin, 0.0f, fSin, fCos, 0.0f, 0.0f, 0.0f, 1.0f);

            fCos = (float)global::System.Math.Cos(fRAngle);
            fSin = (float)global::System.Math.Sin(fRAngle);
            Matrix3 kYMat = new Matrix3(fCos, 0.0f, fSin, 0.0f, 1.0f, 0.0f, -fSin, 0.0f, fCos);

            return kXMat * (kZMat * kYMat);
        }

        public static Matrix3 fromEulerAnglesYXZ(float fYAngle, float fPAngle, float fRAngle)
        {
            float fCos, fSin;

            fCos = (float)global::System.Math.Cos(fYAngle);
            fSin = (float)global::System.Math.Sin(fYAngle);
            Matrix3 kYMat = new Matrix3(fCos, 0.0f, fSin, 0.0f, 1.0f, 0.0f, -fSin, 0.0f, fCos);

            fCos = (float)global::System.Math.Cos(fPAngle);
            fSin = (float)global::System.Math.Sin(fPAngle);
            Matrix3 kXMat = new Matrix3(1.0f, 0.0f, 0.0f, 0.0f, fCos, -fSin, 0.0f, fSin, fCos);

            fCos = (float)global::System.Math.Cos(fRAngle);
            fSin = (float)global::System.Math.Sin(fRAngle);
            Matrix3 kZMat = new Matrix3(fCos, -fSin, 0.0f, fSin, fCos, 0.0f, 0.0f, 0.0f, 1.0f);

            return kYMat * (kXMat * kZMat);
        }

        public static Matrix3 fromEulerAnglesYZX(float fYAngle, float fPAngle, float fRAngle)
        {
            float fCos, fSin;

            fCos = (float)global::System.Math.Cos(fYAngle);
            fSin = (float)global::System.Math.Sin(fYAngle);
            Matrix3 kYMat = new Matrix3(fCos, 0.0f, fSin, 0.0f, 1.0f, 0.0f, -fSin, 0.0f, fCos);

            fCos = (float)global::System.Math.Cos(fPAngle);
            fSin = (float)global::System.Math.Sin(fPAngle);
            Matrix3 kZMat = new Matrix3(fCos, -fSin, 0.0f, fSin, fCos, 0.0f, 0.0f, 0.0f, 1.0f);

            fCos = (float)global::System.Math.Cos(fRAngle);
            fSin = (float)global::System.Math.Sin(fRAngle);
            Matrix3 kXMat = new Matrix3(1.0f, 0.0f, 0.0f, 0.0f, fCos, -fSin, 0.0f, fSin, fCos);

            return kYMat * (kZMat * kXMat);
        }

        public static Matrix3 fromEulerAnglesZXY(float fYAngle, float fPAngle, float fRAngle)
        {
            float fCos, fSin;

            fCos = (float)global::System.Math.Cos(fYAngle);
            fSin = (float)global::System.Math.Sin(fYAngle);
            Matrix3 kZMat = new Matrix3(fCos, -fSin, 0.0f, fSin, fCos, 0.0f, 0.0f, 0.0f, 1.0f);

            fCos = (float)global::System.Math.Cos(fPAngle);
            fSin = (float)global::System.Math.Sin(fPAngle);
            Matrix3 kXMat = new Matrix3(1.0f, 0.0f, 0.0f, 0.0f, fCos, -fSin, 0.0f, fSin, fCos);

            fCos = (float)global::System.Math.Cos(fRAngle);
            fSin = (float)global::System.Math.Sin(fRAngle);
            Matrix3 kYMat = new Matrix3(fCos, 0.0f, fSin, 0.0f, 1.0f, 0.0f, -fSin, 0.0f, fCos);

            return kZMat * (kXMat * kYMat);
        }

        public static Matrix3 fromEulerAnglesZYX(float fYAngle, float fPAngle, float fRAngle)
        {
            float fCos, fSin;

            fCos = (float)global::System.Math.Cos(fYAngle);
            fSin = (float)global::System.Math.Sin(fYAngle);
            Matrix3 kZMat = new Matrix3(fCos, -fSin, 0.0f, fSin, fCos, 0.0f, 0.0f, 0.0f, 1.0f);

            fCos = (float)global::System.Math.Cos(fPAngle);
            fSin = (float)global::System.Math.Sin(fPAngle);
            Matrix3 kYMat = new Matrix3(fCos, 0.0f, fSin, 0.0f, 1.0f, 0.0f, -fSin, 0.0f, fCos);

            fCos = (float)global::System.Math.Cos(fRAngle);
            fSin = (float)global::System.Math.Sin(fRAngle);
            Matrix3 kXMat = new Matrix3(1.0f, 0.0f, 0.0f, 0.0f, fCos, -fSin, 0.0f, fSin, fCos);

            return kZMat * (kYMat * kXMat);
        }

        public static void tensorProduct(Vector3 rkU, Vector3 rkV, out Matrix3 rkProduct)
        {
            rkProduct = new Matrix3();
            for (int iRow = 0; iRow < 3; iRow++)
            {
                for (int iCol = 0; iCol < 3; iCol++)
                {
                    rkProduct.setElt(iRow, iCol, rkU[iRow] * rkV[iCol]);
                }
            }
        }

        public string toString()
        {
            return Format.format("[%g, %g, %g; %g, %g, %g; %g, %g, %g]",
                elt(0, 0), elt(0, 1), elt(0, 2),
                elt(1, 0), elt(1, 1), elt(1, 2),
                elt(2, 0), elt(2, 1), elt(2, 2));
        }

        public static Matrix3 zero()
        {
            return new Matrix3(0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        public static Matrix3 identity()
        {
            return new Matrix3(1, 0, 0, 0, 1, 0, 0, 0, 1);
        }

        public static readonly Matrix3 ZERO = zero();
        public static readonly Matrix3 IDENTITY = identity();
    }
}
