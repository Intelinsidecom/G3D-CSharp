/**
  @file TextureFormat.h

  Used to describe texture formats to the G3D::Texture class.
  Don't construct these; use the static constants provided.

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2003-05-23
  @edited  2006-01-11
*/

using System;

namespace G3D {

    /**
     Used to describe texture formats to the G3D::Texture class.
     Don't construct these; use the static constants provided.

     @deprecated In 7.00, all static members will become functions with the
     same name to avoid global initialization issues.
     */
    public sealed class TextureFormat {

        /**
         A hack to deal with app destruction. Set to false when static TextureFormats are deleted
         */
        public static bool valid = true;

        public enum Code {
            CODE_L8,
            CODE_L16,
            CODE_L16F,
            CODE_L32F,

            CODE_A8,
            CODE_A16,
            CODE_A16F,
            CODE_A32F,

            CODE_LA4,
            CODE_LA8,
            CODE_LA16,
            CODE_LA16F,
            CODE_LA32F,

            CODE_RGB5,
            CODE_RGB5A1,
            CODE_RGB8,
            CODE_RGB16,
            CODE_RGB16F,
            CODE_RGB32F,

            CODE_ARGB8,
            CODE_BGR8,

            CODE_RGBA8,
            CODE_RGBA16,
            CODE_RGBA16F,
            CODE_RGBA32F,

            CODE_BAYER_RGGB8,
            CODE_BAYER_GRBG8,
            CODE_BAYER_GBRG8,
            CODE_BAYER_BGGR8,
            CODE_BAYER_RGGB32F,
            CODE_BAYER_GRBG32F,
            CODE_BAYER_GBRG32F,
            CODE_BAYER_BGGR32F,

            CODE_HSV8,
            CODE_HSV32F,

            CODE_YUV8,
            CODE_YUV32F,

            CODE_RGB_DXT1,
            CODE_RGBA_DXT1,
            CODE_RGBA_DXT3,
            CODE_RGBA_DXT5,

            CODE_DEPTH16,
            CODE_DEPTH24,
            CODE_DEPTH32,

            CODE_STENCIL1,
            CODE_STENCIL4,
            CODE_STENCIL8,
            CODE_STENCIL16
            };

        public enum ColorSpace {
            COLOR_SPACE_NONE,
            COLOR_SPACE_RGB,
            COLOR_SPACE_HSV,
            COLOR_SPACE_YUV
        };

        public enum BayerPattern {
            BAYER_PATTERN_NONE,
            BAYER_PATTERN_RGGB,
            BAYER_PATTERN_GRBG,
            BAYER_PATTERN_GBRG,
            BAYER_PATTERN_BGGR
        };

        /**
         Number of channels (1 for a depth texture).
         */
        public readonly int numComponents;

        public readonly bool compressed;

        /** Useful for serializing */
        public readonly Code code;

        public readonly ColorSpace colorSpace;

        public readonly BayerPattern bayerPattern;

        /**
         The GL format equivalent to this one.  Zero if there is no equivalent.
         */
        public readonly uint OpenGLFormat;

        /**
         The GL base format equivalent to this one (e.g., GL_RGB, GL_ALPHA).  Zero if there is no equivalent.
         */
        public readonly uint OpenGLBaseFormat;

        public readonly int luminanceBits;

        /**
         Number of bits per texel storage for alpha values; Zero for compressed textures.
         */
        public readonly int alphaBits;

        /**
         Number of bits per texel storage for red values; Zero for compressed textures.
         */
        public readonly int redBits;

        /**
         Number of bits per texel storage for green values; Zero for compressed textures.
         */
        public readonly int greenBits;

        /**
         Number of bits per texel storage for blue values; Zero for compressed textures.
         */
        public readonly int blueBits;

        public readonly int stencilBits;

        /**
         Number of depth bits (for depth textures; e.g. shadow maps)
         */
        public readonly int depthBits;

        /**
         Sum of the per-channel bits, plus any additional bits required
         for byte alignment.
         */
        public readonly int packedBitsPerTexel;

        /**
         This may be greater than the sum of the per-channel bits
         because graphics cards need to pad to the nearest 1, 2, or
         4 bytes.
         */
        public readonly int hardwareBitsPerTexel;

        /**
         True if there is no alpha channel for this texture.
         */
        public readonly bool opaque;

        /** True if the bit depths specified are for float formats. */
        public readonly bool floatingPoint;

        /**
         The GL type used when uploading texels of this format.  The original
         always uploads with GL_UNSIGNED_BYTE; float formats require
         GL_FLOAT, so the type is derived from the format itself.
         */
        private readonly uint glType;

        /**
         Queries the number of bits in the screen's depth buffer.  Set by the
         graphics backend when it initializes; used by depth(SAME_AS_SCREEN).
         */
        public static Func<int> screenDepthBitsProvider = null;

        /**
         Queries the number of bits in the screen's stencil buffer.  Set by the
         graphics backend when it initializes; used by stencil(SAME_AS_SCREEN).
         */
        public static Func<int> screenStencilBitsProvider = null;

        private const uint GL_UNSIGNED_BYTE = 0x1401;
        private const uint GL_FLOAT = 0x1406;

        private TextureFormat(
            int             _numComponents,
            bool            _compressed,
            uint            _glFormat,
            uint            _glBaseFormat,
            int             _luminanceBits,
            int             _alphaBits,
            int             _redBits,
            int             _greenBits,
            int             _blueBits,
            int             _depthBits,
            int             _stencilBits,
            int             _hardwareBitsPerTexel,
            int             _packedBitsPerTexel,
            bool            _opaque,
            bool            _floatingPoint,
            Code            _code,
            ColorSpace      _colorSpace,
            BayerPattern    _bayerPattern = BayerPattern.BAYER_PATTERN_NONE) {
            numComponents = _numComponents;
            compressed = _compressed;
            code = _code;
            colorSpace = _colorSpace;
            bayerPattern = _bayerPattern;
            OpenGLFormat = _glFormat;
            OpenGLBaseFormat = _glBaseFormat;
            luminanceBits = _luminanceBits;
            alphaBits = _alphaBits;
            redBits = _redBits;
            greenBits = _greenBits;
            blueBits = _blueBits;
            stencilBits = _stencilBits;
            depthBits = _depthBits;
            packedBitsPerTexel = _packedBitsPerTexel;
            hardwareBitsPerTexel = _hardwareBitsPerTexel;
            opaque = _opaque;
            floatingPoint = _floatingPoint;
            glType = _floatingPoint ? GL_FLOAT : GL_UNSIGNED_BYTE;
        }

        /**
         Default argument for TextureFormat::depth.
         */
        public const int SAME_AS_SCREEN = 0;

        public static readonly TextureFormat L8;

        public static readonly TextureFormat L16;

        public static readonly TextureFormat L16F;

        public static readonly TextureFormat L32F;

        public static readonly TextureFormat A8;

        public static readonly TextureFormat A16;

        public static readonly TextureFormat A16F;

        public static readonly TextureFormat A32F;

        public static readonly TextureFormat LA4;

        public static readonly TextureFormat LA8;

        public static readonly TextureFormat LA16;

        public static readonly TextureFormat LA16F;

        public static readonly TextureFormat LA32F;

        public static readonly TextureFormat RGB5;

        public static readonly TextureFormat RGB5A1;

        public static readonly TextureFormat RGB8;

        public static readonly TextureFormat RGB16;

        public static readonly TextureFormat RGB16F;

        public static readonly TextureFormat RGB32F;

        public static readonly TextureFormat RGBA8;

        public static readonly TextureFormat RGBA16;

        public static readonly TextureFormat RGBA16F;

        public static readonly TextureFormat RGBA32F;

        public static readonly TextureFormat RGB_DXT1;

        public static readonly TextureFormat RGBA_DXT1;

        public static readonly TextureFormat RGBA_DXT3;

        public static readonly TextureFormat RGBA_DXT5;

        public static readonly TextureFormat DEPTH16;

        public static readonly TextureFormat DEPTH24;

        public static readonly TextureFormat DEPTH32;

        public static readonly TextureFormat STENCIL1;

        public static readonly TextureFormat STENCIL4;

        public static readonly TextureFormat STENCIL8;

        public static readonly TextureFormat STENCIL16;

        /**
         NULL pointer; indicates that the texture class should choose
         either RGBA8 or RGB8 depending on the presence of an alpha channel
         in the input.
         */
        public static readonly TextureFormat AUTO = null;

        static TextureFormat() {
            const bool INT = false;
            const bool FLOAT = true;
            const bool OPAQUEx = true;

            // GL constants
            const uint GL_ALPHA = 0x1906;
            const uint GL_RGB = 0x1907;
            const uint GL_RGBA = 0x1908;
            const uint GL_LUMINANCE = 0x1909;
            const uint GL_LUMINANCE_ALPHA = 0x190A;
            const uint GL_DEPTH_COMPONENT = 0x1902;
            const uint GL_STENCIL_INDEX_EXT = 0x1901;

            const uint GL_ALPHA8 = 0x803C;
            const uint GL_ALPHA16 = 0x803E;

            const uint GL_LUMINANCE8 = 0x8040;
            const uint GL_LUMINANCE16 = 0x8042;
            const uint GL_LUMINANCE4_ALPHA4 = 0x8043;
            const uint GL_LUMINANCE8_ALPHA8 = 0x8045;
            const uint GL_LUMINANCE16_ALPHA16 = 0x8048;

            const uint GL_RGB5 = 0x8050;
            const uint GL_RGB8 = 0x8051;
            const uint GL_RGB16 = 0x8053;
            const uint GL_RGB5_A1 = 0x8057;
            const uint GL_RGBA8 = 0x8058;
            const uint GL_RGBA16 = 0x805A;

            const uint GL_DEPTH_COMPONENT16_ARB = 0x81A5;
            const uint GL_DEPTH_COMPONENT24_ARB = 0x81A6;
            const uint GL_DEPTH_COMPONENT32_ARB = 0x81A7;

            const uint GL_COMPRESSED_RGB_S3TC_DXT1_EXT = 0x83F0;
            const uint GL_COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1;
            const uint GL_COMPRESSED_RGBA_S3TC_DXT3_EXT = 0x83F2;
            const uint GL_COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3;

            const uint GL_STENCIL_INDEX1_EXT = 0x8D46;
            const uint GL_STENCIL_INDEX4_EXT = 0x8D47;
            const uint GL_STENCIL_INDEX8_EXT = 0x8D48;
            const uint GL_STENCIL_INDEX16_EXT = 0x8D49;

            const uint GL_ALPHA16F_ARB = 0x881C;
            const uint GL_ALPHA32F_ARB = 0x8816;
            const uint GL_LUMINANCE16F_ARB = 0x881E;
            const uint GL_LUMINANCE32F_ARB = 0x8818;
            const uint GL_LUMINANCE_ALPHA16F_ARB = 0x881F;
            const uint GL_LUMINANCE_ALPHA32F_ARB = 0x8819;
            const uint GL_RGB16F_ARB = 0x881B;
            const uint GL_RGB32F_ARB = 0x8815;
            const uint GL_RGBA16F_ARB = 0x881A;
            const uint GL_RGBA32F_ARB = 0x8814;

            L8 = new TextureFormat(1, false, GL_LUMINANCE8, GL_LUMINANCE, 8, 0, 0, 0, 0, 0, 0, 8, 8, OPAQUEx, INT, Code.CODE_L8, ColorSpace.COLOR_SPACE_NONE);

            L16 = new TextureFormat(1, false, GL_LUMINANCE16, GL_LUMINANCE, 16, 0, 0, 0, 0, 0, 0, 16, 16, OPAQUEx, INT, Code.CODE_L16, ColorSpace.COLOR_SPACE_NONE);

            L16F = new TextureFormat(1, false, GL_LUMINANCE16F_ARB, GL_LUMINANCE, 16, 0, 0, 0, 0, 0, 0, 16, 16, OPAQUEx, FLOAT, Code.CODE_L16F, ColorSpace.COLOR_SPACE_NONE);

            L32F = new TextureFormat(1, false, GL_LUMINANCE32F_ARB, GL_LUMINANCE, 32, 0, 0, 0, 0, 0, 0, 32, 32, OPAQUEx, FLOAT, Code.CODE_L32F, ColorSpace.COLOR_SPACE_NONE);

            A8 = new TextureFormat(1, false, GL_ALPHA8, GL_ALPHA, 0, 8, 0, 0, 0, 0, 0, 8, 8, !OPAQUEx, INT, Code.CODE_A8, ColorSpace.COLOR_SPACE_NONE);

            A16 = new TextureFormat(1, false, GL_ALPHA16, GL_ALPHA, 0, 16, 0, 0, 0, 0, 0, 16, 16, !OPAQUEx, INT, Code.CODE_A16, ColorSpace.COLOR_SPACE_NONE);

            A16F = new TextureFormat(1, false, GL_ALPHA16F_ARB, GL_ALPHA, 0, 16, 0, 0, 0, 0, 0, 16, 16, !OPAQUEx, FLOAT, Code.CODE_A16F, ColorSpace.COLOR_SPACE_NONE);

            A32F = new TextureFormat(1, false, GL_ALPHA32F_ARB, GL_ALPHA, 0, 32, 0, 0, 0, 0, 0, 32, 32, !OPAQUEx, FLOAT, Code.CODE_A32F, ColorSpace.COLOR_SPACE_NONE);

            LA4 = new TextureFormat(2, false, GL_LUMINANCE4_ALPHA4, GL_LUMINANCE_ALPHA, 4, 4, 0, 0, 0, 0, 0, 8, 8, !OPAQUEx, INT, Code.CODE_LA4, ColorSpace.COLOR_SPACE_NONE);

            LA8 = new TextureFormat(2, false, GL_LUMINANCE8_ALPHA8, GL_LUMINANCE_ALPHA, 8, 8, 0, 0, 0, 0, 0, 16, 16, !OPAQUEx, INT, Code.CODE_LA8, ColorSpace.COLOR_SPACE_NONE);

            LA16 = new TextureFormat(2, false, GL_LUMINANCE16_ALPHA16, GL_LUMINANCE_ALPHA, 16, 16, 0, 0, 0, 0, 0, 16 * 2, 16 * 2, !OPAQUEx, INT, Code.CODE_LA16, ColorSpace.COLOR_SPACE_NONE);

            LA16F = new TextureFormat(2, false, GL_LUMINANCE_ALPHA16F_ARB, GL_LUMINANCE_ALPHA, 16, 16, 0, 0, 0, 0, 0, 16 * 2, 16 * 2, !OPAQUEx, FLOAT, Code.CODE_LA16F, ColorSpace.COLOR_SPACE_NONE);

            LA32F = new TextureFormat(2, false, GL_LUMINANCE_ALPHA32F_ARB, GL_LUMINANCE_ALPHA, 32, 32, 0, 0, 0, 0, 0, 32 * 2, 32 * 2, !OPAQUEx, FLOAT, Code.CODE_LA32F, ColorSpace.COLOR_SPACE_NONE);

            RGB5 = new TextureFormat(3, false, GL_RGB5, GL_RGBA, 0, 0, 5, 5, 5, 0, 0, 16, 16, OPAQUEx, INT, Code.CODE_RGB5, ColorSpace.COLOR_SPACE_RGB);

            RGB5A1 = new TextureFormat(4, false, GL_RGB5_A1, GL_RGBA, 0, 1, 5, 5, 5, 0, 0, 16, 16, OPAQUEx, INT, Code.CODE_RGB5A1, ColorSpace.COLOR_SPACE_RGB);

            RGB8 = new TextureFormat(3, false, GL_RGB8, GL_RGB, 0, 0, 8, 8, 8, 0, 0, 24, 32, OPAQUEx, INT, Code.CODE_RGB8, ColorSpace.COLOR_SPACE_RGB);

            RGB16 = new TextureFormat(3, false, GL_RGB16, GL_RGB, 0, 0, 16, 16, 16, 0, 0, 16 * 3, 16 * 3, OPAQUEx, INT, Code.CODE_RGB16, ColorSpace.COLOR_SPACE_RGB);

            RGB16F = new TextureFormat(3, false, GL_RGB16F_ARB, GL_RGB, 0, 0, 16, 16, 16, 0, 0, 16 * 3, 16 * 3, OPAQUEx, FLOAT, Code.CODE_RGB16F, ColorSpace.COLOR_SPACE_RGB);

            RGB32F = new TextureFormat(3, false, GL_RGB32F_ARB, GL_RGB, 0, 0, 32, 32, 32, 0, 0, 32 * 3, 32 * 3, OPAQUEx, FLOAT, Code.CODE_RGB32F, ColorSpace.COLOR_SPACE_RGB);

            RGBA8 = new TextureFormat(4, false, GL_RGBA8, GL_RGBA, 0, 8, 8, 8, 8, 0, 0, 32, 32, false, INT, Code.CODE_RGBA8, ColorSpace.COLOR_SPACE_RGB);

            RGBA16 = new TextureFormat(4, false, GL_RGBA16, GL_RGBA, 0, 16, 16, 16, 16, 0, 0, 16 * 4, 16 * 4, false, INT, Code.CODE_RGBA16, ColorSpace.COLOR_SPACE_RGB);

            RGBA16F = new TextureFormat(4, false, GL_RGBA16F_ARB, GL_RGBA, 0, 16, 16, 16, 16, 0, 0, 16 * 4, 16 * 4, false, FLOAT, Code.CODE_RGB16F, ColorSpace.COLOR_SPACE_RGB);

            RGBA32F = new TextureFormat(4, false, GL_RGBA32F_ARB, GL_RGBA, 0, 32, 32, 32, 32, 0, 0, 32 * 4, 32 * 4, false, FLOAT, Code.CODE_RGBA32F, ColorSpace.COLOR_SPACE_RGB);

            RGB_DXT1 = new TextureFormat(3, true, GL_COMPRESSED_RGB_S3TC_DXT1_EXT, GL_RGB, 0, 0, 0, 0, 0, 0, 0, 64, 64, OPAQUEx, INT, Code.CODE_RGB_DXT1, ColorSpace.COLOR_SPACE_RGB);

            RGBA_DXT1 = new TextureFormat(4, true, GL_COMPRESSED_RGBA_S3TC_DXT1_EXT, GL_RGBA, 0, 0, 0, 0, 0, 0, 0, 64, 64, !OPAQUEx, INT, Code.CODE_RGBA_DXT1, ColorSpace.COLOR_SPACE_RGB);

            RGBA_DXT3 = new TextureFormat(4, true, GL_COMPRESSED_RGBA_S3TC_DXT3_EXT, GL_RGBA, 0, 0, 0, 0, 0, 0, 0, 128, 128, !OPAQUEx, INT, Code.CODE_RGBA_DXT3, ColorSpace.COLOR_SPACE_RGB);

            RGBA_DXT5 = new TextureFormat(4, true, GL_COMPRESSED_RGBA_S3TC_DXT5_EXT, GL_RGBA, 0, 0, 0, 0, 0, 0, 0, 128, 128, !OPAQUEx, INT, Code.CODE_RGBA_DXT5, ColorSpace.COLOR_SPACE_RGB);

            DEPTH16 = new TextureFormat(1, false, GL_DEPTH_COMPONENT16_ARB, GL_DEPTH_COMPONENT, 0, 0, 0, 0, 0, 0, 16, 16, 16, !OPAQUEx, INT, Code.CODE_DEPTH16, ColorSpace.COLOR_SPACE_NONE);

            DEPTH24 = new TextureFormat(1, false, GL_DEPTH_COMPONENT24_ARB, GL_DEPTH_COMPONENT, 0, 0, 0, 0, 0, 0, 24, 32, 24, !OPAQUEx, INT, Code.CODE_DEPTH24, ColorSpace.COLOR_SPACE_NONE);

            DEPTH32 = new TextureFormat(1, false, GL_DEPTH_COMPONENT32_ARB, GL_DEPTH_COMPONENT, 0, 0, 0, 0, 0, 0, 32, 32, 32, !OPAQUEx, INT, Code.CODE_DEPTH32, ColorSpace.COLOR_SPACE_NONE);

            // These formats are for use with Renderbuffers only!
            STENCIL1 = new TextureFormat(1, false, GL_STENCIL_INDEX1_EXT, GL_STENCIL_INDEX_EXT, 0, 0, 0, 0, 0, 0, 1, 1, 1, !OPAQUEx, INT, Code.CODE_STENCIL1, ColorSpace.COLOR_SPACE_NONE);

            STENCIL4 = new TextureFormat(1, false, GL_STENCIL_INDEX4_EXT, GL_STENCIL_INDEX_EXT, 0, 0, 0, 0, 0, 0, 4, 4, 4, !OPAQUEx, INT, Code.CODE_STENCIL4, ColorSpace.COLOR_SPACE_NONE);

            STENCIL8 = new TextureFormat(1, false, GL_STENCIL_INDEX8_EXT, GL_STENCIL_INDEX_EXT, 0, 0, 0, 0, 0, 0, 8, 8, 8, !OPAQUEx, INT, Code.CODE_STENCIL8, ColorSpace.COLOR_SPACE_NONE);

            STENCIL16 = new TextureFormat(1, false, GL_STENCIL_INDEX16_EXT, GL_STENCIL_INDEX_EXT, 0, 0, 0, 0, 0, 0, 16, 16, 16, !OPAQUEx, INT, Code.CODE_STENCIL16, ColorSpace.COLOR_SPACE_NONE);
        }

        /**
         Returns DEPTH16, DEPTH24, or DEPTH32 according to the bits
         specified.
         */
        public static TextureFormat depth(int depthBits) {

            if (depthBits == SAME_AS_SCREEN) {
                // Detect screen depth
                if (screenDepthBitsProvider == null) {
                    throw new NotSupportedException("The screen depth bit count is not known; the graphics backend must set TextureFormat.screenDepthBitsProvider.");
                }
                depthBits = screenDepthBitsProvider();
            }

            switch (depthBits) {
            case 16:
                return DEPTH16;

            case 24:
                return DEPTH24;

            case 32:
                return DEPTH32;

            default:
                Debug.debugAssertM(false, "Depth must be 16, 24, or 32.");
                return DEPTH32;
            }
        }

        /**
         Returns DEPTH16, DEPTH24, or DEPTH32 according to the bits
         specified.
         */
        public static TextureFormat depth() {
            return depth(SAME_AS_SCREEN);
        }

        /**
         Returns STENCIL1, STENCIL4, STENCIL8 or STENCIL16 according to the bits
         specified.
         */
        public static TextureFormat stencil(int bits) {

            if (bits == SAME_AS_SCREEN) {
                // Detect screen depth
                if (screenStencilBitsProvider == null) {
                    throw new NotSupportedException("The screen stencil bit count is not known; the graphics backend must set TextureFormat.screenStencilBitsProvider.");
                }
                bits = screenStencilBitsProvider();
            }

            switch (bits) {
            case 1:
                return STENCIL1;

            case 4:
                return STENCIL4;

            case 8:
                return STENCIL8;

            case 16:
                return STENCIL16;

            default:
                Debug.debugAssertM(false, "Stencil must be 1, 4, 8 or 16.");
                return STENCIL16;
            }
        }

        /**
         Returns STENCIL1, STENCIL4, STENCIL8 or STENCIL16 according to the bits
         specified.
         */
        public static TextureFormat stencil() {
            return stencil(SAME_AS_SCREEN);
        }

        /**
         Returns the matching TextureFormat identified by the Code.
         */
        public static TextureFormat fromCode(Code code) {
            switch (code) {
            case Code.CODE_L8:
                return L8;
            case Code.CODE_L16:
                return L16;
            case Code.CODE_L16F:
                return L16F;
            case Code.CODE_L32F:
                return L32F;

            case Code.CODE_A8:
                return A8;
            case Code.CODE_A16:
                return A16;
            case Code.CODE_A16F:
                return A16F;
            case Code.CODE_A32F:
                return A32F;

            case Code.CODE_LA4:
                return LA4;
            case Code.CODE_LA8:
                return LA8;
            case Code.CODE_LA16:
                return LA16;
            case Code.CODE_LA16F:
                return LA16F;
            case Code.CODE_LA32F:
                return LA32F;

            case Code.CODE_RGB5:
                return RGB5;
            case Code.CODE_RGB5A1:
                return RGB5A1;
            case Code.CODE_RGB8:
                return RGB8;
            case Code.CODE_RGB16:
                return RGB16;
            case Code.CODE_RGB16F:
                return RGB16F;
            case Code.CODE_RGB32F:
                return RGB32F;

            case Code.CODE_ARGB8:
                Debug.debugAssertM(AUTO != null, "Unsupported TextureFormat at decoding.");
                return AUTO;
            case Code.CODE_BGR8:
                Debug.debugAssertM(AUTO != null, "Unsupported TextureFormat at decoding.");
                return AUTO;

            case Code.CODE_RGBA8:
                return RGBA8;
            case Code.CODE_RGBA16:
                return RGBA16;
            case Code.CODE_RGBA16F:
                return RGBA16F;
            case Code.CODE_RGBA32F:
                return RGBA32F;

            case Code.CODE_BAYER_RGGB8:
            case Code.CODE_BAYER_GRBG8:
            case Code.CODE_BAYER_GBRG8:
            case Code.CODE_BAYER_BGGR8:
            case Code.CODE_BAYER_RGGB32F:
            case Code.CODE_BAYER_GRBG32F:
            case Code.CODE_BAYER_GBRG32F:
            case Code.CODE_BAYER_BGGR32F:

            case Code.CODE_HSV8:
            case Code.CODE_HSV32F:

            case Code.CODE_YUV8:
            case Code.CODE_YUV32F:
                Debug.debugAssertM(AUTO != null, "Unsupported TextureFormat at decoding.");
                return AUTO;

            case Code.CODE_RGB_DXT1:
                return RGB_DXT1;
            case Code.CODE_RGBA_DXT1:
                return RGBA_DXT1;
            case Code.CODE_RGBA_DXT3:
                return RGBA_DXT3;
            case Code.CODE_RGBA_DXT5:
                return RGBA_DXT5;

            case Code.CODE_DEPTH16:
                return DEPTH16;
            case Code.CODE_DEPTH24:
                return DEPTH24;
            case Code.CODE_DEPTH32:
                return DEPTH32;

            case Code.CODE_STENCIL1:
                return STENCIL1;
            case Code.CODE_STENCIL4:
                return STENCIL4;
            case Code.CODE_STENCIL8:
                return STENCIL8;
            case Code.CODE_STENCIL16:
                return STENCIL16;

            default:
                return null;
            }
        }

        public uint getGLInternalFormat() {
            return OpenGLFormat;
        }

        public uint getGLFormat() {
            return OpenGLBaseFormat;
        }

        public uint getGLType() {
            return glType;
        }

        public int getBitsPerComponent() {
            return redBits;
        }

        public int getComponents() {
            return numComponents;
        }

        public bool getIsDepth() {
            return OpenGLBaseFormat == 0x1902; // GL_DEPTH_COMPONENT
        }

        public bool getIsStencil() {
            return OpenGLBaseFormat == 0x1901; // GL_STENCIL_INDEX
        }

        public bool getIsCompressed() {
            return compressed;
        }

        public override string ToString() {
            return code.ToString();
        }

        public override bool Equals(object obj) {
            TextureFormat other = obj as TextureFormat;
            if (other == null) {
                return false;
            }
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode() {
            return global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        }

        /**
         The original returns the address of the format; the managed port hashes
         the format code so that equal formats have equal hash codes.
         */
        public static uint hashCode(TextureFormat f) {
            if (f == null) {
                return 0;
            }
            return (uint)f.GetHashCode();
        }
    }
}
