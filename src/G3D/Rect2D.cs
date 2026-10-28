/**
  @file Rect2D.h
 
  @maintainer Morgan McGuire, matrix@graphics3d.com
 
  @created 2003-11-13
  @created 2005-01-16

  Copyright 2000-2005, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    /**
     If you are using this class for pixel rectangles, keep in mind that the last
     pixel you can draw to is at x0() + width() - 1.
     */
    public struct Rect2D
    {
        private Vector2 min;
        private Vector2 max;

        /**
         Returns true if the whole polygon is clipped.
         @param p Value of the point
         @param axis Index [0 or 1] of the axis to clip along?
         @param clipGreater Are we clipping greater than or less than the line?
         @param inPoly Polygon being clipped
         @param outPoly The clipped polygon
         */
        private static bool clipSide2D<T>(
            float p, bool clipGreater, int axis,
            Array<T> inPoly, Array<T> outPoly,
            Func<T, int, float> component,
            Func<T, T, float, T> lerp)
        {

            outPoly.clear();
            int i0 = -1;

            T pt1 = default(T);
            bool c1 = true;

            float negate = clipGreater ? -1 : 1;

            // Find a point that is not clipped
            for (i0 = 0; (i0 < inPoly.length()) && c1; ++i0)
            {
                pt1 = inPoly[i0];
                c1 = (negate * component(pt1, axis)) < (negate * p);
            }

            // We incremented i0 one time to many
            --i0;

            if (c1)
            {
                // We could not find an unclipped point
                return true;
            }

            outPoly.append(pt1);

            // for each point in inPoly,
            //     if the point is outside the side and the previous one was also outside, continue
            //     if the point is outside the side and the previous one was inside, cut the line
            //     if the point is inside the side and the previous one was also inside, append the points
            //     if the point is inside the side and the previous one was outside, cut the line    
            for (int i = 1; i <= inPoly.length(); ++i)
            {
                T pt2 = inPoly[(i + i0) % inPoly.length()];
                bool c2 = (negate * component(pt2, axis)) < (negate * p);

                if (c1 ^ c2)
                {

                    if (!c1 && c2 && (i > 1))
                    {
                        // Unclipped to clipped trasition and not the first iteration
                        outPoly.append(pt1);
                    }

                    // only one point is clipped, find where the line crosses the clipping plane


                    float alpha;
                    if (component(pt2, axis) == component(pt1, axis))
                    {
                        alpha = 0;
                    }
                    else
                    {
                        alpha = (p - component(pt1, axis)) / (component(pt2, axis) - component(pt1, axis));
                    }
                    outPoly.append(lerp(pt1, pt2, alpha));
                }
                else if (!(c1 || c2) && (i != 1))
                {
                    // neither point is clipped (don't do this the first time 
                    // because we appended the first pt before the loop)
                    outPoly.append(pt1);
                }

                pt1 = pt2;
                c1 = c2;
            }

            return false;
        }

        /** Creates a rectangle at 0,0 with the given width and height*/
        public Rect2D(Vector2 wh)
        {
            min = new Vector2(0, 0);
            max = new Vector2(wh.x, wh.y);
        }

        /** Creates a rectangle with the given x, y, width, height */
        public Rect2D(float x, float y, float w, float h)
        {
            min = new Vector2(x, y);
            max = new Vector2(x + w, y + h);
        }

        /** Computes a rectangle that contains both @a a and @a b.*/
        public Rect2D(Rect2D a, Rect2D b)
        {
            min = a.min.min(b.min);
            max = a.max.max(b.max);
        }

        public float width()
        {
            return max.x - min.x;
        }

        public float height()
        {
            return max.y - min.y;
        }

        public float x
        {
            get { return min.x; }
        }

        public float y
        {
            get { return min.y; }
        }

        public float x0()
        {
            return min.x;
        }

        public float x1()
        {
            return max.x;
        }

        public float y0()
        {
            return min.y;
        }

        public float y1()
        {
            return max.y;
        }

        /** Min, min corner */
        public Vector2 x0y0()
        {
            return min;
        }

        public Vector2 x1y0()
        {
            return new Vector2(max.x, min.y);
        }

        public Vector2 x0y1()
        {
            return new Vector2(min.x, max.y);
        }

        /** Max,max corner */
        public Vector2 x1y1()
        {
            return max;
        }

        /** Width and height */
        public Vector2 wh()
        {
            return max - min;
        }

        public Vector2 center()
        {
            return (max + min) * 0.5f;
        }

        public static Rect2D xyxy(float x0, float y0, float x1, float y1)
        {
            Rect2D r = default(Rect2D);

            r.min.x = Math.Min(x0, x1);
            r.min.y = Math.Min(y0, y1);
            r.max.x = Math.Max(x0, x1);
            r.max.y = Math.Max(y0, y1);

            return r;
        }

        public static Rect2D xyxy(Vector2 v0, Vector2 v1)
        {
            Rect2D r = default(Rect2D);

            r.min = v0.min(v1);
            r.max = v0.max(v1);

            return r;
        }

        public static Rect2D xywh(float x, float y, float w, float h)
        {
            return xyxy(x, y, x + w, y + h);
        }

        public static Rect2D xywh(Vector2 v, Vector2 w)
        {
            return xyxy(v.x, v.y, v.x + w.x, v.y + w.y);
        }

        public bool contains(Vector2 v)
        {
            return (v.x >= min.x) && (v.y >= min.y) && (v.x <= max.x) && (v.y <= max.y);
        }

        public bool contains(Rect2D r)
        {
            return (min.x <= r.min.x) && (min.y <= r.min.y) &&
                   (max.x >= r.max.x) && (max.y >= r.max.y);
        }

        /** True if there is non-zero area to the intersection between @a this and @a r.
            Note that two rectangles that are adjacent do not intersect because there is
            zero area to the overlap, even though one of them "contains" the corners of the other.*/
        public bool intersects(Rect2D r)
        {
            return (min.x < r.max.x) && (min.y < r.max.y) &&
                   (max.x > r.min.x) && (max.y > r.min.y);
        }

        /** Like intersection, but counts the adjacent case as touching. */
        public bool intersectsOrTouches(Rect2D r)
        {
            return (min.x <= r.max.x) && (min.y <= r.max.y) &&
                   (max.x >= r.min.x) && (max.y >= r.min.y);
        }

        public static Rect2D operator *(Rect2D r, float s)
        {
            return xyxy(r.min.x * s, r.min.y * s, r.max.x * s, r.max.y * s);
        }

        public static Rect2D operator /(Rect2D r, float s)
        {
            return xyxy(r.min / s, r.max / s);
        }

        public static Rect2D operator +(Rect2D r, Vector2 v)
        {
            return xyxy(r.min + v, r.max + v);
        }

        public static Rect2D operator -(Rect2D r, Vector2 v)
        {
            return xyxy(r.min - v, r.max - v);
        }

        public static bool operator ==(Rect2D r, Rect2D other)
        {
            return (r.min == other.min) && (r.max == other.max);
        }

        public static bool operator !=(Rect2D r, Rect2D other)
        {
            return (r.min != other.min) || (r.max != other.max);
        }

        /** Returns the corners in the order: (min,min), (max,min), (max,max), (min,max). */
        public Vector2 corner(int i)
        {
            Debug.debugAssert(i >= 0 && i < 4);
            switch (i & 3)
            {
                case 0:
                    return new Vector2(min.x, min.y);
                case 1:
                    return new Vector2(max.x, min.y);
                case 2:
                    return new Vector2(max.x, max.y);
                case 3:
                    return new Vector2(min.x, max.y);
                default:
                    // Should never get here
                    return new Vector2(0, 0);
            }
        }


        /** Returns a new Rect2D that is bigger/smaller by the specified amount 
            (negative is shrink.) */
        public Rect2D border(float delta)
        {
            return Rect2D.xywh(x0() + delta,
                     y0() + delta,
                     width() - 2.0f * delta,
                     height() - 2.0f * delta);
        }


        /** 
         Clips so that the rightmost point of the outPoly is at rect.x1 (e.g. a 800x600 window produces
         rightmost point 799, not 800).  The results are suitable for pixel rendering if iRounded.
         Templated so that it will work for Vector2,3,4 (the z and w components are interpolated linearly).
         The template parameter must define T.lerp and contain x and y components.

         If the entire polygon is clipped by a single side, the result will be empty.
         The result might also have zero area but not be empty.
         */
        public void clip(Array<Vector2> inPoly, Array<Vector2> outPoly)
        {
            clip<Vector2>(inPoly, outPoly, componentVector2, lerpVector2);
        }

        /**
         Documented adaptation: the C++ template is bound to Vector3.
         */
        public void clip(Array<Vector3> inPoly, Array<Vector3> outPoly)
        {
            clip<Vector3>(inPoly, outPoly, componentVector3, lerpVector3);
        }

        /**
         Documented adaptation: the C++ template is bound to Vector4.
         */
        public void clip(Array<Vector4> inPoly, Array<Vector4> outPoly)
        {
            clip<Vector4>(inPoly, outPoly, componentVector4, lerpVector4);
        }

        private void clip<T>(
            Array<T> inPoly, Array<T> outPoly,
            Func<T, int, float> component,
            Func<T, T, float, T> lerp)
        {

            const bool greaterThan = true;
            const bool lessThan = false;
            const int X = 0;
            const int Y = 1;

            Array<T> temp = new Array<T>();

            bool entirelyClipped =
              clipSide2D(x0(), lessThan, X, inPoly, temp, component, lerp) ||
              clipSide2D(x1(), greaterThan, X, temp, outPoly, component, lerp) ||
              clipSide2D(y0(), lessThan, Y, outPoly, temp, component, lerp) ||
              clipSide2D(y1(), greaterThan, Y, temp, outPoly, component, lerp);

            if (entirelyClipped)
            {
                outPoly.clear();
            }
        }

        private static float componentVector2(Vector2 v, int axis)
        {
            return v[axis];
        }

        private static Vector2 lerpVector2(Vector2 a, Vector2 b, float alpha)
        {
            return a.lerp(b, alpha);
        }

        private static float componentVector3(Vector3 v, int axis)
        {
            return v[axis];
        }

        private static Vector3 lerpVector3(Vector3 a, Vector3 b, float alpha)
        {
            return a.lerp(b, alpha);
        }

        private static float componentVector4(Vector4 v, int axis)
        {
            return v[axis];
        }

        private static Vector4 lerpVector4(Vector4 a, Vector4 b, float alpha)
        {
            return a.lerp(b, alpha);
        }


        /** Returns the largest, centered Rect2D that can fit inside this
            while maintaining the aspect ratio of x:y.  Convenient for
            displaying images in odd-shaped windows.
        */
        public Rect2D largestCenteredSubRect(float ww, float hh)
        {
            float textureAspect = hh / ww;
            float viewAspect = height() / width();

            if (viewAspect > textureAspect)
            {
                // The view is too tall
                float h = width() * textureAspect;
                float y = (height() - h) / 2;
                return Rect2D.xywh(0, y, width(), h) + corner(0);
            }
            else
            {
                // The view is too wide
                float w = height() / textureAspect;
                float x = (width() - w) / 2;
                return Rect2D.xywh(x, 0, w, height()) + corner(0);
            }
        }

        /**
         Returns the overlap region between the two rectangles.  This may have zero area
         if they do not intersect.  See the two-Rect2D constructor for a way to compute
         a union-like rectangle.
         */
        public Rect2D intersect(Rect2D other)
        {
            if (intersects(other))
            {
                return Rect2D.xyxy(min.max(other.min), max.min(other.max));
            }
            else
            {
                return Rect2D.xywh(0, 0, 0, 0);
            }
        }

        public float area()
        {
            return width() * height();
        }
    }
}
