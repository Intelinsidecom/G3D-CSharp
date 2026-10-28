/**
  @file Vector3.h

  3D vector class

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2001-06-02
  @edited  2005-08-23
  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    public struct Vector3
    {
        public enum Axis { X_AXIS = 0, Y_AXIS = 1, Z_AXIS = 2, DETECT_AXIS = -1 }

        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3(Vector2 v, float z)
        {
            x = v.x;
            y = v.y;
            this.z = z;
        }

        public Vector3(float[] coordinate)
        {
            x = coordinate[0];
            y = coordinate[1];
            z = coordinate[2];
        }

        public Vector3(double[] coordinate)
        {
            x = (float)coordinate[0];
            y = (float)coordinate[1];
            z = (float)coordinate[2];
        }

        public Vector3(Vector3 rkVector)
        {
            x = rkVector.x;
            y = rkVector.y;
            z = rkVector.z;
        }

        public Vector3(Vector3int16 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3(BinaryInput b)
        {
            x = 0.0f;
            y = 0.0f;
            z = 0.0f;
            deserialize(b);
        }

        public Vector3(TextInput t)
        {
            x = 0.0f;
            y = 0.0f;
            z = 0.0f;
            deserialize(t);
        }

        public void serialize(BinaryOutput b)
        {
            b.writeFloat32(x);
            b.writeFloat32(y);
            b.writeFloat32(z);
        }

        public void deserialize(BinaryInput b)
        {
            x = b.readFloat32();
            y = b.readFloat32();
            z = b.readFloat32();
        }

        public void serialize(TextOutput t)
        {
            t.writeSymbol("(");
            t.writeNumber(x);
            t.writeSymbol(",");
            t.writeNumber(y);
            t.writeSymbol(",");
            t.writeNumber(z);
            t.writeSymbol(")");
        }

        public void deserialize(TextInput t)
        {
            t.readSymbol("(");
            x = (float)t.readNumber();
            t.readSymbol(",");
            y = (float)t.readNumber();
            t.readSymbol(",");
            z = (float)t.readNumber();
            t.readSymbol(")");
        }

        public float this[int i]
        {
            get
            {
                if (i == 0) return x;
                if (i == 1) return y;
                if (i == 2) return z;
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (i == 0) x = value;
                else if (i == 1) y = value;
                else if (i == 2) z = value;
                else throw new IndexOutOfRangeException();
            }
        }

        public float[] ToArray()
        {
            return new float[] { x, y, z };
        }

        public void CopyTo(float[] destination, int destinationIndex)
        {
            destination[destinationIndex] = x;
            destination[destinationIndex + 1] = y;
            destination[destinationIndex + 2] = z;
        }

        public static Vector3 operator +(Vector3 left, Vector3 right)
        {
            return new Vector3(left.x + right.x, left.y + right.y, left.z + right.z);
        }

        public static Vector3 operator -(Vector3 left, Vector3 right)
        {
            return new Vector3(left.x - right.x, left.y - right.y, left.z - right.z);
        }

        public static Vector3 operator *(Vector3 left, float right)
        {
            return new Vector3(left.x * right, left.y * right, left.z * right);
        }

        public static Vector3 operator *(Vector3 left, double right)
        {
            return left * (float)right;
        }

        public static Vector3 operator *(Vector3 left, int right)
        {
            return left * (float)right;
        }

        public static Vector3 operator *(float left, Vector3 right)
        {
            return right * left;
        }

        public static Vector3 operator *(double left, Vector3 right)
        {
            return right * (float)left;
        }

        public static Vector3 operator *(int left, Vector3 right)
        {
            return right * (float)left;
        }

        public static Vector3 operator *(Vector3 left, Vector3 right)
        {
            return new Vector3(left.x * right.x, left.y * right.y, left.z * right.z);
        }

        public static Vector3 operator /(Vector3 left, float right)
        {
            if (right != 0.0f)
            {
                float inv = 1.0f / right;
                return new Vector3(left.x * inv, left.y * inv, left.z * inv);
            }
            return Vector3.inf();
        }

        public static Vector3 operator /(Vector3 left, Vector3 right)
        {
            return new Vector3(left.x / right.x, left.y / right.y, left.z / right.z);
        }

        public static Vector3 operator -(Vector3 value)
        {
            return new Vector3(-value.x, -value.y, -value.z);
        }

        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return left.x == right.x && left.y == right.y && left.z == right.z;
        }

        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3)
            {
                return this == (Vector3)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            uint xhash = BitConverter.ToUInt32(BitConverter.GetBytes(x), 0);
            uint yhash = BitConverter.ToUInt32(BitConverter.GetBytes(y), 0);
            uint zhash = BitConverter.ToUInt32(BitConverter.GetBytes(z), 0);
            return (int)(xhash + (yhash * 37) + (zhash * 101));
        }

        public bool fuzzyEq(Vector3 other)
        {
            return g3dmath.fuzzyEq((double)(this - other).squaredMagnitude(), 0.0);
        }

        public bool fuzzyNe(Vector3 other)
        {
            return g3dmath.fuzzyNe((double)(this - other).squaredMagnitude(), 0.0);
        }

        public bool isFinite()
        {
            return !float.IsNaN(x) && !float.IsInfinity(x) &&
                   !float.IsNaN(y) && !float.IsInfinity(y) &&
                   !float.IsNaN(z) && !float.IsInfinity(z);
        }

        public bool isZero()
        {
            return x == 0.0f && y == 0.0f && z == 0.0f;
        }

        public bool isUnit()
        {
            return squaredMagnitude() == 1.0f;
        }

        public Axis primaryAxis()
        {
            Axis a = Axis.X_AXIS;
            double nx = Math.Abs(x);
            double ny = Math.Abs(y);
            double nz = Math.Abs(z);
            if (nx > ny)
            {
                if (nx > nz) a = Axis.X_AXIS;
                else a = Axis.Z_AXIS;
            }
            else
            {
                if (ny > nz) a = Axis.Y_AXIS;
                else a = Axis.Z_AXIS;
            }
            return a;
        }

        private Vector3 reflectAbout(Vector3 normal)
        {
            Vector3 n = normal.direction();
            return n * (2.0f * this.dot(n)) - this;
        }

        public Vector3 reflectionDirection(Vector3 normal)
        {
            return -reflectAbout(normal).direction();
        }

        public Vector3 refractionDirection(Vector3 normal, float iInside, float iOutside)
        {
            Vector3 w = -direction();
            Vector3 n = normal.direction();
            float h1 = iOutside;
            float h2 = iInside;
            if (normal.dot(this) > 0.0f)
            {
                h1 = iInside;
                h2 = iOutside;
                n = -n;
            }
            float hRatio = h1 / h2;
            float wDotN = w.dot(n);
            float det = 1.0f - (float)(hRatio * hRatio) * (1.0f - (wDotN * wDotN));
            if (det < 0.0f)
            {
                return Vector3.zero();
            }
            return -hRatio * (w - wDotN * n) - n * (float)Math.Sqrt(det);
        }

        public Vector3 cross(Vector3 rkVector)
        {
            return new Vector3(
                y * rkVector.z - z * rkVector.y,
                z * rkVector.x - x * rkVector.z,
                x * rkVector.y - y * rkVector.x);
        }

        public Vector3 unitCross(Vector3 rkVector)
        {
            Vector3 c = cross(rkVector);
            float m = length() * rkVector.length();
            if (m > 0.0f)
            {
                return c / m;
            }
            return Vector3.zero();
        }

        public Matrix3 cross()
        {
            return new Matrix3(0.0f, -z, y,
                               z, 0.0f, -x,
                               -y, x, 0.0f);
        }

        public Vector3 min(Vector3 v)
        {
            return new Vector3((float)Math.Min(v.x, x), (float)Math.Min(v.y, y), (float)Math.Min(v.z, z));
        }

        public Vector3 max(Vector3 v)
        {
            return new Vector3((float)Math.Max(v.x, x), (float)Math.Max(v.y, y), (float)Math.Max(v.z, z));
        }

        public string toString()
        {
            return Format.format("(%g, %g, %g)", x, y, z);
        }

        public Vector3 clamp(Vector3 low, Vector3 high)
        {
            return new Vector3(
                (float)g3dmath.clamp(x, low.x, high.x),
                (float)g3dmath.clamp(y, low.y, high.y),
                (float)g3dmath.clamp(z, low.z, high.z));
        }

        public Vector3 clamp(float low, float high)
        {
            return new Vector3(
                (float)g3dmath.clamp(x, low, high),
                (float)g3dmath.clamp(y, low, high),
                (float)g3dmath.clamp(z, low, high));
        }

        public Vector3 lerp(Vector3 v, float alpha)
        {
            return this + (v - this) * alpha;
        }

        public static Vector3 random()
        {
            Vector3 result;
            do
            {
                result = new Vector3(g3dmath.uniformRandom(-1.0f, 1.0f),
                                     g3dmath.uniformRandom(-1.0f, 1.0f),
                                     g3dmath.uniformRandom(-1.0f, 1.0f));
            } while (result.squaredMagnitude() >= 1.0f);
            result.unitize(1e-06f);
            return result;
        }

        public static Vector3 cosRandom(Vector3 normal)
        {
            float e1 = (float)g3dmath.random(0.0, 1.0);
            float e2 = (float)g3dmath.random(0.0, 1.0);
            double theta = Math.Acos(Math.Sqrt(e1));
            double phi = 2.0 * g3dmath.G3D_PI * e2;

            Vector3 u = normal.direction();
            Vector3 v = Vector3.unitX();
            if (Math.Abs(u.dot(v)) > 0.9)
            {
                v = Vector3.unitY();
            }
            Vector3 w = u.cross(v).direction();
            v = w.cross(u);

            return (float)Math.Cos(theta) * u + (float)Math.Sin(theta) * ((float)Math.Cos(phi) * v + (float)Math.Sin(phi) * w);
        }

        public static Vector3 hemiRandom(Vector3 normal)
        {
            Vector3 v = Vector3.random();
            if (v.dot(normal) < 0.0f)
            {
                return -v;
            }
            return v;
        }

        public static void orthonormalize(Vector3[] akVector)
        {
            akVector[0].unitize(1e-06f);
            float dot0 = akVector[0].dot(akVector[1]);
            akVector[1] -= akVector[0] * dot0;
            akVector[1].unitize(1e-06f);
            float dot1 = akVector[1].dot(akVector[2]);
            dot0 = akVector[0].dot(akVector[2]);
            akVector[2] -= akVector[0] * dot0 + akVector[1] * dot1;
            akVector[2].unitize(1e-06f);
        }

        public static void generateOrthonormalBasis(ref Vector3 rkU, ref Vector3 rkV, ref Vector3 rkW, bool bUnitLengthW)
        {
            if (!bUnitLengthW)
            {
                rkW.unitize(1e-06f);
            }

            if (Math.Abs(rkW.x) >= Math.Abs(rkW.y) && Math.Abs(rkW.x) >= Math.Abs(rkW.z))
            {
                rkU.x = -rkW.y;
                rkU.y = rkW.x;
                rkU.z = 0.0f;
            }
            else
            {
                rkU.x = 0.0f;
                rkU.y = rkW.z;
                rkU.z = -rkW.y;
            }

            rkU.unitize(1e-06f);
            rkV = rkW.cross(rkU);
        }

        public float length()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        public float magnitude()
        {
            return length();
        }

        public Vector3 direction()
        {
            float lenSquared = x * x + y * y + z * z;
            if (lenSquared != 1.0f)
            {
                return this / (float)Math.Sqrt(lenSquared);
            }
            return this;
        }

        public Vector3 fastDirection()
        {
            return direction();
        }

        public Vector3 directionOrZero()
        {
            float mag = magnitude();
            if (g3dmath.fuzzyEq(mag, 0.0f))
            {
                return Vector3.zero();
            }
            if (g3dmath.fuzzyEq(mag, 1.0f))
            {
                return this;
            }
            return this * (1.0f / mag);
        }

        public Vector3 unit()
        {
            return direction();
        }

        public Vector3 fastUnit()
        {
            return fastDirection();
        }

        public float squaredLength()
        {
            return x * x + y * y + z * z;
        }

        public float squaredMagnitude()
        {
            return squaredLength();
        }

        public float sum()
        {
            return x + y + z;
        }

        public float average()
        {
            return sum() / 3.0f;
        }

        public float norm()
        {
            return squaredMagnitude();
        }

        public float dot(Vector3 rkVector)
        {
            return x * rkVector.x + y * rkVector.y + z * rkVector.z;
        }

        public float unitize(float fTolerance = 1e-06f)
        {
            float fMagnitude = magnitude();
            if (fMagnitude > fTolerance)
            {
                float fInvMagnitude = 1.0f / fMagnitude;
                x *= fInvMagnitude;
                y *= fInvMagnitude;
                z *= fInvMagnitude;
            }
            else
            {
                fMagnitude = 0.0f;
            }
            return fMagnitude;
        }

        public static Vector3 zero()
        {
            return new Vector3(0.0f, 0.0f, 0.0f);
        }

        public static Vector3 one()
        {
            return new Vector3(1.0f, 1.0f, 1.0f);
        }

        public static Vector3 unitX()
        {
            return new Vector3(1.0f, 0.0f, 0.0f);
        }

        public static Vector3 unitY()
        {
            return new Vector3(0.0f, 1.0f, 0.0f);
        }

        public static Vector3 unitZ()
        {
            return new Vector3(0.0f, 0.0f, 1.0f);
        }

        public static Vector3 inf()
        {
            return new Vector3((float)g3dmath.inf(), (float)g3dmath.inf(), (float)g3dmath.inf());
        }

        public static Vector3 nan()
        {
            return new Vector3((float)g3dmath.nan(), (float)g3dmath.nan(), (float)g3dmath.nan());
        }

        public static Vector3 minFinite()
        {
            return new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
        }

        public static Vector3 maxFinite()
        {
            return new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        }

        public static readonly Vector3 ZERO = new Vector3(0.0f, 0.0f, 0.0f);
        public static readonly Vector3 ZERO3 = new Vector3(0.0f, 0.0f, 0.0f);
        public static readonly Vector3 UNIT_X = new Vector3(1.0f, 0.0f, 0.0f);
        public static readonly Vector3 UNIT_Y = new Vector3(0.0f, 1.0f, 0.0f);
        public static readonly Vector3 UNIT_Z = new Vector3(0.0f, 0.0f, 1.0f);
        public static readonly Vector3 INF3 = new Vector3((float)g3dmath.inf(), (float)g3dmath.inf(), (float)g3dmath.inf());
        public static readonly Vector3 NAN3 = new Vector3((float)g3dmath.nan(), (float)g3dmath.nan(), (float)g3dmath.nan());

        public static Vector3 dummy = new Vector3(0.0f, 0.0f, 0.0f);
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

    }

    public static class Vector3Operators
    {
        public static uint hashCode(Vector3 v)
        {
            return (uint)v.GetHashCode();
        }
    }
}
