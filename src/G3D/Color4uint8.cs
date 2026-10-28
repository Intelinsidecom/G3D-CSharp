/**
  @file Color4uint8.h

  @maintainer Morgan McGuire, graphics3d.com

  @created 2003-04-07
  @edited 2006-03-24

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    /**
     Represents a Color4 as a packed integer.  Convenient
     for creating unsigned int vertex arrays.  Used by
     G3D::GImage as the underlying format.

     <B>WARNING</B>: Integer color formats are different than
     integer vertex formats.  The color channels are automatically
     scaled by 255 (because OpenGL automatically scales integer
     colors back by this factor).  So Color4(1,1,1) == Color4uint8(255,255,255)
     but Vector3(1,1,1) == Vector3int16(1,1,1).

     */
    public struct Color4uint8
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color4uint8(byte _r, byte _g, byte _b, byte _a)
        {
            r = _r;
            g = _g;
            b = _b;
            a = _a;
        }

        public Color4uint8(Color3uint8 c, byte _a)
        {
            r = c.r;
            g = c.g;
            b = c.b;
            a = _a;
        }

        public Color4uint8(Color4 c)
        {
            r = (byte)g3dmath.iMin(255, g3dmath.iRound(c.r * 256.0f));
            g = (byte)g3dmath.iMin(255, g3dmath.iRound(c.g * 256.0f));
            b = (byte)g3dmath.iMin(255, g3dmath.iRound(c.b * 256.0f));
            a = (byte)g3dmath.iMin(255, g3dmath.iRound(c.a * 256.0f));
        }

        public Color4uint8(BinaryInput bi)
        {
            r = 0;
            g = 0;
            b = 0;
            a = 0;
            deserialize(bi);
        }

        public static Color4uint8 fromARGB(uint i)
        {
            Color4uint8 c;
            c.a = (byte)((i >> 24) & 0xFF);
            c.r = (byte)((i >> 16) & 0xFF);
            c.g = (byte)((i >> 8) & 0xFF);
            c.b = (byte)(i & 0xFF);
            return c;
        }

        public uint asUInt32()
        {
            return ((uint)a << 24) + ((uint)r << 16) + ((uint)g << 8) + b;
        }

        public byte[] ToArray()
        {
            return new byte[] { r, g, b, a };
        }

        public void CopyTo(byte[] destination, int destinationIndex)
        {
            destination[destinationIndex] = r;
            destination[destinationIndex + 1] = g;
            destination[destinationIndex + 2] = b;
            destination[destinationIndex + 3] = a;
        }

        public byte this[int i]
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

        public void serialize(BinaryOutput bo)
        {
            bo.writeUInt8(r);
            bo.writeUInt8(g);
            bo.writeUInt8(b);
            bo.writeUInt8(a);
        }

        public void deserialize(BinaryInput bi)
        {
            r = bi.readUInt8();
            g = bi.readUInt8();
            b = bi.readUInt8();
            a = bi.readUInt8();
        }

        public static bool operator ==(Color4uint8 left, Color4uint8 right)
        {
            return left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a;
        }

        public static bool operator !=(Color4uint8 left, Color4uint8 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is Color4uint8 && this == (Color4uint8)obj;
        }

        public override int GetHashCode()
        {
            return r.GetHashCode() ^ (g.GetHashCode() * 31) ^ (b.GetHashCode() * 37) ^ (a.GetHashCode() * 41);
        }
    }
}
