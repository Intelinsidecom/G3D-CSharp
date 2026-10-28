/**
 @file Capsule.h
   
 @maintainer Morgan McGuire, matrix@graphics3d.com
   
 @created 2003-02-07
 @edited  2005-08-20

 Copyright 2000-2006, Morgan McGuire.
 All rights reserved.
 */

using System;

namespace G3D
{
    /**
     A shape formed by extruding a sphere along a line segment.
     */
    public struct Capsule
    {
        private Vector3 p1;
        private Vector3 p2;

        /** @deprecated To be replaced with a (float) radius accessor of the same name.*/
        public double radius;

        public Capsule(BinaryInput b)
        {
            p1 = Vector3.zero();
            p2 = Vector3.zero();
            radius = 0;
            deserialize(b);
        }

        public Capsule(Vector3 _p1, Vector3 _p2, double _r)
        {
            p1 = _p1;
            p2 = _p2;
            radius = _r;
        }

        public void serialize(BinaryOutput b)
        {
            p1.serialize(b);
            p2.serialize(b);
            b.writeFloat64(radius);
        }

        public void deserialize(BinaryInput b)
        {
            p1.deserialize(b);
            p2.deserialize(b);
            radius = b.readFloat64();
        }

        /** The line down the center of the capsule */
        public Line getAxis()
        {
            return Line.fromTwoPoints(p1, p2);
        }

        public Vector3 getPoint1()
        {
            return p1;
        }

        public Vector3 getPoint2()
        {
            return p2;
        }

        /** Distance between the sphere centers.  The total extent of the cylinder is
            2r + h. */
        public float height()
        {
            return (p1 - p2).magnitude();
        }

        public Vector3 center()
        {
            return (p1 + p2) / 2.0f;
        }

        /** Get a reference frame in which the center of mass is the origin and Y is the axis of the capsule.*/
        public void getReferenceFrame(ref CoordinateFrame cframe)
        {
            cframe.translation = center();

            Vector3 Y = (p1 - p2).direction();
            Vector3 X = (Math.Abs(Y.dot(Vector3.unitX())) > 0.9) ? Vector3.unitY() : Vector3.unitX();
            Vector3 Z = X.cross(Y).direction();
            X = Y.cross(Z);
            cframe.rotation.setColumn(0, X);
            cframe.rotation.setColumn(1, Y);
            cframe.rotation.setColumn(2, Z);
        }

        /**
         Returns true if the point is inside the capsule or on its surface.
         */
        public bool contains(Vector3 p)
        {
            return LineSegment.fromTwoPoints(p1, p2).distanceSquared(p) <= g3dmath.square(radius);
        }

        /** @deprecated */
        public float getRadius()
        {
            return (float)radius;
        }

        /** @deprecated Use volume() */
        public float getVolume()
        {
            return
                // Sphere volume
                (float)Math.Pow(radius, 3) * (float)g3dmath.G3D_PI * 4 / 3 +

                // Cylinder volume
                (float)Math.Pow(radius, 2) * (p1 - p2).magnitude();
        }

        public float volume()
        {
            return getVolume();
        }

        /** @deprecated */
        public float getSurfaceArea()
        {
            return
                // Sphere area
                (float)Math.Pow(radius, 2) * 4 * (float)g3dmath.G3D_PI +

                // Cylinder area
                2 * (float)g3dmath.G3D_PI * (float)radius * (p1 - p2).magnitude();
        }

        public float area()
        {
            return getSurfaceArea();
        }

        /** Get axis aligned bounding box */
        public void getBounds(out AABox result)
        {
            Vector3 min = p1.min(p2) - (new Vector3(1, 1, 1) * (float)radius);
            Vector3 max = p1.max(p2) + (new Vector3(1, 1, 1) * (float)radius);

            result = new AABox(min, max);
        }

        /** Random world space point with outward facing normal. */
        public void getRandomSurfacePoint(out Vector3 p, out Vector3 N)
        {
            float h = height();
            float r = getRadius();

            // Create a random point on a standard capsule and then rotate to the global frame.

            // Relative areas
            float capRelArea = (float)Math.Sqrt(r) / 2.0f;
            float sideRelArea = r * h;

            float r1 = (float)g3dmath.random(0, capRelArea * 2 + sideRelArea);

            if (r1 < capRelArea * 2)
            {
                // Select a point uniformly at random on a sphere
                N = new Sphere(Vector3.zero(), 1).randomSurfacePoint();
                p = N * r;
                p.y += Math.Sign(p.y) * h / 2.0f;
            }
            else
            {
                // Side
                float a = (float)g3dmath.random(0, g3dmath.G3D_TWO_PI);
                N.x = (float)Math.Cos(a);
                N.y = 0;
                N.z = (float)Math.Sin(a);
                p.x = N.x * r;
                p.z = N.y * r;
                p.y = (float)g3dmath.random(-h / 2.0f, h / 2.0f);
            }

            // Transform to world space
            CoordinateFrame cframe = CoordinateFrame.identity();
            getReferenceFrame(ref cframe);

            p = cframe.pointToWorldSpace(p);
            N = cframe.normalToWorldSpace(N);
        }

        /** Point selected uniformly at random over the volume. */
        public Vector3 randomInteriorPoint()
        {
            float h = height();
            float r = getRadius();

            // Create a random point in a standard capsule and then rotate to the global frame.

            Vector3 p = Vector3.zero();

            float hemiVolume = (float)(g3dmath.G3D_PI * (r * r * r) * 4 / 6.0);
            float cylVolume = (float)(g3dmath.G3D_PI * g3dmath.square(r) * h);

            float r1 = (float)g3dmath.random(0, 2.0 * hemiVolume + cylVolume);

            if (r1 < 2.0 * hemiVolume)
            {
                p = new Sphere(Vector3.zero(), r).randomInteriorPoint();

                p.y += Math.Sign(p.y) * h / 2.0f;

            }
            else
            {
                // Select a point uniformly at random on a disk
                float a = (float)g3dmath.random(0, g3dmath.G3D_TWO_PI);
                float r2 = (float)Math.Sqrt(g3dmath.random(0, 1)) * r;

                p = new Vector3((float)Math.Cos(a) * r2,
                                (float)g3dmath.random(-h / 2.0f, h / 2.0f),
                                (float)Math.Sin(a) * r2);
            }

            // Transform to world space
            CoordinateFrame cframe = CoordinateFrame.identity();
            getReferenceFrame(ref cframe);

            return cframe.pointToWorldSpace(p);
        }
    }
}
