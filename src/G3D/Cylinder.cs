/**
 @file Cylinder.h
   
 @maintainer Morgan McGuire, matrix@graphics3d.com
   
 @created 2003-02-07
 @edited  2005-09-26

 Copyright 2000-2005, Morgan McGuire.
 All rights reserved.
 */

using System;

namespace G3D
{
    /**
     Right cylinder
     */
    public struct Cylinder
    {
        private Vector3 p1;
        private Vector3 p2;

        private float mRadius;

        public Cylinder(BinaryInput b)
        {
            p1 = Vector3.zero();
            p2 = Vector3.zero();
            mRadius = 0;
            deserialize(b);
        }

        public Cylinder(Vector3 _p1, Vector3 _p2, float _r)
        {
            p1 = _p1;
            p2 = _p2;
            mRadius = _r;
        }

        public void serialize(BinaryOutput b)
        {
            p1.serialize(b);
            p2.serialize(b);
            b.writeFloat64(mRadius);
        }

        public void deserialize(BinaryInput b)
        {
            p1.deserialize(b);
            p2.deserialize(b);
            mRadius = (float)b.readFloat64();
        }

        /** The line down the center of the Cylinder */
        public Line getAxis()
        {
            return Line.fromTwoPoints(p1, p2);
        }

        /**
          A reference frame in which the center of mass is at the origin and
          the Y-axis is the cylinder's axis.  If the cylinder is transformed, this reference frame
          may freely rotate around its axis.*/
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

        public Vector3 getPoint1()
        {
            return p1;
        }

        public Vector3 getPoint2()
        {
            return p2;
        }

        /**
         Returns true if the point is inside the Cylinder or on its surface.
         */
        public bool contains(Vector3 p)
        {
            return LineSegment.fromTwoPoints(p1, p2).distanceSquared(p) <= g3dmath.square(mRadius);
        }

        public float area()
        {
            return (float)(
                // Sides
                (2.0f * g3dmath.G3D_PI * mRadius) * height() +

                // Caps
                2.0f * g3dmath.G3D_PI * g3dmath.square(mRadius));
        }

        public float volume()
        {
            return
                (float)(g3dmath.G3D_PI * g3dmath.square((double)mRadius) * (p1 - p2).magnitude());
        }

        public float radius()
        {
            return mRadius;
        }

        /** Center of mass */
        public Vector3 center()
        {
            return (p1 + p2) / 2.0f;
        }

        public float height()
        {
            return (p1 - p2).magnitude();
        }

        /**
         Get close axis aligned bounding box.
         With vertical world orientation, the top and bottom might not be very tight. */
        public void getBounds(out AABox result)
        {
            Vector3 min = p1.min(p2) - (new Vector3(1, 1, 1) * mRadius);
            Vector3 max = p1.max(p2) + (new Vector3(1, 1, 1) * mRadius);
            result = new AABox(min, max);
        }

        /** Random world space point with outward facing normal. */
        public void getRandomSurfacePoint(out Vector3 p, out Vector3 N)
        {
            float h = height();
            float r = radius();

            // Create a random point on a standard cylinder and then rotate to the global frame.

            // Relative areas (factor of 2PI already taken out)
            float capRelArea = (float)g3dmath.square(r) / 2.0f;
            float sideRelArea = r * h;

            float r1 = (float)g3dmath.random(0, capRelArea * 2 + sideRelArea);

            if (r1 < capRelArea * 2)
            {
                // Select a point uniformly at random on a disk
                // @cite http://mathworld.wolfram.com/DiskPointPicking.html
                float a = (float)g3dmath.random(0, g3dmath.G3D_TWO_PI);
                float r2 = (float)Math.Sqrt(g3dmath.random(0, 1)) * r;
                p.x = (float)Math.Cos(a) * r2;
                p.z = (float)Math.Sin(a) * r2;

                N.x = 0;
                N.z = 0;
                if (r1 < capRelArea)
                {
                    // Top
                    p.y = h / 2.0f;
                    N.y = 1;
                }
                else
                {
                    // Bottom
                    p.y = -h / 2.0f;
                    N.y = -1;
                }
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
            float r = radius();

            // Create a random point in a standard cylinder and then rotate to the global frame.

            // Select a point uniformly at random on a disk
            // @cite http://mathworld.wolfram.com/DiskPointPicking.html
            float a = (float)g3dmath.random(0, g3dmath.G3D_TWO_PI);
            float r2 = (float)Math.Sqrt(g3dmath.random(0, 1)) * r;

            Vector3 p = new Vector3((float)Math.Cos(a) * r2,
                                    (float)g3dmath.random(-h / 2.0f, h / 2.0f),
                                    (float)Math.Sin(a) * r2);

            // Transform to world space
            CoordinateFrame cframe = CoordinateFrame.identity();
            getReferenceFrame(ref cframe);

            return cframe.pointToWorldSpace(p);
        }
    }
}
