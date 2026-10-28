/**
 @file LineSegment.h
   
 @maintainer Morgan McGuire, matrix@graphics3d.com
 
 @created 2003-02-08
 @edited  2006-02-22
*/

using System;

namespace G3D
{
    /**
     An finite segment of an infinite 3D line.
     */
    public struct LineSegment
    {
        internal Vector3 _point;

        /** Not normalized */
        internal Vector3 direction;

        internal LineSegment(Vector3 __point, Vector3 _direction)
        {
            _point = __point;
            direction = _direction;
        }

        public LineSegment(BinaryInput b)
        {
            this = default(LineSegment);
            deserialize(b);
        }

        public void serialize(BinaryOutput b)
        {
            _point.serialize(b);
            direction.serialize(b);
        }

        public void deserialize(BinaryInput b)
        {
            _point.deserialize(b);
            direction.deserialize(b);
        }

        /**
         * Constructs a line from two (not equal) points.
         */
        public static LineSegment fromTwoPoints(Vector3 point1, Vector3 point2)
        {
            return new LineSegment(point1, point2 - point1);
        }

        /** Returns end point 0 or 1.
            @deprecated Use point. */
        public Vector3 endPoint(int i)
        {
            return point(i);
        }

        public Vector3 point(int i)
        {
            switch (i)
            {
                case 0:
                    return _point;

                case 1:
                    return _point + direction;

                default:
                    Debug.debugAssertM(i == 0 || i == 1, "Argument to point must be 0 or 1");
                    return _point;
            }
        }

        public float length()
        {
            return direction.magnitude();
        }

        /**
         * Returns the closest point on the line segment to point.
         */
        public Vector3 closestPoint(Vector3 p)
        {
            // The vector from the end of the capsule to the point in question.
            Vector3 v = p - _point;

            // Projection of v onto the line segment scaled by 
            // the length of direction.
            float t = direction.dot(v);

            // Avoid some square roots.  Derivation:
            //    t/direction.length() <= direction.length()
            //      t <= direction.squaredLength()

            if ((t >= 0) && (t <= direction.squaredMagnitude()))
            {
                // The point falls within the segment.  Normalize direction,
                // divide t by the length of direction.
                return _point + direction * t / direction.squaredMagnitude();
            }
            else
            {
                // The point does not fall within the segment; see which end is closer.

                // Distance from 0, squared
                float d0Squared = v.squaredMagnitude();

                // Distance from 1, squared
                float d1Squared = (v - direction).squaredMagnitude();

                if (d0Squared < d1Squared)
                {
                    // Point 0 is closer
                    return _point;
                }
                else
                {
                    // Point 1 is closer
                    return _point + direction;
                }
            }
        }

        /**
         Returns the distance between point and the line
         */
        public double distance(Vector3 p)
        {
            return (closestPoint(p) - p).magnitude();
        }

        public double distanceSquared(Vector3 p)
        {
            return (closestPoint(p) - p).squaredMagnitude();
        }

        /** Returns true if some part of this segment is inside the sphere */
        public bool intersectsSolidSphere(Sphere s)
        {
            return distanceSquared(s.center) <= g3dmath.square(s.radius);
        }

        public Vector3 randomPoint()
        {
            return _point + (float)g3dmath.random(0, 1) * direction;
        }
    }

    public struct LineSegment2D
    {
        private Vector2 m_origin;

        /** Not normalized */
        private Vector2 m_direction;

        /** Length of m_direction */
        private float m_length;

        public static LineSegment2D fromTwoPoints(Vector2 p0, Vector2 p1)
        {
            LineSegment2D s = new LineSegment2D();
            s.m_origin = p0;
            s.m_direction = p1 - p0;
            s.m_length = s.m_direction.length();
            return s;
        }

        public Vector2 point(int i)
        {
            Debug.debugAssert(i == 0 || i == 1);
            if (i == 0)
            {
                return m_origin;
            }
            else
            {
                return m_direction + m_origin;
            }
        }

        public Vector2 closestPoint(Vector2 Q)
        {
            // Two constants that appear in the result
            Vector2 k1 = m_origin - Q;
            Vector2 k2 = m_direction;

            if (g3dmath.fuzzyEq(m_length, 0))
            {
                // This line segment has no length
                return m_origin;
            }

            // Time [0, 1] at which we hit the closest point travelling from p0 to p1.
            // Derivation can be obtained by minimizing the expression
            //     ||P0 + (P1 - P0)t - Q||.
            float t = -k1.dot(k2) / (m_length * m_length);

            if (t < 0)
            {
                // Clipped to low end point
                return m_origin;
            }
            else if (t > 1)
            {
                // Clipped to high end point
                return m_origin + m_direction;
            }
            else
            {
                // Subsitute into the line equation to find 
                // the point on the segment.
                return m_origin + k2 * t;
            }
        }

        public float distance(Vector2 p)
        {
            Vector2 closest = closestPoint(p);
            return (closest - p).length();
        }

        public float length()
        {
            return m_length;
        }
    }
}
