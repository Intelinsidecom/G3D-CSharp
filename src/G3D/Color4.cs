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
    public class Color4
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color4()
        {
        }

        public Color4(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = 1.0f;
        }

        public Color4(Color3 color, float a)
        {
            this.r = color.r;
            this.g = color.g;
            this.b = color.b;
            this.a = a;
        }

        public Color4(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
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
            return new Color4(left.r * right, left.g * right, left.b * right, left.a * right);
        }

        public static Color4 operator *(float left, Color4 right)
        {
            return right * left;
        }

        public static Color4 operator /(Color4 left, float right)
        {
            return new Color4(left.r / right, left.g / right, left.b / right, left.a / right);
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

        public override bool Equals(object obj)
        {
            return obj is Color4 && this == (Color4)obj;
        }

        public override int GetHashCode()
        {
            return r.GetHashCode() ^ (g.GetHashCode() * 31) ^ (b.GetHashCode() * 37) ^ (a.GetHashCode() * 41);
        }
    }
}
