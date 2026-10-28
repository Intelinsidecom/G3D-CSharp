/**
 @file Ray.h
 
 Ray class
 
 @maintainer Morgan McGuire, matrix@graphics3d.com
 
 @created 2002-07-12
 @edited 2006-02-21
 */

using System;

namespace G3D
{
    /**
     A 3D Ray.
     */
    public struct Ray
    {
        private Ray(Vector3 origin, Vector3 direction)
        {
            this.origin = origin;
            this.direction = direction;
        }

        public Vector3 origin;

        /**
         Not unit length
         */
        public Vector3 direction;

        public Ray(BinaryInput b)
        {
            origin = Vector3.zero();
            direction = Vector3.zero();
            deserialize(b);
        }

        public void serialize(BinaryOutput b)
        {
            origin.serialize(b);
            direction.serialize(b);
        }

        public void deserialize(BinaryInput b)
        {
            origin.deserialize(b);
            direction.deserialize(b);
        }

        /**
         Creates a Ray from a origin and a (nonzero) direction.
         */
        public static Ray fromOriginAndDirection(Vector3 point, Vector3 direction)
        {
            return new Ray(point, direction);
        }

        public Ray unit()
        {
            Ray result = new Ray(origin, direction.unit());
            return result;
        }

        /**
         Returns the closest point on the Ray to point.
         */
        public Vector3 closestPoint(Vector3 point)
        {
            float t = direction.dot(point - this.origin);
            if (t < 0)
            {
                return this.origin;
            }
            else
            {
                return this.origin + direction * t;
            }
        }

        /**
         Returns the closest distance between point and the Ray
         */
        public float distance(Vector3 point)
        {
            return (closestPoint(point) - point).magnitude();
        }

        /**
         Returns the point where the Ray and plane intersect.  If there
         is no intersection, returns a point at infinity.

          Planes are considered one-sided, so the ray will not intersect
          a plane where the normal faces in the traveling direction.
        */
        public Vector3 intersection(Plane plane)
        {
            float d;
            Vector3 normal = plane.normal();
            plane.getEquation(out normal, out d);
            float rate = direction.dot(normal);

            if (rate >= 0.0f)
            {
                return Vector3.inf();
            }
            else
            {
                float t = -(d + origin.dot(normal)) / rate;

                return origin + direction * t;
            }
        }

        /**
         Returns the distance until intersection with the (solid) sphere.
         Will be 0 if inside the sphere, inf if there is no intersection.

         The ray direction is <B>not</B> normalized.  If the ray direction
         has unit length, the distance from the origin to intersection
         is equal to the time.  If the direction does not have unit length,
         the distance = time * direction.length().

         See also the G3D::CollisionDetection "movingPoint" methods,
         which give more information about the intersection.
        */
        public float intersectionTime(Sphere sphere)
        {
            Vector3 dummy = Vector3.zero();
            return CollisionDetection.collisionTimeForMovingPointFixedSphere(
                origin, direction, sphere, out dummy);
        }

        public float intersectionTime(Plane plane)
        {
            Vector3 dummy = Vector3.zero();
            return CollisionDetection.collisionTimeForMovingPointFixedPlane(
                origin, direction, plane, out dummy);
        }

        public float intersectionTime(Box box)
        {
            Vector3 dummy = Vector3.zero();
            float time = CollisionDetection.collisionTimeForMovingPointFixedBox(
                origin, direction, box, out dummy);

            if ((time == g3dmath.inf()) && (box.contains(origin)))
            {
                return 0.0f;
            }
            else
            {
                return time;
            }
        }

        public float intersectionTime(AABox box)
        {
            Vector3 dummy = Vector3.zero();
            bool inside = false;
            float time = CollisionDetection.collisionTimeForMovingPointFixedAABox(
                origin, direction, box, out dummy, out inside);

            if ((time == g3dmath.inf()) && inside)
            {
                return 0.0f;
            }
            else
            {
                return time;
            }
        }

        /**
         The three extra arguments are the weights of vertices 0, 1, and 2
         at the intersection point; they are useful for texture mapping
         and interpolated normals.
        */
        public float intersectionTime(
            Vector3 v0, Vector3 v1, Vector3 v2,
            Vector3 edge01, Vector3 edge02,
            out double w0, out double w1, out double w2)
        {
            // Barycenteric coords
            float u, v;

            // begin calculating determinant - also used to calculate U parameter
            Vector3 pvec = direction.cross(edge02);

            // if determinant is near zero, ray lies in plane of triangle
            float det = edge01.dot(pvec);

            if (det < EPSILON)
            {
                w0 = 0;
                w1 = 0;
                w2 = 0;
                return (float)g3dmath.inf();
            }

            // calculate distance from vert0 to ray origin
            Vector3 tvec = origin - v0;

            // calculate U parameter and test bounds
            u = tvec.dot(pvec);
            if ((u < 0.0f) || (u > det))
            {
                // Hit the plane outside the triangle
                w0 = 0;
                w1 = 0;
                w2 = 0;
                return (float)g3dmath.inf();
            }

            // prepare to test V parameter
            Vector3 qvec = tvec.cross(edge01);

            // calculate V parameter and test bounds
            v = direction.dot(qvec);
            if ((v < 0.0f) || (u + v > det))
            {
                // Hit the plane outside the triangle
                w0 = 0;
                w1 = 0;
                w2 = 0;
                return (float)g3dmath.inf();
            }

            float t = edge02.dot(qvec);

            if (t >= 0)
            {
                float inv_det = 1.0f / det;
                t *= inv_det;
                u *= inv_det;
                v *= inv_det;

                w0 = (1.0f - u - v);
                w1 = u;
                w2 = v;

                return t;
            }
            else
            {
                // We had to travel backwards in time to intersect
                w0 = 0;
                w1 = 0;
                w2 = 0;
                return (float)g3dmath.inf();
            }
        }

        /**
         Ray-triangle intersection for a 1-sided triangle.  Fastest version.
           @cite http://www.acm.org/jgt/papers/MollerTrumbore97/
           http://www.graphics.cornell.edu/pubs/1997/MT97.html
        */
        public float intersectionTime(
            Vector3 vert0,
            Vector3 vert1,
            Vector3 vert2,
            Vector3 edge01,
            Vector3 edge02)
        {
            // Barycenteric coords
            float u, v;

            // begin calculating determinant - also used to calculate U parameter
            Vector3 pvec = direction.cross(edge02);

            // if determinant is near zero, ray lies in plane of triangle
            float det = edge01.dot(pvec);

            if (det < EPSILON)
            {
                return (float)g3dmath.inf();
            }

            // calculate distance from vert0 to ray origin
            Vector3 tvec = origin - vert0;

            // calculate U parameter and test bounds
            u = tvec.dot(pvec);
            if ((u < 0.0f) || (u > det))
            {
                // Hit the plane outside the triangle
                return (float)g3dmath.inf();
            }

            // prepare to test V parameter
            Vector3 qvec = tvec.cross(edge01);

            // calculate V parameter and test bounds
            v = direction.dot(qvec);
            if ((v < 0.0f) || (u + v > det))
            {
                // Hit the plane outside the triangle
                return (float)g3dmath.inf();
            }

            // Case where we don't need correct (u, v):
            float t = edge02.dot(qvec);

            if (t >= 0.0f)
            {
                // Note that det must be positive
                return t / det;
            }
            else
            {
                // We had to travel backwards in time to intersect
                return (float)g3dmath.inf();
            }
        }

        public float intersectionTime(Vector3 vert0, Vector3 vert1, Vector3 vert2)
        {
            return intersectionTime(vert0, vert1, vert2, vert1 - vert0, vert2 - vert0);
        }

        public float intersectionTime(
            Vector3 vert0,
            Vector3 vert1,
            Vector3 vert2,
            out double w0,
            out double w1,
            out double w2)
        {
            return intersectionTime(vert0, vert1, vert2, vert1 - vert0, vert2 - vert0, out w0, out w1, out w2);
        }

        /* One-sided triangle
           */
        public float intersectionTime(Triangle triangle)
        {
            return intersectionTime(
                triangle.vertex(0), triangle.vertex(1), triangle.vertex(2),
                triangle.edge01, triangle.edge02);
        }

        public float intersectionTime(
            Triangle triangle,
            out double w0,
            out double w1,
            out double w2)
        {
            return intersectionTime(triangle.vertex(0), triangle.vertex(1), triangle.vertex(2),
                triangle.edge01, triangle.edge02, out w0, out w1, out w2);
        }

        /** Refracts about the normal
            using G3D::Vector3::refractionDirection
            and bumps the ray slightly from the newOrigin. */
        public Ray refract(
            Vector3 newOrigin,
            Vector3 normal,
            float iInside,
            float iOutside)
        {
            Vector3 D = direction.refractionDirection(normal, iInside, iOutside);
            return Ray.fromOriginAndDirection(
                newOrigin + (direction + normal * (float)g3dmath.sign(direction.dot(normal))) * 0.001f, D);
        }

        /** Reflects about the normal
            using G3D::Vector3::reflectionDirection
            and bumps the ray slightly from
            the newOrigin. */
        public Ray reflect(
            Vector3 newOrigin,
            Vector3 normal)
        {
            Vector3 D = direction.reflectionDirection(normal);
            return Ray.fromOriginAndDirection(newOrigin + (D + normal) * 0.001f, D);
        }

        private const float EPSILON = 0.000001f;
    }
}
