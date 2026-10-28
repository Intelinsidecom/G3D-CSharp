/**
  @file Vector4.h

  Homogeneous vector class.

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2002-07-09
  @edited  2005-03-28

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public Vector4(float[] coordinate)
        {
            x = coordinate[0];
            y = coordinate[1];
            z = coordinate[2];
            w = coordinate[3];
        }

        public Vector4(Vector3 rkVector, float w)
        {
            x = rkVector.x;
            y = rkVector.y;
            z = rkVector.z;
            this.w = w;
        }

        public Vector4(Vector2 v1, Vector2 v2)
        {
            x = v1.x;
            y = v1.y;
            z = v2.x;
            w = v2.y;
        }

        public Vector4(Vector2 v1, float z, float w)
        {
            x = v1.x;
            y = v1.y;
            this.z = z;
            this.w = w;
        }

        public Vector4(Color4 c)
        {
            x = c.r;
            y = c.g;
            z = c.b;
            w = c.a;
        }

        public Vector4(BinaryInput b)
        {
            x = 0.0f;
            y = 0.0f;
            z = 0.0f;
            w = 0.0f;
            deserialize(b);
        }

        public void serialize(BinaryOutput b)
        {
            b.writeFloat32(x);
            b.writeFloat32(y);
            b.writeFloat32(z);
            b.writeFloat32(w);
        }

        public void deserialize(BinaryInput b)
        {
            x = b.readFloat32();
            y = b.readFloat32();
            z = b.readFloat32();
            w = b.readFloat32();
        }

        public float this[int i]
        {
            get
            {
                if (i == 0) return x;
                if (i == 1) return y;
                if (i == 2) return z;
                if (i == 3) return w;
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (i == 0) x = value;
                else if (i == 1) y = value;
                else if (i == 2) z = value;
                else if (i == 3) w = value;
                else throw new IndexOutOfRangeException();
            }
        }

        public float[] ToArray()
        {
            return new float[] { x, y, z, w };
        }

        public void CopyTo(float[] destination, int destinationIndex)
        {
            destination[destinationIndex] = x;
            destination[destinationIndex + 1] = y;
            destination[destinationIndex + 2] = z;
            destination[destinationIndex + 3] = w;
        }

        public static Vector4 operator +(Vector4 left, Vector4 right)
        {
            return new Vector4(left.x + right.x, left.y + right.y, left.z + right.z, left.w + right.w);
        }

        public static Vector4 operator -(Vector4 left, Vector4 right)
        {
            return new Vector4(left.x - right.x, left.y - right.y, left.z - right.z, left.w - right.w);
        }

        public static Vector4 operator *(Vector4 left, Vector4 right)
        {
            return new Vector4(left.x * right.x, left.y * right.y, left.z * right.z, left.w * right.w);
        }

        public static Vector4 operator /(Vector4 left, Vector4 right)
        {
            return new Vector4(left.x / right.x, left.y / right.y, left.z / right.z, left.w / right.w);
        }

        public static Vector4 operator *(Vector4 left, float right)
        {
            return new Vector4(left.x * right, left.y * right, left.z * right, left.w * right);
        }

        public static Vector4 operator *(float left, Vector4 right)
        {
            return right * left;
        }

        public static Vector4 operator /(Vector4 left, float right)
        {
            if (right != 0.0f)
            {
                float inv = 1.0f / right;
                return new Vector4(left.x * inv, left.y * inv, left.z * inv, left.w * inv);
            }
            return Vector4.inf();
        }

        public static Vector4 operator -(Vector4 value)
        {
            return new Vector4(-value.x, -value.y, -value.z, -value.w);
        }

        public static bool operator ==(Vector4 left, Vector4 right)
        {
            return left.x == right.x && left.y == right.y && left.z == right.z && left.w == right.w;
        }

        public static bool operator !=(Vector4 left, Vector4 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector4)
            {
                return this == (Vector4)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            uint xhash = BitConverter.ToUInt32(BitConverter.GetBytes(x), 0);
            uint yhash = BitConverter.ToUInt32(BitConverter.GetBytes(y), 0);
            uint zhash = BitConverter.ToUInt32(BitConverter.GetBytes(z), 0);
            uint whash = BitConverter.ToUInt32(BitConverter.GetBytes(w), 0);
            return (int)(xhash + (yhash * 37) + (zhash * 101) + (whash * 241));
        }

        public bool fuzzyEq(Vector4 other)
        {
            return g3dmath.fuzzyEq((double)(this - other).squaredLength(), 0.0);
        }

        public bool fuzzyNe(Vector4 other)
        {
            return g3dmath.fuzzyNe((double)(this - other).squaredLength(), 0.0);
        }

        public bool isFinite()
        {
            return !float.IsNaN(x) && !float.IsInfinity(x) &&
                   !float.IsNaN(y) && !float.IsInfinity(y) &&
                   !float.IsNaN(z) && !float.IsInfinity(z) &&
                   !float.IsNaN(w) && !float.IsInfinity(w);
        }

        public bool isZero()
        {
            return x == 0.0f && y == 0.0f && z == 0.0f && w == 0.0f;
        }

        public bool isUnit()
        {
            return squaredLength() == 1.0f;
        }

        public float length()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z + w * w);
        }

        public float squaredLength()
        {
            return x * x + y * y + z * z + w * w;
        }

        public float sum()
        {
            return x + y + z + w;
        }

        public float dot(Vector4 other)
        {
            return x * other.x + y * other.y + z * other.z + w * other.w;
        }

        public Vector4 min(Vector4 v)
        {
            return new Vector4((float)Math.Min(v.x, x), (float)Math.Min(v.y, y), (float)Math.Min(v.z, z), (float)Math.Min(v.w, w));
        }

        public Vector4 max(Vector4 v)
        {
            return new Vector4((float)Math.Max(v.x, x), (float)Math.Max(v.y, y), (float)Math.Max(v.z, z), (float)Math.Max(v.w, w));
        }

        public string toString()
        {
            return Format.format("(%g, %g, %g, %g)", x, y, z, w);
        }

        public Vector4 clamp(Vector4 low, Vector4 high)
        {
            return new Vector4(
                (float)g3dmath.clamp(x, low.x, high.x),
                (float)g3dmath.clamp(y, low.y, high.y),
                (float)g3dmath.clamp(z, low.z, high.z),
                (float)g3dmath.clamp(w, low.w, high.w));
        }

        public Vector4 clamp(float low, float high)
        {
            return new Vector4(
                (float)g3dmath.clamp(x, low, high),
                (float)g3dmath.clamp(y, low, high),
                (float)g3dmath.clamp(z, low, high),
                (float)g3dmath.clamp(w, low, high));
        }

        public Vector4 lerp(Vector4 v, float alpha)
        {
            return this + (v - this) * alpha;
        }

        public static Vector4 inf()
        {
            return new Vector4((float)g3dmath.inf(), (float)g3dmath.inf(), (float)g3dmath.inf(), (float)g3dmath.inf());
        }

        public static Vector4 nan()
        {
            return new Vector4((float)g3dmath.nan(), (float)g3dmath.nan(), (float)g3dmath.nan(), (float)g3dmath.nan());
        }
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
}
