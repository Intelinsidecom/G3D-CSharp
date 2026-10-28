/**
  @file Texture.h

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2001-02-28
  @edited  2006-02-28
*/

using System;
using System.Collections.Generic;
using G3D;

namespace G3D
{

    /**
      Reference-counted smart pointer to Texture.
      In C# this is implicit via reference semantics.
    */
    // typedef ReferenceCountedPointer<class Texture> TextureRef;

    /**
      Abstraction of OpenGL textures.  This class can be used with raw OpenGL,
      without RenderDevice.  G3D::Texture supports all of the image formats
      that G3D::GImage can load, and DDS (DirectX textures), and Quake-style cube
      maps.

      If you enable texture compression, textures will be compressed on the fly.
      This can be slow (up to a second).

      Unless DIM_2D_RECT, DIM_2D_NPOT, DIM_CUBE_MAP_NPOT are used, the texture is automatically
      scaled to the next power of 2 along each dimension to meet hardware requirements, if not
      already a power of 2.  However, DIM_2D_NPOT and DIM_CUBE_MAP_NPOT will safely fallback to
      POT requirements if the ARB_non_power_of_two extension is not supported. Developers can
      check if this will happen by calling GLCaps::supports_GL_ARB_texture_non_power_of_two().

      Textures are loaded so that (0, 0) is the upper-left corner of the image.
      If you set the invertY flag, RenderDevice will automatically turn them upside
      down when rendering to allow a (0, 0) <B>lower</B>-left corner.  If you
      aren't using RenderDevice, you must change the texture matrix to have
      a -1 in the Y column yourself.

      DIM_2D_RECT requires the GL_EXT_texture_rectangle extension.
      Texture compression requires the EXT_texture_compression_s3tc extensions.
      You can either query OpenGL for whether these are supported or
      use the G3D::GLCaps facility for doing so.

      G3D::Texture can be used with straight OpenGL, without G3D::RenderDevice, as
      follows:

      <PRE>
       TextureRef texture = new Texture("logo.jpg");

       ...

       GLint u = texture->getOpenGLTextureTarget();
       glEnable(u);
       glBindTexture(u, texture->getOpenGLID());
      </PRE>

      To use Texture with RenderDevice:

      <PRE>
       TextureRef texture = new Texture("logo.jpg");
       ...
       renderDevice->setTexture(0, texture);
       // (to disable: renderDevice->setTexture(0, NULL);)
      </PRE>


      3D MIP Maps are not supported because gluBuild3DMipMaps is not in all GLU implementations.

      See G3D::RenderDevice::setBlendFunc for important information about turning on alpha blending.
    */
    public sealed class Texture : ReferenceCountedObject
    {
        /**
          DIM_2D_NPOT and DIM_CUBE_MAP_NPOT attempt to use ARB_non_power_of_two texture support with POT fallback.
        */
        public enum Dimension
        {
            DIM_2D = 2,
            DIM_2D_RECT = 4,
            DIM_CUBE_MAP = 5,
            DIM_2D_NPOT = 6,
            DIM_CUBE_MAP_NPOT = 7
        }

        /**
          TRANSPARENT_BORDER provides a border of Color4(0,0,0,0) and clamps to it.
        */
        public enum WrapMode
        {
            TILE = 1,
            CLAMP = 0,
            TRANSPARENT_BORDER = 2
        }

        /**
          Trilinear mipmap is the best quality (and frequently fastest) mode.  The no-mipmap modes conserve memory.
          Non-interpolating ("Nearest") modes are generally useful only when packing lookup tables into textures
          for shaders.

          @deprecated NO_INTERPOLATION will be replaced by NEAREST_NO_MIPMAP in a future release.
        */
        public enum InterpolateMode
        {
            TRILINEAR_MIPMAP = 3,
            BILINEAR_MIPMAP = 4,
            NEAREST_MIPMAP = 5,

            BILINEAR_NO_MIPMAP = 2,
            NEAREST_NO_MIPMAP = 6,
            NO_INTERPOLATION = 1
        }

        /**
          A depth texture can automatically perform the depth comparison used for shadow mapping
          on a texture lookup.  The result of a texture lookup is thus the shadowed amount
          (which will be percentage closer filtered on newer hardware) and <I>not</I> the
          actual depth from the light's point of view.

          This combines GL_TEXTURE_COMPARE_MODE_ARB and GL_TEXTURE_COMPARE_FUNC_ARB from
          http://www.nvidia.com/dev_content/nvopenglspecs/GL_ARB_shadow.txt

          For best results on percentage closer hardware (GeForceFX and Radeon9xxx or better),
          create shadow maps as depth textures with BILINEAR_NO_MIPMAP sampling.

          See also G3D::RenderDevice::configureShadowMap and the Collision_Demo.
        */
        public enum DepthReadMode
        {
            DEPTH_NORMAL = 0,
            DEPTH_LEQUAL = 1,
            DEPTH_GEQUAL = 2
        }

        /**
          All parameters of a texture that are independent of the underlying image data.
        */
        public class Settings
        {
            /** Default is TRILINEAR_MIPMAP */
            public InterpolateMode interpolateMode;

            /** Default is TILE */
            public WrapMode wrapMode;

            /** Default is DEPTH_NORMAL */
            public DepthReadMode depthReadMode;

            /** Default is 2.0 */
            public float maxAnisotropy;

            /** Default is true */
            public bool autoMipMap;

            /**
             Highest MIP-map level that will be used during rendering.  The highest
             level that actually exists will be L = log(max(width, height), 2)), although
             it is fine to set maxMipMap higher than this.  Must be larger than minMipMap.
             Default is 1000.

             Setting the max mipmap level is useful for preventing adjacent areas of
             a texture from being blurred together when viewed at a distance.  It may
             decrease performance, however, by forcing a larger texture into cache
             than would otherwise be required.
             */
            public int maxMipMap;

            /**
             Lowest MIP-map level that will be used during rendering.  Level 0 is the
             full-size image.  Default is -1000, matching the OpenGL spec.
             @cite http://oss.sgi.com/projects/ogl-sample/registry/SGIS/texture_lod.txt
             */
            public int minMipMap;

            public Settings()
            {
                interpolateMode = InterpolateMode.TRILINEAR_MIPMAP;
                wrapMode = WrapMode.TILE;
                depthReadMode = DepthReadMode.DEPTH_NORMAL;
                maxAnisotropy = 2.0f;
                autoMipMap = true;
                maxMipMap = 1000;
                minMipMap = -1000;
            }

            private static Settings defaultsInstance = new Settings();
            private static Settings videoInstance = new Settings
            {
                interpolateMode = InterpolateMode.BILINEAR_NO_MIPMAP,
                wrapMode = WrapMode.CLAMP,
                depthReadMode = DepthReadMode.DEPTH_NORMAL,
                maxAnisotropy = 1.0f,
                autoMipMap = false
            };
            private static Settings shadowInstance = new Settings
            {
                interpolateMode = InterpolateMode.BILINEAR_NO_MIPMAP,
                wrapMode = WrapMode.CLAMP,
                depthReadMode = DepthReadMode.DEPTH_LEQUAL,
                maxAnisotropy = 1.0f,
                autoMipMap = false
            };

            public static Settings defaults()
            {
                return defaultsInstance;
            }

            /**
              Useful defaults for video/image processing.
              BILINEAR_NO_MIPMAP / CLAMP / DEPTH_NORMAL / 1.0 / false
            */
            public static Settings video()
            {
                return videoInstance;
            }

            /**
              Useful defaults for shadow maps.
              BILINEAR_NO_MIPMAP / CLAMP / DEPTH_LEQUAL / 1.0 / false
            */
            public static Settings shadow()
            {
                return shadowInstance;
            }

            public uint hashCode()
            {
                return
                    (uint)interpolateMode +
                    16 * (uint)wrapMode +
                    256 * (uint)depthReadMode +
                    (autoMipMap ? 512u : 0u) +
                    (uint)(1024 * maxAnisotropy) +
                    (uint)(minMipMap ^ (maxMipMap << 16));
            }
        }

        /**
          @deprecated Use Texture::Settings
         */
        public class Parameters : Settings
        {
        }

        public sealed class PreProcess
        {
            /** Amount to brighten colors by (useful for Quake textures, which are dark). */
            public float brighten;

            /** Amount to resize images by before loading to save memory;
                typically a negative power of 2 (e.g., 1.0, 0.5, 0.25). */
            public float scaleFactor;

            public PreProcess()
            {
                brighten = 1.0f;
                scaleFactor = 1.0f;
            }

            private static PreProcess defaultsInstance = new PreProcess();
            public static PreProcess defaults()
            {
                return defaultsInstance;
            }
        }

        // Private fields
        private int textureID; // OpenGL texture ID or Direct3D texture handle
        private DepthReadMode _depthRead;
        private float _maxAnisotropy;
        private InterpolateMode interpolate;
        private WrapMode wrap;
        private Settings _settings;
        private string name;
        private Dimension dimension;
        private bool _opaque;
        private TextureFormat format;
        private int width;
        private int height;
        private int depth;
        private static long _sizeOfAllTexturesInMemory;
        private bool _invertY;
        private long accountedSize;

        private static readonly string[] cubeMapString = { "ft", "bk", "up", "dn", "rt", "lf" };

        // Backend-specific handle (set by backend implementation)
        private object backendHandle;

        // CPU side pixel data, uploaded to the backend the first time the
        // texture is used.
        private byte[] pixelData;
        private byte[][] cubeFaceData;
        private List<List<byte[]>> mipMapData;
        private TextureFormat pixelFormat;
        private Dictionary<object, int> backendTextureIDs;

        /**
          Private constructor; use static factory methods.
        */
        private Texture(
            string _name,
            int _textureID,
            Dimension _dimension,
            TextureFormat _format,
            InterpolateMode _interpolate,
            WrapMode _wrap,
            bool _opaque,
            DepthReadMode _depthRead,
            float _anisotropy)
        {
            name = _name;
            textureID = _textureID;
            dimension = _dimension;
            format = _format;
            interpolate = _interpolate;
            wrap = _wrap;
            this._opaque = _opaque;
            this._depthRead = _depthRead;
            _maxAnisotropy = _anisotropy;
            _settings = new Settings();
            _settings.interpolateMode = _interpolate;
            _settings.wrapMode = _wrap;
            _settings.maxAnisotropy = _anisotropy;
            _settings.depthReadMode = _depthRead;
            _settings.autoMipMap = true;
            depth = 1;
            invertY = false;
            backendTextureIDs = new Dictionary<object, int>();

        }

        /**
          Returns the CPU side pixel data of a 2D texture or null.
         */
        internal byte[] getPixelData()
        {
            return pixelData;
        }

        /**
          Returns the CPU side pixel data of the six cube map faces or null.
         */
        internal byte[][] getCubeFaceData()
        {
            return cubeFaceData;
        }

        internal List<List<byte[]>> getMipMapData()
        {
            return mipMapData;
        }

        /**
          Returns the format of the CPU side pixel data.
         */
        internal TextureFormat getPixelFormat()
        {
            return pixelFormat ?? format;
        }

        /**
          Returns the texture id allocated by a specific backend, or 0.
         */
        internal int getBackendTextureID(object backendKey)
        {
            int id;
            if (backendKey != null && backendTextureIDs.TryGetValue(backendKey, out id))
            {
                return id;
            }
            return 0;
        }

        /**
          Associates a texture id with a specific backend.
         */
        internal void setBackendTextureID(object backendKey, int id)
        {
            if (backendKey != null)
            {
                backendTextureIDs[backendKey] = id;
            }
            textureID = id;
        }

        /**
          Returns the texture format that matches the number of channels in
          the image.
         */
        private static TextureFormat sourceFormatForImage(GImage image)
        {
            switch (image.channels)
            {
            case 4:
                return TextureFormat.RGBA8;

            case 3:
                return TextureFormat.RGB8;

            case 1:
                return TextureFormat.L8;

            default:
                throw new InvalidOperationException(
                    Format.format("GImage has an unexpected number of channels (%d)", image.channels));
            }
        }

        /**
          Returns the number of bytes occupied by one texel of the format.
          Block compressed formats report the size of one compressed block.
         */
        private static int bytesPerTexel(TextureFormat format)
        {
            if (format == null)
            {
                return 0;
            }

            if (format.compressed)
            {
                if (format == TextureFormat.RGB_DXT1 || format == TextureFormat.RGBA_DXT1)
                {
                    return 8;
                }
                return 16;
            }

            return format.packedBitsPerTexel / 8;
        }

        private static bool isMipMapFormat(InterpolateMode i)
        {
            switch (i)
            {
            case InterpolateMode.TRILINEAR_MIPMAP:
            case InterpolateMode.BILINEAR_MIPMAP:
            case InterpolateMode.NEAREST_MIPMAP:
                return true;

            case InterpolateMode.BILINEAR_NO_MIPMAP:
            case InterpolateMode.NEAREST_NO_MIPMAP:
            case InterpolateMode.NO_INTERPOLATION:
                return false;
            }

            throw new InvalidOperationException("Illegal interpolate mode");
        }

        /**
          Scales the intensity up or down of an entire image.
          @param skipAlpha 0 if there is no alpha channel, 1 if there is
         */
        private static void brightenImage(byte[] bytes, int n, double brighten, int skipAlpha)
        {
            // Make a lookup table
            byte[] bright = new byte[256];
            for (int i = 0; i < 256; ++i)
            {
                bright[i] = (byte)g3dmath.iClamp(g3dmath.iRound(i * brighten), 0, 255);
            }

            if (skipAlpha < 0)
            {
                // Single channel image: brighten every channel
                for (int i = 0; i < n; ++i)
                {
                    bytes[i] = bright[bytes[i]];
                }
                return;
            }

            for (int i = 0; i < n; i += skipAlpha)
            {
                for (int c = 0; c < 3; ++c, ++i)
                {
                    if (i >= n)
                    {
                        return;
                    }
                    bytes[i] = bright[bytes[i]];
                }
            }
        }

        /**
          Rescales an image to the requested dimensions using nearest neighbor
          sampling.
         */
        private static byte[] rescaleImage(byte[] source, int sourceWidth, int sourceHeight,
                                           int channels, int destinationWidth, int destinationHeight)
        {
            byte[] destination = new byte[destinationWidth * destinationHeight * channels];
            if (sourceWidth <= 0 || sourceHeight <= 0)
            {
                return destination;
            }
            for (int y = 0; y < destinationHeight; ++y)
            {
                int sy = destinationHeight == 1 ? 0 : (int)((long)y * sourceHeight / destinationHeight);
                if (sy >= sourceHeight)
                {
                    sy = sourceHeight - 1;
                }
                for (int x = 0; x < destinationWidth; ++x)
                {
                    int sx = destinationWidth == 1 ? 0 : (int)((long)x * sourceWidth / destinationWidth);
                    if (sx >= sourceWidth)
                    {
                        sx = sourceWidth - 1;
                    }
                    Array.Copy(source, (sx + sy * sourceWidth) * channels,
                               destination, (x + y * destinationWidth) * channels, channels);
                }
            }
            return destination;
        }

        /**
          Splits a filename at the last wildcard, as used by the cube map
          loader.
         */
        public static void splitFilenameAtWildCard(
            string filename,
            out string filenameBase,
            out string filenameExt)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

            const string splitter = "*";

            int i = filename.LastIndexOf(splitter, StringComparison.Ordinal);
            if (i != -1)
            {
                filenameBase = filename.Substring(0, i);
                filenameExt = filename.Substring(i + splitter.Length);
            }
            else
            {
                throw new GImage.Error("Cube map filenames must contain \"*\" as a placeholder for up/lf/rt/bk/ft/dn", filename);
            }
        }

        private void updateMemoryAccounting()
        {
            long current = calculateSizeInMemory();
            _sizeOfAllTexturesInMemory += current - accountedSize;
            accountedSize = current;
        }

        private long calculateSizeInMemory()
        {
            if (!TextureFormat.valid)
            {
                return 0;
            }

            if (format == null || width <= 0 || height <= 0)
            {
                return 0;
            }

            int baseSize = (int)((width * height * depth * format.hardwareBitsPerTexel) / 8);

            long total = 0;

            if (interpolate == InterpolateMode.TRILINEAR_MIPMAP)
            {
                int w = width;
                int h = height;

                while ((w > 2) && (h > 2))
                {
                    total += baseSize;
                    baseSize /= 4;
                    w /= 2;
                    h /= 2;
                }

            } else
            {
                total = baseSize;
            }

            if (dimension == Dimension.DIM_CUBE_MAP)
            {
                total *= 6;
            }

            return total;
        }

        /**
          Returns a new OpenGL texture ID.
          In C#, this would be handled by the backend.
        */
        public static int newGLTextureID()
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new NotSupportedException("newGLTextureID requires an active graphics backend; the backend-neutral GLG3D layer cannot create an OpenGL texture name by itself.");
            }
            return backend.CreateTexture();
        }

        /**
          Creates an empty texture (useful for later reading from the screen).
        */
        public static Texture createEmpty(
            int width,
            int height,
            string name = "Texture",
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f)
        {
            if (desiredFormat == null || desiredFormat == TextureFormat.AUTO)
            {
                throw new ArgumentException("desiredFormat may not be TextureFormat::AUTO", "desiredFormat");
            }

            // We must pretend the input is in the desired format otherwise
            // OpenGL might refuse to negotiate formats for us.
            int size = width * height * desiredFormat.packedBitsPerTexel / 8;
            byte[] data = new byte[size];

            if (dimension == Dimension.DIM_CUBE_MAP || dimension == Dimension.DIM_CUBE_MAP_NPOT)
            {
                byte[][] faces = new byte[6][];
                for (int i = 0; i < 6; ++i)
                {
                    faces[i] = data;
                }

                return fromMemory(name, faces, desiredFormat, width, height, 1, desiredFormat,
                                  wrap, interpolate, dimension, depthRead, maxAnisotropy, 1.0f);
            }

            return fromMemory(name, data, desiredFormat, width, height, desiredFormat,
                              wrap, interpolate, dimension, depthRead, maxAnisotropy, 1.0f);
        }

        /**
          Creates an empty texture (useful for later reading from the screen).
        */
        public static Texture createEmpty(
            string name,
            int width,
            int height,
            TextureFormat desiredFormat = null,
            Dimension dimension = Dimension.DIM_2D,
            Settings param = null)
        {
            if (param == null) param = Settings.defaults();
            if (desiredFormat == null) desiredFormat = TextureFormat.RGBA8;
            return createEmpty(width, height, name, desiredFormat, param.wrapMode,
                   param.interpolateMode, dimension, param.depthReadMode, param.maxAnisotropy);
        }

        /**
          Wrap and interpolate will override the existing parameters on the
          GL texture.

          @param name Arbitrary name for this texture to identify it
          @param textureID Set to newGLTextureID() to create an empty texture.
          @param maxAnisotropy Values over 1.0 will give better rendering of textures on shallow angles
        */
        public static Texture fromGLTexture(
            string name,
            int textureID,
            TextureFormat textureFormat,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f)
        {
            if (textureFormat == null)
            {
                throw new ArgumentNullException("textureFormat");
            }

            Texture t = new Texture(
                name,
                textureID,
                dimension,
                textureFormat,
                interpolate,
                wrap,
                textureFormat.opaque,
                depthRead,
                maxAnisotropy);
            // Width/height/depth unknown for existing GL texture
            return t;
        }


        /**
          Creates a texture from a single image.  The image must have a format understood
          by G3D::GImage.  If dimension is DIM_CUBE_MAP, this loads the 6 files with names
          _ft, _bk, ... following the G3D::Sky documentation.
          @param brighten A value to multiply all color channels by; useful for loading
                 dark Quake textures.
        */
        public static Texture fromFile(
            string filename,
            TextureFormat desiredFormat = null,
            Dimension dimension = Dimension.DIM_2D,
            Settings param = null,
            PreProcess process = null)
        {
            if (param == null) param = Settings.defaults();
            if (process == null) process = new PreProcess();
            if (desiredFormat == null) desiredFormat = TextureFormat.AUTO;

            return Texture.fromFile(
                filename,
                desiredFormat,
                param.wrapMode,
                param.interpolateMode,
                dimension,
                process.brighten,
                param.depthReadMode,
                param.maxAnisotropy,
                process.scaleFactor);
        }

        public static Texture fromFile(
            string filename,
            TextureFormat desiredFormat,
            WrapMode wrap,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            double brighten = 1.0,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f,
            float resizeFactor = 1.0f)
        {
            string[] f = new string[6];
            f[0] = filename;
            f[1] = "";
            f[2] = "";
            f[3] = "";
            f[4] = "";
            f[5] = "";

            return fromFile(f, desiredFormat, wrap, interpolate, dimension, brighten, depthRead, maxAnisotropy, resizeFactor);
        }

        /**
          Creates a cube map from six independently named files.  The first
          becomes the name of the texture.
        */
        public static Texture fromFile(
            string[] filename,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            double brighten = 1.0,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f,
            float resizeFactor = 1.0f)
        {
            if (filename == null || filename.Length != 6)
            {
                throw new ArgumentException("A cube map requires exactly 6 filenames", "filename");
            }

            if (desiredFormat == null) desiredFormat = TextureFormat.AUTO;

            string[] realFilename = new string[6];

            TextureFormat format = TextureFormat.RGB8;
            bool opaque = true;

            // The six cube map faces, or the one texture and 5 dummys.
            GImage[] image = new GImage[6];
            byte[][] array = new byte[6][];

            int numFaces = (dimension == Dimension.DIM_CUBE_MAP) ?
                6 : (dimension == Dimension.DIM_CUBE_MAP_NPOT) ?
                6 : 1;

            // Check for DDS file and load separately.
            string ddsExt;

            // Find the period
            int period = filename[0].LastIndexOf('.');

            // Make sure it is before a slash!
            int j = Math.Max(filename[0].LastIndexOf('/'), filename[0].LastIndexOf('\\'));
            if ((period != -1) && (period > j))
            {
                ddsExt = filename[0].Substring(period + 1);
            }
            else
            {
                ddsExt = "";
            }

            if (StringUtils.toUpper(ddsExt) == "DDS") {

                DDSTexture ddsTexture = DDSTexture.load(filename[0]);

                TextureFormat bytesFormat = ddsTexture.BytesFormat;

                Dimension ddsDimension = dimension;
                if (ddsTexture.MipMaps[0].Count == 6 &&
                    (dimension == Dimension.DIM_2D || dimension == Dimension.DIM_2D_NPOT))
                {
                    ddsDimension = Dimension.DIM_CUBE_MAP;
                }

                int ddsNumFaces = (ddsDimension == Dimension.DIM_CUBE_MAP || ddsDimension == Dimension.DIM_CUBE_MAP_NPOT) ? 6 : 1;

                // Assert that we are loading a cubemap DDS
                Debug.debugAssert(ddsNumFaces == ddsTexture.MipMaps[0].Count);

                return Texture.fromMemory(filename[0], ddsTexture.MipMaps, bytesFormat, ddsTexture.Width, ddsTexture.Height, 1, desiredFormat, wrap, interpolate, ddsDimension, depthRead, maxAnisotropy, resizeFactor);
            }

            // Test for both DIM_CUBE_MAP and DIM_CUBE_MAP_NPOT
            if (numFaces == 6) {
                if (filename[1] == "") {
                    // Wildcard format
                    // Parse the filename into a base name and extension
                    string filenameBase, filenameExt;
                    splitFilenameAtWildCard(filename[0], out filenameBase, out filenameExt);
                    for (int f = 0; f < 6; ++f) {
                        realFilename[f] = filenameBase + cubeMapString[f] + filenameExt;
                    }
                } else {
                    // Separate filenames have been provided
                    realFilename[0] = filename[0];
                    for (int f = 1; f < 6; ++f) {
                        Debug.debugAssert(filename[f] != "");
                        realFilename[f] = filename[f];
                    }
                }
            } else {
                Debug.debugAssertM(filename[1] == "",
                    "Can't specify more than one filename unless loading a cube map");
                realFilename[0] = filename[0];
            }

            for (int f = 0; f < numFaces; ++f) {

                image[f] = new GImage();
                image[f].load(realFilename[f]);

                if (image[f].channels == 4) {
                    format = TextureFormat.RGBA8;
                    opaque = false;
                } else if (image[f].channels == 1) {
                    format = TextureFormat.L8;
                }

                if (desiredFormat == TextureFormat.AUTO) {
                    desiredFormat = format;
                }

                array[f] = image[f].getBytes();
            }


            if (brighten != 1.0) {
                for (int f = 0; f < numFaces; ++f) {
                    brightenImage(
                        image[f].getBytes(),
                        image[f].width * image[f].height * image[f].channels,
                        brighten,
                        image[f].channels - 3);
                }
            }

            Texture t =
                Texture.fromMemory(filename[0], array, format,
                    image[0].width, image[0].height, 1,
                    desiredFormat, wrap, interpolate, dimension,
                    depthRead, maxAnisotropy, resizeFactor);

            t._opaque = opaque;

            return t;
        }

        /**
          Creates a texture from the colors of filename and takes the alpha values
          from the red channel of alpha filename. See G3D::RenderDevice::setBlendFunc
          for important information about turning on alpha blending.
        */
        public static Texture fromTwoFiles(
            string filename,
            string alphaFilename,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f)
        {
            if (desiredFormat == null) desiredFormat = TextureFormat.RGBA8;

            // The six cube map faces, or the one texture and 5 dummys.
            byte[][] array = new byte[6][];

            int numFaces = (dimension == Dimension.DIM_CUBE_MAP) ?
                6 : (dimension == Dimension.DIM_CUBE_MAP_NPOT) ?
                6 : 1;

            // Parse the filename into a base name and extension
            string filenameBase = filename;
            string filenameExt = "";
            string alphaFilenameBase = alphaFilename;
            string alphaFilenameExt = "";

            // Test for both DIM_CUBE_MAP and DIM_CUBE_MAP_NPOT
            if (numFaces == 6) {
                splitFilenameAtWildCard(filename, out filenameBase, out filenameExt);
                splitFilenameAtWildCard(alphaFilename, out alphaFilenameBase, out alphaFilenameExt);
            }

            GImage[] color = new GImage[6];
            GImage[] alpha = new GImage[6];
            Texture t;

            try {
            for (int f = 0; f < numFaces; ++f) {

                string fn = filename;
                string an = alphaFilename;

                // Test for both DIM_CUBE_MAP and DIM_CUBE_MAP_NPOT
                if (numFaces == 6) {
                    fn = filenameBase + cubeMapString[f] + filenameExt;
                    an = alphaFilenameBase + cubeMapString[f] + alphaFilenameExt;
                }

                // Compose the two images to a single RGBA
                color[f] = new GImage();
                color[f].load(fn);
                alpha[f] = new GImage();
                alpha[f].load(an);

                GImage combined = color[f].insertRedAsAlpha(alpha[f]);
                array[f] = combined.getBytes();
            }

            t = Texture.fromMemory(filename, array, TextureFormat.RGBA8,
                    color[0].width, color[0].height, 1,
                    desiredFormat, wrap, interpolate, dimension, depthRead, maxAnisotropy);

            } catch (GImage.Error e) {
                Log.common().printf("\n**************************\n\n" +
                    "Loading \"%s\" failed. %s\n", e.filename, e.reason);
                t = null;
            }

            return t;
        }

        /**
          Creates a texture from raw memory data.
        */
        public static Texture fromMemory(
            string name,
            List<List<byte[]>> bytes, // bytes[miplevel][cubeface]
            TextureFormat bytesFormat,
            int width,
            int height,
            int depth,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f,
            float resizeFactor = 1.0f)
        {
            if (bytesFormat == null)
            {
                throw new ArgumentNullException("bytesFormat");
            }

            if (bytes == null || bytes.Count == 0)
            {
                throw new ArgumentException("At least one mipmap level must be provided.", "bytes");
            }

            if (desiredFormat == null || desiredFormat == TextureFormat.AUTO || bytesFormat.compressed)
            {
                desiredFormat = bytesFormat;
            }

            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend != null)
            {
                if (backend.HasBug_redBlueMipmapSwap() && (desiredFormat == TextureFormat.RGB8))
                {
                    desiredFormat = TextureFormat.RGBA8;
                }

                if (!backend.SupportsTextureFormat(desiredFormat))
                {
                    Debug.debugAssertM(false, "Unsupported texture format.");
                }
            }

            int numFaces = (dimension == Dimension.DIM_CUBE_MAP || dimension == Dimension.DIM_CUBE_MAP_NPOT) ? 6 : 1;

            bool useNPOT = (dimension == Dimension.DIM_2D_NPOT) || (dimension == Dimension.DIM_CUBE_MAP_NPOT);
            bool npotSupported = (backend != null) && backend.SupportsExtension("GL_ARB_texture_non_power_of_two");

            int channels = bytesPerTexel(bytesFormat);
            bool compressed = bytesFormat.compressed;

            List<List<byte[]>> levels = new List<List<byte[]>>();
            int mipWidth = width;
            int mipHeight = height;

            for (int mipLevel = 0; mipLevel < bytes.Count; ++mipLevel)
            {
                List<byte[]> level = bytes[mipLevel];
                if (level == null)
                {
                    throw new ArgumentException("A mipmap level is missing.", "bytes");
                }

                if (level.Count != numFaces)
                {
                    throw new ArgumentException("Each mipmap level must contain exactly " + numFaces + " face(s).", "bytes");
                }

                int newWidth = mipWidth;
                int newHeight = mipHeight;

                if (!compressed &&
                    ((!g3dmath.isPow2(mipWidth) || !g3dmath.isPow2(mipHeight)) &&
                     (!useNPOT || !npotSupported)))
                {
                    // NPOT texture with useNPOT disabled: resize to a power of 2
                    newWidth = g3dmath.ceilPow2((uint)(mipWidth * resizeFactor));
                    newHeight = g3dmath.ceilPow2((uint)(mipHeight * resizeFactor));
                    if (newWidth < 1) newWidth = 1;
                    if (newHeight < 1) newHeight = 1;
                }

                List<byte[]> scaled = new List<byte[]>();
                for (int f = 0; f < numFaces; ++f)
                {
                    byte[] data = level[f];
                    if (data == null)
                    {
                        throw new ArgumentException("A texture face is missing.", "bytes");
                    }

                    if (newWidth != mipWidth || newHeight != mipHeight)
                    {
                        // Rescale the image to a power of 2
                        data = rescaleImage(data, mipWidth, mipHeight, channels, newWidth, newHeight);
                    }

                    scaled.Add(data);
                }
                levels.Add(scaled);

                mipWidth = g3dmath.iMax(1, mipWidth / 2);
                mipHeight = g3dmath.iMax(1, mipHeight / 2);
            }

            Texture t = new Texture(
                name,
                0,
                dimension,
                desiredFormat,
                interpolate,
                wrap,
                desiredFormat.opaque,
                depthRead,
                maxAnisotropy);

            t.pixelFormat = bytesFormat;
            t.mipMapData = levels;
            t.cubeFaceData = null;
            t.pixelData = null;
            t.depth = depth;

            if (numFaces == 6 && levels.Count == 1)
            {
                t.cubeFaceData = levels[0].ToArray();
            }

            if ((dimension != Dimension.DIM_2D_RECT) &&
                ((dimension != Dimension.DIM_2D_NPOT && dimension != Dimension.DIM_CUBE_MAP_NPOT))) {
                width  = g3dmath.ceilPow2((uint)width);
                height = g3dmath.ceilPow2((uint)height);
            }

            t.width = width;
            t.height = height;
            t.updateMemoryAccounting();
            return t;
        }

        public static Texture fromMemory(
            string name,
            byte[][] bytes, // For cube maps: 6 faces
            TextureFormat bytesFormat,
            int width,
            int height,
            int depth,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f,
            float resizeFactor = 1.0f)
        {
            if (bytesFormat == null)
            {
                throw new ArgumentNullException("bytesFormat");
            }

            int numFaces = (dimension == Dimension.DIM_CUBE_MAP || dimension == Dimension.DIM_CUBE_MAP_NPOT) ? 6 : 1;

            if (bytes == null || bytes.Length < numFaces)
            {
                throw new ArgumentException("Exactly " + numFaces + " face(s) must be provided.", "bytes");
            }

            List<byte[]> faces = new List<byte[]>();
            for (int f = 0; f < numFaces; ++f)
            {
                if (bytes[f] == null)
                {
                    throw new ArgumentException("A texture face is missing.", "bytes");
                }
                faces.Add(bytes[f]);
            }

            List<List<byte[]>> arrayMipMapFaces = new List<List<byte[]>>();
            arrayMipMapFaces.Add(faces);

            return Texture.fromMemory(name, arrayMipMapFaces, bytesFormat, width, height,
                depth, desiredFormat, wrap, interpolate, dimension, depthRead, maxAnisotropy,
                resizeFactor);
        }

        public static Texture fromMemory(
            string name,
            byte[] bytes,
            TextureFormat bytesFormat,
            int width,
            int height,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f,
            float resizeFactor = 1.0f)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (bytesFormat == null)
            {
                throw new ArgumentNullException("bytesFormat");
            }

            byte[][] faces = new byte[1][];
            faces[0] = bytes;

            return Texture.fromMemory(name, faces, bytesFormat, width, height, 1,
                desiredFormat, wrap, interpolate, dimension, depthRead, maxAnisotropy,
                resizeFactor);
        }

        public static Texture fromMemory(
            string name,
            byte[] bytes,
            TextureFormat bytesFormat,
            int width,
            int height,
            int depth,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f,
            float resizeFactor = 1.0f)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (bytesFormat == null)
            {
                throw new ArgumentNullException("bytesFormat");
            }

            byte[][] faces = new byte[1][];
            faces[0] = bytes;

            return Texture.fromMemory(name, faces, bytesFormat, width, height, depth,
                desiredFormat, wrap, interpolate, dimension, depthRead, maxAnisotropy,
                resizeFactor);
        }

        public static Texture fromMemory(
            string name,
            byte[] bytes,
            TextureFormat bytesFormat,
            int width,
            int height,
            TextureFormat desiredFormat = null,
            Dimension dimension = Dimension.DIM_2D,
            Settings param = null)
        {
            if (param == null) param = Settings.defaults();
            if (desiredFormat == null) desiredFormat = TextureFormat.AUTO;
            return fromMemory(name, bytes, bytesFormat, width, height, desiredFormat, param.wrapMode, param.interpolateMode, dimension, param.depthReadMode, param.maxAnisotropy);
        }

        public static Texture fromData(
            string name,
            byte[] data,
            TextureFormat bytesFormat,
            int width,
            int height,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f,
            float resizeFactor = 1.0f)
        {
            return fromMemory(name, data, bytesFormat, width, height, desiredFormat, wrap, interpolate, dimension, depthRead, maxAnisotropy, resizeFactor);
        }

        public static Texture fromData(
            string name,
            byte[][] data,
            TextureFormat bytesFormat,
            int width,
            int height,
            int depth,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f,
            float resizeFactor = 1.0f)
        {
            return fromMemory(name, data, bytesFormat, width, height, depth, desiredFormat, wrap, interpolate, dimension, depthRead, maxAnisotropy, resizeFactor);
        }

        public static Texture fromData(
            string name,
            List<List<byte[]>> data,
            TextureFormat bytesFormat,
            int width,
            int height,
            int depth,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f,
            float resizeFactor = 1.0f)
        {
            return fromMemory(name, data, bytesFormat, width, height, depth, desiredFormat, wrap, interpolate, dimension, depthRead, maxAnisotropy, resizeFactor);
        }

        public static Texture fromGImage(
            string name,
            GImage image,
            TextureFormat desiredFormat = null,
            WrapMode wrap = WrapMode.TILE,
            InterpolateMode interpolate = InterpolateMode.TRILINEAR_MIPMAP,
            Dimension dimension = Dimension.DIM_2D,
            DepthReadMode depthRead = DepthReadMode.DEPTH_NORMAL,
            float maxAnisotropy = 2.0f)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            TextureFormat format = sourceFormatForImage(image);
            bool opaque = format.opaque;

            if (desiredFormat == null) desiredFormat = TextureFormat.AUTO;

            byte[][] array = new byte[1][];
            array[0] = image.getBytes();

            Texture t =
                Texture.fromMemory(name, array, format,
                    image.width, image.height, 1,
                    desiredFormat, wrap, interpolate,
                    dimension, depthRead, maxAnisotropy);

            t._opaque = opaque;

            return t;
        }

        public static Texture fromGImage(
            string name,
            GImage image,
            TextureFormat desiredFormat = null,
            Dimension dimension = Dimension.DIM_2D,
            Settings param = null)
        {
            if (param == null) param = Settings.defaults();
            if (desiredFormat == null) desiredFormat = TextureFormat.AUTO;
            return fromGImage(name, image, desiredFormat, param.wrapMode, param.interpolateMode, dimension, param.depthReadMode, param.maxAnisotropy);
        }

        /**
          Returns the level 0 pixel data of the texture, or null when the
          texture has no CPU side copy.
         */
        private byte[] level0Data()
        {
            if (pixelData != null)
            {
                return pixelData;
            }

            if (mipMapData != null && mipMapData.Count > 0 && mipMapData[0] != null && mipMapData[0].Count > 0)
            {
                return mipMapData[0][0];
            }

            if (cubeFaceData != null && cubeFaceData.Length > 0)
            {
                return cubeFaceData[0];
            }

            return null;
        }

        /**
          Creates another texture that is the same as this one but contains only
          an alpha channel.  Alpha-only textures are useful as mattes.

          If the current texture is opaque(), returns NULL (since it is not useful
          to construct an alpha-only version of a texture without an alpha channel).

          Like all texture construction methods, this is fairly
          slow and should not be called every frame during interactive rendering.
        */
        public Texture alphaOnlyVersion()
        {
            if (opaque())
            {
                return null;
            }

            Debug.debugAssert(_depthRead == DepthReadMode.DEPTH_NORMAL);
            Debug.debugAssertM(
                dimension == Dimension.DIM_2D ||
                dimension == Dimension.DIM_2D_RECT ||
                dimension == Dimension.DIM_2D_NPOT,
                "alphaOnlyVersion only supported for 2D textures");

            if (dimension == Dimension.DIM_CUBE_MAP || dimension == Dimension.DIM_CUBE_MAP_NPOT)
            {
                throw new NotSupportedException("alphaOnlyVersion only supported for 2D textures");
            }

            byte[] source = level0Data();
            if (source == null)
            {
                IGraphicsBackend backend = GLCaps.getBackendInstance();
                int id = backend == null ? 0 : getBackendTextureID(backend);
                if (backend == null || id == 0)
                {
                    throw new InvalidOperationException("The texture has not been allocated and has no CPU-side pixel data.");
                }
                source = new byte[width * height * 4];
                backend.BindTexture(0, id, dimension);
                backend.GetTexImage(backend.GetTextureTarget(dimension), 0, TextureFormat.RGBA8, source);
            }

            int sourceChannels = pixelFormat == null ? 4 : bytesPerTexel(pixelFormat);
            if (sourceChannels < 1)
            {
                sourceChannels = 4;
            }

            if (source.Length < width * height * sourceChannels)
            {
                throw new InvalidOperationException("The texture data does not match the texture dimensions.");
            }

            byte[] alpha = new byte[width * height];
            for (int i = 0; i < alpha.Length; ++i)
            {
                if (sourceChannels == 4)
                {
                    alpha[i] = source[i * 4 + 3];
                }
                else if (sourceChannels == 1)
                {
                    alpha[i] = source[i];
                }
                else
                {
                    alpha[i] = 255;
                }
            }

            return fromMemory(name + " Alpha", alpha, TextureFormat.A8, width, height, TextureFormat.A8,
                              wrap, interpolate, dimension, DepthReadMode.DEPTH_NORMAL, _maxAnisotropy, 1.0f);
        }

        /**
          Copies data from screen into an existing texture (replacing whatever was
          previously there).  The dimensions must be powers of two or a texture
          rectangle will be created (not supported on some cards).

          The (x, y) coordinates are in real screen pixels.  (0, 0) is the top left
          of the screen.

          The texture dimensions will be updated but all other properties will be preserved:
          The previous wrap mode will be preserved.
          The interpolation mode will be preserved (unless it required a mipmap,
          in which case it will be set to BILINEAR_NO_MIPMAP).  The previous color depth
          and alpha depth will be preserved.  Texture compression is not supported for
          textures copied from the screen.

          To copy a depth texture, first create an empty depth texture then copy into it.

          If you invoke this method on a texture that is currently set on RenderDevice,
          the texture will immediately be updated (there is no need to rebind).

          @param useBackBuffer If true, the texture is created from the back buffer.
          If false, the texture is created from the front buffer.

          @param rect The rectangle to copy (relative to the viewport)
          See also RenderDevice::screenShotPic
        */
        public void copyFromScreen(Rect2D rect, bool useBackBuffer = true)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new NotSupportedException("copyFromScreen requires an active graphics backend.");
            }

            Debug.debugAssert(dimension == Dimension.DIM_2D || dimension == Dimension.DIM_2D_RECT || dimension == Dimension.DIM_2D_NPOT);

            int id = getBackendTextureID(backend);
            if (id == 0)
            {
                throw new InvalidOperationException("The texture has not been allocated by a RenderDevice; it cannot be copied from the screen.");
            }

            _sizeOfAllTexturesInMemory -= accountedSize;
            accountedSize = 0;

            // Set up new state
            width = (int)rect.width();
            height = (int)rect.height();
            depth = 1;

            int target = backend.GetTextureTarget(dimension);
            backend.BindTexture(0, id, dimension);
            backend.CopyTexImage2D(target, 0,
                                   g3dmath.iRound(rect.x0()),
                                   g3dmath.iRound(rect.y0()),
                                   g3dmath.iRound(rect.width()),
                                   g3dmath.iRound(rect.height()),
                                   0);

            // Reset the original properties
            backend.TexParameter(target, wrap, wrap, interpolate, interpolate, _maxAnisotropy);
            backend.SetTextureDepthCompare(target, _depthRead);

            // Once copied from the screen, the direction will be reversed.
            invertY = true;

            updateMemoryAccounting();
        }

        /**
          Argument for copyFromScreen
        */
        public enum CubeFace
        {
            CUBE_POS_X = 0,
            CUBE_NEG_X = 1,
            CUBE_POS_Y = 2,
            CUBE_NEG_Y = 3,
            CUBE_POS_Z = 4,
            CUBE_NEG_Z = 5
        }

        /**
          Copies into the specified face of a cube map.  Because cube maps can't have
          the Y direction inverted (and still do anything useful), you should render
          the cube map faces <B>upside-down</B> before copying them into the map.  This
          is an unfortunate side-effect of OpenGL's cube map convention.

          Use G3D::Texture::getCameraRotation to generate the (upside-down) camera
          orientations.
        */
        public void copyFromScreen(Rect2D rect, CubeFace face, bool useBackBuffer = true)
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            if (backend == null)
            {
                throw new NotSupportedException("copyFromScreen requires an active graphics backend.");
            }

            int id = getBackendTextureID(backend);
            if (id == 0)
            {
                throw new InvalidOperationException("The texture has not been allocated by a RenderDevice; it cannot be copied from the screen.");
            }

            // Set up new state
            Debug.debugAssertM(width == rect.width(), "Cube maps require all six faces to have the same dimensions");
            Debug.debugAssertM(height == rect.height(), "Cube maps require all six faces to have the same dimensions");
            Debug.debugAssert(dimension == Dimension.DIM_CUBE_MAP || dimension == Dimension.DIM_CUBE_MAP_NPOT);
            Debug.debugAssert((int)face >= (int)CubeFace.CUBE_POS_X);
            Debug.debugAssert((int)face <= (int)CubeFace.CUBE_NEG_Z);

            backend.BindTexture(0, id, dimension);

            int target = cubeFaceTarget(face);
            backend.CopyTexImage2D(target, 0,
                                   g3dmath.iRound(rect.x0()),
                                   g3dmath.iRound(rect.y0()),
                                   g3dmath.iRound(rect.width()),
                                   g3dmath.iRound(rect.height()),
                                   0);
        }

        /**
          Returns the OpenGL texture target of a cube map face.
         */
        private static int cubeFaceTarget(CubeFace face)
        {
            // GL_TEXTURE_CUBE_MAP_POSITIVE_X .. GL_TEXTURE_CUBE_MAP_NEGATIVE_Z
            return 0x8515 + (int)face;
        }

        /**
          Returns the rotation matrix that should be used for rendering the
          given cube map face.
        */
        public static void getCameraRotation(CubeFace face, out Matrix3 outMatrix)
        {
            switch (face)
            {
            case CubeFace.CUBE_POS_X:
                outMatrix = Matrix3.fromEulerAnglesYXZ((float)g3dmath.G3D_HALF_PI, (float)g3dmath.G3D_PI, 0);
                break;

            case CubeFace.CUBE_NEG_X:
                outMatrix = Matrix3.fromEulerAnglesYXZ(-(float)g3dmath.G3D_HALF_PI, (float)g3dmath.G3D_PI, 0);
                break;

            case CubeFace.CUBE_POS_Y:
                outMatrix = Matrix3.fromEulerAnglesXYZ((float)g3dmath.G3D_HALF_PI, 0, 0);
                break;

            case CubeFace.CUBE_NEG_Y:
                outMatrix = Matrix3.fromEulerAnglesXYZ(-(float)g3dmath.G3D_HALF_PI, 0, 0);
                break;

            case CubeFace.CUBE_POS_Z:
                outMatrix = Matrix3.fromEulerAnglesYZX((float)g3dmath.G3D_PI, (float)g3dmath.G3D_PI, 0);
                break;

            case CubeFace.CUBE_NEG_Z:
                outMatrix = Matrix3.fromAxisAngle(Vector3.unitZ(), (float)g3dmath.G3D_PI);
                break;

            default:
                outMatrix = new Matrix3();
                break;
            }
        }

        /**
          When true, rendering code that uses this texture is responsible for
          flipping texture coordinates applied to this texture vertically (initially,
          this is false).

          RenderDevice watches this flag and performs the appropriate transformation.
          If you are not using RenderDevice (or are writing shaders), you must do it yourself.
        */
        public bool invertY
        {
            get { return _invertY; }
            set { _invertY = value; }
        }

        /**
          How much (texture) memory this texture occupies.  OpenGL backs
          video memory textures with main memory, so the total memory
          is actually twice this number.
        */
        public long sizeInMemory()
        {
            return calculateSizeInMemory();
        }

        /**
          Video memory occupied by all OpenGL textures allocated using Texture
          or maintained by pointers to a Texture.
        */
        public static long sizeOfAllTexturesInMemory()
        {
            return _sizeOfAllTexturesInMemory;
        }

        /**
          True if this texture was created with an alpha channel.  Note that
          a texture may have a format that is not opaque (e.g. RGBA8) yet still
          have a completely opaque alpha channel, causing texture->opaque to
          be true.  This is just a flag set for the user's convenience-- it does
          not affect rendering in any way.
           See G3D::RenderDevice::setBlendFunc
           for important information about turning on alpha blending.
        */
        public bool opaque()
        {
            return _opaque;
        }

        public DepthReadMode depthReadMode()
        {
            return _depthRead;
        }

        public float maxAnisotropy()
        {
            return _maxAnisotropy;
        }

        public int getOpenGLID()
        {
            return textureID;
        }

        /**
          Returns the level 0 mip-map data in the format that most closely matches
          outFormat.
          @param outFormat Must be one of: TextureFormat::AUTO, TextureFormat::RGB8, TextureFormat::RGBA8, TextureFormat::L8, TextureFormat::A8
        */
        public void getImage(GImage dst, TextureFormat outFormat = null)
        {
            if (dst == null)
            {
                throw new ArgumentNullException("dst");
            }

            if (outFormat != null &&
                outFormat != TextureFormat.AUTO &&
                outFormat != TextureFormat.RGB8 &&
                outFormat != TextureFormat.RGBA8 &&
                outFormat != TextureFormat.L8 &&
                outFormat != TextureFormat.A8)
            {
                throw new ArgumentOutOfRangeException("outFormat", "Illegal texture format.");
            }

            byte[] source = level0Data();

            TextureFormat readFormat = pixelFormat == null ? format : pixelFormat;

            int sourceChannels;
            if (readFormat == TextureFormat.RGBA8)
            {
                sourceChannels = 4;
            }
            else if (readFormat == TextureFormat.RGB8)
            {
                sourceChannels = 3;
            }
            else if (readFormat == TextureFormat.A8 || readFormat == TextureFormat.L8)
            {
                sourceChannels = 1;
            }
            else
            {
                sourceChannels = 4;
            }

            if (source == null)
            {
                IGraphicsBackend backend = GLCaps.getBackendInstance();
                int id = backend == null ? 0 : getBackendTextureID(backend);
                if (backend == null || id == 0)
                {
                    throw new InvalidOperationException("The texture has not been allocated and has no CPU-side pixel data.");
                }
                source = new byte[width * height * sourceChannels];
                backend.BindTexture(0, id, dimension);
                backend.GetTexImage(backend.GetTextureTarget(dimension), 0, readFormat, source);
            }

            if (outFormat == null || outFormat == TextureFormat.AUTO)
            {
                uint baseFormat = (format == null) ? 0x1908u : format.OpenGLBaseFormat;
                switch (baseFormat)
                {
                case 0x1906: // GL_ALPHA
                    outFormat = TextureFormat.A8;
                    break;

                case 0x1909: // GL_LUMINANCE
                    outFormat = TextureFormat.L8;
                    break;

                case 0x1907: // GL_RGB
                    outFormat = TextureFormat.RGB8;
                    break;

                case 0x1908: // GL_RGBA
                    // Fall through intentionally
                default:
                    outFormat = TextureFormat.RGBA8;
                    break;
                }
            }

            int destinationChannels = outFormat == TextureFormat.RGBA8 ? 4 : (outFormat == TextureFormat.RGB8 ? 3 : 1);
            byte[] destination = new byte[width * height * destinationChannels];
            for (int i = 0; i < width * height; ++i)
            {
                if (outFormat == TextureFormat.A8)
                {
                    if (sourceChannels == 4)
                    {
                        destination[i] = source[i * 4 + 3];
                    }
                    else if (sourceChannels == 1)
                    {
                        destination[i] = source[i];
                    }
                    else
                    {
                        destination[i] = 255;
                    }
                }
                else if (outFormat == TextureFormat.L8)
                {
                    int r = sourceChannels >= 3 ? source[i * sourceChannels] : source[i];
                    int g = sourceChannels >= 3 ? source[i * sourceChannels + 1] : r;
                    int b = sourceChannels >= 3 ? source[i * sourceChannels + 2] : r;
                    destination[i] = (byte)((r * 77 + g * 150 + b * 29) >> 8);
                }
                else
                {
                    int r = sourceChannels >= 3 ? source[i * sourceChannels] : source[i];
                    int g = sourceChannels >= 3 ? source[i * sourceChannels + 1] : r;
                    int b = sourceChannels >= 3 ? source[i * sourceChannels + 2] : r;
                    int a = sourceChannels == 4 ? source[i * 4 + 3] : 255;
                    destination[i * destinationChannels] = (byte)r;
                    destination[i * destinationChannels + 1] = (byte)g;
                    destination[i * destinationChannels + 2] = (byte)b;
                    if (destinationChannels == 4)
                    {
                        destination[i * 4 + 3] = (byte)a;
                    }
                }
            }
            dst.width = width;
            dst.height = height;
            dst.channels = destinationChannels;
            dst.setBytes(destination);
        }

        public int openGLID()
        {
            return textureID;
        }

        /** @deprecated Use texelWidth */
        public int getTexelWidth()
        {
            return width;
        }

        /** @deprecated Use texelHeight */
        public int getTexelHeight()
        {
            return height;
        }

        /**
          For 3D textures.
         @deprecated Use texelDepth
        */
        public int getTexelDepth()
        {
            return depth;
        }

        /**
          The OpenGL texture target this binds (e.g. GL_TEXTURE_2D)
        */
        public int getOpenGLTextureTarget()
        {
            switch (dimension)
            {
            case Dimension.DIM_CUBE_MAP_NPOT:
            case Dimension.DIM_CUBE_MAP:
                return 0x8513; // GL_TEXTURE_CUBE_MAP_ARB

            case Dimension.DIM_2D_NPOT:
            case Dimension.DIM_2D:
                return 0x0DE1; // GL_TEXTURE_2D

            case Dimension.DIM_2D_RECT:
                return 0x84F3; // GL_TEXTURE_RECTANGLE_EXT

            default:
                Debug.debugAssertM(false, "Fell through switch");
                return 0;
            }
        }

        public int texelWidth()
        {
            return width;
        }

        public int texelHeight()
        {
            return height;
        }

        public Vector2 vector2Bounds()
        {
            return new Vector2((float)width, (float)height);
        }

        public Rect2D rect2DBounds()
        {
            return Rect2D.xywh(0, 0, (float)width, (float)height);
        }

        public int texelDepth()
        {
            return depth;
        }

        public string getName()
        {
            return name;
        }

        public InterpolateMode getInterpolateMode()
        {
            return interpolate;
        }

        public WrapMode getWrapMode()
        {
            return wrap;
        }

        public WrapMode wrapMode()
        {
            return wrap;
        }

        public TextureFormat getFormat()
        {
            return format;
        }

        public Dimension getDimension()
        {
            return dimension;
        }

        public int getWidth()
        {
            return width;
        }

        public int getHeight()
        {
            return height;
        }

        public int getDepth()
        {
            return depth;
        }

        public DepthReadMode getDepthReadMode()
        {
            return _depthRead;
        }

        public float getMaxAnisotropy()
        {
            return _maxAnisotropy;
        }

        public Settings parameters()
        {
            return _settings;
        }

        public Settings settings()
        {
            return _settings;
        }

        public Settings getSettings()
        {
            return _settings;
        }

        public void setSettings(Settings value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            _settings = value;
            interpolate = value.interpolateMode;
            wrap = value.wrapMode;
            _depthRead = value.depthReadMode;
            _maxAnisotropy = value.maxAnisotropy;

            IGraphicsBackend backend = GLCaps.getBackendInstance();
            int id = backend == null ? 0 : getBackendTextureID(backend);
            if (backend != null && id != 0)
            {
                int target = backend.GetTextureTarget(dimension);
                backend.BindTexture(0, id, dimension);
                backend.TexParameter(target, wrap, wrap, interpolate, interpolate, _maxAnisotropy);
                backend.SetTextureDepthCompare(target, _depthRead);
            }

            updateMemoryAccounting();
        }

        public void resize(int newWidth, int newHeight)
        {
            if (newWidth < 0)
            {
                throw new ArgumentOutOfRangeException("newWidth");
            }
            if (newHeight < 0)
            {
                throw new ArgumentOutOfRangeException("newHeight");
            }

            byte[] data = level0Data();
            int channels = bytesPerTexel(pixelFormat == null ? format : pixelFormat);
            if (channels < 1)
            {
                channels = 4;
            }

            if (data != null && (width != newWidth || height != newHeight))
            {
                data = rescaleImage(data, width, height, channels, newWidth, newHeight);
            }

            width = newWidth;
            height = newHeight;
            depth = 1;

            if (pixelData != null)
            {
                pixelData = data;
            }
            else if (mipMapData != null && mipMapData.Count > 0 && mipMapData[0].Count > 0)
            {
                mipMapData[0][0] = data;
                mipMapData.RemoveRange(1, mipMapData.Count - 1);
            }
            else if (cubeFaceData != null && cubeFaceData.Length > 0)
            {
                cubeFaceData[0] = data;
            }

            updateMemoryAccounting();
        }

        public void clear()
        {
            byte[] data = level0Data();
            if (data != null)
            {
                Array.Clear(data, 0, data.Length);
            }

            if (mipMapData != null)
            {
                for (int level = 0; level < mipMapData.Count; ++level)
                {
                    List<byte[]> faces = mipMapData[level];
                    if (faces == null)
                    {
                        continue;
                    }
                    for (int f = 0; f < faces.Count; ++f)
                    {
                        if (faces[f] != null)
                        {
                            Array.Clear(faces[f], 0, faces[f].Length);
                        }
                    }
                }
            }

            if (cubeFaceData != null)
            {
                for (int f = 0; f < cubeFaceData.Length; ++f)
                {
                    if (cubeFaceData[f] != null)
                    {
                        Array.Clear(cubeFaceData[f], 0, cubeFaceData[f].Length);
                    }
                }
            }
        }

        public void free()
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            int id = backend == null ? 0 : getBackendTextureID(backend);
            if (backend != null && id != 0)
            {
                backend.DeleteTexture(id);
                backendTextureIDs.Remove(backend);
            }

            textureID = 0;
            pixelData = null;
            cubeFaceData = null;
            mipMapData = null;
            width = 0;
            height = 0;
            depth = 1;

            _sizeOfAllTexturesInMemory -= accountedSize;
            accountedSize = 0;
        }

        public int findMissingMipmaps()
        {
            int levels = 1;
            int w = width;
            int h = height;
            while ((w > 1) || (h > 1))
            {
                w = g3dmath.iMax(1, w / 2);
                h = g3dmath.iMax(1, h / 2);
                ++levels;
            }

            int present = (mipMapData == null || mipMapData.Count == 0) ? 1 : mipMapData.Count;
            return levels - present > 0 ? levels - present : 0;
        }

        public bool didLoad()
        {
            return level0Data() != null;
        }

        public byte[] getData()
        {
            return level0Data();
        }

        public GImage getRawImage()
        {
            byte[] data = level0Data();
            if (data == null)
            {
                throw new InvalidOperationException("The texture has no CPU-side pixel data.");
            }

            int channels = bytesPerTexel(pixelFormat == null ? format : pixelFormat);
            if (channels < 1)
            {
                channels = 4;
            }

            if (data.Length != width * height * channels)
            {
                throw new InvalidOperationException("The texture data does not match the texture dimensions.");
            }

            GImage image = new GImage(width, height, channels);
            image.setBytes(data);
            return image;
        }

        public byte[] readPixels(int x, int y, int readWidth, int readHeight)
        {
            if (readWidth < 0)
            {
                throw new ArgumentOutOfRangeException("readWidth");
            }
            if (readHeight < 0)
            {
                throw new ArgumentOutOfRangeException("readHeight");
            }

            GImage image = new GImage();
            getImage(image, TextureFormat.RGBA8);

            byte[] source = image.getBytes();
            byte[] result = new byte[readWidth * readHeight * 4];
            for (int row = 0; row < readHeight; ++row)
            {
                int sy = y + row;
                if (sy < 0 || sy >= image.height)
                {
                    continue;
                }
                for (int col = 0; col < readWidth; ++col)
                {
                    int sx = x + col;
                    if (sx < 0 || sx >= image.width)
                    {
                        continue;
                    }
                    Array.Copy(source, (sx + sy * image.width) * 4, result, (col + row * readWidth) * 4, 4);
                }
            }
            return result;
        }

        /** Set the autoMipMap value, which only affects textures when they are rendered
            to or copied from the screen.

            You can read the automipmap value from <code>settings().autoMipMap</code>.
        */
        public void setAutoMipMap(bool b)
        {
            _settings.autoMipMap = b;

            IGraphicsBackend backend = GLCaps.getBackendInstance();
            int id = backend == null ? 0 : getBackendTextureID(backend);
            if (backend != null && id != 0 && b && isMipMapFormat(interpolate))
            {
                TextureFormat sourceFormat = pixelFormat == null ? format : pixelFormat;
                if (sourceFormat != null && !sourceFormat.compressed && !sourceFormat.getIsDepth())
                {
                    backend.BindTexture(0, id, dimension);
                    backend.GenerateMipmap(backend.GetTextureTarget(dimension));
                }
            }
        }

        /** For a texture with automipmap off that supports the FrameBufferObject extension,
           generate mipmaps from the level 0 mipmap immediately.  For other textures, does nothing.*/
        public void generateMipMaps()
        {
            IGraphicsBackend backend = GLCaps.getBackendInstance();
            int id = backend == null ? 0 : getBackendTextureID(backend);
            if (backend == null || id == 0)
            {
                throw new InvalidOperationException("The texture must be allocated by a RenderDevice before generating mipmaps.");
            }
            backend.GenerateMipmap(backend.GetTextureTarget(dimension));
        }

        // Backend handle management
        internal object getBackendHandle()
        {
            return backendHandle;
        }

        internal void setBackendHandle(object handle)
        {
            backendHandle = handle;
        }

        internal void setTextureID(int id)
        {
            textureID = id;
        }

        internal void setDimensions(int w, int h, int d)
        {
            width = w;
            height = h;
            depth = d;
        }

        internal void setFormat(TextureFormat f)
        {
            format = f;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sizeOfAllTexturesInMemory -= accountedSize;
                accountedSize = 0;
                // Backend will clean up the texture
            }
            base.Dispose(disposing);
        }

        // Hash code for Texture::Parameters
        public static uint hashCode(Settings p)
        {
            return p.hashCode();
        }
    }
}