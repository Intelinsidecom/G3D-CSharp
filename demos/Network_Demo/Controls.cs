/**
  @file Network_Demo/Controls.cs

  Port of the Controls class in Network_Demo/Entity.h and Entity.cpp.

  Entity controls.
 */

using System;
using G3D;

namespace Network_Demo
{
    public class Controls
    {
        /** Desired change in facing velocity in world frame. */
        public double desiredYawVelocity;

        /** Desired change in position rate in world frame. */
        public Vector3 desiredVelocity;

        /** Initializes all values to zero */
        public Controls()
        {
            desiredYawVelocity = 0;
            desiredVelocity = Vector3.zero();
        }

        public static bool operator ==(Controls other, Controls self)
        {
            if (ReferenceEquals(other, self))
            {
                return true;
            }
            if (ReferenceEquals(other, null) || ReferenceEquals(self, null))
            {
                return false;
            }
            return
                (other.desiredYawVelocity == self.desiredYawVelocity) &&
                (other.desiredVelocity == self.desiredVelocity);
        }

        public static bool operator !=(Controls other, Controls self)
        {
            return !(other == self);
        }

        public override bool Equals(object obj)
        {
            return obj is Controls && (Controls)obj == this;
        }

        public override int GetHashCode()
        {
            return desiredVelocity.GetHashCode() ^ desiredYawVelocity.GetHashCode();
        }

        public void serialize(BinaryOutput b)
        {
            b.writeFloat32((float)desiredYawVelocity);
            desiredVelocity.serialize(b);
        }

        public void deserialize(BinaryInput b)
        {
            desiredYawVelocity = b.readFloat32();
            desiredVelocity.deserialize(b);
        }
    }
}
