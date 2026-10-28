/**
  @file glFormat.h

  Maps the original G3D 6.09 texture formats onto the OpenGL core profile
  internal, base, and type values.

  The original TextureFormat stored legacy OpenGL enumerants such as
  GL_LUMINANCE8 and GL_ALPHA8, which were removed from the OpenGL core
  profile.  This translation keeps the original format descriptions
  intact while presenting formats the current driver accepts.

  @maintainer Morgan McGuire, matrix@graphics3d.com
*/

using System;

namespace G3D.OpenGL
{
    internal static class GLTextureFormatMap
    {
        public const uint GL_RED = 0x1903;
        public const uint GL_RG = 0x8227;
        public const uint GL_RGB = 0x1907;
        public const uint GL_RGBA = 0x1908;
        public const uint GL_DEPTH_COMPONENT = 0x1902;
        public const uint GL_STENCIL_INDEX = 0x1901;

        public const uint GL_R8 = 0x8229;
        public const uint GL_R16 = 0x822D;
        public const uint GL_R16F = 0x822A;
        public const uint GL_R32F = 0x822E;
        public const uint GL_RG8 = 0x822B;
        public const uint GL_RG16 = 0x822C;
        public const uint GL_RG16F = 0x822F;
        public const uint GL_RG32F = 0x8230;

        public const uint GL_UNSIGNED_BYTE = 0x1401;
        public const uint GL_FLOAT = 0x1406;

        /**
          The internal (sized) format used when allocating the texture.
         */
        public static uint internalFormat(TextureFormat format)
        {
            if (format == null)
            {
                return 0x8058; // GL_RGBA8
            }

            switch (format.code)
            {
                case TextureFormat.Code.CODE_L8:
                    return GL_R8;
                case TextureFormat.Code.CODE_A8:
                    return GL_R8;
                case TextureFormat.Code.CODE_L16:
                case TextureFormat.Code.CODE_A16:
                    return GL_R16;
                case TextureFormat.Code.CODE_L16F:
                case TextureFormat.Code.CODE_A16F:
                    return GL_R16F;
                case TextureFormat.Code.CODE_L32F:
                case TextureFormat.Code.CODE_A32F:
                    return GL_R32F;
                case TextureFormat.Code.CODE_LA4:
                case TextureFormat.Code.CODE_LA8:
                    return GL_RG8;
                case TextureFormat.Code.CODE_LA16:
                    return GL_RG16;
                case TextureFormat.Code.CODE_LA16F:
                    return GL_RG16F;
                case TextureFormat.Code.CODE_LA32F:
                    return GL_RG32F;
                default:
                    return format.OpenGLFormat;
            }
        }

        /**
          The base format of the texel data.
         */
        public static uint baseFormat(TextureFormat format)
        {
            if (format == null)
            {
                return GL_RGBA;
            }

            switch (format.code)
            {
                case TextureFormat.Code.CODE_L8:
                case TextureFormat.Code.CODE_A8:
                case TextureFormat.Code.CODE_L16:
                case TextureFormat.Code.CODE_A16:
                case TextureFormat.Code.CODE_L16F:
                case TextureFormat.Code.CODE_A16F:
                case TextureFormat.Code.CODE_L32F:
                case TextureFormat.Code.CODE_A32F:
                    return GL_RED;
                case TextureFormat.Code.CODE_LA4:
                case TextureFormat.Code.CODE_LA8:
                case TextureFormat.Code.CODE_LA16:
                case TextureFormat.Code.CODE_LA16F:
                case TextureFormat.Code.CODE_LA32F:
                    return GL_RG;
                default:
                    return format.OpenGLBaseFormat;
            }
        }

        /**
          The type of the texel data.
         */
        public static uint type(TextureFormat format)
        {
            if (format == null)
            {
                return GL_UNSIGNED_BYTE;
            }
            return format.getGLType();
        }
    }
}
