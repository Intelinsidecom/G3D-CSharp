/**
  @file Vector2int16.h

  @maintainer Morgan McGuire, matrix@brown.edu

  @created 2003-08-09
  @edited  2004-01-03

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    public struct Vector2int16
    {
        public short x;
        public short y;

        public Vector2int16(short x, short y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2int16(Vector2 v)
        {
            x = (short)g3dmath.iFloor(v.x + 0.5f);
            y = (short)g3dmath.iFloor(v.y + 0.5f);
        }

        public Vector2int16(BinaryInput bi)
        {
            x = 0;
            y = 0;
            deserialize(bi);
        }

        public void serialize(BinaryOutput bo)
        {
            bo.writeInt16(x);
            bo.writeInt16(y);
        }

        public void deserialize(BinaryInput bi)
        {
            x = bi.readInt16();
            y = bi.readInt16();
        }

        public short this[int i]
        {
            get
            {
                if (i == 0)
                {
                    return x;
                }
                if (i == 1)
                {
                    return y;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                if (i == 0)
                {
                    x = value;
                }
                else if (i == 1)
                {
                    y = value;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public static bool operator ==(Vector2int16 left, Vector2int16 right)
        {
            return left.x == right.x && left.y == right.y;
        }

        public static bool operator !=(Vector2int16 left, Vector2int16 right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2int16)
            {
                return this == (Vector2int16)obj;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() * 37);
        }
    }
}
