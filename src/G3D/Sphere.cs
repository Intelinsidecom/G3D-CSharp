/**
 @file Sphere.h
 
 Sphere class
 
 @maintainer Morgan McGuire, matrix@graphics3d.com
 
 @created 2001-06-02
 @edited  2004-07-05
*/

using System;

namespace G3D
{
    /**
     Sphere.
     */
    public struct Sphere
    {
        public Vector3 center;
        public float radius;

        public Sphere(BinaryInput b)
        {
            center = Vector3.zero();
            radius = 0;
            deserialize(b);
        }

        public Sphere(Vector3 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public void serialize(BinaryOutput b)
        {
            center.serialize(b);
            b.writeFloat64(radius);
        }

        public void deserialize(BinaryInput b)
        {
            center.deserialize(b);
            radius = (float)b.readFloat64();
        }

        public static bool operator ==(Sphere left, Sphere right)
        {
            return (left.center == right.center) && (left.radius == right.radius);
        }

        public static bool operator !=(Sphere left, Sphere right)
        {
            return !((left.center == right.center) && (left.radius == right.radius));
        }

        public override bool Equals(object obj)
        {
            return obj is Sphere && this == (Sphere)obj;
        }

        public override int GetHashCode()
        {
            return center.GetHashCode() ^ (int)(radius * 13);
        }

        /**
         Returns true if point is less than or equal to radius away from
         the center.
         */
        public bool contains(Vector3 point)
        {
            double distance = (center - point).squaredMagnitude();
            return distance <= (radius * radius);
        }

        /**
	 @deprecated Use culledBy(Array<Plane>&)
         */
        public bool culledBy(
            Plane[] plane,
            int numPlanes,
            ref int cullingPlaneIndex,
            uint testMask,
            out uint childMask)
        {
            childMask = 0;

            if (radius == float.PositiveInfinity)
            {
                // No plane can cull the infinite box
                return false;
            }

            uint inMask = testMask;
            Debug.debugAssert(numPlanes < 31);

            // See if there is one plane for which all of the
            // vertices are in the negative half space.
            for (int p = 0; p < numPlanes; p++)
            {
                // Only test planes that are not masked
                if ((inMask & 1) != 0)
                {
                    bool culledLow = !plane[p].halfSpaceContainsFinite(center + plane[p].normal() * radius);
                    bool culledHigh = !plane[p].halfSpaceContainsFinite(center - plane[p].normal() * radius);

                    if (culledLow)
                    {
                        // Plane p culled the sphere
                        cullingPlaneIndex = p;

                        // The caller should not recurse into the children,
                        // since the parent is culled.  If they do recurse,
                        // make them only test against this one plane, which
                        // will immediately cull the volume.
                        childMask = (uint)(1 << p);
                        return true;

                    }
                    else if (culledHigh)
                    {
                        // The bounding volume straddled the plane; we have
                        // to keep testing against this plane
                        childMask |= (uint)(1 << p);
                    }
                }

                // Move on to the next bit.
                inMask = inMask >> 1;
            }

            // None of the planes could cull this box
            cullingPlaneIndex = -1;
            return false;
        }

        /**
	 @deprecated Use culledBy(Array<Plane>&)
         */
        public bool culledBy(
            Plane[] plane,
            int numPlanes,
            ref int cullingPlaneIndex,
            uint testMask)
        {
            uint inMask = testMask;
            Debug.debugAssert(numPlanes < 31);

            // See if there is one plane for which all of the
            // vertices are in the negative half space.
            for (int p = 0; p < numPlanes; p++)
            {
                // Only test planes that are not masked
                if ((inMask & 1) != 0)
                {
                    bool culled = !plane[p].halfSpaceContains(center + plane[p].normal() * radius);
                    if (culled)
                    {
                        // Plane p culled the sphere
                        cullingPlaneIndex = p;
                        return true;
                    }
                }

                // Move on to the next bit.
                inMask = inMask >> 1;
            }

            // None of the planes could cull this box
            cullingPlaneIndex = -1;
            return false;
        }

        /**
          See AABox::culledBy
         */
        public bool culledBy(
            Array<Plane> plane,
            ref int cullingPlaneIndex,
            uint testMask,
            out uint childMask)
        {
            return culledBy(plane.ToArray(), plane.size(), ref cullingPlaneIndex, testMask, out childMask);
        }

        /**
         Conservative culling test that does not produce a mask for children.
         */
        public bool culledBy(
            Array<Plane> plane,
            ref int cullingPlaneIndex,
            uint testMask)
        {
            return culledBy(plane.ToArray(), plane.size(), ref cullingPlaneIndex, testMask);
        }

        public override string ToString()
        {
            return Format.format("Sphere(<%g, %g, %g>, %g)",
                center.x, center.y, center.z, radius);
        }

        public float volume()
        {
            // The C++ computes powf((float)radius, 3.0f) in single precision.
            return (float)g3dmath.G3D_PI * (4.0f / 3.0f) * (float)Math.Pow((double)radius, 3.0);
        }

        /** @deprecated */
        public float surfaceArea()
        {
            return (float)g3dmath.G3D_PI * 4.0f * (float)Math.Pow((double)radius, 2.0);
        }

        public float area()
        {
            return surfaceArea();
        }

        /**
         Uniformly distributed on the surface.
         */
        public Vector3 randomSurfacePoint()
        {
            return Vector3.random() * radius + center;
        }

        /**
         Uniformly distributed on the interior (includes surface)
         */
        public Vector3 randomInteriorPoint()
        {
            Vector3 result;
            do
            {
                result = new Vector3((float)g3dmath.symmetricRandom(),
                                     (float)g3dmath.symmetricRandom(),
                                     (float)g3dmath.symmetricRandom());
            } while (result.squaredMagnitude() >= 1.0f);

            return result * radius + center;
        }

        public void getBounds(out AABox result)
        {
            Vector3 extent = new Vector3(radius, radius, radius);
            result = new AABox(center - extent, center + extent);
        }
    }

    public static class SphereOperators
    {
        public static uint hashCode(Sphere sphere)
        {
            return (uint)(Vector3Operators.hashCode(sphere.center) + (sphere.radius * 13));
        }
    }
}
