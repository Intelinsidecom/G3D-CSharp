/**
 @file Plane.h

 Plane class

 @maintainer Morgan McGuire, matrix@graphics3d.com

 @created 2001-06-02
 @edited  2004-07-18
*/

using System;

namespace G3D
{
    /**
     An infinite 2D plane in 3D space.
     */
    public struct Plane
    {
        /** normal.Dot(x,y,z) = distance */
        private Vector3 _normal;
        private float _distance;

        /**
         Assumes the normal has unit length.
         */
        private Plane(Vector3 n, float d)
        {
            _normal = n;
            _distance = d;
        }

        /**
         C# 4 forbids a parameterless constructor on a struct, so the C++
         default Plane (unit Y normal, zero distance) cannot be reproduced by
         default(Plane), which yields a zero normal.  Use Plane.fromEquation,
         the point/normal constructors, or Plane.up() to build a usable plane.
         */
        public static Plane up()
        {
            return new Plane(Vector3.unitY(), 0.0f);
        }

        /**
         Constructs a plane from three points.
         */
        public Plane(Vector3 point0, Vector3 point1, Vector3 point2)
        {
            _normal = (point1 - point0).cross(point2 - point0).direction();
            _distance = _normal.dot(point0);
        }

        /**
         Constructs a plane from three points, where at most two are
         at infinity (w = 0, not xyz = inf).
         */
        public Plane(Vector4 point0, Vector4 point1, Vector4 point2)
        {
            Debug.debugAssertM(
                point0.w != 0 ||
                point1.w != 0 ||
                point2.w != 0,
                "At least one point must be finite.");

            // Rotate the points around so that the finite points come first.

            while ((point0.w == 0) &&
                   ((point1.w == 0) || (point2.w != 0)))
            {
                Vector4 temp = point0;
                point0 = point1;
                point1 = point2;
                point2 = temp;
            }

            Vector3 dir1;
            Vector3 dir2;

            if (point1.w == 0)
            {
                // 1 finite, 2 infinite points; the plane must contain
                // the direction of the two direcitons
                dir1 = point1.xyz();
                dir2 = point2.xyz();
            }
            else if (point2.w != 0)
            {
                // 3 finite points, the plane must contain the directions
                // betwseen the points.
                dir1 = point1.xyz() - point0.xyz();
                dir2 = point2.xyz() - point0.xyz();
            }
            else
            {
                // 2 finite, 1 infinite point; the plane must contain
                // the direction between the first two points and the
                // direction of the third point.
                dir1 = point1.xyz() - point0.xyz();
                dir2 = point2.xyz();
            }

            _normal = dir1.cross(dir2).direction();
            _distance = _normal.dot(point0.xyz());
        }

        /**
         The normal will be unitized.
         */
        public Plane(Vector3 __normal, Vector3 point)
        {
            _normal = __normal.direction();
            _distance = _normal.dot(point);
        }

        public static Plane fromEquation(float a, float b, float c, float d)
        {
            Vector3 n = new Vector3(a, b, c);
            float magnitude = n.magnitude();
            d /= magnitude;
            n = n / magnitude;
            return new Plane(n, -d);
        }

        public Plane(BinaryInput b)
        {
            _normal = Vector3.zero();
            _distance = 0;
            deserialize(b);
        }

        public void serialize(BinaryOutput b)
        {
            _normal.serialize(b);
            b.writeFloat64(_distance);
        }

        public void deserialize(BinaryInput b)
        {
            _normal.deserialize(b);
            _distance = (float)b.readFloat64();
        }

        /**
         Returns true if point is on the side the normal points to or 
         is in the plane.
         */
        public bool halfSpaceContains(Vector3 point)
        {
            // Clamp to a finite range for testing
            point = point.clamp(Vector3.minFinite(), Vector3.maxFinite());

            // We can get away with putting values *at* the limits of the float32 range into
            // a dot product, since the dot product is carried out on float64.
            return _normal.dot(point) >= _distance;
        }

        /**
         Returns true if point is on the side the normal points to or 
         is in the plane.
         */
        public bool halfSpaceContains(Vector4 point)
        {
            if (point.w == 0)
            {
                return _normal.dot(point.xyz()) > 0;
            }
            else
            {
                return halfSpaceContains(point.xyz() / point.w);
            }
        }

        /**
         Returns true if point is on the side the normal points to or 
         is in the plane.  Only call on finite points.  Faster than halfSpaceContains.
         */
        public bool halfSpaceContainsFinite(Vector3 point)
        {
            Debug.debugAssert(point.isFinite());
            return _normal.dot(point) >= _distance;
        }

        /**
         Returns true if the point is nearly in the plane.
         */
        public bool fuzzyContains(Vector3 point)
        {
            return g3dmath.fuzzyEq(point.dot(_normal), _distance);
        }

        public Vector3 normal()
        {
            return _normal;
        }

        /**
          Returns distance from point to plane. Distance is negative if point is behind (not in plane in direction opposite normal) the plane.
        */
        public float distance(Vector3 x)
        {
            return (_normal.dot(x) - _distance);
        }

        public Vector3 closestPoint(Vector3 x)
        {
            return x + (_normal * (-distance(x)));
        }

        /** Returns normal * distance from origin */
        public Vector3 center()
        {
            return _normal * _distance;
        }

        /**
         Inverts the facing direction of the plane so the new normal
         is the inverse of the old normal.
         */
        public void flip()
        {
            _normal = -_normal;
            _distance = -_distance;
        }

        /**
          Returns the equation in the form:

          <CODE>normal.Dot(Vector3(<I>x</I>, <I>y</I>, <I>z</I>)) + d = 0</CODE>
         */
        public void getEquation(out Vector3 normal, out double d)
        {
            normal = _normal;
            d = -_distance;
        }

        public void getEquation(out Vector3 normal, out float d)
        {
            double _d;
            getEquation(out normal, out _d);
            d = (float)_d;
        }

        /**
          ax + by + cz + d = 0
         */
        public void getEquation(out double a, out double b, out double c, out double d)
        {
            a = _normal.x;
            b = _normal.y;
            c = _normal.z;
            d = -_distance;
        }

        public void getEquation(out float a, out float b, out float c, out float d)
        {
            double _a, _b, _c, _d;
            getEquation(out _a, out _b, out _c, out _d);
            a = (float)_a;
            b = (float)_b;
            c = (float)_c;
            d = (float)_d;
        }

        public override string ToString()
        {
            return Format.format("Plane(%g, %g, %g, %g)", _normal.x, _normal.y, _normal.z, _distance);
        }
    }
}
