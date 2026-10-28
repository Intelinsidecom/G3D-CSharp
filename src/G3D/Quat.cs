/**
  @file Quat.h
 
  Quaternion
  
  @maintainer Morgan McGuire, matrix@graphics3d.com
  
  @created 2002-01-23
  @edited  2006-05-10
 */

using System;

namespace G3D
{
    /**
      Unit quaternions are used in computer graphics to represent
      rotation about an axis.  Any 3x3 rotation matrix can
      be stored as a quaternion.

      A quaternion represents the sum of a real scalar and
      an imaginary vector: ix + jy + kz + w.  A unit quaternion
      representing a rotation by A about axis v has the form 
      [sin(A/2)*v, cos(A/2)].  For a unit quaternion, q.conj() == q.inverse()
      is a rotation by -A about v.  -q is the same rotation as q
      (negate both the axis and angle).  
      
      A non-unit quaterion q represents the same rotation as
      q.unitize() (Dam98 pg 28).

      Although quaternion-vector operations (eg. Quat + Vector3) are
      well defined, they are not supported by this class because
      they typically are bugs when they appear in code.

      Do not subclass.

      <B>BETA API -- subject to change</B>
      @cite Erik B. Dam, Martin Koch, Martin Lillholm, Quaternions, Interpolation and Animation.  Technical Report DIKU-TR-98/5, Department of Computer Science, University of Copenhagen, Denmark.  1998.
     */
    public struct Quat
    {
        /**
         q = [sin(angle / 2) * axis, cos(angle / 2)]
         
         In Watt & Watt's notation, s = w, v = (x, y, z)
         In the Real-Time Rendering notation, u = (x, y, z), w = w 
         */
        public float x;
        public float y;
        public float z;
        public float w;

        /**
         Initializes to a zero degree rotation.
         */
        public Quat(float _x, float _y, float _z, float _w)
        {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        /** Defaults to a pure vector quaternion */
        public Quat(Vector3 v, float _w = 0)
        {
            x = v.x;
            y = v.y;
            z = v.z;
            w = _w;
        }

        public Quat(Matrix3 rot)
        {
            int[] plus1mod3 = new int[] { 1, 2, 0 };

            // Find the index of the largest diagonal component
            // These ? operations hopefully compile to conditional
            // move instructions instead of branches.
            int i = (rot.elt(1, 1) > rot.elt(0, 0)) ? 1 : 0;
            i = (rot.elt(2, 2) > rot.elt(i, i)) ? 2 : i;

            // Find the indices of the other elements
            int j = plus1mod3[i];
            int k = plus1mod3[j];

            // Index the elements of the vector part of the quaternion as a float*
            float[] v = new float[3];

            // If we attempted to pre-normalize and trusted the matrix to be
            // perfectly orthonormal, the result would be:
            //
            //   double c = sqrt((rot[i][i] - (rot[j][j] + rot[k][k])) + 1.0)
            //   v[i] = -c * 0.5
            //   v[j] = -(rot[i][j] + rot[j][i]) * 0.5 / c
            //   v[k] = -(rot[i][k] + rot[k][i]) * 0.5 / c
            //   w    =  (rot[j][k] - rot[k][j]) * 0.5 / c
            //
            // Since we're going to pay the sqrt anyway, we perform a post normalization, which also
            // fixes any poorly normalized input.  Multiply all elements by 2*c in the above, giving:

            // nc2 = -c^2
            double nc2 = ((rot.elt(j, j) + rot.elt(k, k)) - rot.elt(i, i)) - 1.0;
            v[i] = (float)nc2;
            w = (rot.elt(j, k) - rot.elt(k, j));
            v[j] = -(rot.elt(i, j) + rot.elt(j, i));
            v[k] = -(rot.elt(i, k) + rot.elt(k, i));

            x = v[0];
            y = v[1];
            z = v[2];

            // We now have the correct result with the wrong magnitude, so normalize it:
            float s = (float)Math.Sqrt(x * x + y * y + z * z + w * w);
            if (s > 0.00001f)
            {
                s = 1.0f / s;
                x *= s;
                y *= s;
                z *= s;
                w *= s;
            }
            else
            {
                // The quaternion is nearly zero.  Make it 0 0 0 1
                x = 0.0f;
                y = 0.0f;
                z = 0.0f;
                w = 1.0f;
            }
        }

        /**
         C# 4 forbids a parameterless constructor on a struct, so the C++
         default Quat (a zero degree rotation) cannot be reproduced by
         default(Quat), which yields a zero quaternion.  Use Quat.identity()
         to build the zero degree rotation.
         */
        public static Quat identity()
        {
            return new Quat(0, 0, 0, 1);
        }

        /**
         The real part of the quaternion.
         */
        public float real()
        {
            return w;
        }

        /** Note: two quats can represent the Quat::sameRotation and not be equal. */
        public bool fuzzyEq(Quat q)
        {
            return g3dmath.fuzzyEq(x, q.x) && g3dmath.fuzzyEq(y, q.y) && g3dmath.fuzzyEq(z, q.z) && g3dmath.fuzzyEq(w, q.w);
        }

        /** True if these quaternions represent the same rotation (note that every rotation is 
            represented by two values; q and -q).
          */
        public bool sameRotation(Quat q)
        {
            return fuzzyEq(q) || fuzzyEq(-q);
        }

        public static Quat operator -(Quat value)
        {
            return new Quat(-value.x, -value.y, -value.z, -value.w);
        }

        /**
         Returns the imaginary part (x, y, z)
         */
        public Vector3 imag()
        {
            return new Vector3(x, y, z);
        }

        /** q = [sin(angle/2)*axis, cos(angle/2)] */
        public static Quat fromAxisAngleRotation(Vector3 axis, float angle)
        {
            Quat q;

            q.w = (float)Math.Cos(angle / 2.0f);
            q.x = axis.direction().x * (float)Math.Sin(angle / 2.0f);
            q.y = axis.direction().y * (float)Math.Sin(angle / 2.0f);
            q.z = axis.direction().z * (float)Math.Sin(angle / 2.0f);

            return q;
        }

        /** Returns the axis and angle of rotation represented
            by this quaternion (i.e. q = [sin(angle/2)*axis, cos(angle/2)]) */
        public void toAxisAngleRotation(out Vector3 axis, out double angle)
        {
            // Decompose the quaternion into an angle and an axis.

            axis = new Vector3(x, y, z);
            angle = 2 * Math.Acos(w);

            float len = (float)Math.Sqrt(1.0f - w * w);

            if (g3dmath.fuzzyGt(g3dmath.abs(len), 0.0f))
            {
                axis /= len;
            }

            // Reduce the range of the angle.

            if (angle < 0)
            {
                angle = -angle;
                axis = -axis;
            }

            while (angle > 2 * g3dmath.pi())
            {
                angle -= 2 * g3dmath.pi();
            }

            if (g3dmath.abs(angle) > g3dmath.pi())
            {
                angle -= 2 * g3dmath.pi();
            }

            // Make the angle positive.

            if (angle < 0.0f)
            {
                angle = -angle;
                axis = -axis;
            }
        }

        public void toAxisAngleRotation(out Vector3 axis, out float angle)
        {
            double d;
            toAxisAngleRotation(out axis, out d);
            angle = (float)d;
        }

        public Matrix3 toRotationMatrix()
        {
            Matrix3 rot;
            toRotationMatrix(out rot);
            return rot;
        }

        public void toRotationMatrix(out Matrix3 rot)
        {
            rot = new Matrix3(this);
        }

        /**
         Spherical linear interpolation: linear interpolation along the 
         shortest (3D) great-circle route between two quaternions.

         Note: Correct rotations are expected between 0 and PI in the right order.

         @cite Based on Game Physics -- David Eberly pg 538-540
         @param threshold Critical angle between between rotations at which
	        the algorithm switches to normalized lerp, which is more
			numerically stable in those situations. 0.0 will always slerp. 
         */
        public Quat slerp(Quat other, float alpha, float threshold = 0.05f)
        {
            // From: Game Physics -- David Eberly pg 538-540
            // Modified to include lerp for small angles, which
            // is a common practice.

            // See also:
            // http://number-none.com/product/Understanding%20Slerp,%20Then%20Not%20Using%20It/index.html

            Quat quat0 = this;
            Quat quat1 = other;

            // angle between quaternion rotations
            float phi;
            float cosphi = quat0.dot(quat1);


            if (cosphi < 0)
            {
                // Change the sign and fix the dot product; we need to
                // loop the other way to get the shortest path
                quat1 = -quat1;
                cosphi = quat0.dot(quat1);
            }

            // Using G3D::aCos will clamp the angle to 0 and pi
            phi = (float)g3dmath.aCos(cosphi);

            if (phi >= threshold)
            {
                // For large angles, slerp
                float scale0, scale1;

                scale0 = (float)Math.Sin((1.0f - alpha) * phi);
                scale1 = (float)Math.Sin(alpha * phi);

                return (((quat0 * scale0) + (quat1 * scale1)) / (float)Math.Sin(phi));
            }
            else
            {
                // For small angles, linear interpolate
                return quat0.nlerp(quat1, alpha);
            }
        }

        /** Normalized linear interpolation of quaternion components. */
        public Quat nlerp(Quat other, float alpha)
        {
            Quat result = (this * (1.0f - alpha)) + (other * alpha);
            return result / result.magnitude();
        }

        /**
         Negates the imaginary part.
         */
        public Quat conj()
        {
            return new Quat(-x, -y, -z, w);
        }

        public float sum()
        {
            return x + y + z + w;
        }

        public float average()
        {
            return sum() / 4.0f;
        }

        public static Quat operator *(Quat left, float right)
        {
            return new Quat(left.x * right, left.y * right, left.z * right, left.w * right);
        }

        /** @cite Based on Watt & Watt, page 360 */
        public static Quat operator *(float left, Quat right)
        {
            return right * left;
        }

        public static Quat operator *(double left, Quat right)
        {
            return right * (float)left;
        }

        public static Quat operator /(Quat left, float right)
        {
            return new Quat(left.x / right, left.y / right, left.z / right, left.w / right);
        }

        public float dot(Quat other)
        {
            return (x * other.x) + (y * other.y) + (z * other.z) + (w * other.w);
        }

        /** Note that q<SUP>-1</SUP> = q.conj() for a unit quaternion. 
            @cite Dam99 page 13 */
        public Quat inverse()
        {
            return conj() / dot(this);
        }

        public static Quat operator -(Quat left, Quat right)
        {
            return new Quat(left.x - right.x, left.y - right.y, left.z - right.z, left.w - right.w);
        }

        public static Quat operator +(Quat left, Quat right)
        {
            return new Quat(left.x + right.x, left.y + right.y, left.z + right.z, left.w + right.w);
        }

        /**
         Quaternion multiplication (composition of rotations).
         Note that this does not commute.
         */
        public static Quat operator *(Quat left, Quat other)
        {
            // Following Watt & Watt, page 360
            Vector3 v1 = left.imag();
            Vector3 v2 = other.imag();
            float s1 = left.w;
            float s2 = other.w;

            return new Quat(s1 * v2 + s2 * v1 + v1.cross(v2), s1 * s2 - v1.dot(v2));
        }

        /* (*this) * other.inverse() */
        public static Quat operator /(Quat left, Quat other)
        {
            return (left) * other.inverse();
        }


        /** Is the magnitude nearly 1.0? */
        public bool isUnit(float tolerance = 1e-5f)
        {
            return g3dmath.abs(dot(this) - 1.0f) < tolerance;
        }


        public float magnitude()
        {
            return (float)Math.Sqrt(dot(this));
        }

        public Quat log()
        {
            if ((x == 0) && (y == 0) && (z == 0))
            {
                if (w > 0)
                {
                    return new Quat(0, 0, 0, (float)Math.Log(w));
                }
                else if (w < 0)
                {
                    // Log of a negative number.  Multivalued, any number of the form
                    // (PI * v, ln(-q.w))
                    return new Quat((float)g3dmath.pi(), 0, 0, (float)Math.Log(-w));
                }
                else
                {
                    // log of zero!
                    float nan = (float)g3dmath.nan();
                    return new Quat(nan, nan, nan, nan);
                }
            }
            else
            {
                // Partly imaginary.
                float imagLen = (float)Math.Sqrt(x * x + y * y + z * z);
                float len = (float)Math.Sqrt(imagLen * imagLen + w * w);
                float theta = (float)Math.Atan2(imagLen, (float)w);
                float t = theta / imagLen;
                return new Quat(t * x, t * y, t * z, (float)Math.Log(len));
            }
        }

        /** exp q = [sin(A) * v, cos(A)] where q = [Av, 0].
            Only defined for pure-vector quaternions */
        public Quat exp()
        {
            Debug.debugAssertM(w == 0, "exp only defined for vector quaternions");
            Vector3 u = new Vector3(x, y, z);
            float A = u.magnitude();
            Vector3 v = u / A;
            return new Quat((float)Math.Sin(A) * v, (float)Math.Cos(A));
        }


        /**
         Raise this quaternion to a power.  For a rotation, this is
         the effect of rotating x times as much as the original
         quaterion.

         Note that q.pow(a).pow(b) == q.pow(a + b)
         @cite Dam98 pg 21
         */
        public Quat pow(float x)
        {
            return (log() * x).exp();
        }


        /**
         @deprecated
         Use toUnit()
         */
        public Quat unitize()
        {
            float mag2 = dot(this);
            if (g3dmath.fuzzyEq(mag2, 1.0f))
            {
                return this;
            }
            else
            {
                return this / (float)Math.Sqrt(mag2);
            }
        }

        /**
         Returns a unit quaterion obtained by dividing through by
         the magnitude.
         */
        public Quat toUnit()
        {
            return unitize();
        }

        /**
         The linear algebra 2-norm, sqrt(q dot q).  This matches
         the value used in Dam's 1998 tech report but differs from
         the n(q) value used in Eberly's 1999 paper, which is the square of the
         norm.
         */
        public float norm()
        {
            return magnitude();
        }

        // access quaternion as q[0] = q.x, q[1] = q.y, q[2] = q.z, q[3] = q.w
        //
        // WARNING.  These member functions rely on
        // (1) Quat not having virtual functions
        // (2) the data packed in a 4*sizeof(float) memory block
        public float this[int i]
        {
            get
            {
                Debug.debugAssert(i >= 0);
                Debug.debugAssert(i < 4);
                switch (i)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default: return w;
                }
            }
            set
            {
                Debug.debugAssert(i >= 0);
                Debug.debugAssert(i < 4);
                switch (i)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    default: w = value; break;
                }
            }
        }

        /** Generate uniform random unit quaternion (i.e. random "direction") 
 	@cite From "Uniform Random Rotations", Ken Shoemake, Graphics Gems III.
   */
        public static Quat unitRandom()
        {
            float x0 = g3dmath.uniformRandom();
            float r1 = (float)Math.Sqrt(1 - x0),
                  r2 = (float)Math.Sqrt(x0);
            float t1 = (float)g3dmath.twoPi() * g3dmath.uniformRandom();
            float t2 = (float)g3dmath.twoPi() * g3dmath.uniformRandom();
            float c1 = (float)Math.Cos(t1),
                  s1 = (float)Math.Sin(t1);
            float c2 = (float)Math.Cos(t2),
                  s2 = (float)Math.Sin(t2);
            return new Quat(s1 * r1, c1 * r1, s2 * r2, c2 * r2);
        }

        public void deserialize(BinaryInput b)
        {
            x = b.readFloat32();
            y = b.readFloat32();
            z = b.readFloat32();
            w = b.readFloat32();
        }

        public void serialize(BinaryOutput b)
        {
            b.writeFloat32(x);
            b.writeFloat32(y);
            b.writeFloat32(z);
            b.writeFloat32(w);
        }

        // 2-char swizzles

        public Vector2 xx()
        {
            return new Vector2(x, x);
        }

        public Vector2 xy()
        {
            return new Vector2(x, y);
        }

        public Vector2 xz()
        {
            return new Vector2(x, z);
        }

        public Vector2 xw()
        {
            return new Vector2(x, w);
        }

        public Vector2 yx()
        {
            return new Vector2(y, x);
        }

        public Vector2 yy()
        {
            return new Vector2(y, y);
        }

        public Vector2 yz()
        {
            return new Vector2(y, z);
        }

        public Vector2 yw()
        {
            return new Vector2(y, w);
        }

        public Vector2 zx()
        {
            return new Vector2(z, x);
        }

        public Vector2 zy()
        {
            return new Vector2(z, y);
        }

        public Vector2 zz()
        {
            return new Vector2(z, z);
        }

        public Vector2 zw()
        {
            return new Vector2(z, w);
        }

        public Vector2 wx()
        {
            return new Vector2(w, x);
        }

        public Vector2 wy()
        {
            return new Vector2(w, y);
        }

        public Vector2 wz()
        {
            return new Vector2(w, z);
        }

        public Vector2 ww()
        {
            return new Vector2(w, w);
        }

        // 3-char swizzles

        public Vector3 xxx()
        {
            return new Vector3(x, x, x);
        }

        public Vector3 xxy()
        {
            return new Vector3(x, x, y);
        }

        public Vector3 xxz()
        {
            return new Vector3(x, x, z);
        }

        public Vector3 xxw()
        {
            return new Vector3(x, x, w);
        }

        public Vector3 xyx()
        {
            return new Vector3(x, y, x);
        }

        public Vector3 xyy()
        {
            return new Vector3(x, y, y);
        }

        public Vector3 xyz()
        {
            return new Vector3(x, y, z);
        }

        public Vector3 xyw()
        {
            return new Vector3(x, y, w);
        }

        public Vector3 xzx()
        {
            return new Vector3(x, z, x);
        }

        public Vector3 xzy()
        {
            return new Vector3(x, z, y);
        }

        public Vector3 xzz()
        {
            return new Vector3(x, z, z);
        }

        public Vector3 xzw()
        {
            return new Vector3(x, z, w);
        }

        public Vector3 xwx()
        {
            return new Vector3(x, w, x);
        }

        public Vector3 xwy()
        {
            return new Vector3(x, w, y);
        }

        public Vector3 xwz()
        {
            return new Vector3(x, w, z);
        }

        public Vector3 xww()
        {
            return new Vector3(x, w, w);
        }

        public Vector3 yxx()
        {
            return new Vector3(y, x, x);
        }

        public Vector3 yxy()
        {
            return new Vector3(y, x, y);
        }

        public Vector3 yxz()
        {
            return new Vector3(y, x, z);
        }

        public Vector3 yxw()
        {
            return new Vector3(y, x, w);
        }

        public Vector3 yyx()
        {
            return new Vector3(y, y, x);
        }

        public Vector3 yyy()
        {
            return new Vector3(y, y, y);
        }

        public Vector3 yyz()
        {
            return new Vector3(y, y, z);
        }

        public Vector3 yyw()
        {
            return new Vector3(y, y, w);
        }

        public Vector3 yzx()
        {
            return new Vector3(y, z, x);
        }

        public Vector3 yzy()
        {
            return new Vector3(y, z, y);
        }

        public Vector3 yzz()
        {
            return new Vector3(y, z, z);
        }

        public Vector3 yzw()
        {
            return new Vector3(y, z, w);
        }

        public Vector3 ywx()
        {
            return new Vector3(y, w, x);
        }

        public Vector3 ywy()
        {
            return new Vector3(y, w, y);
        }

        public Vector3 ywz()
        {
            return new Vector3(y, w, z);
        }

        public Vector3 yww()
        {
            return new Vector3(y, w, w);
        }

        public Vector3 zxx()
        {
            return new Vector3(z, x, x);
        }

        public Vector3 zxy()
        {
            return new Vector3(z, x, y);
        }

        public Vector3 zxz()
        {
            return new Vector3(z, x, z);
        }

        public Vector3 zxw()
        {
            return new Vector3(z, x, w);
        }

        public Vector3 zyx()
        {
            return new Vector3(z, y, x);
        }

        public Vector3 zyy()
        {
            return new Vector3(z, y, y);
        }

        public Vector3 zyz()
        {
            return new Vector3(z, y, z);
        }

        public Vector3 zyw()
        {
            return new Vector3(z, y, w);
        }

        public Vector3 zzx()
        {
            return new Vector3(z, z, x);
        }

        public Vector3 zzy()
        {
            return new Vector3(z, z, y);
        }

        public Vector3 zzz()
        {
            return new Vector3(z, z, z);
        }

        public Vector3 zzw()
        {
            return new Vector3(z, z, w);
        }

        public Vector3 zwx()
        {
            return new Vector3(z, w, x);
        }

        public Vector3 zwy()
        {
            return new Vector3(z, w, y);
        }

        public Vector3 zwz()
        {
            return new Vector3(z, w, z);
        }

        public Vector3 zww()
        {
            return new Vector3(z, w, w);
        }

        public Vector3 wxx()
        {
            return new Vector3(w, x, x);
        }

        public Vector3 wxy()
        {
            return new Vector3(w, x, y);
        }

        public Vector3 wxz()
        {
            return new Vector3(w, x, z);
        }

        public Vector3 wxw()
        {
            return new Vector3(w, x, w);
        }

        public Vector3 wyx()
        {
            return new Vector3(w, y, x);
        }

        public Vector3 wyy()
        {
            return new Vector3(w, y, y);
        }

        public Vector3 wyz()
        {
            return new Vector3(w, y, z);
        }

        public Vector3 wyw()
        {
            return new Vector3(w, y, w);
        }

        public Vector3 wzx()
        {
            return new Vector3(w, z, x);
        }

        public Vector3 wzy()
        {
            return new Vector3(w, z, y);
        }

        public Vector3 wzz()
        {
            return new Vector3(w, z, z);
        }

        public Vector3 wzw()
        {
            return new Vector3(w, z, w);
        }

        public Vector3 wwx()
        {
            return new Vector3(w, w, x);
        }

        public Vector3 wwy()
        {
            return new Vector3(w, w, y);
        }

        public Vector3 wwz()
        {
            return new Vector3(w, w, z);
        }

        public Vector3 www()
        {
            return new Vector3(w, w, w);
        }

        // 4-char swizzles

        public Vector4 xxxx()
        {
            return new Vector4(x, x, x, x);
        }

        public Vector4 xxxy()
        {
            return new Vector4(x, x, x, y);
        }

        public Vector4 xxxz()
        {
            return new Vector4(x, x, x, z);
        }

        public Vector4 xxxw()
        {
            return new Vector4(x, x, x, w);
        }

        public Vector4 xxyx()
        {
            return new Vector4(x, x, y, x);
        }

        public Vector4 xxyy()
        {
            return new Vector4(x, x, y, y);
        }

        public Vector4 xxyz()
        {
            return new Vector4(x, x, y, z);
        }

        public Vector4 xxyw()
        {
            return new Vector4(x, x, y, w);
        }

        public Vector4 xxzx()
        {
            return new Vector4(x, x, z, x);
        }

        public Vector4 xxzy()
        {
            return new Vector4(x, x, z, y);
        }

        public Vector4 xxzz()
        {
            return new Vector4(x, x, z, z);
        }

        public Vector4 xxzw()
        {
            return new Vector4(x, x, z, w);
        }

        public Vector4 xxwx()
        {
            return new Vector4(x, x, w, x);
        }

        public Vector4 xxwy()
        {
            return new Vector4(x, x, w, y);
        }

        public Vector4 xxwz()
        {
            return new Vector4(x, x, w, z);
        }

        public Vector4 xxww()
        {
            return new Vector4(x, x, w, w);
        }

        public Vector4 xyxx()
        {
            return new Vector4(x, y, x, x);
        }

        public Vector4 xyxy()
        {
            return new Vector4(x, y, x, y);
        }

        public Vector4 xyxz()
        {
            return new Vector4(x, y, x, z);
        }

        public Vector4 xyxw()
        {
            return new Vector4(x, y, x, w);
        }

        public Vector4 xyyx()
        {
            return new Vector4(x, y, y, x);
        }

        public Vector4 xyyy()
        {
            return new Vector4(x, y, y, y);
        }

        public Vector4 xyyz()
        {
            return new Vector4(x, y, y, z);
        }

        public Vector4 xyyw()
        {
            return new Vector4(x, y, y, w);
        }

        public Vector4 xyzx()
        {
            return new Vector4(x, y, z, x);
        }

        public Vector4 xyzy()
        {
            return new Vector4(x, y, z, y);
        }

        public Vector4 xyzz()
        {
            return new Vector4(x, y, z, z);
        }

        public Vector4 xyzw()
        {
            return new Vector4(x, y, z, w);
        }

        public Vector4 xywx()
        {
            return new Vector4(x, y, w, x);
        }

        public Vector4 xywy()
        {
            return new Vector4(x, y, w, y);
        }

        public Vector4 xywz()
        {
            return new Vector4(x, y, w, z);
        }

        public Vector4 xyww()
        {
            return new Vector4(x, y, w, w);
        }

        public Vector4 xzxx()
        {
            return new Vector4(x, z, x, x);
        }

        public Vector4 xzxy()
        {
            return new Vector4(x, z, x, y);
        }

        public Vector4 xzxz()
        {
            return new Vector4(x, z, x, z);
        }

        public Vector4 xzxw()
        {
            return new Vector4(x, z, x, w);
        }

        public Vector4 xzyx()
        {
            return new Vector4(x, z, y, x);
        }

        public Vector4 xzyy()
        {
            return new Vector4(x, z, y, y);
        }

        public Vector4 xzyz()
        {
            return new Vector4(x, z, y, z);
        }

        public Vector4 xzyw()
        {
            return new Vector4(x, z, y, w);
        }

        public Vector4 xzzx()
        {
            return new Vector4(x, z, z, x);
        }

        public Vector4 xzzy()
        {
            return new Vector4(x, z, z, y);
        }

        public Vector4 xzzz()
        {
            return new Vector4(x, z, z, z);
        }

        public Vector4 xzzw()
        {
            return new Vector4(x, z, z, w);
        }

        public Vector4 xzwx()
        {
            return new Vector4(x, z, w, x);
        }

        public Vector4 xzwy()
        {
            return new Vector4(x, z, w, y);
        }

        public Vector4 xzwz()
        {
            return new Vector4(x, z, w, z);
        }

        public Vector4 xzww()
        {
            return new Vector4(x, z, w, w);
        }

        public Vector4 xwxx()
        {
            return new Vector4(x, w, x, x);
        }

        public Vector4 xwxy()
        {
            return new Vector4(x, w, x, y);
        }

        public Vector4 xwxz()
        {
            return new Vector4(x, w, x, z);
        }

        public Vector4 xwxw()
        {
            return new Vector4(x, w, x, w);
        }

        public Vector4 xwyx()
        {
            return new Vector4(x, w, y, x);
        }

        public Vector4 xwyy()
        {
            return new Vector4(x, w, y, y);
        }

        public Vector4 xwyz()
        {
            return new Vector4(x, w, y, z);
        }

        public Vector4 xwyw()
        {
            return new Vector4(x, w, y, w);
        }

        public Vector4 xwzx()
        {
            return new Vector4(x, w, z, x);
        }

        public Vector4 xwzy()
        {
            return new Vector4(x, w, z, y);
        }

        public Vector4 xwzz()
        {
            return new Vector4(x, w, z, z);
        }

        public Vector4 xwzw()
        {
            return new Vector4(x, w, z, w);
        }

        public Vector4 xwwx()
        {
            return new Vector4(x, w, w, x);
        }

        public Vector4 xwwy()
        {
            return new Vector4(x, w, w, y);
        }

        public Vector4 xwwz()
        {
            return new Vector4(x, w, w, z);
        }

        public Vector4 xwww()
        {
            return new Vector4(x, w, w, w);
        }

        public Vector4 yxxx()
        {
            return new Vector4(y, x, x, x);
        }

        public Vector4 yxxy()
        {
            return new Vector4(y, x, x, y);
        }

        public Vector4 yxxz()
        {
            return new Vector4(y, x, x, z);
        }

        public Vector4 yxxw()
        {
            return new Vector4(y, x, x, w);
        }

        public Vector4 yxyx()
        {
            return new Vector4(y, x, y, x);
        }

        public Vector4 yxyy()
        {
            return new Vector4(y, x, y, y);
        }

        public Vector4 yxyz()
        {
            return new Vector4(y, x, y, z);
        }

        public Vector4 yxyw()
        {
            return new Vector4(y, x, y, w);
        }

        public Vector4 yxzx()
        {
            return new Vector4(y, x, z, x);
        }

        public Vector4 yxzy()
        {
            return new Vector4(y, x, z, y);
        }

        public Vector4 yxzz()
        {
            return new Vector4(y, x, z, z);
        }

        public Vector4 yxzw()
        {
            return new Vector4(y, x, z, w);
        }

        public Vector4 yxwx()
        {
            return new Vector4(y, x, w, x);
        }

        public Vector4 yxwy()
        {
            return new Vector4(y, x, w, y);
        }

        public Vector4 yxwz()
        {
            return new Vector4(y, x, w, z);
        }

        public Vector4 yxww()
        {
            return new Vector4(y, x, w, w);
        }

        public Vector4 yyxx()
        {
            return new Vector4(y, y, x, x);
        }

        public Vector4 yyxy()
        {
            return new Vector4(y, y, x, y);
        }

        public Vector4 yyxz()
        {
            return new Vector4(y, y, x, z);
        }

        public Vector4 yyxw()
        {
            return new Vector4(y, y, x, w);
        }

        public Vector4 yyyx()
        {
            return new Vector4(y, y, y, x);
        }

        public Vector4 yyyy()
        {
            return new Vector4(y, y, y, y);
        }

        public Vector4 yyyz()
        {
            return new Vector4(y, y, y, z);
        }

        public Vector4 yyyw()
        {
            return new Vector4(y, y, y, w);
        }

        public Vector4 yyzx()
        {
            return new Vector4(y, y, z, x);
        }

        public Vector4 yyzy()
        {
            return new Vector4(y, y, z, y);
        }

        public Vector4 yyzz()
        {
            return new Vector4(y, y, z, z);
        }

        public Vector4 yyzw()
        {
            return new Vector4(y, y, z, w);
        }

        public Vector4 yywx()
        {
            return new Vector4(y, y, w, x);
        }

        public Vector4 yywy()
        {
            return new Vector4(y, y, w, y);
        }

        public Vector4 yywz()
        {
            return new Vector4(y, y, w, z);
        }

        public Vector4 yyww()
        {
            return new Vector4(y, y, w, w);
        }

        public Vector4 yzxx()
        {
            return new Vector4(y, z, x, x);
        }

        public Vector4 yzxy()
        {
            return new Vector4(y, z, x, y);
        }

        public Vector4 yzxz()
        {
            return new Vector4(y, z, x, z);
        }

        public Vector4 yzxw()
        {
            return new Vector4(y, z, x, w);
        }

        public Vector4 yzyx()
        {
            return new Vector4(y, z, y, x);
        }

        public Vector4 yzyy()
        {
            return new Vector4(y, z, y, y);
        }

        public Vector4 yzyz()
        {
            return new Vector4(y, z, y, z);
        }

        public Vector4 yzyw()
        {
            return new Vector4(y, z, y, w);
        }

        public Vector4 yzzx()
        {
            return new Vector4(y, z, z, x);
        }

        public Vector4 yzzy()
        {
            return new Vector4(y, z, z, y);
        }

        public Vector4 yzzz()
        {
            return new Vector4(y, z, z, z);
        }

        public Vector4 yzzw()
        {
            return new Vector4(y, z, z, w);
        }

        public Vector4 yzwx()
        {
            return new Vector4(y, z, w, x);
        }

        public Vector4 yzwy()
        {
            return new Vector4(y, z, w, y);
        }

        public Vector4 yzwz()
        {
            return new Vector4(y, z, w, z);
        }

        public Vector4 yzww()
        {
            return new Vector4(y, z, w, w);
        }

        public Vector4 ywxx()
        {
            return new Vector4(y, w, x, x);
        }

        public Vector4 ywxy()
        {
            return new Vector4(y, w, x, y);
        }

        public Vector4 ywxz()
        {
            return new Vector4(y, w, x, z);
        }

        public Vector4 ywxw()
        {
            return new Vector4(y, w, x, w);
        }

        public Vector4 ywyx()
        {
            return new Vector4(y, w, y, x);
        }

        public Vector4 ywyy()
        {
            return new Vector4(y, w, y, y);
        }

        public Vector4 ywyz()
        {
            return new Vector4(y, w, y, z);
        }

        public Vector4 ywyw()
        {
            return new Vector4(y, w, y, w);
        }

        public Vector4 ywzx()
        {
            return new Vector4(y, w, z, x);
        }

        public Vector4 ywzy()
        {
            return new Vector4(y, w, z, y);
        }

        public Vector4 ywzz()
        {
            return new Vector4(y, w, z, z);
        }

        public Vector4 ywzw()
        {
            return new Vector4(y, w, z, w);
        }

        public Vector4 ywwx()
        {
            return new Vector4(y, w, w, x);
        }

        public Vector4 ywwy()
        {
            return new Vector4(y, w, w, y);
        }

        public Vector4 ywwz()
        {
            return new Vector4(y, w, w, z);
        }

        public Vector4 ywww()
        {
            return new Vector4(y, w, w, w);
        }

        public Vector4 zxxx()
        {
            return new Vector4(z, x, x, x);
        }

        public Vector4 zxxy()
        {
            return new Vector4(z, x, x, y);
        }

        public Vector4 zxxz()
        {
            return new Vector4(z, x, x, z);
        }

        public Vector4 zxxw()
        {
            return new Vector4(z, x, x, w);
        }

        public Vector4 zxyx()
        {
            return new Vector4(z, x, y, x);
        }

        public Vector4 zxyy()
        {
            return new Vector4(z, x, y, y);
        }

        public Vector4 zxyz()
        {
            return new Vector4(z, x, y, z);
        }

        public Vector4 zxyw()
        {
            return new Vector4(z, x, y, w);
        }

        public Vector4 zxzx()
        {
            return new Vector4(z, x, z, x);
        }

        public Vector4 zxzy()
        {
            return new Vector4(z, x, z, y);
        }

        public Vector4 zxzz()
        {
            return new Vector4(z, x, z, z);
        }

        public Vector4 zxzw()
        {
            return new Vector4(z, x, z, w);
        }

        public Vector4 zxwx()
        {
            return new Vector4(z, x, w, x);
        }

        public Vector4 zxwy()
        {
            return new Vector4(z, x, w, y);
        }

        public Vector4 zxwz()
        {
            return new Vector4(z, x, w, z);
        }

        public Vector4 zxww()
        {
            return new Vector4(z, x, w, w);
        }

        public Vector4 zyxx()
        {
            return new Vector4(z, y, x, x);
        }

        public Vector4 zyxy()
        {
            return new Vector4(z, y, x, y);
        }

        public Vector4 zyxz()
        {
            return new Vector4(z, y, x, z);
        }

        public Vector4 zyxw()
        {
            return new Vector4(z, y, x, w);
        }

        public Vector4 zyyx()
        {
            return new Vector4(z, y, y, x);
        }

        public Vector4 zyyy()
        {
            return new Vector4(z, y, y, y);
        }

        public Vector4 zyyz()
        {
            return new Vector4(z, y, y, z);
        }

        public Vector4 zyyw()
        {
            return new Vector4(z, y, y, w);
        }

        public Vector4 zyzx()
        {
            return new Vector4(z, y, z, x);
        }

        public Vector4 zyzy()
        {
            return new Vector4(z, y, z, y);
        }

        public Vector4 zyzz()
        {
            return new Vector4(z, y, z, z);
        }

        public Vector4 zyzw()
        {
            return new Vector4(z, y, z, w);
        }

        public Vector4 zywx()
        {
            return new Vector4(z, y, w, x);
        }

        public Vector4 zywy()
        {
            return new Vector4(z, y, w, y);
        }

        public Vector4 zywz()
        {
            return new Vector4(z, y, w, z);
        }

        public Vector4 zyww()
        {
            return new Vector4(z, y, w, w);
        }

        public Vector4 zzxx()
        {
            return new Vector4(z, z, x, x);
        }

        public Vector4 zzxy()
        {
            return new Vector4(z, z, x, y);
        }

        public Vector4 zzxz()
        {
            return new Vector4(z, z, x, z);
        }

        public Vector4 zzxw()
        {
            return new Vector4(z, z, x, w);
        }

        public Vector4 zzyx()
        {
            return new Vector4(z, z, y, x);
        }

        public Vector4 zzyy()
        {
            return new Vector4(z, z, y, y);
        }

        public Vector4 zzyz()
        {
            return new Vector4(z, z, y, z);
        }

        public Vector4 zzyw()
        {
            return new Vector4(z, z, y, w);
        }

        public Vector4 zzzx()
        {
            return new Vector4(z, z, z, x);
        }

        public Vector4 zzzy()
        {
            return new Vector4(z, z, z, y);
        }

        public Vector4 zzzz()
        {
            return new Vector4(z, z, z, z);
        }

        public Vector4 zzzw()
        {
            return new Vector4(z, z, z, w);
        }

        public Vector4 zzwx()
        {
            return new Vector4(z, z, w, x);
        }

        public Vector4 zzwy()
        {
            return new Vector4(z, z, w, y);
        }

        public Vector4 zzwz()
        {
            return new Vector4(z, z, w, z);
        }

        public Vector4 zzww()
        {
            return new Vector4(z, z, w, w);
        }

        public Vector4 zwxx()
        {
            return new Vector4(z, w, x, x);
        }

        public Vector4 zwxy()
        {
            return new Vector4(z, w, x, y);
        }

        public Vector4 zwxz()
        {
            return new Vector4(z, w, x, z);
        }

        public Vector4 zwxw()
        {
            return new Vector4(z, w, x, w);
        }

        public Vector4 zwyx()
        {
            return new Vector4(z, w, y, x);
        }

        public Vector4 zwyy()
        {
            return new Vector4(z, w, y, y);
        }

        public Vector4 zwyz()
        {
            return new Vector4(z, w, y, z);
        }

        public Vector4 zwyw()
        {
            return new Vector4(z, w, y, w);
        }

        public Vector4 zwzx()
        {
            return new Vector4(z, w, z, x);
        }

        public Vector4 zwzy()
        {
            return new Vector4(z, w, z, y);
        }

        public Vector4 zwzz()
        {
            return new Vector4(z, w, z, z);
        }

        public Vector4 zwzw()
        {
            return new Vector4(z, w, z, w);
        }

        public Vector4 zwwx()
        {
            return new Vector4(z, w, w, x);
        }

        public Vector4 zwwy()
        {
            return new Vector4(z, w, w, y);
        }

        public Vector4 zwwz()
        {
            return new Vector4(z, w, w, z);
        }

        public Vector4 zwww()
        {
            return new Vector4(z, w, w, w);
        }

        public Vector4 wxxx()
        {
            return new Vector4(w, x, x, x);
        }

        public Vector4 wxxy()
        {
            return new Vector4(w, x, x, y);
        }

        public Vector4 wxxz()
        {
            return new Vector4(w, x, x, z);
        }

        public Vector4 wxxw()
        {
            return new Vector4(w, x, x, w);
        }

        public Vector4 wxyx()
        {
            return new Vector4(w, x, y, x);
        }

        public Vector4 wxyy()
        {
            return new Vector4(w, x, y, y);
        }

        public Vector4 wxyz()
        {
            return new Vector4(w, x, y, z);
        }

        public Vector4 wxyw()
        {
            return new Vector4(w, x, y, w);
        }

        public Vector4 wxzx()
        {
            return new Vector4(w, x, z, x);
        }

        public Vector4 wxzy()
        {
            return new Vector4(w, x, z, y);
        }

        public Vector4 wxzz()
        {
            return new Vector4(w, x, z, z);
        }

        public Vector4 wxzw()
        {
            return new Vector4(w, x, z, w);
        }

        public Vector4 wxwx()
        {
            return new Vector4(w, x, w, x);
        }

        public Vector4 wxwy()
        {
            return new Vector4(w, x, w, y);
        }

        public Vector4 wxwz()
        {
            return new Vector4(w, x, w, z);
        }

        public Vector4 wxww()
        {
            return new Vector4(w, x, w, w);
        }

        public Vector4 wyxx()
        {
            return new Vector4(w, y, x, x);
        }

        public Vector4 wyxy()
        {
            return new Vector4(w, y, x, y);
        }

        public Vector4 wyxz()
        {
            return new Vector4(w, y, x, z);
        }

        public Vector4 wyxw()
        {
            return new Vector4(w, y, x, w);
        }

        public Vector4 wyyx()
        {
            return new Vector4(w, y, y, x);
        }

        public Vector4 wyyy()
        {
            return new Vector4(w, y, y, y);
        }

        public Vector4 wyyz()
        {
            return new Vector4(w, y, y, z);
        }

        public Vector4 wyyw()
        {
            return new Vector4(w, y, y, w);
        }

        public Vector4 wyzx()
        {
            return new Vector4(w, y, z, x);
        }

        public Vector4 wyzy()
        {
            return new Vector4(w, y, z, y);
        }

        public Vector4 wyzz()
        {
            return new Vector4(w, y, z, z);
        }

        public Vector4 wyzw()
        {
            return new Vector4(w, y, z, w);
        }

        public Vector4 wywx()
        {
            return new Vector4(w, y, w, x);
        }

        public Vector4 wywy()
        {
            return new Vector4(w, y, w, y);
        }

        public Vector4 wywz()
        {
            return new Vector4(w, y, w, z);
        }

        public Vector4 wyww()
        {
            return new Vector4(w, y, w, w);
        }

        public Vector4 wzxx()
        {
            return new Vector4(w, z, x, x);
        }

        public Vector4 wzxy()
        {
            return new Vector4(w, z, x, y);
        }

        public Vector4 wzxz()
        {
            return new Vector4(w, z, x, z);
        }

        public Vector4 wzxw()
        {
            return new Vector4(w, z, x, w);
        }

        public Vector4 wzyx()
        {
            return new Vector4(w, z, y, x);
        }

        public Vector4 wzyy()
        {
            return new Vector4(w, z, y, y);
        }

        public Vector4 wzyz()
        {
            return new Vector4(w, z, y, z);
        }

        public Vector4 wzyw()
        {
            return new Vector4(w, z, y, w);
        }

        public Vector4 wzzx()
        {
            return new Vector4(w, z, z, x);
        }

        public Vector4 wzzy()
        {
            return new Vector4(w, z, z, y);
        }

        public Vector4 wzzz()
        {
            return new Vector4(w, z, z, z);
        }

        public Vector4 wzzw()
        {
            return new Vector4(w, z, z, w);
        }

        public Vector4 wzwx()
        {
            return new Vector4(w, z, w, x);
        }

        public Vector4 wzwy()
        {
            return new Vector4(w, z, w, y);
        }

        public Vector4 wzwz()
        {
            return new Vector4(w, z, w, z);
        }

        public Vector4 wzww()
        {
            return new Vector4(w, z, w, w);
        }

        public Vector4 wwxx()
        {
            return new Vector4(w, w, x, x);
        }

        public Vector4 wwxy()
        {
            return new Vector4(w, w, x, y);
        }

        public Vector4 wwxz()
        {
            return new Vector4(w, w, x, z);
        }

        public Vector4 wwxw()
        {
            return new Vector4(w, w, x, w);
        }

        public Vector4 wwyx()
        {
            return new Vector4(w, w, y, x);
        }

        public Vector4 wwyy()
        {
            return new Vector4(w, w, y, y);
        }

        public Vector4 wwyz()
        {
            return new Vector4(w, w, y, z);
        }

        public Vector4 wwyw()
        {
            return new Vector4(w, w, y, w);
        }

        public Vector4 wwzx()
        {
            return new Vector4(w, w, z, x);
        }

        public Vector4 wwzy()
        {
            return new Vector4(w, w, z, y);
        }

        public Vector4 wwzz()
        {
            return new Vector4(w, w, z, z);
        }

        public Vector4 wwzw()
        {
            return new Vector4(w, w, z, w);
        }

        public Vector4 wwwx()
        {
            return new Vector4(w, w, w, x);
        }

        public Vector4 wwwy()
        {
            return new Vector4(w, w, w, y);
        }

        public Vector4 wwwz()
        {
            return new Vector4(w, w, w, z);
        }

        public Vector4 wwww()
        {
            return new Vector4(w, w, w, w);
        }


    }

    public static class QuatOperators
    {
        public static Quat exp(Quat q)
        {
            return q.exp();
        }

        public static Quat log(Quat q)
        {
            return q.log();
        }

        public static Quat pow(Quat q, double x)
        {
            return q.pow((float)x);
        }
    }
}
