/**
 @file Line.h
 
 Line class
 
 @maintainer Morgan McGuire, matrix@graphics3d.com
 
 @created 2001-06-02
 @edited  2006-02-28
*/

using System;

namespace G3D
{
    /**
     An infinite 3D line.
     */
    public struct Line
    {
        internal Vector3 _point;
        internal Vector3 _direction;

        internal Line(Vector3 point, Vector3 direction)
        {
            _point = point;
            _direction = direction.direction();
        }

        public Line(BinaryInput b)
        {
            this = default(Line);
            deserialize(b);
        }

        public void serialize(BinaryOutput b)
        {
            _point.serialize(b);
            _direction.serialize(b);
        }

        public void deserialize(BinaryInput b)
        {
            _point.deserialize(b);
            _direction.deserialize(b);
        }

        /**
          Constructs a line from two (not equal) points.
         */
        public static Line fromTwoPoints(Vector3 point1, Vector3 point2)
        {
            return new Line(point1, point2 - point1);
        }

        /**
          Creates a line from a point and a (nonzero) direction.
         */
        public static Line fromPointAndDirection(Vector3 point, Vector3 direction)
        {
            return new Line(point, direction);
        }

        /**
          Returns the closest point on the line to point.
         */
        public Vector3 closestPoint(Vector3 pt)
        {
            float t = _direction.dot(pt - _point);
            return _point + _direction * t;
        }

        /**
          Returns the distance between point and the line
         */
        public double distance(Vector3 point)
        {
            return (closestPoint(point) - point).magnitude();
        }

        /** Returns a point on the line */
        public Vector3 point()
        {
            return _point;
        }

        /** Returns the direction (or negative direction) of the line */
        public Vector3 direction()
        {
            return _direction;
        }

        /**
         Returns the point where the line and plane intersect.  If there
         is no intersection, returns a point at infinity.
         */
        public Vector3 intersection(Plane plane)
        {
            float d;
            Vector3 normal = plane.normal();
            plane.getEquation(out normal, out d);
            float rate = _direction.dot(normal);

            if (rate == 0)
            {
                return Vector3.inf();
            }
            else
            {
                float t = -(d + _point.dot(normal)) / rate;

                return _point + _direction * t;
            }
        }
    }
}
