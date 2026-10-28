/**
 @file CoordinateFrame.h

 @maintainer Morgan McGuire, matrix@graphics3d.com
 
 @created 2001-03-04
 @edited  2006-04-07

 Copyright 2000-2006, Morgan McGuire.
 All rights reserved.
*/

using System;

namespace G3D
{
    /**
     A rigid body RT (rotation-translation) transformation.
    
    CoordinateFrame abstracts a 4x4 matrix that maps object space to world space:
  
      v_world = C * v_object

    CoordinateFrame::rotation is the upper 3x3 submatrix, CoordinateFrame::translation
    is the right 3x1 column.  The 4th row is always [0 0 0 1], so it isn't stored.  
    So you don't have to remember which way the multiplication and transformation work,
    it provides explicit toWorldSpace and toObjectSpace methods.  Also, points, vectors 
    (directions), and surface normals transform differently, so they have separate methods.
 
    Some helper functions transform whole primitives like boxes in and out of object space.

    Convert to Matrix4 using CoordinateFrame::toMatrix4.  You <I>can</I> construct a CoordinateFrame
    from a Matrix4 using Matrix4::approxCoordinateFrame, however, because a Matrix4 is more 
    general than a CoordinateFrame, some information may be lost.

    See also: G3D::Matrix4, G3D::Quat
    */
    public struct CoordinateFrame
    {
        /**
         Takes object space points to world space.
         */
        public Matrix3 rotation;

        /**
         Takes object space points to world space.
         */
        public Vector3 translation;

        /**
         The direction an object "looks" relative to its own axes.
         @deprecated This is always -1 and will be fixed at that value in future releases.
         */
        public const float zLookDirection = -1;

        public static bool operator ==(CoordinateFrame left, CoordinateFrame right)
        {
            return (left.translation == right.translation) && (left.rotation == right.rotation);
        }

        public static bool operator !=(CoordinateFrame left, CoordinateFrame right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is CoordinateFrame && this == (CoordinateFrame)obj;
        }

        public override int GetHashCode()
        {
            return rotation.GetHashCode() ^ translation.GetHashCode();
        }

        public bool fuzzyEq(CoordinateFrame other)
        {
            for (int c = 0; c < 3; ++c)
            {
                for (int r = 0; r < 3; ++r)
                {
                    if (!g3dmath.fuzzyEq(other.rotation.elt(r, c), rotation.elt(r, c)))
                    {
                        return false;
                    }
                }
                if (!g3dmath.fuzzyEq(translation[c], other.translation[c]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool fuzzyIsIdentity()
        {
            Matrix3 I = Matrix3.identity();

            for (int c = 0; c < 3; ++c)
            {
                for (int r = 0; r < 3; ++r)
                {
                    if (g3dmath.fuzzyNe(I.elt(r, c), rotation.elt(r, c)))
                    {
                        return false;
                    }
                }
                if (g3dmath.fuzzyNe(translation[c], 0))
                {
                    return false;
                }
            }

            return true;
        }

        public bool isIdentity()
        {
            return
                (translation == Vector3.zero()) &&
                (rotation == Matrix3.identity());
        }

        /**
         Initializes to the identity coordinate frame.
         */
        public static CoordinateFrame identity()
        {
            return new CoordinateFrame(Matrix3.identity(), Vector3.zero());
        }

        public CoordinateFrame(Vector3 _translation)
        {
            rotation = Matrix3.identity();
            translation = _translation;
        }

        public CoordinateFrame(Matrix3 _rotation, Vector3 _translation)
        {
            rotation = _rotation;
            translation = _translation;
        }

        public CoordinateFrame(Matrix3 _rotation)
        {
            rotation = _rotation;
            translation = Vector3.zero();
        }

        public CoordinateFrame(BinaryInput b)
        {
            this = default(CoordinateFrame);
            rotation = Matrix3.zero();
            deserialize(b);
        }

        public CoordinateFrame(Matrix4 m)
        {
            // Extract rotation (upper 3x3) and translation (right column) from Matrix4
            rotation = new Matrix3(
                m.elt(0, 0), m.elt(0, 1), m.elt(0, 2),
                m.elt(1, 0), m.elt(1, 1), m.elt(1, 2),
                m.elt(2, 0), m.elt(2, 1), m.elt(2, 2));
            translation = new Vector3(m.elt(0, 3), m.elt(1, 3), m.elt(2, 3));
        }

        public void deserialize(BinaryInput b)
        {
            rotation.deserialize(b);
            translation.deserialize(b);
        }

        public void serialize(BinaryOutput b)
        {
            rotation.serialize(b);
            translation.serialize(b);
        }

        /**
          Computes the inverse of this coordinate frame.
         */
        public CoordinateFrame inverse()
        {
            CoordinateFrame result = CoordinateFrame.identity();
            // Use left multiply to avoid data dependence with the
            // transpose operation.
//            result.translation = -translation * result.rotation;
            result.rotation = rotation.transpose();
            result.translation = -(result.rotation * translation);
            return result;
        }

        /** See also Matrix4::approxCoordinateFrame */
        public Matrix4 toMatrix4()
        {
            return new Matrix4(this);
        }

        /**
         Produces an XML serialization of this coordinate frame.
         */
        public string toXML()
        {
            return Format.format(
                "<COORDINATEFRAME>\n  %lf,%lf,%lf,%lf,\n  %lf,%lf,%lf,%lf,\n  %lf,%lf,%lf,%lf,\n  %lf,%lf,%lf,%lf\n</COORDINATEFRAME>\n",
                rotation.elt(0, 0), rotation.elt(0, 1), rotation.elt(0, 2), translation.x,
                rotation.elt(1, 0), rotation.elt(1, 1), rotation.elt(1, 2), translation.y,
                rotation.elt(2, 0), rotation.elt(2, 1), rotation.elt(2, 2), translation.z,
                0.0, 0.0, 0.0, 1.0);
        }

        /**
         Returns the heading of the lookVector as an angle in radians relative to
         the world -z axis.  That is, a counter-clockwise heading where north (-z) 
         is 0 and west (-x) is PI/2.

         Note that the heading ignores the Y axis, so an inverted
         object has an inverted heading.
         */
        public float getHeading()
        {
            Vector3 look = rotation.getColumnVector(2);
            float angle = -(float)g3dmath.aTan2(-look.x, look.z);
            return angle;
        }

        /**
         Takes the coordinate frame into object space.
         this->inverse() * c
         */
        public CoordinateFrame toObjectSpace(CoordinateFrame c)
        {
            return this.inverse() * c;
        }

        public Vector4 toObjectSpace(Vector4 v)
        {
            return this.inverse().toWorldSpace(v);
        }

        public Vector4 toWorldSpace(Vector4 v)
        {
            return new Vector4(rotation * new Vector3(v.x, v.y, v.z) + translation * v.w, v.w);
        }

        /**
         Transforms the point into world space.
         */
        public Vector3 pointToWorldSpace(Vector3 v)
        {
            return new Vector3(
                rotation.elt(0, 0) * v[0] + rotation.elt(0, 1) * v[1] + rotation.elt(0, 2) * v[2] + translation[0],
                rotation.elt(1, 0) * v[0] + rotation.elt(1, 1) * v[1] + rotation.elt(1, 2) * v[2] + translation[1],
                rotation.elt(2, 0) * v[0] + rotation.elt(2, 1) * v[1] + rotation.elt(2, 2) * v[2] + translation[2]);
        }

        /**
         Transforms the point into object space.
         */
        public Vector3 pointToObjectSpace(Vector3 v)
        {
            float[] p = new float[3];
            p[0] = v[0] - translation[0];
            p[1] = v[1] - translation[1];
            p[2] = v[2] - translation[2];
            return new Vector3(
                rotation.elt(0, 0) * p[0] + rotation.elt(1, 0) * p[1] + rotation.elt(2, 0) * p[2],
                rotation.elt(0, 1) * p[0] + rotation.elt(1, 1) * p[1] + rotation.elt(2, 1) * p[2],
                rotation.elt(0, 2) * p[0] + rotation.elt(1, 2) * p[1] + rotation.elt(2, 2) * p[2]);
        }

        /**
         Transforms the vector into world space (no translation).
         */
        public Vector3 vectorToWorldSpace(Vector3 v)
        {
            return rotation * v;
        }

        public Vector3 normalToWorldSpace(Vector3 v)
        {
            return rotation * v;
        }

        public Ray toObjectSpace(Ray r)
        {
            return Ray.fromOriginAndDirection(pointToObjectSpace(r.origin), vectorToObjectSpace(r.direction));
        }

        public Ray toWorldSpace(Ray r)
        {
            return Ray.fromOriginAndDirection(pointToWorldSpace(r.origin), vectorToWorldSpace(r.direction));
        }

        /**
         Transforms the vector into object space (no translation).
         */
        public Vector3 vectorToObjectSpace(Vector3 v)
        {
            return rotation.transpose() * v;
        }

        public Vector3 normalToObjectSpace(Vector3 v)
        {
            return rotation.transpose() * v;
        }

        public void pointToWorldSpace(Array<Vector3> v, out Array<Vector3> vout)
        {
            vout = new Array<Vector3>();
            vout.resize(v.size());

            for (int i = v.size() - 1; i >= 0; --i)
            {
                vout[i] = pointToWorldSpace(v[i]);
            }
        }

        public void normalToWorldSpace(Array<Vector3> v, out Array<Vector3> vout)
        {
            vout = new Array<Vector3>();
            vout.resize(v.size());

            for (int i = v.size() - 1; i >= 0; --i)
            {
                vout[i] = normalToWorldSpace(v[i]);
            }
        }

        public void vectorToWorldSpace(Array<Vector3> v, out Array<Vector3> vout)
        {
            vout = new Array<Vector3>();
            vout.resize(v.size());

            for (int i = v.size() - 1; i >= 0; --i)
            {
                vout[i] = vectorToWorldSpace(v[i]);
            }
        }

        public void pointToObjectSpace(Array<Vector3> v, out Array<Vector3> vout)
        {
            vout = new Array<Vector3>();
            vout.resize(v.size());

            for (int i = v.size() - 1; i >= 0; --i)
            {
                vout[i] = pointToObjectSpace(v[i]);
            }
        }

        public void normalToObjectSpace(Array<Vector3> v, out Array<Vector3> vout)
        {
            vout = new Array<Vector3>();
            vout.resize(v.size());

            for (int i = v.size() - 1; i >= 0; --i)
            {
                vout[i] = normalToObjectSpace(v[i]);
            }
        }

        public void vectorToObjectSpace(Array<Vector3> v, out Array<Vector3> vout)
        {
            vout = new Array<Vector3>();
            vout.resize(v.size());

            for (int i = v.size() - 1; i >= 0; --i)
            {
                vout[i] = vectorToObjectSpace(v[i]);
            }
        }

        public Box toWorldSpace(AABox b)
        {
            return toWorldSpace(b.toBox());
        }

        public Box toWorldSpace(Box b)
        {
            Box result = b;

            for (int i = 0; i < 8; i++)
            {
                result.setCornerAt(i, pointToWorldSpace(result.cornerAt(i)));
            }

            for (int i = 0; i < 3; i++)
            {
                result.setAxisAt(i, vectorToWorldSpace(result.axisAt(i)));
            }

            result.setCenter(pointToWorldSpace(result.getCenter()));

            return result;
        }

        public Cylinder toWorldSpace(Cylinder b)
        {
            return new Cylinder(
                pointToWorldSpace(b.getPoint1()),
                pointToWorldSpace(b.getPoint2()),
                b.radius());
        }

        public Capsule toWorldSpace(Capsule b)
        {
            return new Capsule(
                pointToWorldSpace(b.getPoint1()),
                pointToWorldSpace(b.getPoint2()),
                b.getRadius());
        }

        public Plane toWorldSpace(Plane p)
        {
            Vector3 N, P;
            double d;
            p.getEquation(out N, out d);
            P = N * (float)d;
            P = pointToWorldSpace(P);
            N = normalToWorldSpace(N);
            return new Plane(N, P);
        }

        public Sphere toWorldSpace(Sphere b)
        {
            return new Sphere(pointToWorldSpace(b.center), b.radius);
        }

        public Triangle toWorldSpace(Triangle t)
        {
            return new Triangle(pointToWorldSpace(t.vertex(0)),
                pointToWorldSpace(t.vertex(1)),
                pointToWorldSpace(t.vertex(2)));
        }

        public Box toObjectSpace(AABox b)
        {
            return toObjectSpace(b.toBox());
        }

        public Box toObjectSpace(Box b)
        {
            return inverse().toWorldSpace(b);
        }

        public Plane toObjectSpace(Plane p)
        {
            Vector3 N, P;
            double d;
            p.getEquation(out N, out d);
            P = N * (float)d;
            P = pointToObjectSpace(P);
            N = normalToObjectSpace(N);
            return new Plane(N, P);
        }

        public Sphere toObjectSpace(Sphere b)
        {
            return new Sphere(pointToObjectSpace(b.center), b.radius);
        }

        public Triangle toObjectSpace(Triangle t)
        {
            return new Triangle(pointToObjectSpace(t.vertex(0)),
                pointToObjectSpace(t.vertex(1)),
                pointToObjectSpace(t.vertex(2)));
        }

        /** Compose: create the transformation that is <I>other</I> followed by <I>this</I>.*/
        public static CoordinateFrame operator *(CoordinateFrame left, CoordinateFrame other)
        {
            return new CoordinateFrame(left.rotation * other.rotation,
                               left.pointToWorldSpace(other.translation));
        }

        public static CoordinateFrame operator +(CoordinateFrame left, Vector3 v)
        {
            return new CoordinateFrame(left.rotation, left.translation + v);
        }

        public static CoordinateFrame operator -(CoordinateFrame left, Vector3 v)
        {
            return new CoordinateFrame(left.rotation, left.translation - v);
        }

        public void lookAt(Vector3 target)
        {
            lookAt(target, Vector3.unitY());
        }

        public void lookAt(Vector3 target, Vector3 up)
        {
            up = up.direction();

            Vector3 look = (target - translation).direction();
            if (look.dot(up) > .99)
            {
                up = Vector3.unitX();
                if (look.dot(up) > .99)
                {
                    up = Vector3.unitY();
                }
            }

            up -= look * look.dot(up);
            up.unitize(1e-06f);

            Vector3 z = -look;
            Vector3 x = -z.cross(up);
            x.unitize(1e-06f);

            Vector3 y = z.cross(x);

            rotation.setColumn(0, x);
            rotation.setColumn(1, y);
            rotation.setColumn(2, z);
        }

        /** @deprecated See lookVector */
        public Vector3 getLookVector()
        {
            return rotation.getColumnVector(2) * zLookDirection;
        }

        /** The direction this camera is looking (its negative z axis)*/
        public Vector3 lookVector()
        {
            return rotation.getColumnVector(2) * zLookDirection;
        }

        /** Returns the ray starting at the camera origin travelling in direction CoordinateFrame::lookVector. */
        public Ray lookRay()
        {
            return Ray.fromOriginAndDirection(translation, lookVector());
        }

        /** Up direction for this camera (its y axis). */
        public Vector3 upVector()
        {
            return rotation.getColumnVector(1);
        }

        /**
         If a viewer looks along the look vector, this is the viewer's "left"
         @deprecated leftVector
         */
        public Vector3 getLeftVector()
        {
            return -rotation.getColumnVector(0);
        }

        /** @deprecated See rightVector */
        public Vector3 getRightVector()
        {
            return rotation.getColumnVector(0);
        }

        /**
         If a viewer looks along the look vector, this is the viewer's "left".
         Useful for strafing motions and building alternative coordinate frames.
         */
        public Vector3 leftVector()
        {
            return -rotation.getColumnVector(0);
        }

        public Vector3 rightVector()
        {
            return rotation.getColumnVector(0);
        }

        /**
         Linearly interpolates between two coordinate frames, using
         Quat::slerp for the rotations.
         */
        public CoordinateFrame lerp(CoordinateFrame other, float alpha)
        {
            if (alpha == 1.0f)
            {
                return other;
            }
            else if (alpha == 0.0f)
            {
                return this;
            }
            else
            {
                Quat q1 = new Quat(this.rotation);
                Quat q2 = new Quat(other.rotation);

                return new CoordinateFrame(
                    q1.slerp(q2, alpha).toRotationMatrix(),
                    this.translation * (1 - alpha) + other.translation * alpha);
            }
        }
    }
}
