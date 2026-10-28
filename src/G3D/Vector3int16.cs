using System;

/**
  @file Vector3int16.h

  @maintainer Morgan McGuire, matrix@brown.edu

  @created 2003-04-07
  @edited  2003-06-24
  Copyright 2000-2004, Morgan McGuire.
  All rights reserved.
 */

namespace G3D
{
    public struct Vector3int16
    {
        public short x;
        public short y;
        public short z;

        public Vector3int16(short x, short y, short z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3int16(Vector3 v)
        {
            x = (short)g3dmath.iFloor(v.x + 0.5f);
            y = (short)g3dmath.iFloor(v.y + 0.5f);
            z = (short)g3dmath.iFloor(v.z + 0.5f);
        }

        public Vector3int16(BinaryInput bi)
        {
            x = 0;
            y = 0;
            z = 0;
            deserialize(bi);
        }

        public void serialize(BinaryOutput bo)
        {
            bo.writeInt16(x);
            bo.writeInt16(y);
            bo.writeInt16(z);
        }

        public void deserialize(BinaryInput bi)
        {
            x = bi.readInt16();
            y = bi.readInt16();
            z = bi.readInt16();
        }
    }
}
