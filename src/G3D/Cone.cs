/**
  @file Cone.h

  Cone class

  @maintainer Morgan McGuire, matrix@graphics3d.com
  @cite Portions based on Dave Eberly's Magic Software Library at <A HREF="http://www.magic-software.com">http://www.magic-software.com</A>

  @created 2001-06-02
  @edited  2006-02-23

  Copyright 2000-2006, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    /**
     An infinite cone.
     */
    public struct Cone
    {
        private Vector3 tip;
        private Vector3 direction;

        /** Angle from the center line to the edge. */
        private float angle;

        /**
         @param angle Angle from the center line to the edge, in radians
         */
        public Cone(Vector3 tip, Vector3 direction, float angle)
        {
            Debug.debugAssert(angle >= 0);
            Debug.debugAssert(angle <= g3dmath.G3D_PI);

            this.tip = tip;
            this.direction = direction.direction();
            this.angle = angle;
        }

        /**
         Forms the smallest cone that contains the box.  Undefined if
         the tip is inside or on the box.
         */
        public Cone(Vector3 tip, Box box)
        {
            this.tip = tip;
            direction = (box.getCenter() - tip).direction();

            // Find the biggest angle
            float smallestDotProduct = direction.dot((box.corner(0) - tip).direction());

            for (int i = 1; i < 8; ++i)
            {
                float dp = direction.dot((box.corner(i) - tip).direction());

                Debug.debugAssert(dp > 0);

                if (dp < smallestDotProduct)
                {
                    smallestDotProduct = dp;
                }
            }

            angle = (float)Math.Acos(smallestDotProduct);
        }

        /**
         Returns true if the cone touches, intersects, or contains b.

         If c.intersects(s) and c.intersects(Sphere(s.center, s.radius * 2)
         then the sphere s is entirely within cone c.
         */
        public bool intersects(Sphere b)
        {
            // If the bounding sphere contains the tip, then
            // they definitely touch.
            if (b.contains(tip))
            {
                return true;
            }

            // Move the tip backwards, effectively making the cone bigger
            // to account for the radius of the sphere.

            Vector3 movedTip = tip - direction * b.radius / (float)Math.Sin(angle);

            return new Cone(movedTip, direction, angle).contains(b.center);
        }

        /**
         True if v is a point inside the cone.
         */
        public bool contains(Vector3 v)
        {
            Vector3 d = (v - tip).direction();

            float x = d.dot(direction);

            return (x > 0) && (x >= (float)Math.Cos(angle));
        }
    }
}
