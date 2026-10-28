/**
  @file Color3uint8.h

  @maintainer Morgan McGuire, graphics3d.com

  @created 2003-04-07
  @edited  2006-06-24

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    public struct Color3uint8
    {
        public byte r;
        public byte g;
        public byte b;

        public Color3uint8(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public Color3uint8(Color3 c)
        {
            r = (byte)g3dmath.iMin(255, g3dmath.iRound(c.r * 256.0f));
            g = (byte)g3dmath.iMin(255, g3dmath.iRound(c.g * 256.0f));
            b = (byte)g3dmath.iMin(255, g3dmath.iRound(c.b * 256.0f));
        }

        public Color3uint8(BinaryInput bi)
        {
            r = 0;
            g = 0;
            b = 0;
            deserialize(bi);
        }

        public static Color3uint8 fromARGB(uint i)
        {
            Color3uint8 c;
            c.r = (byte)((i >> 16) & 0xFF);
            c.g = (byte)((i >> 8) & 0xFF);
            c.b = (byte)(i & 0xFF);
            return c;
        }

        /**
          Returns the color packed into a uint32
          (the upper byte is 0xFF)
         */
        public uint asUInt32()
        {
            return unchecked((uint)(0xFF << 24)) + ((uint)r << 16) + ((uint)g << 8) + b;
        }

        public void serialize(BinaryOutput bo)
        {
            bo.writeUInt8(r);
            bo.writeUInt8(g);
            bo.writeUInt8(b);
        }

        public void deserialize(BinaryInput bi)
        {
            r = bi.readUInt8();
            g = bi.readUInt8();
            b = bi.readUInt8();
        }

        // access vector V as V[0] = V.r, V[1] = V.g, V[2] = V.b
        //
        // WARNING.  These member functions rely on
        // (1) Color3 not having virtual functions
        // (2) the data packed in a 3*sizeof(uint8) memory block
        public byte this[int i]
        {
            get
            {
                Debug.debugAssert((uint)i < 3);
                if (i == 0) return r;
                if (i == 1) return g;
                return b;
            }
            set
            {
                Debug.debugAssert((uint)i < 3);
                if (i == 0) r = value;
                else if (i == 1) g = value;
                else b = value;
            }
        }

        public static bool operator ==(Color3uint8 left, Color3uint8 right)
        {
            return left.r == right.r && left.g == right.g && left.b == right.b;
        }

        public static bool operator !=(Color3uint8 left, Color3uint8 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is Color3uint8 && this == (Color3uint8)obj;
        }

        public override int GetHashCode()
        {
            return r.GetHashCode() ^ (g.GetHashCode() * 31) ^ (b.GetHashCode() * 37);
        }
    }
}
