/**
 @file Color3.h

 Color class

 @maintainer Morgan McGuire, matrix@graphics3d.com
 @cite Portions based on Dave Eberly's Magic Software Library
      at <A HREF="http://www.magic-software.com">http://www.magic-software.com</A>

 @created 2001-06-02
 @edited  2005-11-13

 Copyright 2000-2004, Morgan McGuire.
 All rights reserved.
 */

using System;

namespace G3D
{
    public struct Color3
    {
        public float r;
        public float g;
        public float b;

        public Color3(float r, float g, float b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public Color3(Vector3 v)
        {
            r = v.x;
            g = v.y;
            b = v.z;
        }

        public Color3(float[] value)
        {
            r = value[0];
            g = value[1];
            b = value[2];
        }

        public Color3(Color3 other)
        {
            r = other.r;
            g = other.g;
            b = other.b;
        }

        public Color3(Color3uint8 other)
        {
            r = other.r / 255.0f;
            g = other.g / 255.0f;
            b = other.b / 255.0f;
        }

        public Color3(BinaryInput b)
        {
            r = 0.0f;
            g = 0.0f;
            this.b = 0.0f;
            deserialize(b);
        }

        public static Color3 fromARGB(uint x)
        {
            return new Color3((float)((x >> 16) & 0xFF), (float)((x >> 8) & 0xFF), (float)(x & 0xFF)) / 255.0f;
        }

        public static Color3 wheelRandom()
        {
            Color3[] colorArray = new Color3[] { Color3.blue(), Color3.red(), Color3.green(),
                Color3.orange(), Color3.yellow(), Color3.cyan(), Color3.purple(), Color3.brown() };
            return colorArray[g3dmath.iRandom(0, 7)];
        }

        public void serialize(BinaryOutput b)
        {
            b.writeFloat32(r);
            b.writeFloat32(g);
            b.writeFloat32(this.b);
        }

        public void deserialize(BinaryInput b)
        {
            r = b.readFloat32();
            g = b.readFloat32();
            this.b = b.readFloat32();
        }

        public float this[int i]
        {
            get
            {
                if (i == 0) return r;
                if (i == 1) return g;
                if (i == 2) return b;
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (i == 0) r = value;
                else if (i == 1) g = value;
                else if (i == 2) b = value;
                else throw new IndexOutOfRangeException();
            }
        }

        public float[] ToArray()
        {
            return new float[] { r, g, b };
        }

        public void CopyTo(float[] destination, int destinationIndex)
        {
            destination[destinationIndex] = r;
            destination[destinationIndex + 1] = g;
            destination[destinationIndex + 2] = b;
        }

        public static Color3 operator +(Color3 left, Color3 right)
        {
            return new Color3(left.r + right.r, left.g + right.g, left.b + right.b);
        }

        public static Color3 operator -(Color3 left, Color3 right)
        {
            return new Color3(left.r - right.r, left.g - right.g, left.b - right.b);
        }

        public static Color3 operator *(Color3 left, float right)
        {
            return new Color3(left.r * right, left.g * right, left.b * right);
        }

        public static Color3 operator *(float left, Color3 right)
        {
            return right * left;
        }

        public static Color3 operator *(Color3 left, Color3 right)
        {
            return new Color3(left.r * right.r, left.g * right.g, left.b * right.b);
        }

        public static Color3 operator /(Color3 left, float right)
        {
            if (right != 0.0f)
            {
                float inv = 1.0f / right;
                return new Color3(left.r * inv, left.g * inv, left.b * inv);
            }
            return Color3.inf();
        }

        public static Color3 operator -(Color3 value)
        {
            return new Color3(-value.r, -value.g, -value.b);
        }

        public static bool operator ==(Color3 left, Color3 right)
        {
            return left.r == right.r && left.g == right.g && left.b == right.b;
        }

        public static bool operator !=(Color3 left, Color3 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Color3)
            {
                return this == (Color3)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            uint rhash = BitConverter.ToUInt32(BitConverter.GetBytes(r), 0);
            uint ghash = BitConverter.ToUInt32(BitConverter.GetBytes(g), 0);
            uint bhash = BitConverter.ToUInt32(BitConverter.GetBytes(b), 0);
            return (int)(rhash + (ghash * 37) + (bhash * 101));
        }

        public Color3 max(Color3 other)
        {
            return new Color3(g3dmath.max(r, other.r), g3dmath.max(g, other.g), g3dmath.max(b, other.b));
        }

        public Color3 min(Color3 other)
        {
            return new Color3(g3dmath.min(r, other.r), g3dmath.min(g, other.g), g3dmath.min(b, other.b));
        }

        public Color3 lerp(Color3 other, float a)
        {
            return this + (other - this) * a;
        }

        public double sum()
        {
            return r + g + b;
        }

        public double average()
        {
            return sum() / 3.0;
        }

        public static Color3 fromHSV(Vector3 hsv)
        {
            Debug.debugAssertM(hsv.x >= 0.0f && hsv.x <= 1.0f && hsv.y >= 0.0f && hsv.y <= 1.0f && hsv.z >= 0.0f && hsv.z <= 1.0f, "H,S,V must be between [0,1]");
            int i = g3dmath.iFloor(6.0 * hsv.x);
            float f = 6.0f * hsv.x - i;
            float m = hsv.z * (1.0f - hsv.y);
            float n = hsv.z * (1.0f - hsv.y * f);
            float k = hsv.z * (1.0f - hsv.y * (1.0f - f));
            switch (i)
            {
                case 0: return new Color3(hsv.z, k, m);
                case 1: return new Color3(n, hsv.z, m);
                case 2: return new Color3(m, hsv.z, k);
                case 3: return new Color3(m, n, hsv.z);
                case 4: return new Color3(k, m, hsv.z);
                case 5: return new Color3(hsv.z, m, n);
                default:
                    Debug.debugAssertM(false, "fell through switch..");
                    return Color3.black();
            }
        }

        public static Vector3 toHSV(Color3 rgb)
        {
            Debug.debugAssertM(rgb.r >= 0.0f && rgb.r <= 1.0f && rgb.g >= 0.0f && rgb.g <= 1.0f && rgb.b >= 0.0f && rgb.b <= 1.0f, "R,G,B must be between [0,1]");
            Vector3 hsv = Vector3.zero();
            hsv.z = g3dmath.max(g3dmath.max(rgb.r, rgb.g), rgb.b);
            if (g3dmath.fuzzyEq(hsv.z, 0.0))
            {
                return hsv;
            }
            float x = g3dmath.min(g3dmath.min(rgb.r, rgb.g), rgb.b);
            hsv.y = (hsv.z - x) / hsv.z;
            if (g3dmath.fuzzyEq(hsv.y, 0.0))
            {
                return hsv;
            }
            Vector3 rgbN = new Vector3((hsv.z - rgb.r) / (hsv.z - x), (hsv.z - rgb.g) / (hsv.z - x), (hsv.z - rgb.b) / (hsv.z - x));
            if (rgb.r == hsv.z)
            {
                hsv.x = (rgb.g == x) ? 5.0f + rgbN.z : 1.0f - rgbN.y;
            }
            else if (rgb.g == hsv.z)
            {
                hsv.x = (rgb.b == x) ? 1.0f + rgbN.x : 3.0f - rgbN.z;
            }
            else
            {
                hsv.x = (rgb.r == x) ? 3.0f + rgbN.y : 5.0f - rgbN.x;
            }
            hsv.x /= 6.0f;
            return hsv;
        }

        public static Color3 jetColorMap(float val)
        {
            Debug.debugAssertM(val >= 0.0f && val <= 1.0f, "value should be in [0,1]");
            Color3 jet = new Color3(0.0f, 0.0f, 0.0f);
            jet.r = g3dmath.min(4.0f * val - 1.5f, -4.0f * val + 4.5f);
            jet.g = g3dmath.min(4.0f * val - 0.5f, -4.0f * val + 3.5f);
            jet.b = g3dmath.min(4.0f * val + 0.5f, -4.0f * val + 2.5f);
            jet.r = g3dmath.clamp(jet.r, 0.0f, 1.0f);
            jet.g = g3dmath.clamp(jet.g, 0.0f, 1.0f);
            jet.b = g3dmath.clamp(jet.b, 0.0f, 1.0f);
            return jet;
        }

        public string toString()
        {
            return Format.format("(%g, %g, %g)", r, g, b);
        }

        public static Color3 random()
        {
            return (new Color3(g3dmath.uniformRandom(), g3dmath.uniformRandom(), g3dmath.uniformRandom())).direction();
        }

        public float length()
        {
            return (float)Math.Sqrt(r * r + g * g + b * b);
        }

        public Color3 direction()
        {
            float mag = length();
            if (mag != 0.0f)
            {
                return this / mag;
            }
            return Color3.inf();
        }

        public float squaredLength()
        {
            return r * r + g * g + b * b;
        }

        public float dot(Color3 rkVector)
        {
            return r * rkVector.r + g * rkVector.g + b * rkVector.b;
        }

        public float unitize(float fTolerance)
        {
            float fLength = length();
            if (fLength > fTolerance)
            {
                float fInvLength = 1.0f / fLength;
                r *= fInvLength;
                g *= fInvLength;
                b *= fInvLength;
            }
            else
            {
                fLength = 0.0f;
            }
            return fLength;
        }

        public Color3 cross(Color3 rkVector)
        {
            return new Color3(
                g * rkVector.b - b * rkVector.g,
                b * rkVector.r - r * rkVector.b,
                r * rkVector.g - g * rkVector.r);
        }

        public Color3 unitCross(Color3 rkVector)
        {
            Color3 c = cross(rkVector);
            float m = length() * rkVector.length();
            if (m > 0.0f)
            {
                return c / m;
            }
            return Color3.zero();
        }

        public bool fuzzyEq(Color3 other)
        {
            return g3dmath.fuzzyEq((double)(this - other).squaredLength(), 0.0);
        }

        public bool fuzzyNe(Color3 other)
        {
            return g3dmath.fuzzyNe((double)(this - other).squaredLength(), 0.0);
        }

        public static Color3 red() { return new Color3(1.0f, 0.0f, 0.0f); }
        public static Color3 green() { return new Color3(0.0f, 1.0f, 0.0f); }
        public static Color3 blue() { return new Color3(0.0f, 0.0f, 1.0f); }
        public static Color3 purple() { return new Color3(0.7f, 0.0f, 1.0f); }
        public static Color3 cyan() { return new Color3(0.0f, 0.7f, 1.0f); }
        public static Color3 yellow() { return new Color3(1.0f, 1.0f, 0.0f); }
        public static Color3 brown() { return new Color3(0.5f, 0.5f, 0.0f); }
        public static Color3 orange() { return new Color3(1.0f, 0.5f, 0.0f); }
        public static Color3 black() { return new Color3(0.0f, 0.0f, 0.0f); }
        public static Color3 gray() { return new Color3(0.7f, 0.7f, 0.7f); }
        public static Color3 white() { return new Color3(1.0f, 1.0f, 1.0f); }

        public static readonly Color3 RED = new Color3(1.0f, 0.0f, 0.0f);
        public static readonly Color3 GREEN = new Color3(0.0f, 1.0f, 0.0f);
        public static readonly Color3 BLUE = new Color3(0.0f, 0.0f, 1.0f);
        public static readonly Color3 PURPLE = new Color3(0.7f, 0.0f, 1.0f);
        public static readonly Color3 CYAN = new Color3(0.0f, 0.7f, 1.0f);
        public static readonly Color3 YELLOW = new Color3(1.0f, 1.0f, 0.0f);
        public static readonly Color3 BROWN = new Color3(0.5f, 0.5f, 0.0f);
        public static readonly Color3 ORANGE = new Color3(1.0f, 0.5f, 0.0f);
        public static readonly Color3 BLACK = new Color3(0.0f, 0.0f, 0.0f);
        public static readonly Color3 GRAY = new Color3(0.7f, 0.7f, 0.7f);
        public static readonly Color3 WHITE = new Color3(1.0f, 1.0f, 1.0f);

        public static Color3 zero()
        {
            return new Color3(0.0f, 0.0f, 0.0f);
        }

        public static Color3 one()
        {
            return new Color3(1.0f, 1.0f, 1.0f);
        }

        public static Color3 inf()
        {
            return new Color3((float)g3dmath.inf(), (float)g3dmath.inf(), (float)g3dmath.inf());
        }

        public static Color3 nan()
        {
            return new Color3((float)g3dmath.nan(), (float)g3dmath.nan(), (float)g3dmath.nan());
        }
    }
}
