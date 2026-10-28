/**
  @file vectorMath.h

  Function aliases for popular vector methods.

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created: 2001-06-02
  @edited:  2004-02-02
  Copyright 2000-2004, Morgan McGuire.
  All rights reserved.
 */

using System;

namespace G3D
{
    public static class VectorMath
    {
        public static Matrix4 mul(Matrix4 a, Matrix4 b)
        {
            return a * b;
        }

        public static Vector4 mul(Matrix4 m, Vector4 v)
        {
            return m * v;
        }

        public static Vector3 mul(Matrix3 m, Vector3 v)
        {
            return m * v;
        }

        public static Matrix3 mul(Matrix3 a, Matrix3 b)
        {
            return a * b;
        }

        public static float dot(Vector2 a, Vector2 b)
        {
            return a.dot(b);
        }

        public static float dot(Vector3 a, Vector3 b)
        {
            return a.dot(b);
        }

        public static float dot(Vector4 a, Vector4 b)
        {
            return a.dot(b);
        }

        public static Vector2 normalize(Vector2 v)
        {
            return v / v.length();
        }

        public static Vector3 normalize(Vector3 v)
        {
            return v / v.magnitude();
        }

        public static Vector4 normalize(Vector4 v)
        {
            return v / v.length();
        }

        public static Vector2 abs(Vector2 v)
        {
            return new Vector2(Math.Abs(v.x), Math.Abs(v.y));
        }

        public static Vector3 abs(Vector3 v)
        {
            return new Vector3(Math.Abs(v.x), Math.Abs(v.y), Math.Abs(v.z));
        }

        public static Vector4 abs(Vector4 v)
        {
            return new Vector4(Math.Abs(v.x), Math.Abs(v.y), Math.Abs(v.z), Math.Abs(v.w));
        }

        public static bool all(Vector2 v)
        {
            return (v.x != 0) && (v.y != 0);
        }

        public static bool all(Vector3 v)
        {
            return (v.x != 0) && (v.y != 0) && (v.z != 0);
        }

        public static bool all(Vector4 v)
        {
            return (v.x != 0) && (v.y != 0) && (v.z != 0) && (v.w != 0);
        }

        public static bool any(Vector2 v)
        {
            return (v.x != 0) || (v.y != 0);
        }

        public static bool any(Vector3 v)
        {
            return (v.x != 0) || (v.y != 0) || (v.z != 0);
        }

        public static bool any(Vector4 v)
        {
            return (v.x != 0) || (v.y != 0) || (v.z != 0) || (v.w != 0);
        }

        public static Vector2 clamp(Vector2 v, Vector2 a, Vector2 b)
        {
            return v.clamp(a, b);
        }

        public static Vector3 clamp(Vector3 v, Vector3 a, Vector3 b)
        {
            return v.clamp(a, b);
        }

        public static Vector4 clamp(Vector4 v, Vector4 a, Vector4 b)
        {
            return v.clamp(a, b);
        }

        public static Vector2 lerp(Vector2 v1, Vector2 v2, float f)
        {
            return v1.lerp(v2, f);
        }

        public static Vector3 lerp(Vector3 v1, Vector3 v2, float f)
        {
            return v1.lerp(v2, f);
        }

        public static Vector4 lerp(Vector4 v1, Vector4 v2, float f)
        {
            return v1.lerp(v2, f);
        }

        public static Color3 lerp(Color3 v1, Color3 v2, float f)
        {
            return v1.lerp(v2, f);
        }

        public static Vector3 cross(Vector3 v1, Vector3 v2)
        {
            return v1.cross(v2);
        }

        public static double determinant(Matrix3 m)
        {
            return m.determinant();
        }

        public static double determinant(Matrix4 m)
        {
            return m.determinant();
        }

        public static Vector2 min(Vector2 v1, Vector2 v2)
        {
            return v1.min(v2);
        }

        public static Vector3 min(Vector3 v1, Vector3 v2)
        {
            return v1.min(v2);
        }

        public static Vector4 min(Vector4 v1, Vector4 v2)
        {
            return v1.min(v2);
        }

        public static Color3 min(Color3 v1, Color3 v2)
        {
            return v1.min(v2);
        }

        public static Color4 min(Color4 v1, Color4 v2)
        {
            return v1.min(v2);
        }

        public static Vector2 max(Vector2 v1, Vector2 v2)
        {
            return v1.max(v2);
        }

        public static Vector3 max(Vector3 v1, Vector3 v2)
        {
            return v1.max(v2);
        }

        public static Vector4 max(Vector4 v1, Vector4 v2)
        {
            return v1.max(v2);
        }

        public static Color3 max(Color3 v1, Color3 v2)
        {
            return v1.max(v2);
        }

        public static Color4 max(Color4 v1, Color4 v2)
        {
            return v1.max(v2);
        }

        public static Vector2 sign(Vector2 v)
        {
            return new Vector2((float)g3dmath.sign(v.x), (float)g3dmath.sign(v.y));
        }

        public static Vector3 sign(Vector3 v)
        {
            return new Vector3((float)g3dmath.sign(v.x), (float)g3dmath.sign(v.y), (float)g3dmath.sign(v.z));
        }

        public static Vector4 sign(Vector4 v)
        {
            return new Vector4((float)g3dmath.sign(v.x), (float)g3dmath.sign(v.y), (float)g3dmath.sign(v.z), (float)g3dmath.sign(v.w));
        }

        public static float length(float v)
        {
            return Math.Abs(v);
        }

        public static float length(Vector2 v)
        {
            return v.length();
        }

        public static float length(Vector3 v)
        {
            return v.magnitude();
        }

        public static float length(Vector4 v)
        {
            return v.length();
        }

        /**
         Computes the log of each component.  Useful for
         inverting the monitor gamma function or simulating
         perceptual response.
         */
        public static Color3 log(Color3 c)
        {
            return new Color3((float)Math.Log(c.r), (float)Math.Log(c.g), (float)Math.Log(c.b));
        }
    }
}
