/**
  @file Matrix4.h
 
  4x4 matrix class
 
  @maintainer Morgan McGuire, matrix@graphics3d.com
 
  @created 2003-10-02
  @edited  2006-02-05
 */

using System;

namespace G3D
{
    /**
      A 4x4 matrix.

      See also G3D::CoordinateFrame, G3D::Matrix3, G3D::Quat
     */
    public struct Matrix4
    {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;
        public float m30, m31, m32, m33;

        /**
          Computes the determinant of the 3x3 matrix that lacks excludeRow
          and excludeCol.
        */
        private float subDeterminant(int excludeRow, int excludeCol)
        {
            // Compute non-excluded row and column indices
            int r0, r1, r2;
            int c0, c1, c2;

            if (excludeRow == 0) { r0 = 1; r1 = 2; r2 = 3; }
            else if (excludeRow == 1) { r0 = 0; r1 = 2; r2 = 3; }
            else if (excludeRow == 2) { r0 = 0; r1 = 1; r2 = 3; }
            else { r0 = 0; r1 = 1; r2 = 2; }

            if (excludeCol == 0) { c0 = 1; c1 = 2; c2 = 3; }
            else if (excludeCol == 1) { c0 = 0; c1 = 2; c2 = 3; }
            else if (excludeCol == 2) { c0 = 0; c1 = 1; c2 = 3; }
            else { c0 = 0; c1 = 1; c2 = 2; }

            // Compute the first row of cofactors
            float cofactor00 =
              elt(r1, c1) * elt(r2, c2) -
              elt(r1, c2) * elt(r2, c1);

            float cofactor10 =
              elt(r1, c2) * elt(r2, c0) -
              elt(r1, c0) * elt(r2, c2);

            float cofactor20 =
              elt(r1, c0) * elt(r2, c1) -
              elt(r1, c1) * elt(r2, c0);

            // Product of the first row and the cofactors along the first row
            return
              elt(r0, c0) * cofactor00 +
              elt(r0, c1) * cofactor10 +
              elt(r0, c2) * cofactor20;
        }

        public Matrix4(
            float r1c1, float r1c2, float r1c3, float r1c4,
            float r2c1, float r2c2, float r2c3, float r2c4,
            float r3c1, float r3c2, float r3c3, float r3c4,
            float r4c1, float r4c2, float r4c3, float r4c4)
        {
            m00 = r1c1; m01 = r1c2; m02 = r1c3; m03 = r1c4;
            m10 = r2c1; m11 = r2c2; m12 = r2c3; m13 = r2c4;
            m20 = r3c1; m21 = r3c2; m22 = r3c3; m23 = r3c4;
            m30 = r4c1; m31 = r4c2; m32 = r4c3; m33 = r4c4;
        }

        /**
         init should be <B>row major</B>.
         */
        public Matrix4(float[] init)
        {
            this = default(Matrix4);
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    setElt(r, c, init[r * 4 + c]);
                }
            }
        }

        public Matrix4(CoordinateFrame c)
        {
            this = default(Matrix4);
            for (int r = 0; r < 3; ++r)
            {
                for (int cc = 0; cc < 3; ++cc)
                {
                    setElt(r, cc, c.rotation.elt(r, cc));
                }
                setElt(r, 3, c.translation[r]);
            }
            setElt(3, 0, 0.0f);
            setElt(3, 1, 0.0f);
            setElt(3, 2, 0.0f);
            setElt(3, 3, 1.0f);
        }

        public Matrix4(double[] init)
        {
            this = default(Matrix4);
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    setElt(r, c, (float)init[r * 4 + c]);
                }
            }
        }

        /**
         Produces an RT transformation that nearly matches this Matrix4.
            Because a Matrix4 may not be precisely a rotation and translation,
            this may introduce error. */
        public CoordinateFrame approxCoordinateFrame()
        {
            CoordinateFrame cframe = CoordinateFrame.identity();

            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 3; ++c)
                {
                    cframe.rotation.setElt(r, c, elt(r, c));
                }
                cframe.translation[r] = elt(r, 3);
            }

            // Ensure that the rotation matrix is orthonormal
            cframe.rotation.orthonormalize();

            return cframe;
        }

        public float elt(int row, int column)
        {
            if (row == 0)
            {
                if (column == 0) return m00;
                if (column == 1) return m01;
                if (column == 2) return m02;
                return m03;
            }
            if (row == 1)
            {
                if (column == 0) return m10;
                if (column == 1) return m11;
                if (column == 2) return m12;
                return m13;
            }
            if (row == 2)
            {
                if (column == 0) return m20;
                if (column == 1) return m21;
                if (column == 2) return m22;
                return m23;
            }
            if (column == 0) return m30;
            if (column == 1) return m31;
            if (column == 2) return m32;
            return m33;
        }

        public void setElt(int row, int column, float value)
        {
            if (row == 0)
            {
                if (column == 0) m00 = value;
                else if (column == 1) m01 = value;
                else if (column == 2) m02 = value;
                else m03 = value;
            }
            else if (row == 1)
            {
                if (column == 0) m10 = value;
                else if (column == 1) m11 = value;
                else if (column == 2) m12 = value;
                else m13 = value;
            }
            else if (row == 2)
            {
                if (column == 0) m20 = value;
                else if (column == 1) m21 = value;
                else if (column == 2) m22 = value;
                else m23 = value;
            }
            else
            {
                if (column == 0) m30 = value;
                else if (column == 1) m31 = value;
                else if (column == 2) m32 = value;
                else m33 = value;
            }
        }

        public float this[int row, int column]
        {
            get
            {
                Debug.debugAssert(row >= 0);
                Debug.debugAssert(row < 4);
                Debug.debugAssert(column >= 0);
                Debug.debugAssert(column < 4);
                return elt(row, column);
            }
            set
            {
                Debug.debugAssert(row >= 0);
                Debug.debugAssert(row < 4);
                Debug.debugAssert(column >= 0);
                Debug.debugAssert(column < 4);
                setElt(row, column, value);
            }
        }

        /**
         Special values.
         */
        public static Matrix4 identity()
        {
            return new Matrix4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
        }

        public static Matrix4 zero()
        {
            return new Matrix4(
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0);
        }

        /**
         Deprecated. See Matrix3::identity() for details.
         */
        public static readonly Matrix4 IDENTITY = identity();
        /** @deprecated Use Matrix4::zero() */
        public static readonly Matrix4 ZERO = zero();

        /**
         Constructs an orthogonal projection matrix from the given parameters.
         */
        public static Matrix4 orthogonalProjection(
            float left,
            float right,
            float bottom,
            float top,
            float nearval,
            float farval)
        {
            // Adapted from Mesa
            float x, y, z;
            float tx, ty, tz;

            x = 2.0f / (right - left);
            y = 2.0f / (top - bottom);
            z = -2.0f / (farval - nearval);
            tx = -(right + left) / (right - left);
            ty = -(top + bottom) / (top - bottom);
            tz = -(farval + nearval) / (farval - nearval);

            return
                new Matrix4(x, 0.0f, 0.0f, tx,
                            0.0f, y, 0.0f, ty,
                            0.0f, 0.0f, z, tz,
                            0.0f, 0.0f, 0.0f, 1.0f);
        }

        public static Matrix4 perspectiveProjection(
            float left,
            float right,
            float bottom,
            float top,
            float nearval,
            float farval)
        {
            float x, y, a, b, c, d;

            x = (2.0f * nearval) / (right - left);
            y = (2.0f * nearval) / (top - bottom);
            a = (right + left) / (right - left);
            b = (top + bottom) / (top - bottom);

            if ((float)farval >= (float)g3dmath.inf())
            {
               // Infinite view frustum
               c = -1.0f;
               d = -2.0f * nearval;
            }
            else
            {
               c = -(farval + nearval) / (farval - nearval);
               d = -(2.0f * farval * nearval) / (farval - nearval);
            }

            return new Matrix4(
                x, 0, a, 0,
                0, y, b, 0,
                0, 0, c, d,
                0, 0, -1, 0);
        }

        public void setRow(int r, Vector4 v)
        {
            for (int c = 0; c < 4; ++c)
            {
                setElt(r, c, v[c]);
            }
        }

        public void setColumn(int c, Vector4 v)
        {
            for (int r = 0; r < 4; ++r)
            {
                setElt(r, c, v[r]);
            }
        }

        public Vector4 getRow(int r)
        {
            Vector4 v = new Vector4();
            for (int c = 0; c < 4; ++c)
            {
                v[c] = elt(r, c);
            }
            return v;
        }

        public Vector4 getColumn(int c)
        {
            Vector4 v = new Vector4();
            for (int r = 0; r < 4; ++r)
            {
                v[r] = elt(r, c);
            }
            return v;
        }

        public static Matrix4 operator *(Matrix4 left, Matrix4 other)
        {
            Matrix4 result = new Matrix4();
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        result.setElt(r, c, result.elt(r, c) + left.elt(r, i) * other.elt(i, c));
                    }
                }
            }

            return result;
        }

        public static Matrix4 operator *(Matrix4 left, float s)
        {
            Matrix4 result = new Matrix4();
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    result.setElt(r, c, left.elt(r, c) * s);
                }
            }

            return result;
        }

        public static Vector4 operator *(Matrix4 left, Vector4 vector)
        {
            Vector4 result = new Vector4(0, 0, 0, 0);
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    result[r] += left.elt(r, c) * vector[c];
                }
            }

            return result;
        }

        public static Matrix4 operator *(Matrix4 left, CoordinateFrame right)
        {
            return left * right.toMatrix4();
        }

        public Matrix4 transpose()
        {
            Matrix4 result = new Matrix4();
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    result.setElt(c, r, elt(r, c));
                }
            }

            return result;
        }

        public static bool operator !=(Matrix4 left, Matrix4 right)
        {
            return !(left == right);
        }

        public static bool operator ==(Matrix4 left, Matrix4 right)
        {
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    if (left.elt(r, c) != right.elt(r, c))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is Matrix4 && this == (Matrix4)obj;
        }

        public override int GetHashCode()
        {
            return m00.GetHashCode() ^ m11.GetHashCode() ^ m22.GetHashCode() ^ m33.GetHashCode();
        }

        public float determinant()
        {
            // Determinant is the dot product of the first row and the first row
            // of cofactors (i.e. the first col of the adjoint matrix)
            return cofactor().getRow(0).dot(getRow(0));
        }

        public Matrix4 inverse()
        {
            // Inverse = adjoint / determinant

            Matrix4 A = adjoint();

            // Determinant is the dot product of the first row and the first row
            // of cofactors (i.e. the first col of the adjoint matrix)
            float det = A.getColumn(0).dot(getRow(0));

            return A * (1.0f / det);
        }

        /**
         Transpose of the cofactor matrix (used in computing the inverse).
         Note: This is in fact only one type of adjoint. More generally,
         an adjoint of a matrix is any mapping of a matrix which possesses
         certain properties.  This returns the so-called adjugate
         or classical adjoint.
        */
        public Matrix4 adjoint()
        {
            return cofactor().transpose();
        }

        public Matrix4 cofactor()
        {
            Matrix4 result = new Matrix4();

            // We'll use i to incrementally compute -1 ^ (r+c)
            int i = 1;

            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    // Compute the determinant of the 3x3 submatrix
                    float det = subDeterminant(r, c);
                    result.setElt(r, c, i * det);
                    i = -i;
                }
                i = -i;
            }

            return result;
        }

        /** Serializes row-major */
        public void serialize(BinaryOutput b)
        {
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    b.writeFloat32(elt(r, c));
                }
            }
        }

        public void deserialize(BinaryInput b)
        {
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    setElt(r, c, b.readFloat32());
                }
            }
        }

        public override string ToString()
        {
            return Format.format("[%g, %g, %g, %g; %g, %g, %g, %g; %g, %g, %g, %g; %g, %g, %g, %g]",
                elt(0, 0), elt(0, 1), elt(0, 2), elt(0, 3),
                elt(1, 0), elt(1, 1), elt(1, 2), elt(1, 3),
                elt(2, 0), elt(2, 1), elt(2, 2), elt(2, 3),
                elt(3, 0), elt(3, 1), elt(3, 2), elt(3, 3));
        }
    }
}
