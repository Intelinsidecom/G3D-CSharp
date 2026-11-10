/**
  @file Color4.h

  Color class

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2001-06-02
  @edited  2005-11-13

  Copyright 2000-2004, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    /**
      Do not subclass-- this implementation makes assumptions about the
      memory layout.
     */
    public class Color4
    {
        /**
         * Channel values.
         */
        public float r, g, b, a;

        /**
         * Does not initialize fields.
         */
        public Color4()
        {
        }

        public Color4(Color3 c3, float a = 1.0f)
        {
            r = c3.r;
            g = c3.g;
            b = c3.b;
            this.a = a;
        }

        public Color4(Color4uint8 c)
        {
            r = c.r;
            g = c.g;
            b = c.b;
            a = c.a;
            float inv = 1.0f / 255.0f;
            r *= inv;
            g *= inv;
            b *= inv;
            a *= inv;
        }

        public Color4(BinaryInput bi)
        {
            deserialize(bi);
        }

        public Color4(Vector4 v)
        {
            r = v.x;
            g = v.y;
            b = v.z;
            a = v.w;
        }

        public Color4(float r, float g, float b, float a = 1.0f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public Color4(float[] value)
        {
            r = value[0];
            g = value[1];
            b = value[2];
            a = value[3];
        }

        public Color4(Color4 other)
        {
            r = other.r;
            g = other.g;
            b = other.b;
            a = other.a;
        }

        public void serialize(BinaryOutput bo)
        {
            bo.writeFloat32(r);
            bo.writeFloat32(g);
            bo.writeFloat32(b);
            bo.writeFloat32(a);
        }

        public void deserialize(BinaryInput bi)
        {
            r = bi.readFloat32();
            g = bi.readFloat32();
            b = bi.readFloat32();
            a = bi.readFloat32();
        }

        /**
         Initialize from an HTML-style color (e.g. 0xFFFF0000 == RED)
         */
        public static Color4 fromARGB(uint x)
        {
            return new Color4(
                (float)((x >> 16) & 0xFF),
                (float)((x >> 8) & 0xFF),
                (float)(x & 0xFF),
                (float)((x >> 24) & 0xFF)) / 255.0f;
        }

        public float this[int i]
        {
            get
            {
                if (i == 0) return r;
                if (i == 1) return g;
                if (i == 2) return b;
                if (i == 3) return a;
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (i == 0) r = value;
                else if (i == 1) g = value;
                else if (i == 2) b = value;
                else if (i == 3) a = value;
                else throw new IndexOutOfRangeException();
            }
        }

        public static Color4 operator +(Color4 left, Color4 right)
        {
            return new Color4(left.r + right.r, left.g + right.g, left.b + right.b, left.a + right.a);
        }

        public static Color4 operator -(Color4 left, Color4 right)
        {
            return new Color4(left.r - right.r, left.g - right.g, left.b - right.b, left.a - right.a);
        }

        public static Color4 operator *(Color4 left, float right)
        {
            return new Color4(right * left.r, right * left.g, right * left.b, right * left.a);
        }

        public static Color4 operator *(float left, Color4 right)
        {
            return new Color4(left * right.r, left * right.g, left * right.b, left * right.a);
        }

        public static Color4 operator /(Color4 left, float right)
        {
            if (right != 0.0f)
            {
                float fInvScalar = 1.0f / right;
                return new Color4(fInvScalar * left.r, fInvScalar * left.g,
                                  fInvScalar * left.b, fInvScalar * left.a);
            }

            return Color4.inf();
        }

        public static Color4 operator -(Color4 value)
        {
            return new Color4(-value.r, -value.g, -value.b, -value.a);
        }

        public static bool operator ==(Color4 left, Color4 right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            {
                return false;
            }

            return left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a;
        }

        public static bool operator !=(Color4 left, Color4 right)
        {
            return !(left == right);
        }

        public static Color4 operator *(Color3 color, Color4 c4)
        {
            return new Color4(color.r * c4.r, color.g * c4.g, color.b * c4.b, c4.a);
        }

        public Color4 max(Color4 other)
        {
            return new Color4(Math.Max(r, other.r), Math.Max(g, other.g), Math.Max(b, other.b), Math.Max(a, other.a));
        }

        public Color4 min(Color4 other)
        {
            return new Color4(Math.Min(r, other.r), Math.Min(g, other.g), Math.Min(b, other.b), Math.Min(a, other.a));
        }

        public bool fuzzyEq(Color4 other)
        {
            Color4 dif = this - other;
            return g3dmath.fuzzyEq(dif.r * dif.r + dif.g * dif.g + dif.b * dif.b + dif.a * dif.a, 0);
        }

        public bool fuzzyNe(Color4 other)
        {
            Color4 dif = this - other;
            return g3dmath.fuzzyNe(dif.r * dif.r + dif.g * dif.g + dif.b * dif.b + dif.a * dif.a, 0);
        }

        public override bool Equals(object obj)
        {
            return obj is Color4 && this == (Color4)obj;
        }

        public override int GetHashCode()
        {
            uint rhash = BitConverter.ToUInt32(BitConverter.GetBytes(r), 0);
            uint ghash = BitConverter.ToUInt32(BitConverter.GetBytes(g), 0);
            uint bhash = BitConverter.ToUInt32(BitConverter.GetBytes(b), 0);
            uint ahash = BitConverter.ToUInt32(BitConverter.GetBytes(a), 0);
            return (int)(rhash + (ghash * 37) + (bhash * 101) + (ahash * 241));
        }

        public static Color4 zero()
        {
            return new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        }

        public static Color4 one()
        {
            return new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        }

        public static Color4 inf()
        {
            return new Color4((float)g3dmath.inf(), (float)g3dmath.inf(), (float)g3dmath.inf(), (float)g3dmath.inf());
        }

        public static Color4 nan()
        {
            return new Color4((float)g3dmath.nan(), (float)g3dmath.nan(), (float)g3dmath.nan(), (float)g3dmath.nan());
        }

        public static Color4 clear()
        {
            return Color4.zero();
        }

        public static readonly Color4 ZERO = new Color4(0.0f, 0.0f, 0.0f, 0.0f);
        public static readonly Color4 CLEAR = new Color4(0.0f, 0.0f, 0.0f, 0.0f);

        public override string ToString()
        {
            return Format.format("(%g, %g, %g, %g)", r, g, b, a);
        }
    }
}
