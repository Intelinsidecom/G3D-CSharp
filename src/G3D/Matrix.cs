/**
  @file Matrix.cs

  Port of contrib/Matrix/Matrix.h and contrib/Matrix/Matrix.cpp.

  @maintainer Morgan McGuire, matrix@graphics3d.com

  <I>G3D::Matrix is licensed under the <A HREF="http://www.opensource.org/licenses/bsd-license.php">BSD license</A>

  Copyright &copy; 2000-2005, Morgan McGuire
  All rights reserved.
*/

using System;

namespace G3D
{
/**
  N x M matrix.

  Although this class has the semantics
  The actual data is tracked internally by a reference counted pointer;
  it is efficient to pass and assign Matrix objects because no data is actually copied.
  This avoids the headache of pointers and allows natural math notation:

  <PRE>
    Matrix A, B, C;
    // ...

    C = A * f(B);
    C = C.inverse();

    A = Matrix.identity(4);
    C = A;
    C.set(0, 0, 2.0); // Triggers a copy of the data so that A remains unchanged.

    // etc.

  </PRE>

  The Matrix.debugNumCopyOps and Matrix.debugNumAllocOps counters
  increment every time an operation forces the copy and allocation of matrices.  You
  can use these to detect slow operations when efficiency is a major concern.

  Some methods accept an output argument instead of returning a value.  For example,
  <CODE>A = B.transpose()</CODE> can also be invoked as <CODE>B.transpose(A)</CODE>.
  The latter may be more efficient, since Matrix may be able to re-use the storage of
  A (if it has approximatly the right size and isn't currently shared with another matrix).


*/
public class Matrix
{
    /**
      Internal precision.  Currently float, but this may become a templated class in the future
      to allow operations like Matrix<double> and Matrix<ComplexFloat>.

      Not necessarily a plain-old-data type (e.g., could ComplexFloat), but must be something
      with no constructor, that can be safely memcpyd, and that has a bit pattern of all zeros
      when zero.*/
    public const float T = 0;

    /** Incremented every time the elements of a matrix are copied.  Useful for profiling your
        own code that uses Matrix to determine when it is slow due to copying.*/
    public static int debugNumCopyOps;

    /** Incremented every time a new matrix object is allocated.  Useful for profiling your
        own code that uses Matrix to determine when it is slow due to allocation.*/
    public static int debugNumAllocOps;

    /**
      The reference-counted element storage.  Safe C# has no pointer type, so
      the row pointers of the original (T** elt) are replaced by index
      arithmetic over one row-major array, and the original's public
      svdCore(float**, int, int, float*, float**) low-level entry point keeps
      its behaviour but takes these storage objects instead of the pointers
      it cannot express.  The class and that entry point are therefore internal
      rather than public; Matrix::svd is the public way in.
    */
    internal class Impl : ReferenceCountedObject
    {
        /** Row major data for the entire matrix.  elt(r, c) indexes it. */
        public float[] data;

        /** The number of rows */
        public int R;

        /** The number of columns */
        public int C;

        private int dataSize;

        /**
          If R*C is much larger or smaller than the current, deletes all previous data
          and resets to random data.  Otherwise re-uses existing memory and just resets
          R, C, and the row pointers.
        */
        public void setSize(int newRows, int newCols)
        {
            if ((R == newRows) && (C == newCols))
            {
                // Nothing to do
                return;
            }

            int newSize = newRows * newCols;

            R = newRows; C = newCols;

            // Only allocate if we need more space
            // or the size difference is ridiculous
            if (((uint)newSize > (uint)dataSize) || ((uint)newSize < (uint)dataSize / 4))
            {
                data = new float[R * C];
                ++Matrix.debugNumAllocOps;
                dataSize = newSize;
            }

            // Safe C# has no null-with-zero-length allocation no-op: the original
            // memsets zero bytes through a possibly-null pointer, which C permits,
            // but Array.Clear throws on null.
            if (data != null)
            {
                Array.Clear(data, 0, R * C);
            }
        }

        public Impl()
        {
            data = null;
            R = 0;
            C = 0;
            dataSize = 0;
        }

        public Impl(Matrix3 M)
        {
            data = null;
            R = 0;
            C = 0;
            dataSize = 0;
            setSize(3, 3);
            for (int r = 0; r < 3; ++r)
            {
                for (int c = 0; c < 3; ++c)
                {
                    set(r, c, M.elt(r, c));
                }
            }
        }

        public Impl(Matrix4 M)
        {
            data = null;
            R = 0;
            C = 0;
            dataSize = 0;
            setSize(4, 4);
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    set(r, c, M.elt(r, c));
                }
            }
        }

        public Impl(int r, int c)
        {
            data = null;
            R = 0;
            C = 0;
            dataSize = 0;
            setSize(r, c);
        }

        public Impl(Impl B)
        {
            data = null;
            R = 0;
            C = 0;
            dataSize = 0;
            // Use the assignment operator
            this.assign(B);
        }

        public Impl assign(Impl m)
        {
            setSize(m.R, m.C);
            Array.Copy(m.data, data, R * C);
            ++Matrix.debugNumCopyOps;
            return this;
        }

        public void setZero()
        {
            // See setSize: the original's memset tolerates a null pointer with a
            // zero length, which Array.Clear does not.
            if (data != null)
            {
                Array.Clear(data, 0, R * C);
            }
        }

        public void set(int r, int c, float v)
        {
            Debug.debugAssert((uint)r < (uint)R);
            Debug.debugAssert((uint)c < (uint)C);
            data[r * C + c] = v;
        }

        public float get(int r, int c)
        {
            Debug.debugAssert((uint)r < (uint)R);
            Debug.debugAssert((uint)c < (uint)C);
            return data[r * C + c];
        }

        /** Multiplies this by B and puts the result in out. */
        public void mul(Impl B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssertM(
                !ReferenceEquals(this, outp) && !ReferenceEquals(B, outp),
                "Output argument to mul cannot be the same as an input argument.");

            Debug.debugAssert(A.C == B.R);
            Debug.debugAssert(A.R == outp.R);
            Debug.debugAssert(B.C == outp.C);

            for (int r = 0; r < outp.R; ++r)
            {
                for (int c = 0; c < outp.C; ++c)
                {
                    T_sum = 0.0f;
                    for (int i = 0; i < A.C; ++i)
                    {
                        T_sum += A.get(r, i) * B.get(i, c);
                    }
                    outp.set(r, c, T_sum);
                }
            }
        }

        /// Scratch accumulator so the inner loop needs no allocation.
        private static float T_sum;

        /** Ok if out == this or out == B */
        public void add(Impl B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == B.C);
            Debug.debugAssert(A.R == B.R);
            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = (float)(A.data[i] + B.data[i]);
            }
        }

        /** Ok if out == this or out == B */
        public void add(float B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = (float)(A.data[i] + B);
            }
        }

        /** Ok if out == this or out == B */
        public void sub(Impl B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == B.C);
            Debug.debugAssert(A.R == B.R);
            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = (float)(A.data[i] - B.data[i]);
            }
        }

        /** Ok if out == this or out == B */
        public void sub(float B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = (float)(A.data[i] - B);
            }
        }

        /** B - this */
        public void lsub(float B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = (float)(B - A.data[i]);
            }
        }

        /** Ok if out == this or out == B */
        public void arrayMul(Impl B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == B.C);
            Debug.debugAssert(A.R == B.R);
            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = (float)(A.data[i] * B.data[i]);
            }
        }

        /** Ok if out == this or out == B */
        public void mul(float B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = (float)(A.data[i] * B);
            }
        }

        /** Ok if out == this or out == B */
        public void arrayDiv(Impl B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == B.C);
            Debug.debugAssert(A.R == B.R);
            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = (float)(A.data[i] / B.data[i]);
            }
        }

        /** Ok if out == this or out == B */
        public void div(float B, Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = (float)(A.data[i] / B);
            }
        }

        public void negate(Impl outp)
        {
            Impl A = this;

            Debug.debugAssert(A.C == outp.C);
            Debug.debugAssert(A.R == outp.R);

            for (int i = R * C - 1; i >= 0; --i)
            {
                outp.data[i] = -A.data[i];
            }
        }

        /** Slow way of computing an inverse; for reference */
        public void inverseViaAdjoint(Impl outp)
        {
            Debug.debugAssert(!ReferenceEquals(this, outp));

            // Inverse = adjoint / determinant

            adjoint(outp);

            // Don't call the determinant method when we already have an
            // adjoint matrix; there's a faster way of computing it: the dot
            // product of the first row and the adjoint's first col.
            double det = 0.0;
            for (int r = R - 1; r >= 0; --r)
            {
                det += get(0, r) * outp.get(r, 0);
            }

            outp.div((float)det, outp);
        }

        /** Use Gaussian elimination with pivots to solve for the inverse destructively in place. */
        public void inverseInPlaceGaussJordan()
        {
            Debug.debugAssert(R == C);

            // Exchange to float elements
            // SWAP is spelled out inline below because C# has no macro.

            // The integer arrays pivot, rowIndex, and colIndex are
            // used for bookkeeping on the pivoting
            Array<int> colIndex = new Array<int>(), rowIndex = new Array<int>(), pivot = new Array<int>();

            int col = 0, row = 0;

            colIndex.resize(R);
            rowIndex.resize(R);
            pivot.resize(R);

            const int NO_PIVOT = -1;

            // Initialize the pivot array to default values.
            for (int i = 0; i < R; ++i)
            {
                pivot[i] = NO_PIVOT;
            }

            // This is the main loop over the columns to be reduced
            // Loop over the columns.
            for (int c = 0; c < R; ++c)
            {

                // Find the largest element and use that as a pivot
                float largestMagnitude = 0.0f;

                // This is the outer loop of the search for a pivot element
                for (int r = 0; r < R; ++r)
                {

                    // Unless we've already found the pivot, keep going
                    if (pivot[r] != 0)
                    {

                        // Find the largest pivot
                        for (int k = 0; k < R; ++k)
                        {
                            if (pivot[k] == NO_PIVOT)
                            {
                                float mag = Math.Abs(get(r, k));

                                if (mag >= largestMagnitude)
                                {
                                    largestMagnitude = mag;
                                    row = r; col = k;
                                }
                            }
                        }
                    }
                }

                pivot[col] += 1;

                // Interchange columns so that the pivot element is on the diagonal (we'll have to undo this
                // at the end)
                if (row != col)
                {
                    for (int k = 0; k < R; ++k)
                    {
                        // SWAP(elt[row][k], elt[col][k])
                        float temp = get(row, k);
                        set(row, k, get(col, k));
                        set(col, k, temp);
                    }
                }

                // The pivot is now at [row, col]
                rowIndex[c] = row;
                colIndex[c] = col;

                double piv = get(col, col);

                Debug.debugAssertM(piv != 0.0, "Matrix is singular");

                // Divide everything by the pivot (avoid computing the division
                // multiple times).
                double pivotInverse = 1.0 / piv;
                set(col, col, 1.0f);

                for (int k = 0; k < R; ++k)
                {
                    set(col, k, (float)(get(col, k) * pivotInverse));
                }

                // Reduce all rows
                for (int r = 0; r < R; ++r)
                {
                    // Skip over the pivot row
                    if (r != col)
                    {

                        double oldValue = get(r, col);
                        set(r, col, 0.0f);

                        for (int k = 0; k < R; ++k)
                        {
                            set(r, k, (float)(get(r, k) - get(col, k) * oldValue));
                        }
                    }
                }
            }


            // Put the columns back in the correct locations
            for (int i = R - 1; i >= 0; --i)
            {
                if (rowIndex[i] != colIndex[i])
                {
                    for (int k = 0; k < R; ++k)
                    {
                        // SWAP(elt[k][rowIndex[i]], elt[k][colIndex[i]])
                        float temp = get(k, rowIndex[i]);
                        set(k, rowIndex[i], get(k, colIndex[i]));
                        set(k, colIndex[i], temp);
                    }
                }
            }
        }


        public bool anyNonZero()
        {
            int N = R * C;
            for (int i = 0; i < N; ++i)
            {
                if (data[i] != 0.0)
                {
                    return true;
                }
            }
            return false;
        }


        public bool allNonZero()
        {
            int N = R * C;
            for (int i = 0; i < N; ++i)
            {
                if (data[i] == 0.0)
                {
                    return false;
                }
            }
            return true;
        }


        public void adjoint(Impl outp)
        {
            cofactor(outp);
            // Transpose is safe to perform in place
            outp.transpose(outp);
        }


        public void cofactor(Impl outp)
        {
            Debug.debugAssert(!ReferenceEquals(this, outp));
            for (int r = 0; r < R; ++r)
            {
                for (int c = 0; c < C; ++c)
                {
                    outp.set(r, c, cofactor(r, c));
                }
            }
        }


        /**
          Cofactor [r][c] is defined as C[r][c] = -1 ^(r+c) * det(A[r][c]),
          where A[r][c] is the (R-1)x(C-1) matrix formed by removing row r and
          column c from the original matrix.
        */
        public float cofactor(int r, int c)
        {
            // Strang p. 217
            float s = g3dmath.isEven(r + c) ? 1 : -1;

            return s * determinant(r, c);
        }


        public float determinant(int nr, int nc)
        {
            Debug.debugAssert(R > 0);
            Debug.debugAssert(C > 0);
            Impl A = new Impl(R - 1, C - 1);
            withoutRowAndCol(nr, nc, A);
            return A.determinant();
        }


        public void setRow(int r, float[] vals)
        {
            Debug.debugAssert(r >= 0);
            Array.Copy(vals, 0, data, r * C, C);
        }


        public void setCol(int c, float[] vals)
        {
            for (int r = 0; r < R; ++r)
            {
                data[r * C + c] = vals[r];
            }
        }


        public float determinant()
        {

            Debug.debugAssert(R == C);

            // Compute using cofactors
            switch (R)
            {
            case 0:
                return 0;

            case 1:
                // Determinant of a 1x1 is the element
                return get(0, 0);

            case 2:
                // Determinant of a 2x2 is ad-bc
                return (float)(get(0, 0) * get(1, 1) - get(0, 1) * get(1, 0));

            case 3:
                {
                    // Determinant of an nxn matrix is the dot product of the first
                    // row with the first row of cofactors.  The base cases of this
                    // method get called a lot, so we spell out the implementation
                    // for the 3x3 case.

                    double cofactor00 = get(1, 1) * get(2, 2) - get(1, 2) * get(2, 1);
                    double cofactor10 = get(1, 2) * get(2, 0) - get(1, 0) * get(2, 2);
                    double cofactor20 = get(1, 0) * get(2, 1) - get(1, 1) * get(2, 0);

                    return
                        (float)(get(0, 0) * cofactor00 +
                            get(0, 1) * cofactor10 +
                            get(0, 2) * cofactor20);
                }

            default:
                {
                    // Determinant of an n x n matrix is the dot product of the first
                    // row with the first row of cofactors
                    float det = 0.0f;

                    for (int c = 0; c < C; ++c)
                    {
                        det += get(0, c) * cofactor(0, c);
                    }

                    return det;
                }
            }
        }


        /** Makes a (R-1)x(C-1) copy of this matrix */
        public void withoutRowAndCol(int excludeRow, int excludeCol, Impl outp)
        {
            Debug.debugAssert(outp.R == R - 1);
            Debug.debugAssert(outp.C == C - 1);

            for (int r = 0; r < outp.R; ++r)
            {
                for (int c = 0; c < outp.C; ++c)
                {
                    outp.set(r, c,
                        get(r + ((r >= excludeRow) ? 1 : 0),
                            c + ((c >= excludeCol) ? 1 : 0)));
                }
            }
        }


        /** Ok if out == this or out == B */
        public void transpose(Impl outp)
        {
            Debug.debugAssert(outp.R == C);
            Debug.debugAssert(outp.C == R);

            if (ReferenceEquals(outp, this))
            {
                // Square matrix in place
                for (int r = 0; r < R; ++r)
                {
                    for (int c = r + 1; c < C; ++c)
                    {
                        float temp = get(r, c);
                        outp.set(r, c, get(c, r));
                        outp.set(c, r, temp);
                    }
                }
            }
            else
            {
                for (int r = 0; r < R; ++r)
                {
                    for (int c = 0; c < C; ++c)
                    {
                        outp.set(c, r, get(r, c));
                    }
                }
            }
        }


        public void swapRows(int r0, int r1)
        {
            for (int c = 0; c < C; ++c)
            {
                float temp = get(r0, c);
                set(r0, c, get(r1, c));
                set(r1, c, temp);
            }
        }


        /** Swaps columns c0 and c1 and negates both */
        public void swapAndNegateCols(int c0, int c1)
        {
            for (int r = 0; r < R; ++r)
            {
                float temp = -get(r, c0);
                set(r, c0, -get(r, c1));
                set(r, c1, temp);
            }
        }


        public void mulRow(int r, float v)
        {
            for (int c = 0; c < C; ++c)
            {
                set(r, c, get(r, c) * v);
            }
        }
    }

    private class SortRank
    {
        public float value;
        public int col;

        /* The original's operator< compares the other operand's value against
           this one, so the Array's increasing sort yields descending order. */
        public static int compareSortRank(SortRank a, SortRank b)
        {
            if (b.value < a.value) { return -1; }
            else if (b.value > a.value) { return 1; }
            else { return 0; }
        }
    }

    private ReferenceCountedPointer<Impl> impl;

    private Matrix(ReferenceCountedPointer<Impl> i)
    {
        impl = i;
    }

    public Matrix()
    {
        impl = new ReferenceCountedPointer<Impl>(new Impl(0, 0));
    }

    public Matrix(Matrix3 M)
    {
        impl = new ReferenceCountedPointer<Impl>(new Impl(M));
    }

    public Matrix(Matrix4 M)
    {
        impl = new ReferenceCountedPointer<Impl>(new Impl(M));
    }

    /** The original's copy constructor: a new Matrix object that shares the
        same reference-counted storage.  Plain C# assignment would alias the
        same Matrix object instead, which would make "Matrix Uold = U" corrupt
        both when one of them is re-assigned. */
    public Matrix(Matrix other)
    {
        impl = new ReferenceCountedPointer<Impl>(other.impl);
    }

    public Matrix(int R, int C)
    {
        impl = new ReferenceCountedPointer<Impl>(new Impl(R, C));
        impl.pointer().setZero();
    }

    private Impl implRef
    {
        get { return impl.pointer(); }
    }

    /** Returns a new matrix that is all zero. */
    public static Matrix zero(int R, int C)
    {
        var A = new Impl(R, C);
        A.setZero();
        return new Matrix(new ReferenceCountedPointer<Impl>(A));
    }

    /** Returns a new matrix that is all one. */
    public static Matrix one(int R, int C)
    {
        var A = new Impl(R, C);
        for (int i = R * C - 1; i >= 0; --i)
        {
            A.data[i] = 1.0f;
        }
        return new Matrix(new ReferenceCountedPointer<Impl>(A));
    }

    /** Returns a new identity matrix */
    public static Matrix identity(int N)
    {
        var m = new Impl(N, N);
        m.setZero();
        for (int i = 0; i < N; ++i)
        {
            m.set(i, i, 1.0f);
        }
        return new Matrix(new ReferenceCountedPointer<Impl>(m));
    }

    /** Uniformly distributed values between zero and one. */
    public static Matrix random(int R, int C)
    {
        var A = new Impl(R, C);
        for (int i = R * C - 1; i >= 0; --i)
        {
            A.data[i] = (float)g3dmath.random(0.0, 1.0);
        }
        return new Matrix(new ReferenceCountedPointer<Impl>(A));
    }

    /** The number of rows */
    public int rows()
    {
        return implRef.R;
    }

    /** Number of columns */
    public int cols()
    {
        return implRef.C;
    }

    public static Matrix operator *(Matrix A, Matrix B)
    {
        Matrix C = new Matrix(A.implRef.R, B.implRef.C);
        A.implRef.mul(B.implRef, C.implRef);
        return C;
    }

    public static Matrix operator *(Matrix A, float B)
    {
        Matrix C = new Matrix(A.implRef.R, A.implRef.C);
        A.implRef.mul(B, C.implRef);
        return C;
    }

    public static Matrix operator *(float B, Matrix A)
    {
        return A * B;
    }

    public static Matrix operator +(Matrix A, Matrix B)
    {
        Matrix C = new Matrix(A.implRef.R, A.implRef.C);
        A.implRef.add(B.implRef, C.implRef);
        return C;
    }

    public static Matrix operator +(Matrix A, float v)
    {
        Matrix C = new Matrix(A.implRef.R, A.implRef.C);
        A.implRef.add(v, C.implRef);
        return C;
    }

    public static Matrix operator +(float v, Matrix A)
    {
        return A + v;
    }

    public static Matrix operator -(Matrix A, Matrix B)
    {
        Matrix C = new Matrix(A.implRef.R, A.implRef.C);
        A.implRef.sub(B.implRef, C.implRef);
        return C;
    }

    public static Matrix operator -(Matrix A, float v)
    {
        Matrix C = new Matrix(A.implRef.R, A.implRef.C);
        A.implRef.sub(v, C.implRef);
        return C;
    }

    /** scalar B - this */
    public static Matrix operator -(float B, Matrix A)
    {
        return A.lsub(B);
    }

    /** scalar B - this */
    public Matrix lsub(float B)
    {
        Matrix C = new Matrix(implRef.R, implRef.C);
        implRef.lsub(B, C.implRef);
        return C;
    }

    public static Matrix operator -(Matrix A)
    {
        return A.negate();
    }

    public static Matrix operator <(Matrix A, float scalar)
    {
        int R = A.rows();
        int C = A.cols();
        int N = R * C;
        Matrix outp = Matrix.zero(R, C);
        for (int i = 0; i < N; ++i)
        {
            outp.implRef.data[i] = A.implRef.data[i] < scalar ? 1.0f : 0.0f;
        }
        return outp;
    }

    public static Matrix operator <=(Matrix A, float scalar)
    {
        int R = A.rows();
        int C = A.cols();
        int N = R * C;
        Matrix outp = Matrix.zero(R, C);
        for (int i = 0; i < N; ++i)
        {
            outp.implRef.data[i] = A.implRef.data[i] <= scalar ? 1.0f : 0.0f;
        }
        return outp;
    }

    public static Matrix operator >(Matrix A, float scalar)
    {
        int R = A.rows();
        int C = A.cols();
        int N = R * C;
        Matrix outp = Matrix.zero(R, C);
        for (int i = 0; i < N; ++i)
        {
            outp.implRef.data[i] = A.implRef.data[i] > scalar ? 1.0f : 0.0f;
        }
        return outp;
    }

    public static Matrix operator >=(Matrix A, float scalar)
    {
        int R = A.rows();
        int C = A.cols();
        int N = R * C;
        Matrix outp = Matrix.zero(R, C);
        for (int i = 0; i < N; ++i)
        {
            outp.implRef.data[i] = A.implRef.data[i] >= scalar ? 1.0f : 0.0f;
        }
        return outp;
    }

    public static Matrix operator ==(Matrix A, float scalar)
    {
        int R = A.rows();
        int C = A.cols();
        int N = R * C;
        Matrix outp = Matrix.zero(R, C);
        for (int i = 0; i < N; ++i)
        {
            outp.implRef.data[i] = A.implRef.data[i] == scalar ? 1.0f : 0.0f;
        }
        return outp;
    }

    public static Matrix operator !=(Matrix A, float scalar)
    {
        int R = A.rows();
        int C = A.cols();
        int N = R * C;
        Matrix outp = Matrix.zero(R, C);
        for (int i = 0; i < N; ++i)
        {
            outp.implRef.data[i] = A.implRef.data[i] != scalar ? 1.0f : 0.0f;
        }
        return outp;
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj);
    }

    public override int GetHashCode()
    {
        return implRef != null ? implRef.GetHashCode() : 0;
    }

    private void unshare()
    {
        if (!impl.isLastReference())
        {
            impl = new ReferenceCountedPointer<Impl>(new Impl(implRef));
        }
    }

    public void set(int r, int c, float v)
    {
        unshare();
        implRef.set(r, c, v);
    }

    public void setCol(int c, Matrix vec)
    {
        Debug.debugAssertM(vec.rows() == rows(),
            "A column must be set to a vector of the same size.");
        Debug.debugAssertM(vec.cols() == 1,
            "A column must be set to a column vector.");

        Debug.debugAssert(c >= 0);

        Debug.debugAssert(c < cols());

        unshare();
        implRef.setCol(c, vec.implRef.data);
    }

    public void setRow(int r, Matrix vec)
    {
        Debug.debugAssertM(vec.cols() == cols(),
            "A row must be set to a vector of the same size.");
        Debug.debugAssertM(vec.rows() == 1,
            "A row must be set to a row vector.");

        Debug.debugAssert(r >= 0);
        Debug.debugAssert(r < rows());

        unshare();
        implRef.setRow(r, vec.implRef.data);
    }

    public float get(int r, int c)
    {
        return implRef.get(r, c);
    }

    public Matrix row(int r)
    {
        Debug.debugAssert(r >= 0);
        Debug.debugAssert(r < rows());
        Matrix outp = new Matrix(1, cols());
        Array.Copy(implRef.data, r * implRef.C, outp.implRef.data, 0, implRef.C);
        return outp;
    }

    public Matrix col(int c)
    {
        Debug.debugAssert(c >= 0);
        Debug.debugAssert(c < cols());
        Matrix outp = new Matrix(rows(), 1);

        float[] outData = outp.implRef.data;
        // Get a pointer to the first element in the column
        int inElt = c;
        int R = rows();
        int C = cols();
        for (int r = 0; r < R; ++r)
        {
            outData[r] = implRef.data[inElt];
            // Skip around to the next row
            inElt += C;
        }

        return outp;
    }

    /**
      A<SUP>-1</SUP>
    */
    public Matrix inverse()
    {
        var A = new Impl(implRef);
        A.inverseInPlaceGaussJordan();
        return new Matrix(new ReferenceCountedPointer<Impl>(A));
    }

    /**
      A<SUP>T</SUP>
    */
    public Matrix transpose()
    {
        var A = new Impl(cols(), rows());
        implRef.transpose(A);
        return new Matrix(new ReferenceCountedPointer<Impl>(A));
    }

    /** Transpose in place; more efficient than transpose */
    public void transpose(ref Matrix outp)
    {
        if (impl == outp.impl && impl.isLastReference() && (implRef.R == implRef.C))
        {
            // In place
            implRef.transpose(outp.implRef);
        }
        else
        {
            outp = this.transpose();
        }
    }

    public Matrix negate()
    {
        Matrix C = new Matrix(implRef.R, implRef.C);
        implRef.negate(C.implRef);
        return C;
    }

    public void negate(ref Matrix outp)
    {
        if ((impl == outp.impl) && impl.isLastReference())
        {
            implRef.negate(outp.implRef);
        }
        else
        {
            outp = this.negate();
        }
    }

    public static Matrix abs(Matrix A)
    {
        return A.abs();
    }

    public Matrix abs()
    {
        Matrix C = new Matrix(implRef.R, implRef.C);
        for (int i = numElements() - 1; i >= 0; --i)
        {
            C.implRef.data[i] = Math.Abs(implRef.data[i]);
        }
        return C;
    }

    public void abs(ref Matrix outp)
    {
        if ((impl == outp.impl) && impl.isLastReference())
        {
            for (int i = numElements() - 1; i >= 0; --i)
            {
                implRef.data[i] = Math.Abs(implRef.data[i]);
            }
        }
        else
        {
            outp = this.abs();
        }
    }

    public Matrix arrayLog()
    {
        Matrix C = new Matrix(implRef.R, implRef.C);
        for (int i = numElements() - 1; i >= 0; --i)
        {
            C.implRef.data[i] = (float)Math.Log(implRef.data[i]);
        }
        return C;
    }

    public Matrix arrayExp()
    {
        Matrix C = new Matrix(implRef.R, implRef.C);
        for (int i = numElements() - 1; i >= 0; --i)
        {
            C.implRef.data[i] = (float)Math.Exp(implRef.data[i]);
        }
        return C;
    }

    public Matrix arraySqrt()
    {
        Matrix C = new Matrix(implRef.R, implRef.C);
        for (int i = numElements() - 1; i >= 0; --i)
        {
            C.implRef.data[i] = (float)Math.Sqrt(implRef.data[i]);
        }
        return C;
    }

    public Matrix arrayCos()
    {
        Matrix C = new Matrix(implRef.R, implRef.C);
        for (int i = numElements() - 1; i >= 0; --i)
        {
            C.implRef.data[i] = (float)Math.Cos(implRef.data[i]);
        }
        return C;
    }

    public Matrix arraySin()
    {
        Matrix C = new Matrix(implRef.R, implRef.C);
        for (int i = numElements() - 1; i >= 0; --i)
        {
            C.implRef.data[i] = (float)Math.Sin(implRef.data[i]);
        }
        return C;
    }

    /** The original's explicit-argument forms reuse this object's storage when
        the output can safely be mutated in place, and otherwise trampoline to
        the returning form. */
    public void arrayLog(ref Matrix outp)
    {
        if ((impl == outp.impl) && impl.isLastReference())
        {
            for (int i = numElements() - 1; i >= 0; --i)
            {
                implRef.data[i] = (float)Math.Log(implRef.data[i]);
            }
        }
        else
        {
            outp = this.arrayLog();
        }
    }

    public void arrayExp(ref Matrix outp)
    {
        if ((impl == outp.impl) && impl.isLastReference())
        {
            for (int i = numElements() - 1; i >= 0; --i)
            {
                implRef.data[i] = (float)Math.Exp(implRef.data[i]);
            }
        }
        else
        {
            outp = this.arrayExp();
        }
    }

    public void arraySqrt(ref Matrix outp)
    {
        if ((impl == outp.impl) && impl.isLastReference())
        {
            for (int i = numElements() - 1; i >= 0; --i)
            {
                implRef.data[i] = (float)Math.Sqrt(implRef.data[i]);
            }
        }
        else
        {
            outp = this.arraySqrt();
        }
    }

    public void arrayCos(ref Matrix outp)
    {
        if ((impl == outp.impl) && impl.isLastReference())
        {
            for (int i = numElements() - 1; i >= 0; --i)
            {
                implRef.data[i] = (float)Math.Cos(implRef.data[i]);
            }
        }
        else
        {
            outp = this.arrayCos();
        }
    }

    public void arraySin(ref Matrix outp)
    {
        if ((impl == outp.impl) && impl.isLastReference())
        {
            for (int i = numElements() - 1; i >= 0; --i)
            {
                implRef.data[i] = (float)Math.Sin(implRef.data[i]);
            }
        }
        else
        {
            outp = this.arraySin();
        }
    }

    public int numElements()
    {
        return rows() * cols();
    }


    /** Returns true if any element is non-zero */
    public bool anyNonZero()
    {
        return implRef.anyNonZero();
    }

    /** Returns true if all elements are non-zero */
    public bool allNonZero()
    {
        return implRef.allNonZero();
    }

    public bool allZero()
    {
        return !anyNonZero();
    }

    public bool anyZero()
    {
        return !allNonZero();
    }

    /** 2-norm (sqrt(sum(squares)) */
    public double norm()
    {
        int R = rows();
        int C = cols();
        int N = R * C;

        double sum = 0.0;

        for (int i = 0; i < N; ++i)
        {
            sum += g3dmath.square(implRef.data[i]);
        }

        return Math.Sqrt(sum);
    }

    public void swapRows(int r0, int r1)
    {
        Debug.debugAssert(r0 >= 0 && r0 < rows());
        Debug.debugAssert(r1 >= 0 && r1 < rows());

        if (r0 == r1)
        {
            return;
        }

        unshare();

        implRef.swapRows(r0, r1);
    }

    /** Swaps columns c0 and c1 and negates both */
    public void swapAndNegateCols(int c0, int c1)
    {
        Debug.debugAssert(c0 >= 0 && c0 < cols());
        Debug.debugAssert(c1 >= 0 && c1 < cols());

        if (c0 == c1)
        {
            return;
        }

        unshare();

        implRef.swapAndNegateCols(c0, c1);
    }

    public void mulRow(int r, float v)
    {
        Debug.debugAssert(r >= 0 && r < rows());

        unshare();

        implRef.mulRow(r, v);
    }

    /** Mutates this */
    public void arrayMulInPlace(Matrix _B)
    {
        Impl B = _B.implRef;

        if (!impl.isLastReference())
        {
            impl = new ReferenceCountedPointer<Impl>(new Impl(implRef));
        }

        implRef.arrayMul(B, implRef);
    }

    /** Mutates this */
    public void arrayDivInPlace(Matrix _B)
    {
        Impl B = _B.implRef;

        if (!impl.isLastReference())
        {
            impl = new ReferenceCountedPointer<Impl>(new Impl(implRef));
        }

        implRef.arrayDiv(B, implRef);
    }

    /** Generally more efficient than A * B */
    public void mulInPlace(float B)
    {
        ReferenceCountedPointer<Impl> A = impl;

        if (!A.isLastReference())
        {
            impl = new ReferenceCountedPointer<Impl>(new Impl(A.pointer().R, A.pointer().C));
        }

        A.pointer().mul(B, implRef);
    }

    /** Generally more efficient than A / B */
    public void divInPlace(float B)
    {
        ReferenceCountedPointer<Impl> A = impl;

        if (!A.isLastReference())
        {
            impl = new ReferenceCountedPointer<Impl>(new Impl(A.pointer().R, A.pointer().C));
        }

        A.pointer().div(B, implRef);
    }

    /** Generally more efficient than A + B */
    public void addInPlace(float B)
    {
        ReferenceCountedPointer<Impl> A = impl;

        if (!A.isLastReference())
        {
            impl = new ReferenceCountedPointer<Impl>(new Impl(A.pointer().R, A.pointer().C));
        }

        A.pointer().add(B, implRef);
    }

    /** Generally more efficient than A - B */
    public void subInPlace(float B)
    {
        ReferenceCountedPointer<Impl> A = impl;

        if (!A.isLastReference())
        {
            impl = new ReferenceCountedPointer<Impl>(new Impl(A.pointer().R, A.pointer().C));
        }

        A.pointer().sub(B, implRef);
    }

    /** No performance advantage over A * B because
        matrix multiplication requires intermediate
        storage. */
    public void mulInPlace(Matrix B)
    {
        // We can't optimize this one
        this.assign(this * B);
    }

    /** Generally more efficient than A + B */
    public void addInPlace(Matrix _B)
    {
        Impl B = _B.implRef;
        ReferenceCountedPointer<Impl> A = impl;

        if (!A.isLastReference())
        {
            impl = new ReferenceCountedPointer<Impl>(new Impl(A.pointer().R, A.pointer().C));
        }

        A.pointer().add(B, implRef);
    }

    /** Generally more efficient than A - B */
    public void subInPlace(Matrix _B)
    {
        Impl B = _B.implRef;
        ReferenceCountedPointer<Impl> A = impl;

        if (!A.isLastReference())
        {
            impl = new ReferenceCountedPointer<Impl>(new Impl(A.pointer().R, A.pointer().C));
        }

        A.pointer().sub(B, implRef);
    }

    /// Copy assignment across Matrix objects, mirroring C++ operator=.
    public Matrix assign(Matrix B)
    {
        impl = B.impl;
        return this;
    }

    /// The C# name of the exchange between two Matrix objects.
    public static void swap(ref Matrix A, ref Matrix B)
    {
        Matrix temp = A;
        A = B;
        B = temp;
    }

    public static Matrix fromDiagonal(Matrix d)
    {
        Debug.debugAssert((d.rows() == 1) || (d.cols() == 1));

        int n = d.numElements();
        Matrix D = zero(n, n);
        for (int i = 0; i < n; ++i)
        {
            D.set(i, i, d.implRef.data[i]);
        }

        return D;
    }

    public static Matrix fromDiagonal(Array<float> d)
    {
        Matrix D = zero(d.length(), d.length());
        for (int i = 0; i < d.length(); ++i)
        {
            D.set(i, i, d[i]);
        }
        return D;
    }

    /** Returns a new matrix that is a subset of this one,
        from r1:r2 to c1:c2, inclusive.*/
    public Matrix subMatrix(int r1, int r2, int c1, int c2)
    {
        Debug.debugAssert(r2 >= r1);
        Debug.debugAssert(c2 >= c1);
        Debug.debugAssert(c2 < cols());
        Debug.debugAssert(r2 < rows());
        Debug.debugAssert(r1 >= 0);
        Debug.debugAssert(c1 >= 0);

        Matrix X = new Matrix(r2 - r1 + 1, c2 - c1 + 1);

        for (int r = 0; r < X.rows(); ++r)
        {
            for (int c = 0; c < X.cols(); ++c)
            {
                X.set(r, c, get(r + r1, c + c1));
            }
        }

        return X;
    }

    public Matrix3 toMatrix3()
    {
        Debug.debugAssert(implRef.R == 3);
        Debug.debugAssert(implRef.C == 3);
        return new Matrix3(
            get(0, 0), get(0, 1), get(0, 2),
            get(1, 0), get(1, 1), get(1, 2),
            get(2, 0), get(2, 1), get(2, 2));
    }


    public Matrix4 toMatrix4()
    {
        Debug.debugAssert(implRef.R == 4);
        Debug.debugAssert(implRef.C == 4);
        return new Matrix4(
            get(0, 0), get(0, 1), get(0, 2), get(0, 3),
            get(1, 0), get(1, 1), get(1, 2), get(1, 3),
            get(2, 0), get(2, 1), get(2, 2), get(2, 3),
            get(3, 0), get(3, 1), get(3, 2), get(3, 3));
    }


    public Vector2 toVector2()
    {
        Debug.debugAssert(implRef.R * implRef.C == 2);
        if (implRef.R > implRef.C)
        {
            return new Vector2(get(0, 0), get(1, 0));
        }
        else
        {
            return new Vector2(get(0, 0), get(0, 1));
        }
    }


    public Vector3 toVector3()
    {
        Debug.debugAssert(implRef.R * implRef.C == 3);
        if (implRef.R > implRef.C)
        {
            return new Vector3(get(0, 0), get(1, 0), get(2, 0));
        }
        else
        {
            return new Vector3(get(0, 0), get(0, 1), get(0, 2));
        }
    }


    public Vector4 toVector4()
    {
        Debug.debugAssert(
            ((implRef.R == 4) && (implRef.C == 1)) ||
            ((implRef.R == 1) && (implRef.C == 4)));

        if (implRef.R > implRef.C)
        {
            return new Vector4(get(0, 0), get(1, 0), get(2, 0), get(3, 0));
        }
        else
        {
            return new Vector4(get(0, 0), get(0, 1), get(0, 2), get(0, 3));
        }
    }


    /** Singular value decomposition.  Factors into three matrices
        such that this = U * fromDiagonal(d) * V.transpose().

        The matrix must have at least as many rows as columns.

        @param sort If true (default), the singular values
        are arranged so that D is sorted from largest to smallest.
    */
    public void svd(Matrix U, Array<float> d, Matrix V, bool sort = true)
    {
        Debug.debugAssert(rows() >= cols());
        Debug.debugAssertM(!ReferenceEquals(U, V), "Arguments to SVD must be different matrices");
        Debug.debugAssertM(!ReferenceEquals(U, this), "Arguments to SVD must be different matrices");
        Debug.debugAssertM(!ReferenceEquals(V, this), "Arguments to SVD must be different matrices");

        int R = rows();
        int C = cols();

        // Make sure we don't overwrite a shared matrix.
        // The original assigns to its reference parameters; because Matrix is a
        // reference type here, the caller sees the change only if the object's
        // own storage is replaced rather than the parameter being rebound.
        if (!V.impl.isLastReference())
        {
            V.impl = new ReferenceCountedPointer<Impl>(new Impl(C, C));
            V.implRef.setZero();
        }
        else
        {
            V.implRef.setSize(C, C);
        }

        if (!ReferenceEquals(U, this) || !impl.isLastReference())
        {
            // Make a copy of this for in-place SVD
            U.impl = new ReferenceCountedPointer<Impl>(new Impl(implRef));
        }

        d.resize(C);
        string ret = svdCore(U, R, C, d.getCArray(), V);

        Debug.debugAssertM(ret == null, ret);

        if (sort)
        {
            // Sort the singular values from greatest to least

            Array<SortRank> rank = new Array<SortRank>(C);
            for (int c = 0; c < C; ++c)
            {
                SortRank s = new SortRank();
                s.col = c;
                s.value = d[c];
                rank[c] = s;
            }

            rank.sort(SortRank.compareSortRank);

            // The original's "Matrix Uold = U" constructs a new Matrix that
            // shares the storage; a plain C# assignment would alias the same
            // object and lose the unsorted factors when the new storage below
            // is installed.
            Matrix Uold = new Matrix(U);
            Matrix Vold = new Matrix(V);

            U.impl = new ReferenceCountedPointer<Impl>(new Impl(U.rows(), U.cols()));
            V.impl = new ReferenceCountedPointer<Impl>(new Impl(V.rows(), V.cols()));

            // Now permute U, d, and V appropriately
            for (int c0 = 0; c0 < C; ++c0)
            {
                int c1 = rank[c0].col;

                d[c0] = rank[c0].value;
                U.setCol(c0, Uold.col(c1));
                V.setCol(c0, Vold.col(c1));
            }

        }
    }


    /**
      (A<SUP>T</SUP>A)<SUP>-1</SUP>A<SUP>T</SUP>) computed
      using SVD.

      @param tolerance Use -1 for automatic tolerance.
    */
    public Matrix pseudoInverse(float tolerance = -1.0f)
    {
        if (cols() > rows())
        {
            return transpose().pseudoInverse(tolerance).transpose();
        }

        Matrix U, V;
        Array<float> d;

        U = new Matrix();
        d = new Array<float>();
        V = new Matrix();

        svd(U, d, V);

        if (rows() == 1)
        {
            d.resize(1);
        }

        if (tolerance < 0)
        {
            // TODO: Should be eps(d[0]), which is the largest diagonal
            tolerance = (float)g3dmath.max(rows(), cols()) * 0.0001f;
        }

        Matrix X;

        int r = 0;
        for (int i = 0; i < d.size(); ++i)
        {
            if (d[i] > tolerance)
            {
                d[i] = 1.0f / d[i];
                ++r;
            }
        }

        if (r == 0)
        {
            // There were no non-zero elements
            X = zero(cols(), rows());
        }
        else
        {
            // Use the first r columns
            U = U.subMatrix(0, U.rows() - 1, 0, r - 1).transpose();
            V = V.subMatrix(0, V.rows() - 1, 0, r - 1);

            d.resize(r);

            X = V * Matrix.fromDiagonal(d) * U;
        }

        return X;
    }

    /**
      (A<SUP>T</SUP>A)<SUP>-1</SUP>A<SUP>T</SUP>) computed
      using Gauss-Jordan elimination.
    */
    public Matrix gaussJordanPseudoInverse()
    {
        Matrix trans = transpose();
        return (trans * this).inverse() * trans;
    }

    /** Helper for svdCore */
    private static double pythag(double a, double b)
    {
        double at = Math.Abs(a), bt = Math.Abs(b), ct, result;

        if (at > bt)
        {
            ct = bt / at;
            result = at * Math.Sqrt(1.0 + g3dmath.square(ct));
        }
        else if (bt > 0.0)
        {
            ct = at / bt;
            result = bt * Math.Sqrt(1.0 + g3dmath.square(ct));
        }
        else
        {
            result = 0.0;
        }

        return result;
    }

    /**
      Low-level SVD functionality.  Useful for applications that do not want
      to construct a Matrix but need to perform the SVD operation.

      this = U * D * V'

      Assumes that rows >= cols

      @return null on success, a string describing the error on failure.
      @param U rows x cols matrix to be decomposed, gets overwritten with U, a rows x cols matrix with orthogonal columns.
      @param D vector of singular values of a (diagonal of the D matrix).  Length cols.
      @param V returns the right orthonormal transformation matrix, size cols x cols

      @cite Based on Dianne Cook's implementation, which is adapted from
      svdecomp.c in XLISP-STAT 2.1, which is code from Numerical Recipes
      adapted by Luke Tierney and David Betz.  The Numerical Recipes code
      is adapted from Forsythe et al, who based their code on Golub and
      Reinsch's original implementation.
    */
    public static string svdCore(Matrix U, int rows, int cols, float[] D, Matrix V)
    {
        const int MAX_ITERATIONS = 30;

        int flag, i, its, j, jj, k, l = 0, nm = 0;
        double c, f, h, s, x, y, z;
        double anorm = 0.0, g = 0.0, scale = 0.0;

        // Temp row vector
        double[] rv1;

        Debug.debugAssertM(rows >= cols, "Must have more rows than columns");

        rv1 = new double[cols];

        // Householder reduction to bidiagonal form
        for (i = 0; i < cols; i++)
        {

            // Left-hand reduction
            l = i + 1;
            rv1[i] = scale * g;
            g = s = scale = 0.0;

            if (i < rows)
            {

                for (k = i; k < rows; k++)
                {
                    scale += Math.Abs((double)U.get(k, i));
                }

                if (scale != 0.0)
                {
                    for (k = i; k < rows; k++)
                    {
                        U.set(k, i, (float)((double)U.get(k, i) / scale));
                        s += ((double)U.get(k, i) * (double)U.get(k, i));
                    }

                    f = (double)U.get(i, i);

                    // TODO: what is this 2-arg sign function?
                    g = -SIGN(Math.Sqrt(s), f);
                    h = f * g - s;
                    U.set(i, i, (float)(f - g));

                    if (i != cols - 1)
                    {
                        for (j = l; j < cols; j++)
                        {

                            for (s = 0.0, k = i; k < rows; k++)
                            {
                                s += ((double)U.get(k, i) * (double)U.get(k, j));
                            }

                            f = s / h;
                            for (k = i; k < rows; k++)
                            {
                                U.set(k, j, (float)((double)U.get(k, j) + f * (double)U.get(k, i)));
                            }
                        }
                    }
                    for (k = i; k < rows; k++)
                    {
                        U.set(k, i, (float)((double)U.get(k, i) * scale));
                    }
                }
            }
            D[i] = (float)(scale * g);

            // right-hand reduction
            g = s = scale = 0.0;
            if (i < rows && i != cols - 1)
            {
                for (k = l; k < cols; k++)
                {
                    scale += Math.Abs((double)U.get(i, k));
                }

                if (scale != 0.0)
                {
                    for (k = l; k < cols; ++k)
                    {
                        U.set(i, k, (float)((double)U.get(i, k) / scale));
                        s += ((double)U.get(i, k) * (double)U.get(i, k));
                    }

                    f = (double)U.get(i, l);
                    g = -SIGN(Math.Sqrt(s), f);
                    h = f * g - s;
                    U.set(i, l, (float)(f - g));

                    for (k = l; k < cols; k++)
                    {
                        rv1[k] = (double)U.get(i, k) / h;
                    }

                    if (i != rows - 1)
                    {

                        for (j = l; j < rows; j++)
                        {
                            for (s = 0.0, k = l; k < cols; ++k)
                            {
                                s += ((double)U.get(j, k) * (double)U.get(i, k));
                            }

                            for (k = l; k < cols; ++k)
                            {
                                U.set(j, k, (float)((double)U.get(j, k) + s * rv1[k]));
                            }
                        }
                    }

                    for (k = l; k < cols; k++)
                    {
                        U.set(i, k, (float)((double)U.get(i, k) * scale));
                    }
                }
            }

            anorm = g3dmath.max(anorm, Math.Abs((double)D[i]) + Math.Abs(rv1[i]));
        }

        // accumulate the right-hand transformation
        for (i = cols - 1; i >= 0; --i)
        {
            if (i < cols - 1)
            {
                if (g != 0.0)
                {
                    for (j = l; j < cols; j++)
                    {
                        V.set(j, i, (float)(((double)U.get(i, j) / (double)U.get(i, l)) / g));
                    }

                    // double division to avoid underflow
                    for (j = l; j < cols; j++)
                    {
                        for (s = 0.0, k = l; k < cols; k++)
                        {
                            s += ((double)U.get(i, k) * (double)V.get(k, j));
                        }

                        for (k = l; k < cols; k++)
                        {
                            V.set(k, j, (float)((double)V.get(k, j) + s * (double)V.get(k, i)));
                        }
                    }
                }

                for (j = l; j < cols; j++)
                {
                    V.set(i, j, 0.0f);
                    V.set(j, i, 0.0f);
                }
            }

            V.set(i, i, 1.0f);
            g = rv1[i];
            l = i;
        }

        // accumulate the left-hand transformation
        for (i = cols - 1; i >= 0; i--)
        {
            l = i + 1;
            g = (double)D[i];
            if (i < cols - 1)
            {
                for (j = l; j < cols; j++)
                {
                    U.set(i, j, 0.0f);
                }
            }

            if (g != 0.0)
            {
                g = 1.0 / g;
                if (i != cols - 1)
                {
                    for (j = l; j < cols; j++)
                    {
                        for (s = 0.0, k = l; k < rows; k++)
                        {
                            s += ((double)U.get(k, i) * (double)U.get(k, j));
                        }

                        f = (s / (double)U.get(i, i)) * g;

                        for (k = i; k < rows; k++)
                        {
                            U.set(k, j, (float)((double)U.get(k, j) + f * (double)U.get(k, i)));
                        }
                    }
                }

                for (j = i; j < rows; j++)
                {
                    U.set(j, i, (float)((double)U.get(j, i) * g));
                }

            }
            else
            {
                for (j = i; j < rows; j++)
                {
                    U.set(j, i, 0.0f);
                }
            }
            U.set(i, i, (float)((double)U.get(i, i) + 1.0));
        }

        // diagonalize the bidiagonal form
        for (k = cols - 1; k >= 0; --k)
        {
            // loop over singular values
            for (its = 0; its < MAX_ITERATIONS; ++its)
            {
                // loop over allowed iterations
                flag = 1;

                for (l = k; l >= 0; --l)
                {
                    // test for splitting
                    nm = l - 1;
                    if (Math.Abs(rv1[l]) + anorm == anorm)
                    {
                        flag = 0;
                        break;
                    }

                    if (Math.Abs((double)D[nm]) + anorm == anorm)
                    {
                        break;
                    }
                }

                if (flag != 0)
                {
                    c = 0.0;
                    s = 1.0;
                    for (i = l; i <= k; ++i)
                    {
                        f = s * rv1[i];
                        if (Math.Abs(f) + anorm != anorm)
                        {
                            g = (double)D[i];
                            h = pythag(f, g);
                            D[i] = (float)h;
                            h = 1.0 / h;
                            c = g * h;
                            s = (-f * h);
                            for (j = 0; j < rows; ++j)
                            {
                                y = (double)U.get(j, nm);
                                z = (double)U.get(j, i);
                                U.set(j, nm, (float)(y * c + z * s));
                                U.set(j, i, (float)(z * c - y * s));
                            }
                        }
                    }
                }

                z = (double)D[k];
                if (l == k)
                {
                    // convergence
                    if (z < 0.0)
                    {
                        // make singular value nonnegative
                        D[k] = (float)(-z);

                        for (j = 0; j < cols; ++j)
                        {
                            V.set(j, k, -V.get(j, k));
                        }
                    }
                    break;
                }

                if (its >= MAX_ITERATIONS)
                {
                    return "Failed to converge.";
                }

                // shift from bottom 2 x 2 minor
                x = (double)D[l];
                nm = k - 1;
                y = (double)D[nm];
                g = rv1[nm];
                h = rv1[k];
                f = ((y - z) * (y + z) + (g - h) * (g + h)) / (2.0 * h * y);
                g = pythag(f, 1.0);
                f = ((x - z) * (x + z) + h * ((y / (f + SIGN(g, f))) - h)) / x;

                // next QR transformation
                c = s = 1.0;
                for (j = l; j <= nm; ++j)
                {
                    i = j + 1;
                    g = rv1[i];
                    y = (double)D[i];
                    h = s * g;
                    g = c * g;
                    z = pythag(f, h);
                    rv1[j] = z;
                    c = f / z;
                    s = h / z;
                    f = x * c + g * s;
                    g = g * c - x * s;
                    h = y * s;
                    y = y * c;

                    for (jj = 0; jj < cols; ++jj)
                    {
                        x = (double)V.get(jj, j);
                        z = (double)V.get(jj, i);
                        V.set(jj, j, (float)(x * c + z * s));
                        V.set(jj, i, (float)(z * c - x * s));
                    }
                    z = pythag(f, h);
                    D[j] = (float)z;
                    if (z != 0.0)
                    {
                        z = 1.0 / z;
                        c = f * z;
                        s = h * z;
                    }
                    f = (c * g) + (s * y);
                    x = (c * y) - (s * g);
                    for (jj = 0; jj < rows; jj++)
                    {
                        y = (double)U.get(jj, j);
                        z = (double)U.get(jj, i);
                        U.set(jj, j, (float)(y * c + z * s));
                        U.set(jj, i, (float)(z * c - y * s));
                    }
                }
                rv1[l] = 0.0;
                rv1[k] = f;
                D[k] = (float)x;
            }
        }

        return null;
    }

    private static double SIGN(double a, double b)
    {
        return b >= 0.0 ? Math.Abs(a) : -Math.Abs(a);
    }
}
}
