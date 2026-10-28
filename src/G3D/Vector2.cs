/**
  @file Vector2.h

  2D vector class

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2001-06-02
  @edited  2006-01-14
  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2(float[] coordinate)
        {
            x = coordinate[0];
            y = coordinate[1];
        }

        public Vector2(double[] coordinate)
        {
            x = (float)coordinate[0];
            y = (float)coordinate[1];
        }

        public Vector2(Vector2 rkVector)
        {
            x = rkVector.x;
            y = rkVector.y;
        }

        public Vector2(Vector2int16 v)
        {
            x = v.x;
            y = v.y;
        }

        public Vector2(BinaryInput b)
        {
            x = 0.0f;
            y = 0.0f;
            deserialize(b);
        }

        public void serialize(BinaryOutput b)
        {
            b.writeFloat32(x);
            b.writeFloat32(y);
        }

        public void deserialize(BinaryInput b)
        {
            x = b.readFloat32();
            y = b.readFloat32();
        }

        public void serialize(TextOutput t)
        {
            t.writeSymbol("(");
            t.writeNumber(x);
            t.writeSymbol(",");
            t.writeNumber(y);
            t.writeSymbol(")");
        }

        public void deserialize(TextInput t)
        {
            t.readSymbol("(");
            x = (float)t.readNumber();
            t.readSymbol(",");
            y = (float)t.readNumber();
            t.readSymbol(")");
        }

        public float this[int i]
        {
            get
            {
                if (i == 0) return x;
                if (i == 1) return y;
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (i == 0) x = value;
                else if (i == 1) y = value;
                else throw new IndexOutOfRangeException();
            }
        }

        public float[] ToArray()
        {
            return new float[] { x, y };
        }

        public void CopyTo(float[] destination, int destinationIndex)
        {
            destination[destinationIndex] = x;
            destination[destinationIndex + 1] = y;
        }

        public static Vector2 operator +(Vector2 left, Vector2 right)
        {
            return new Vector2(left.x + right.x, left.y + right.y);
        }

        public static Vector2 operator -(Vector2 left, Vector2 right)
        {
            return new Vector2(left.x - right.x, left.y - right.y);
        }

        public static Vector2 operator *(Vector2 left, float right)
        {
            return new Vector2(left.x * right, left.y * right);
        }

        public static Vector2 operator *(float left, Vector2 right)
        {
            return right * left;
        }

        public static Vector2 operator *(int left, Vector2 right)
        {
            return right * (float)left;
        }

        public static Vector2 operator *(double left, Vector2 right)
        {
            return right * (float)left;
        }

        public static Vector2 operator *(Vector2 left, Vector2 right)
        {
            return new Vector2(left.x * right.x, left.y * right.y);
        }

        public static Vector2 operator /(Vector2 left, float right)
        {
            if (right != 0.0f)
            {
                float inv = 1.0f / right;
                return new Vector2(left.x * inv, left.y * inv);
            }
            return Vector2.inf();
        }

        public static Vector2 operator /(Vector2 left, Vector2 right)
        {
            return new Vector2(left.x / right.x, left.y / right.y);
        }

        public static Vector2 operator -(Vector2 value)
        {
            return new Vector2(-value.x, -value.y);
        }

        public static bool operator ==(Vector2 left, Vector2 right)
        {
            return left.x == right.x && left.y == right.y;
        }

        public static bool operator !=(Vector2 left, Vector2 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2)
            {
                return this == (Vector2)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            uint xhash = BitConverter.ToUInt32(BitConverter.GetBytes(x), 0);
            uint yhash = BitConverter.ToUInt32(BitConverter.GetBytes(y), 0);
            return (int)(xhash + (yhash * 37));
        }

        public bool fuzzyEq(Vector2 other)
        {
            return g3dmath.fuzzyEq((double)(this - other).squaredLength(), 0.0);
        }

        public bool fuzzyNe(Vector2 other)
        {
            return g3dmath.fuzzyNe((double)(this - other).squaredLength(), 0.0);
        }

        public bool isFinite()
        {
            return !float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(y) && !float.IsInfinity(y);
        }

        public bool isZero()
        {
            return x == 0.0f && y == 0.0f;
        }

        public bool isUnit()
        {
            return squaredLength() == 1.0f;
        }


        public float length()
        {
            return (float)Math.Sqrt(x * x + y * y);
        }

        public float sum()
        {
            return x + y;
        }


        public Vector2 direction()
        {
            float lenSquared = x * x + y * y;
            if (lenSquared != 1.0f)
            {
                return this / (float)Math.Sqrt(lenSquared);
            }
            return this;
        }

        public Vector2 fastDirection()
        {
            return direction();
        }

        public float squaredLength()
        {
            return x * x + y * y;
        }

        public float dot(Vector2 other)
        {
            return x * other.x + y * other.y;
        }

        public float unitize(float tolerance)
        {
            float len = length();
            if (len > tolerance)
            {
                float inv = 1.0f / len;
                x *= inv;
                y *= inv;
            }
            else
            {
                len = 0.0f;
            }
            return len;
        }

        public Vector2 min(Vector2 v)
        {
            return new Vector2((float)Math.Min(v.x, x), (float)Math.Min(v.y, y));
        }

        public Vector2 max(Vector2 v)
        {
            return new Vector2((float)Math.Max(v.x, x), (float)Math.Max(v.y, y));
        }

        public Vector2 lerp(Vector2 v, float alpha)
        {
            return this + (v - this) * alpha;
        }

        public Vector2 clamp(Vector2 low, Vector2 high)
        {
            return new Vector2(
                (float)g3dmath.clamp(x, low.x, high.x),
                (float)g3dmath.clamp(y, low.y, high.y));
        }

        public Vector2 clamp(float low, float high)
        {
            return new Vector2(
                (float)g3dmath.clamp(x, low, high),
                (float)g3dmath.clamp(y, low, high));
        }

        public static Vector2 random()
        {
            Vector2 result;
            do
            {
                result = new Vector2((float)g3dmath.symmetricRandom(), (float)g3dmath.symmetricRandom());
            } while (result.squaredLength() > 1.0f || result.squaredLength() == 1.0f);
            result.unitize(1e-06f);
            return result;
        }

        public static Vector2 zero()
        {
            return new Vector2(0.0f, 0.0f);
        }

        public static Vector2 unitX()
        {
            return new Vector2(1.0f, 0.0f);
        }

        public static Vector2 unitY()
        {
            return new Vector2(0.0f, 1.0f);
        }

        public static Vector2 inf()
        {
            return new Vector2((float)g3dmath.inf(), (float)g3dmath.inf());
        }

        public static Vector2 nan()
        {
            return new Vector2((float)g3dmath.nan(), (float)g3dmath.nan());
        }

        public static Vector2 minFinite()
        {
            return new Vector2(-float.MaxValue, -float.MaxValue);
        }

        public static Vector2 maxFinite()
        {
            return new Vector2(float.MaxValue, float.MaxValue);
        }

        public static readonly Vector2 ZERO = new Vector2(0.0f, 0.0f);
        public static readonly Vector2 UNIT_S = new Vector2(1.0f, 0.0f);
        public static readonly Vector2 UNIT_T = new Vector2(0.0f, 1.0f);

        public static Vector2 one()
        {
            return new Vector2(1.0f, 1.0f);
        }

        public string toString()
        {
            return Format.format("(%g, %g)", x, y);
        }

        public uint hashCode()
        {
            uint xhash = BitConverter.ToUInt32(BitConverter.GetBytes(x), 0);
            uint yhash = BitConverter.ToUInt32(BitConverter.GetBytes(y), 0);
            return xhash + (yhash * 37);
        }

        public Vector2 xx()
        {
            return new Vector2(x, x);
        }

        public Vector2 yx()
        {
            return new Vector2(y, x);
        }

        public Vector2 xy()
        {
            return new Vector2(x, y);
        }

        public Vector2 yy()
        {
            return new Vector2(y, y);
        }

        public Vector3 xxx()
        {
            return new Vector3(x, x, x);
        }

        public Vector3 yxx()
        {
            return new Vector3(y, x, x);
        }

        public Vector3 xyx()
        {
            return new Vector3(x, y, x);
        }

        public Vector3 yyx()
        {
            return new Vector3(y, y, x);
        }

        public Vector3 xxy()
        {
            return new Vector3(x, x, y);
        }

        public Vector3 yxy()
        {
            return new Vector3(y, x, y);
        }

        public Vector3 xyy()
        {
            return new Vector3(x, y, y);
        }

        public Vector3 yyy()
        {
            return new Vector3(y, y, y);
        }

        public Vector4 xxxx()
        {
            return new Vector4(x, x, x, x);
        }

        public Vector4 yxxx()
        {
            return new Vector4(y, x, x, x);
        }

        public Vector4 xyxx()
        {
            return new Vector4(x, y, x, x);
        }

        public Vector4 yyxx()
        {
            return new Vector4(y, y, x, x);
        }

        public Vector4 xxyx()
        {
            return new Vector4(x, x, y, x);
        }

        public Vector4 yxyx()
        {
            return new Vector4(y, x, y, x);
        }

        public Vector4 xyyx()
        {
            return new Vector4(x, y, y, x);
        }

        public Vector4 yyyx()
        {
            return new Vector4(y, y, y, x);
        }

        public Vector4 xxxy()
        {
            return new Vector4(x, x, x, y);
        }

        public Vector4 yxxy()
        {
            return new Vector4(y, x, x, y);
        }

        public Vector4 xyxy()
        {
            return new Vector4(x, y, x, y);
        }

        public Vector4 yyxy()
        {
            return new Vector4(y, y, x, y);
        }

        public Vector4 xxyy()
        {
            return new Vector4(x, x, y, y);
        }

        public Vector4 yxyy()
        {
            return new Vector4(y, x, y, y);
        }

        public Vector4 xyyy()
        {
            return new Vector4(x, y, y, y);
        }

        public Vector4 yyyy()
        {
            return new Vector4(y, y, y, y);
        }
    }

    public static class Vector2Operators
    {
        public static Vector2 Multiply(float s, Vector2 v)
        {
            return v * s;
        }

        public static Vector2 Multiply(int s, Vector2 v)
        {
            return v * (float)s;
        }

        public static Vector2 Multiply(double s, Vector2 v)
        {
            return v * (float)s;
        }
    }
}
