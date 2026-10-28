/**
  @file PhysicsFrame.h

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2002-07-08
  @edited  2006-01-10
*/

namespace G3D
{
    /**
      An RT transformation using a quaternion; suitable for
      physics integration.

      This interface is in "Beta" and will change in the next release.
     */
    public struct PhysicsFrame
    {
        public Quat rotation;

        /**
         Takes object space points to world space.
         */
        public Vector3 translation;

        /**
         Initializes to the identity frame.
         */
        public static PhysicsFrame identity()
        {
            return new PhysicsFrame(Quat.identity(), Vector3.zero());
        }

        private PhysicsFrame(Quat rotation, Vector3 translation)
        {
            this.rotation = rotation;
            this.translation = translation;
        }

        /**
         Purely translational force
         */
        public PhysicsFrame(Vector3 translation)
        {
            rotation = Quat.identity();
            this.translation = translation;
        }

        public PhysicsFrame(CoordinateFrame coordinateFrame)
        {
            translation = coordinateFrame.translation;
            rotation = new Quat(coordinateFrame.rotation);
        }

        /** Compose: create the transformation that is <I>other</I> followed by <I>this</I>.*/
        public static PhysicsFrame operator *(PhysicsFrame left, PhysicsFrame other)
        {
            PhysicsFrame result = identity();
            result.rotation = left.rotation * other.rotation;
            result.translation = left.translation + left.rotation.toRotationMatrix() * other.translation;
            return result;
        }

        public CoordinateFrame toCoordinateFrame()
        {
            CoordinateFrame f = CoordinateFrame.identity();
            f.translation = translation;
            f.rotation = rotation.toRotationMatrix();
            return f;
        }

        /**
         Linear interpolation (spherical linear for the rotations).
         */
        public PhysicsFrame lerp(PhysicsFrame other, float alpha)
        {
            PhysicsFrame result = identity();
            result.translation = translation.lerp(other.translation, alpha);
            result.rotation = rotation.slerp(other.rotation, alpha);
            return result;
        }

        /**
         this + t * dx
         @deprecated
         */
        public PhysicsFrame integrate(float t, PhysicsFrame dx)
        {
            PhysicsFrame result = identity();
            result.translation = translation + t * dx.translation;
            result.rotation = rotation * dx.rotation.pow(t);
            return result;
        }

        /**
         this + t * dx + t*t * ddx
         @deprecated
         */
        public PhysicsFrame integrate(float t, PhysicsFrame dx, PhysicsFrame ddx)
        {
            PhysicsFrame result = identity();

            // TODO: is this correct?
            result.translation = translation + t * dx.translation + t * t * ddx.translation;
            result.rotation = rotation * dx.rotation.pow(t) * ddx.rotation.pow(t * t);

            return result;
        }

        public void deserialize(BinaryInput b)
        {
            translation.deserialize(b);
            rotation.deserialize(b);
        }

        public void serialize(BinaryOutput b)
        {
            translation.serialize(b);
            rotation.serialize(b);
        }
    }
}
