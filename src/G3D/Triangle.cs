/**
 @file Triangle.h
  
 @maintainer Morgan McGuire, matrix@graphics3d.com
 
 @created 2003-04-05
 @edited  2004-03-14

 @cite Random point method by  Greg Turk, Generating random points in triangles.  In A. S. Glassner, ed., Graphics Gems, pp. 24-28. Academic Press, 1990

 Copyright 2000-2006, Morgan McGuire.
 All rights reserved.
*/

using System;

namespace G3D
{
    /**
      A generic triangle representation.  This should not be used
      as the underlying triangle for creating models; it is intended
      for providing fast property queries but requires a lot of
      storage and is mostly immutable.
      */
    public struct Triangle
    {
        private Vector3 _vertex0;
        private Vector3 _vertex1;
        private Vector3 _vertex2;

        /** edgeDirection[i] is the normalized vector v[i+1] - v[i] */
        private Vector3 edgeDirection0;
        private Vector3 edgeDirection1;
        private Vector3 edgeDirection2;

        private double edgeMagnitude0;
        private double edgeMagnitude1;
        private double edgeMagnitude2;

        private Plane _plane;
        private Vector3.Axis _primaryAxis;

        /** vertex[1] - vertex[0] */
        internal Vector3 edge01;
        /** vertex[2] - vertex[0] */
        internal Vector3 edge02;

        private float _area;

        private void init(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            _plane = new Plane(v0, v1, v2);
            _vertex0 = v0;
            _vertex1 = v1;
            _vertex2 = v2;

            int[] next = new int[] { 1, 2, 0 };

            for (int i = 0; i < 3; ++i)
            {
                Vector3 e = _vertex(next[i]) - _vertex(i);
                double mag = e.magnitude();

                if (i == 0)
                {
                    edgeMagnitude0 = mag;
                }
                else if (i == 1)
                {
                    edgeMagnitude1 = mag;
                }
                else
                {
                    edgeMagnitude2 = mag;
                }

                Vector3 dir;
                if (mag == 0)
                {
                    dir = Vector3.zero();
                }
                else
                {
                    dir = e / (float)mag;
                }

                if (i == 0)
                {
                    edgeDirection0 = dir;
                }
                else if (i == 1)
                {
                    edgeDirection1 = dir;
                }
                else
                {
                    edgeDirection2 = dir;
                }
            }

            edge01 = _vertex1 - _vertex0;
            edge02 = _vertex2 - _vertex0;

            _primaryAxis = _plane.normal().primaryAxis();
            _area = (float)(edgeDirection0.cross(edgeDirection2).magnitude() * (edgeMagnitude0 * edgeMagnitude2));
        }

        private Vector3 _vertex(int n)
        {
            if (n == 0)
            {
                return _vertex0;
            }
            if (n == 1)
            {
                return _vertex1;
            }
            return _vertex2;
        }

        /**
         Equivalent to the C++ Triangle() default constructor, which
         initializes the triangle with three zero vertices.  C# 4 does not
         allow parameterless constructors on structs, so the equivalent
         behavior is provided by this factory method.
         */
        public static Triangle zero()
        {
            return new Triangle(Vector3.zero(), Vector3.zero(), Vector3.zero());
        }

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            this = default(Triangle);
            init(v0, v1, v2);
        }

        public Triangle(BinaryInput b)
        {
            this = default(Triangle);
            deserialize(b);
        }

        public void serialize(BinaryOutput b)
        {
            _vertex0.serialize(b);
            _vertex1.serialize(b);
            _vertex2.serialize(b);
        }

        public void deserialize(BinaryInput b)
        {
            _vertex0.deserialize(b);
            _vertex1.deserialize(b);
            _vertex2.deserialize(b);
            init(_vertex0, _vertex1, _vertex2);
        }

        /** 0, 1, or 2 */
        public Vector3 vertex(int n)
        {
            Debug.debugAssert((n >= 0) && (n < 3));
            return _vertex(n);
        }

        /** edgeDirection[i] is the normalized vector v[i+1] - v[i] */
        internal Vector3 edgeDirection(int i)
        {
            if (i == 0)
            {
                return edgeDirection0;
            }
            if (i == 1)
            {
                return edgeDirection1;
            }
            return edgeDirection2;
        }

        internal double edgeMagnitude(int i)
        {
            if (i == 0)
            {
                return edgeMagnitude0;
            }
            if (i == 1)
            {
                return edgeMagnitude1;
            }
            return edgeMagnitude2;
        }

        public double area()
        {
            return _area;
        }

        public Vector3.Axis primaryAxis()
        {
            return _primaryAxis;
        }

        public Vector3 normal()
        {
            return _plane.normal();
        }

        /** Barycenter */
        public Vector3 center()
        {
            return (_vertex0 + _vertex1 + _vertex2) / 3.0f;
        }

        public Plane plane()
        {
            return _plane;
        }

        /** Returns a random point in the triangle. */
        public Vector3 randomPoint()
        {
            // Choose a random point in the parallelogram

            float s = g3dmath.uniformRandom();
            float t = g3dmath.uniformRandom();

            if (t > 1.0f - s)
            {
                // Outside the triangle; reflect about the
                // diagonal of the parallelogram
                t = 1.0f - t;
                s = 1.0f - s;
            }

            return edge01 * s + edge02 * t + _vertex0;
        }

        /**
         For two triangles to be equal they must have
         the same vertices <I>in the same order</I>.
         That is, vertex[0] == vertex[0], etc.
         */
        public static bool operator ==(Triangle left, Triangle right)
        {
            for (int i = 0; i < 3; ++i)
            {
                if (left._vertex(i) != right._vertex(i))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator !=(Triangle left, Triangle right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is Triangle && this == (Triangle)obj;
        }

        public override int GetHashCode()
        {
            return
                _vertex0.GetHashCode() +
                (_vertex1.GetHashCode() >> 2) +
                _vertex2.GetHashCode();
        }

        public void getBounds(out AABox result)
        {
            Vector3 lo = _vertex0;
            Vector3 hi = lo;

            for (int i = 1; i < 3; ++i)
            {
                lo = lo.min(_vertex(i));
                hi = hi.max(_vertex(i));
            }

            result = new AABox(lo, hi);
        }
    }
}
