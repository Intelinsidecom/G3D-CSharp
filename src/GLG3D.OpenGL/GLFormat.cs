/**
  @file glFormat.h

  A macro that maps G3D types to OpenGL formats
  (e.g. glFormat(Vector3) == GL_FLOAT).

  Use DECLARE_GLFORMATOF(MyType, GLType) at top-level to define
  glFormatOf values for your own classes.

  Used by the vertex array infrastructure.

 @maintainer Morgan McGuire, matrix@graphics3d.com
 @created 2005-02-01
 @edited  2006-02-24
 */

using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace G3D.OpenGL
{
    public static class GLFormat
    {
        public const uint FLOAT = (uint)All.Float;
        public const uint UNSIGNED_BYTE = (uint)All.UnsignedByte;
        public const uint SHORT = (uint)All.Short;
        public const int INT = (int)All.Int;

        private static readonly Dictionary<Type, uint> typeToGLFormat = new Dictionary<Type, uint>
        {
            { typeof(Vector2),        (uint)All.Float },
            { typeof(Vector3),        (uint)All.Float },
            { typeof(Vector4),        (uint)All.Float },
            { typeof(Vector3int16),   (uint)All.Short },
            { typeof(Vector2int16),   (uint)All.Short },
            { typeof(Color3),         (uint)All.Float },
            { typeof(Color4),         (uint)All.Float },
            { typeof(Color3uint8),    (uint)All.UnsignedByte },
            { typeof(Color4uint8),    (uint)All.UnsignedByte },
            { typeof(byte),           (uint)All.UnsignedByte },
            { typeof(ushort),         (uint)All.UnsignedShort },
            { typeof(uint),           (uint)All.UnsignedInt },
            { typeof(sbyte),          (uint)All.Byte },
            { typeof(short),          (uint)All.Short },
            { typeof(int),            (uint)All.Int },
            { typeof(float),          (uint)All.Float },
            { typeof(double),         (uint)All.Double },
        };

        /**
         Maps G3D types to OpenGL formats.
         Returns 0 (GL_NONE) for unsupported types.
         */
        public static uint glFormatOf<T>()
        {
            Type t = typeof(T);
            uint format;
            lock (typeToGLFormat)
            {
                if (typeToGLFormat.TryGetValue(t, out format))
                {
                    return format;
                }
            }
            return 0; // GL_NONE
        }

        /**
         Declares the underlying format (as will be returned by glFormatOf)
         of a type, in the spirit of DECLARE_GLFORMATOF in the original.
         */
        public static void declareGLFormatOf<T>(uint format)
        {
            lock (typeToGLFormat)
            {
                typeToGLFormat[typeof(T)] = format;
            }
        }

        /**
         Returns the size in bytes of a GL format.
         Returns 0 for unknown formats.
         */
        public static int sizeOfGLFormat(uint format)
        {
            switch (format)
            {
                case (uint)All.Byte:
                case (uint)All.UnsignedByte:
                    return 1;

                case (uint)All.Short:
                case (uint)All.UnsignedShort:
                    return 2;

                case (uint)All.Int:
                case (uint)All.UnsignedInt:
                case (uint)All.Float:
                    return 4;

                case (uint)All.Double:
                    return 8;

                default:
                    return 0;
            }
        }
    }
}