/**
  @file Box.h
 
  Box class
 
  @maintainer Morgan McGuire, matrix@graphics3d.com
 
  @cite Portions based on Dave Eberly's Magic Software Library at <A HREF="http://www.magic-software.com">http://www.magic-software.com</A>
  @created 2001-06-02
  @edited  2006-01-05
 
  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    /**
     An arbitrary 3D box, useful as a bounding box.


       To construct a box from a coordinate frame, center and extent, use the idiom:

     <CODE>Box box = cframe.toObjectSpace(Box(center - extent/2, center + extent/2));</CODE>
     */
    public struct Box
    {
        /**
          <PRE>
           3    2       7    6

           0    1       4    5

           front    back (seen through front)
          </PRE>
         */
        private Vector3 _corner0;
        private Vector3 _corner1;
        private Vector3 _corner2;
        private Vector3 _corner3;
        private Vector3 _corner4;
        private Vector3 _corner5;
        private Vector3 _corner6;
        private Vector3 _corner7;

        /**
         Unit axes.
         */
        private Vector3 _axis0;
        private Vector3 _axis1;
        private Vector3 _axis2;

        private Vector3 _center;

        /**
         Extent along each axis.
         */
        private Vector3 _extent;

        private float _area;
        private float _volume;

        internal Vector3 cornerAt(int i)
        {
            switch (i)
            {
                case 0: return _corner0;
                case 1: return _corner1;
                case 2: return _corner2;
                case 3: return _corner3;
                case 4: return _corner4;
                case 5: return _corner5;
                case 6: return _corner6;
                case 7: return _corner7;
                default:
                    Debug.debugAssert(false);
                    return Vector3.zero();
            }
        }

        internal void setCornerAt(int i, Vector3 v)
        {
            switch (i)
            {
                case 0: _corner0 = v; break;
                case 1: _corner1 = v; break;
                case 2: _corner2 = v; break;
                case 3: _corner3 = v; break;
                case 4: _corner4 = v; break;
                case 5: _corner5 = v; break;
                case 6: _corner6 = v; break;
                case 7: _corner7 = v; break;
                default:
                    Debug.debugAssert(false);
                    break;
            }
        }

        internal Vector3 axisAt(int a)
        {
            switch (a)
            {
                case 0: return _axis0;
                case 1: return _axis1;
                case 2: return _axis2;
                default:
                    Debug.debugAssert(false);
                    return Vector3.zero();
            }
        }

        internal void setAxisAt(int a, Vector3 v)
        {
            switch (a)
            {
                case 0: _axis0 = v; break;
                case 1: _axis1 = v; break;
                case 2: _axis2 = v; break;
                default:
                    Debug.debugAssert(false);
                    break;
            }
        }

        internal void setCenter(Vector3 v)
        {
            _center = v;
        }

        /**
         Sets a field on four vertices.  Used by the constructor.
         */
        private static void setMany(Vector3[] corner, int i0, int i1, int i2, int i3, int axis, Vector3 extreme)
        {
            float v = (axis == 0) ? extreme.x : ((axis == 1) ? extreme.y : extreme.z);

            int[] index = new int[] { i0, i1, i2, i3 };
            for (int i = 0; i < 4; ++i)
            {
                Vector3 c = corner[index[i]];
                c[axis] = v;
                corner[index[i]] = c;
            }
        }

        private void init(Vector3 min, Vector3 max)
        {
            Vector3[] corner = new Vector3[8];

            setMany(corner, 0, 1, 2, 3, 2, max);
            setMany(corner, 4, 5, 6, 7, 2, min);

            setMany(corner, 1, 2, 5, 6, 0, max);
            setMany(corner, 0, 3, 4, 7, 0, min);

            setMany(corner, 3, 2, 6, 7, 1, max);
            setMany(corner, 0, 1, 5, 4, 1, min);

            _corner0 = corner[0];
            _corner1 = corner[1];
            _corner2 = corner[2];
            _corner3 = corner[3];
            _corner4 = corner[4];
            _corner5 = corner[5];
            _corner6 = corner[6];
            _corner7 = corner[7];

            _extent = max - min;

            _axis0 = Vector3.unitX();
            _axis1 = Vector3.unitY();
            _axis2 = Vector3.unitZ();

            _volume = _extent.x * _extent.y * _extent.z;
            _area = 2 *
                (_extent.x * _extent.y +
                 _extent.y * _extent.z +
                 _extent.z * _extent.x);

            _center = (max + min) / 2.0f;
        }

        /**
         Constructs a box from two opposite corners.
         */
        public Box(Vector3 min, Vector3 max)
        {
            _corner0 = Vector3.zero();
            _corner1 = Vector3.zero();
            _corner2 = Vector3.zero();
            _corner3 = Vector3.zero();
            _corner4 = Vector3.zero();
            _corner5 = Vector3.zero();
            _corner6 = Vector3.zero();
            _corner7 = Vector3.zero();
            _axis0 = Vector3.zero();
            _axis1 = Vector3.zero();
            _axis2 = Vector3.zero();
            _center = Vector3.zero();
            _extent = Vector3.zero();
            _area = 0;
            _volume = 0;

            init(min.min(max), min.max(max));
        }

        public Box(BinaryInput b)
        {
            _corner0 = Vector3.zero();
            _corner1 = Vector3.zero();
            _corner2 = Vector3.zero();
            _corner3 = Vector3.zero();
            _corner4 = Vector3.zero();
            _corner5 = Vector3.zero();
            _corner6 = Vector3.zero();
            _corner7 = Vector3.zero();
            _axis0 = Vector3.zero();
            _axis1 = Vector3.zero();
            _axis2 = Vector3.zero();
            _center = Vector3.zero();
            _extent = Vector3.zero();
            _area = 0;
            _volume = 0;
            deserialize(b);
        }

        public Box(AABox b)
        {
            _corner0 = Vector3.zero();
            _corner1 = Vector3.zero();
            _corner2 = Vector3.zero();
            _corner3 = Vector3.zero();
            _corner4 = Vector3.zero();
            _corner5 = Vector3.zero();
            _corner6 = Vector3.zero();
            _corner7 = Vector3.zero();
            _axis0 = Vector3.zero();
            _axis1 = Vector3.zero();
            _axis2 = Vector3.zero();
            _center = Vector3.zero();
            _extent = Vector3.zero();
            _area = 0;
            _volume = 0;

            init(b.low(), b.high());
        }

        public void serialize(BinaryOutput b)
        {
            for (int i = 0; i < 8; ++i)
            {
                cornerAt(i).serialize(b);
            }

            // Other state can be reconstructed
        }

        public void deserialize(BinaryInput b)
        {
            _center = Vector3.zero();
            for (int i = 0; i < 8; ++i)
            {
                Vector3 c = cornerAt(i);
                c.deserialize(b);
                setCornerAt(i, c);
                _center += c;
            }

            _center = _center / 8.0f;

            // Reconstruct other state from the corners
            Vector3 axis0 = _corner5 - _corner4;
            Vector3 axis1 = _corner7 - _corner4;
            Vector3 axis2 = _corner0 - _corner4;

            for (int i = 0; i < 3; ++i)
            {
                Vector3 a = (i == 0) ? axis0 : ((i == 1) ? axis1 : axis2);
                _extent[i] = a.magnitude();
                a = a / _extent[i];
                setAxisAt(i, a);
            }

            _volume = _extent.x * _extent.y * _extent.z;

            _area = 2 *
                (_extent.x * _extent.y +
                 _extent.y * _extent.z +
                 _extent.z * _extent.x);
        }

        /**
         Returns the object to world transformation for
         this box.  localFrame().worldToObject(...) takes
         objects into the space where the box axes are
         (1,0,0), (0,1,0), (0,0,1).  Note that there
         is no scaling in this transformation.
         */
        public CoordinateFrame localFrame()
        {
            CoordinateFrame result = CoordinateFrame.identity();
            getLocalFrame(ref result);
            return result;
        }

        public void getLocalFrame(ref CoordinateFrame frame)
        {
            frame.rotation = new Matrix3(
                _axis0[0], _axis1[0], _axis2[0],
                _axis0[1], _axis1[1], _axis2[1],
                _axis0[2], _axis1[2], _axis2[2]);

            frame.translation = _center;
        }

        /**
          Returns the centroid of the box.
         */
        public Vector3 center()
        {
            return _center;
        }

        public Vector3 getCenter()
        {
            return center();
        }

        /**
         Returns a corner (0 <= i < 8)
         @deprecated
         */
        public Vector3 getCorner(int i)
        {
            Debug.debugAssert(i < 8);
            return cornerAt(i);
        }

        public Vector3 corner(int i)
        {
            Debug.debugAssert(i < 8);
            return cornerAt(i);
        }

        /**
         Unit length.
         */
        public Vector3 axis(int a)
        {
            Debug.debugAssert(a < 3);
            return axisAt(a);
        }

        /**
         Distance from corner(0) to the next corner
         along the box's local axis a.
         */
        public float extent(int a)
        {
            Debug.debugAssert(a < 3);
            return (float)_extent[a];
        }

        public Vector3 extent()
        {
            return _extent;
        }

        /**
         Returns the four corners of a face (0 <= f < 6).
         The corners are returned to form a counter clockwise quad facing outwards.
         */
        public void getFaceCorners(int f, out Vector3 v0, out Vector3 v1, out Vector3 v2, out Vector3 v3)
        {
            switch (f)
            {
                case 0:
                    v0 = _corner0; v1 = _corner1; v2 = _corner2; v3 = _corner3;
                    break;

                case 1:
                    v0 = _corner1; v1 = _corner5; v2 = _corner6; v3 = _corner2;
                    break;

                case 2:
                    v0 = _corner7; v1 = _corner6; v2 = _corner5; v3 = _corner4;
                    break;

                case 3:
                    v0 = _corner2; v1 = _corner6; v2 = _corner7; v3 = _corner3;
                    break;

                case 4:
                    v0 = _corner3; v1 = _corner7; v2 = _corner4; v3 = _corner0;
                    break;

                case 5:
                    v0 = _corner1; v1 = _corner0; v2 = _corner4; v3 = _corner5;
                    break;

                default:
                    Debug.debugAssert((f >= 0) && (f < 6));
                    v0 = Vector3.zero();
                    v1 = Vector3.zero();
                    v2 = Vector3.zero();
                    v3 = Vector3.zero();
                    break;
            }
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
                        if (plane[p].halfSpaceContains(getCorner(v)))
                        {
                            ++numContained;
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

            // See if there is one plane for which all of the
            // vertices are in the negative half space.
            for (int p = 0; p < numPlanes; p++)
            {
                // Only test planes that are not masked
                if ((inMask & 1) != 0)
                {
                    bool culled = true;

                    int v;

                    // Assume this plane culls all points.  See if there is a point
                    // not culled by the plane... early out when at least one point
                    // is in the positive half space.
                    for (v = 0; (v < 8) && culled; ++v)
                    {
                        culled = !plane[p].halfSpaceContains(getCorner(v));
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

        public bool contains(Vector3 point)
        {
            // Form axes from three edges, transform the point into that
            // space, and perform 3 interval tests

            Vector3 u = _corner4 - _corner0;
            Vector3 v = _corner3 - _corner0;
            Vector3 w = _corner1 - _corner0;

            Matrix3 M = new Matrix3(u.x, v.x, w.x,
                                    u.y, v.y, w.y,
                                    u.z, v.z, w.z);

            // M^-1 * (point - _corner[0]) = point in unit cube's object space
            // compute the inverse of M
            Vector3 osPoint = M.inverse() * (point - _corner0);

            return
                (osPoint.x >= 0) &&
                (osPoint.y >= 0) &&
                (osPoint.z >= 0) &&
                (osPoint.x <= 1) &&
                (osPoint.y <= 1) &&
                (osPoint.z <= 1);
        }

        /** @deprecated */
        public float surfaceArea()
        {
            return _area;
        }

        public float area()
        {
            return surfaceArea();
        }

        public float volume()
        {
            return _volume;
        }

        public void getRandomSurfacePoint(out Vector3 P, out Vector3 N)
        {
            float aXY = _extent.x * _extent.y;
            float aYZ = _extent.y * _extent.z;
            float aZX = _extent.z * _extent.x;

            float r = (float)g3dmath.random(0, aXY + aYZ + aZX);

            // Choose evenly between positive and negative face planes
            float d = (g3dmath.random(0, 1) < 0.5f) ? -1.0f : 1.0f;

            // The probability of choosing a given face is proportional to
            // its area.
            if (r < aXY)
            {
                P = _axis0 * (float)g3dmath.random(-0.5, 0.5) * _extent.x +
                    _axis1 * (float)g3dmath.random(-0.5, 0.5) * _extent.y +
                    _center + _axis2 * d * _extent.z * 0.5f;
                N = _axis2 * d;
            }
            else if (r < aYZ)
            {
                P = _axis1 * (float)g3dmath.random(-0.5, 0.5) * _extent.y +
                    _axis2 * (float)g3dmath.random(-0.5, 0.5) * _extent.z +
                    _center + _axis0 * d * _extent.x * 0.5f;
                N = _axis0 * d;
            }
            else
            {
                P = _axis2 * (float)g3dmath.random(-0.5, 0.5) * _extent.z +
                    _axis0 * (float)g3dmath.random(-0.5, 0.5) * _extent.x +
                    _center + _axis1 * d * _extent.y * 0.5f;
                N = _axis1 * d;
            }
        }

        /**
          @deprecated
         Uniformly distributed on the surface.
         */
        public Vector3 randomSurfacePoint()
        {
            Vector3 V;
            Vector3 N;
            getRandomSurfacePoint(out V, out N);
            return V;
        }

        /**
         Uniformly distributed on the interior (includes surface)
         */
        public Vector3 randomInteriorPoint()
        {
            Vector3 sum = _center;

            for (int a = 0; a < 3; ++a)
            {
                sum += axisAt(a) * (float)g3dmath.random(-0.5, 0.5) * _extent[a];
            }

            return sum;
        }

        public void getBounds(out AABox aabb)
        {
            Vector3 lo = _corner0;
            Vector3 hi = lo;

            for (int v = 1; v < 8; ++v)
            {
                Vector3 C = cornerAt(v);
                lo = lo.min(C);
                hi = hi.max(C);
            }

            aabb = new AABox(lo, hi);
        }
    }
}
