/**
  @file AABox.h
 
  Axis-aligned box class
 
  @maintainer Morgan McGuire, matrix@graphics3d.com
 
  @created 2004-01-10
  @edited  2006-02-10
 
  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    /**
     An axis-aligned box.
     */
    public struct AABox
    {

        /** Optional argument placeholder */
        static int dummy;
        private Vector3 lo;
        private Vector3 hi;

        /**
         Constructs a zero-area AABox at v.
         */
        public AABox(Vector3 v)
        {
            lo = v;
            hi = v;
        }

        /**
         Assumes that low is less than or equal to high along each dimension.
         To have this automatically enforced, use
         <code>AABox(low.min(high), low.max(high));</code>
        */
        public AABox(Vector3 low, Vector3 high)
        {
            this = default(AABox);
            set(low, high);
        }

        /**
         Assumes that low is less than or equal to high along each dimension.
        */
        public void set(Vector3 low, Vector3 high)
        {
            Debug.debugAssert(
                (low.x <= high.x) &&
                (low.y <= high.y) &&
                (low.z <= high.z));
            this.lo = low;
            this.hi = high;
        }

        public void serialize(BinaryOutput b)
        {
            b.writeVector3(lo);
            b.writeVector3(hi);
        }

        public void deserialize(BinaryInput b)
        {
            lo = b.readVector3();
            hi = b.readVector3();
        }

        public Vector3 low()
        {
            return lo;
        }

        public Vector3 high()
        {
            return hi;
        }

        /**
         The largest possible finite box.
         */
        public static readonly AABox MAX_FINITE = new AABox(Vector3.minFinite(), Vector3.maxFinite());

        public static readonly AABox INF = new AABox(-Vector3.inf(), Vector3.inf());

        public static readonly AABox ZERO = new AABox(Vector3.zero(), Vector3.zero());

        /**
          Returns the centroid of the box.
         */
        public Vector3 center()
        {
            return (lo + hi) * 0.5f;
        }

        /**
         Distance from corner(0) to the next corner along axis a.
         */
        public double extent(int a)
        {
            Debug.debugAssert(a < 3);
            return hi[a] - lo[a];
        }

        public Vector3 extent()
        {
            return hi - lo;
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
            uint inMask = testMask;
            Debug.debugAssert(numPlanes < 31);

            childMask = 0;

            bool finite =
                (Math.Abs(lo.x) < g3dmath.inf()) &&
                (Math.Abs(hi.x) < g3dmath.inf()) &&
                (Math.Abs(lo.y) < g3dmath.inf()) &&
                (Math.Abs(hi.y) < g3dmath.inf()) &&
                (Math.Abs(lo.z) < g3dmath.inf()) &&
                (Math.Abs(hi.z) < g3dmath.inf());

            // See if there is one plane for which all of the
            // vertices are in the negative half space.
            for (int p = 0; p < numPlanes; p++)
            {
                // Only test planes that are not masked
                if ((inMask & 1) != 0)
                {
                    Vector3 corner;

                    int numContained = 0;
                    int v = 0;

                    // We can early-out only if we have found one point on each
                    // side of the plane (i.e. if we are straddling).  That
                    // occurs when (numContained < v) && (numContained > 0)
                    for (v = 0; (v < 8) && ((numContained == v) || (numContained == 0)); ++v)
                    {
                        // Unrolling these 3 if's into a switch decreases performance
                        // by about 2x
                        corner.x = (v & 1) != 0 ? hi.x : lo.x;
                        corner.y = (v & 2) != 0 ? hi.y : lo.y;
                        corner.z = (v & 4) != 0 ? hi.z : lo.z;

                        if (finite)
                        { // this branch is highly predictable
                            if (plane[p].halfSpaceContainsFinite(corner))
                            {
                                ++numContained;
                            }
                        }
                        else
                        {
                            if (plane[p].halfSpaceContains(corner))
                            {
                                ++numContained;
                            }
                        }
                    }

                    if (numContained == 0)
                    {
                        // Plane p culled the box
                        cullingPlaneIndex = p;

                        // The caller should not recurse into the children,
                        // since the parent is culled.  If they do recurse,
                        // make them only test against this one plane, which
                        // will immediately cull the volume.
                        childMask = (uint)(1 << p);
                        return true;

                    }
                    else if (numContained < v)
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

            bool finite =
                (Math.Abs(lo.x) < g3dmath.inf()) &&
                (Math.Abs(hi.x) < g3dmath.inf()) &&
                (Math.Abs(lo.y) < g3dmath.inf()) &&
                (Math.Abs(hi.y) < g3dmath.inf()) &&
                (Math.Abs(lo.z) < g3dmath.inf()) &&
                (Math.Abs(hi.z) < g3dmath.inf());

            // See if there is one plane for which all of the
            // vertices are in the negative half space.
            for (int p = 0; p < numPlanes; p++)
            {
                // Only test planes that are not masked
                if ((inMask & 1) != 0)
                {
                    bool culled = true;
                    Vector3 corner;

                    int v;

                    // Assume this plane culls all points.  See if there is a point
                    // not culled by the plane... early out when at least one point
                    // is in the positive half space.
                    for (v = 0; (v < 8) && culled; ++v)
                    {
                        // Unrolling these 3 if's into a switch decreases performance
                        // by about 2x
                        corner.x = (v & 1) != 0 ? hi.x : lo.x;
                        corner.y = (v & 2) != 0 ? hi.y : lo.y;
                        corner.z = (v & 4) != 0 ? hi.z : lo.z;

                        if (finite)
                        { // this branch is highly predictable
                            culled = !plane[p].halfSpaceContainsFinite(corner);
                        }
                        else
                        {
                            culled = !plane[p].halfSpaceContains(corner);
                        }
                    }

                    if (culled)
                    {
                        // Plane p culled the box
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
         Splits the box into two AABoxes along the specified axis.  low contains
         the part that was closer to negative infinity along axis, high contains
         the other part.  Either may have zero volume.
         */
        public void split(Vector3.Axis axis, float location, out AABox low, out AABox high)
        {
            // Low, medium, and high along the chosen axis
            float L = g3dmath.min(location, lo[(int)axis]);
            float M = g3dmath.min(g3dmath.max(location, lo[(int)axis]), hi[(int)axis]);
            float H = g3dmath.max(location, hi[(int)axis]);

            // Copy over this box.
            high = low = this;

            // Now move the split points along the special axis
            Vector3 lowLo = low.lo;
            Vector3 lowHi = low.hi;
            Vector3 highLo = high.lo;
            Vector3 highHi = high.hi;

            lowLo[(int)axis] = L;
            lowHi[(int)axis] = M;
            highLo[(int)axis] = M;
            highHi[(int)axis] = H;

            low = new AABox(lowLo, lowHi);
            high = new AABox(highLo, highHi);
        }

        /**
         Conservative culling test for up to 32 planes.
         Returns true if there exists a <CODE>plane[p]</CODE> for
         which the entire object is in the negative half space
         (opposite the plane normal).
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
            int cullingPlaneIndex = 0,
            uint testMask = uint.MaxValue)
        {
            return culledBy(plane.ToArray(), plane.size(), ref cullingPlaneIndex, testMask);
        }

        public bool contains(Vector3 point)
        {
            return
                (point.x >= lo.x) &&
                (point.y >= lo.y) &&
                (point.z >= lo.z) &&
                (point.x <= hi.x) &&
                (point.y <= hi.y) &&
                (point.z <= hi.z);
        }

        /** @deprecated */
        public float surfaceArea()
        {
            Vector3 diag = hi - lo;
            return 2.0f * (diag.x * diag.y + diag.y * diag.z + diag.x * diag.z);
        }

        public float area()
        {
            return surfaceArea();
        }

        public float volume()
        {
            Vector3 diag = hi - lo;
            return diag.x * diag.y * diag.z;
        }

        public Vector3 randomInteriorPoint()
        {
            return new Vector3(
                (float)g3dmath.random(lo.x, hi.x),
                (float)g3dmath.random(lo.y, hi.y),
                (float)g3dmath.random(lo.z, hi.z));
        }

        public Vector3 randomSurfacePoint()
        {
            Vector3 extent = hi - lo;
            float aXY = extent.x * extent.y;
            float aYZ = extent.y * extent.z;
            float aZX = extent.z * extent.x;

            float r = (float)g3dmath.random(0, aXY + aYZ + aZX);

            // Choose evenly between positive and negative face planes
            float d = ((float)g3dmath.random(0, 1) < 0.5f) ? 0.0f : 1.0f;

            // The probability of choosing a given face is proportional to
            // its area.
            if (r < aXY)
            {
                return
                    lo +
                    new Vector3(
                        (float)g3dmath.random(0, extent.x),
                        (float)g3dmath.random(0, extent.y),
                        d * extent.z);
            }
            else if (r < aYZ)
            {
                return
                    lo +
                    new Vector3(
                        d * extent.x,
                        (float)g3dmath.random(0, extent.y),
                        (float)g3dmath.random(0, extent.z));
            }
            else
            {
                return
                    lo +
                    new Vector3(
                        (float)g3dmath.random(0, extent.x),
                        d * extent.y,
                        (float)g3dmath.random(0, extent.z));
            }
        }

        /** @deprecated use Box constructor */
        public Box toBox()
        {
            return new Box(lo, hi);
        }

        /** Returns true if there is any overlap */
        public bool intersects(AABox other)
        {
            // Must be overlap along all three axes.
            // Try to find a separating axis.

            for (int a = 0; a < 3; ++a)
            {

                //     |--------|
                // |------|

                if ((lo[a] > other.hi[a]) ||
                    (hi[a] < other.lo[a]))
                {
                    return false;
                }
            }

            return true;
        }

        /**
         Returns true if there is any overlap.
            @cite Jim Arvo's algorithm from Graphics Gems II
        */
        public bool intersects(Sphere other)
        {
            double d = 0;

            //find the square of the distance
            //from the sphere to the box
            for (int i = 0; i < 3; ++i)
            {
                if (other.center[i] < lo[i])
                {
                    d += g3dmath.square(other.center[i] - lo[i]);
                }
                else if (other.center[i] > hi[i])
                {
                    d += g3dmath.square(other.center[i] - hi[i]);
                }
            }

            return d <= g3dmath.square(other.radius);
        }

        /** Return the intersection of the two boxes */
        public AABox intersect(AABox other)
        {
            Vector3 H = hi.min(other.hi);
            Vector3 L = lo.max(other.lo).min(H);
            return new AABox(L, H);
        }

        public uint hashCode()
        {
            return Vector3Operators.hashCode(lo) + Vector3Operators.hashCode(hi);
        }

        public static bool operator ==(AABox left, AABox right)
        {
            return (left.lo == right.lo) && (left.hi == right.hi);
        }

        public static bool operator !=(AABox left, AABox right)
        {
            return !((left.lo == right.lo) && (left.hi == right.hi));
        }

        public override bool Equals(object obj)
        {
            return obj is AABox && this == (AABox)obj;
        }

        public override int GetHashCode()
        {
            return (int)hashCode();
        }

        public void getBounds(out AABox result)
        {
            result = this;
        }
    }

    public static class AABoxOperators
    {
        /**
         Hashing function for use with Table.
         */
        public static uint hashCode(AABox b)
        {
            return b.hashCode();
        }
    }
}
