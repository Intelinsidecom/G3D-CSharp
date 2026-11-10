using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace G3D.OpenGL
{
    public sealed partial class OpenGLBackend : IGraphicsBackend
    {
        private OpenGLWindow window;
        private BackendCaps caps;
        private int currentVertexArray;
        private int currentProgram;
        private int currentFramebuffer;
        private Dictionary<int, int> textureTargets = new Dictionary<int, int>();
        private int maxTextureUnits;
        private Color4 clearValue = new Color4(0, 0, 0, 1);
        private readonly float[] matrixBuffer = new float[16];
        private readonly float[] lightPositionBuffer = new float[4];
        private readonly float[] lightColorBuffer = new float[4];
        private readonly float[] lightDirectionBuffer = new float[3];
        private int activeTextureUnit = -1;
        private readonly int[] boundTextures = new int[32];
        private readonly int[] boundTextureTargets = new int[32];
        private bool colorWriteEnabled = true;
        private bool alphaWriteEnabled = true;
        private RenderDevice.BlendFunc blendSrc = RenderDevice.BlendFunc.BLEND_ONE;
        private RenderDevice.BlendFunc blendDst = RenderDevice.BlendFunc.BLEND_ZERO;
        private RenderDevice.BlendEq blendEq = RenderDevice.BlendEq.BLENDEQ_ADD;
        private readonly Dictionary<TextureFormat, bool> supportedTextureFormats = new Dictionary<TextureFormat, bool>(TextureFormatReferenceComparer.Instance);
        private readonly Dictionary<int, IntPtr> milestones = new Dictionary<int, IntPtr>();
        private int nextMilestoneId = 1;

        private sealed class TextureFormatReferenceComparer : IEqualityComparer<TextureFormat>
        {
            public static readonly TextureFormatReferenceComparer Instance = new TextureFormatReferenceComparer();
            public bool Equals(TextureFormat x, TextureFormat y) { return ReferenceEquals(x, y); }
            public int GetHashCode(TextureFormat obj) { return global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
        }

        public OpenGLBackend()
        {
            caps = new BackendCaps();
        }

        public GraphicsBackendKind Kind
        {
            get { return GraphicsBackendKind.OpenGL; }
        }
        public string Description
        {
            get { return string.Format("{0} {1} {2}", caps.Vendor, caps.Renderer, caps.Version); }
        }

        public BackendCaps Caps
        {
            get { return caps; }
        }

        public void Initialize(IGraphicsWindow graphicsWindow)
        {
            window = (OpenGLWindow)graphicsWindow;
            window.MakeCurrent();

            GL.ClearColor(0, 0, 0, 1);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Normalize);
            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);
            GL.Viewport(0, 0, window.Width, window.Height);

            caps.Vendor = GL.GetString(StringName.Vendor) ?? string.Empty;
            caps.Renderer = GL.GetString(StringName.Renderer) ?? string.Empty;
            caps.Version = GL.GetString(StringName.Version) ?? string.Empty;
            caps.DriverVersion = GetDriverVersion();

            // BindTexture routes every texture unit change through glActiveTexture,
            // which is core in OpenGL 1.3.  When Windows falls back to the GDI
            // software rasterer the entry point is unresolvable and the first call
            // faults natively as an AccessViolationException, so the requirement is
            // checked up front and reported with the version that was actually
            // obtained.
            if (!supportsCoreVersion(1, 3))
            {
                throw new NotSupportedException(
                    "The OpenGL backend requires OpenGL 1.3 or later for glActiveTexture, but the context reports version \"" +
                    caps.Version + "\" from renderer \"" + caps.Renderer + "\".");
            }
            OpenGLCapabilities.Extensions.Clear();
            string extensionString = GL.GetString(StringName.Extensions);
            if (!string.IsNullOrEmpty(extensionString))
            {
                string[] extensionTokens = extensionString.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < extensionTokens.Length; ++i)
                {
                    OpenGLCapabilities.Extensions.Add(extensionTokens[i]);
                }
            }
            caps.MaxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);
            caps.MaxTextureUnits = Math.Min(G3D.GLCaps.G3D_MAX_TEXTURE_UNITS, Math.Max(1, GL.GetInteger(GetPName.MaxTextureUnits)));
            caps.MaxTextures = caps.MaxTextureUnits;
            caps.MaxTextureCoords = caps.MaxTextureUnits;
            maxTextureUnits = caps.MaxTextureUnits;
            caps.MaxLights = 8;
            caps.MaxColorAttachments = Math.Max(1, GL.GetInteger(GetPName.MaxDrawBuffers));
            caps.SupportsStencil = GL.GetInteger(GetPName.StencilBits) > 0;
            caps.SupportsTwoSidedStencil = SupportsTwoSidedStencil();
            caps.SupportsCubeMaps = SupportsExtension("GL_EXT_texture_cube_map") || SupportsExtension("GL_ARB_texture_cube_map");
            caps.SupportsVertexProgram = SupportsExtension("GL_ARB_vertex_program") || SupportsExtension("GL_ARB_vertex_shader");
            caps.SupportsVertexProgramNV2 = SupportsExtension("GL_NV_vertex_program2");
            caps.SupportsPixelProgram = SupportsExtension("GL_ARB_fragment_program") || SupportsExtension("GL_ARB_fragment_shader");
            caps.SupportsVertexBufferObject = SupportsExtension("GL_ARB_vertex_buffer_object");
            caps.SupportsTextureRectangle = SupportsExtension("GL_EXT_texture_rectangle") || SupportsExtension("GL_NV_texture_rectangle");
            caps.SupportsShadows = SupportsExtension("GL_ARB_shadow") || SupportsExtension("GL_ARB_shadow_ambient");
            caps.SupportsAlphaTest = true;
            caps.SupportsTextureCombineModes = true;
            caps.SupportsTextureLODBias = SupportsExtension("GL_EXT_texture_lod_bias") || supportsCoreVersion(1, 4);
            caps.SupportsGenericVertexAttributes = true;
            caps.SupportsStereoDrawBuffers = SupportsExtension("GL_EXT_stereo");
            caps.SupportsReflectionMaps = caps.SupportsCubeMaps;
            caps.VideoMemorySize = 0;
            caps.Extensions = new List<string>(OpenGLCapabilities.Extensions);
            caps.ShaderLanguages = new List<GraphicsShaderLanguage>();
            caps.ShaderLanguages.Add(GraphicsShaderLanguage.GLSL);
            OpenGLCapabilities.initialize(caps.Version, caps.Vendor, caps.Renderer, caps.DriverVersion, caps.MaxTextureUnits, caps.MaxTextures, caps.MaxTextureCoords);

            // The hardware-bug probes call GL 1.3+ entry points (glActiveTexture,
            // multi-texture cube-map texgen).  On a machine that has fallen back
            // to the software rasterizer - which reports OpenGL 1.1.0 - those
            // entry points do not exist, so the probes would fault natively
            // instead of returning a result.  A software context has none of the
            // driver bugs being probed for, so the safe documented fallback is
            // "no bug".
            bool probesAvailable = supportsCoreVersion(1, 3);
            if (!probesAvailable)
            {
                OpenGLCapabilities.BugRedBlueMipmapSwap = false;
                OpenGLCapabilities.BugMultiTexCoord3fvARB = false;
                OpenGLCapabilities.BugNormalMapTexGen = false;
            }
            else
            {
                OpenGLCapabilities.BugRedBlueMipmapSwap = checkRedBlueMipmapSwap();
                checkCubeMapBugs();
            }
            OpenGLCapabilities.BugMipmapGeneration = SupportsExtension("GL_SGIS_generate_mipmap") &&
                (caps.Renderer.StartsWith("MOBILITY RADEON 90", StringComparison.Ordinal) ||
                 caps.Renderer.StartsWith("MOBILITY RADEON 57", StringComparison.Ordinal) ||
                 caps.Renderer.StartsWith("Intel 845G", StringComparison.Ordinal) ||
                 caps.Renderer.StartsWith("Intel 854G", StringComparison.Ordinal));
            OpenGLCapabilities.BugSlowVBO = caps.SupportsVertexBufferObject && caps.Renderer.StartsWith("MOBILITY RADEON 7500", StringComparison.Ordinal);
            caps.HasBug_glMultiTexCoord3fvARB = OpenGLCapabilities.BugMultiTexCoord3fvARB;
            caps.HasBug_normalMapTexGen = OpenGLCapabilities.BugNormalMapTexGen;
            caps.HasBug_redBlueMipmapSwap = OpenGLCapabilities.BugRedBlueMipmapSwap;
            caps.HasBug_mipmapGeneration = OpenGLCapabilities.BugMipmapGeneration;
            caps.HasBug_slowVBO = OpenGLCapabilities.BugSlowVBO;
            G3D.GLCaps.setBackend(this);

            TextureFormat.screenDepthBitsProvider = delegate { return GL.GetInteger(GetPName.DepthBits); };
            TextureFormat.screenStencilBitsProvider = delegate { return GL.GetInteger(GetPName.StencilBits); };

            // Initialize texture targets
            for (int i = 0; i < maxTextureUnits; ++i)
            {
                textureTargets[i] = (int)TextureTarget.Texture2D;
            }

            // Enable seamless cube map
            GL.Enable(EnableCap.TextureCubeMapSeamless);
        }

        public void BeginFrame()
        {
        }

        private bool supportsCoreVersion(int major, int minor)
        {
            string version = caps.Version;
            int firstDot = version.IndexOf('.');
            if (firstDot < 0)
            {
                return false;
            }
            int parsedMajor;
            if (!int.TryParse(version.Substring(0, firstDot), out parsedMajor))
            {
                return false;
            }
            int secondDot = version.IndexOf('.', firstDot + 1);
            string minorText = secondDot < 0 ? version.Substring(firstDot + 1) : version.Substring(firstDot + 1, secondDot - firstDot - 1);
            int parsedMinor;
            if (!int.TryParse(minorText, out parsedMinor))
            {
                return false;
            }
            return parsedMajor > major || (parsedMajor == major && parsedMinor >= minor);
        }

        public void Clear(bool color, bool depth, bool stencil)
        {
            GL.ClearColor(clearValue.r, clearValue.g, clearValue.b, clearValue.a);
            ClearBufferMask mask = 0;
            if (color) mask |= ClearBufferMask.ColorBufferBit;
            if (depth) mask |= ClearBufferMask.DepthBufferBit;
            if (stencil) mask |= ClearBufferMask.StencilBufferBit;
            GL.Clear(mask);
        }

        public void EndFrame()
        {
            window.Present();
        }

        public void Resize(int width, int height)
        {
            if (window == null) return;
            // The host window may already be closed; a resize that arrives
            // after disposal has nothing to apply.
            try
            {
                window.MakeCurrent();
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            GL.Viewport(0, 0, width, height);
        }

        public void Dispose()
        {
            window = null;
        }

        // Texture management
        public int CreateTexture()
        {
            int id = GL.GenTexture();
            return id;
        }

        public int CreateTexture(TextureFormat format, int width, int height, Texture.DepthReadMode depthReadMode)
        {
            int id = GL.GenTexture();
            if (format != null && format != TextureFormat.RGB_DXT1 && format != TextureFormat.RGBA_DXT1 &&
                format != TextureFormat.RGBA_DXT3 && format != TextureFormat.RGBA_DXT5)
            {
                GL.BindTexture(TextureTarget.Texture2D, id);
                PixelInternalFormat internalFormat = (PixelInternalFormat)GLTextureFormatMap.internalFormat(format);
                PixelFormat pixelFormat = (PixelFormat)GLTextureFormatMap.baseFormat(format);
                PixelType pixelType = (PixelType)GLTextureFormatMap.type(format);
                GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, pixelFormat, pixelType, IntPtr.Zero);
            }
            return id;
        }

        public void DeleteTexture(int textureID)
        {
            if (textureID > 0)
            {
                GL.DeleteTexture(textureID);
            }
        }

        private void setActiveTextureUnit(int textureUnit)
        {
            if (activeTextureUnit != textureUnit)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                activeTextureUnit = textureUnit;
            }
        }

        private void bindTextureToUnit(int textureUnit, int textureID, TextureTarget target)
        {
            setActiveTextureUnit(textureUnit);

            if (boundTextures[textureUnit] != textureID || boundTextureTargets[textureUnit] != (int)target)
            {
                GL.BindTexture(target, textureID);
                boundTextures[textureUnit] = textureID;
                boundTextureTargets[textureUnit] = (int)target;
            }

            if (textureID == 0)
            {
                GL.Disable(EnableCap.Texture2D);
            }
            else if (target == TextureTarget.TextureCubeMap)
            {
                GL.Enable(EnableCap.TextureCubeMap);
                GL.Disable(EnableCap.Texture2D);
            }
            else
            {
                GL.Enable(EnableCap.Texture2D);
            }
        }

        public void BindTexture(int textureUnit, int textureID, Texture.Dimension dimension)
        {
            TextureTarget target;
            switch (dimension)
            {
                case Texture.Dimension.DIM_2D:
                case Texture.Dimension.DIM_2D_NPOT:
                    target = TextureTarget.Texture2D;
                    break;
                case Texture.Dimension.DIM_2D_RECT:
                    target = TextureTarget.TextureRectangle;
                    break;
                case Texture.Dimension.DIM_CUBE_MAP:
                case Texture.Dimension.DIM_CUBE_MAP_NPOT:
                    target = TextureTarget.TextureCubeMap;
                    break;
                default:
                    target = TextureTarget.Texture2D;
                    break;
            }
            textureTargets[textureUnit] = (int)target;
            bindTextureToUnit(textureUnit, textureID, target);
        }

        public int GetTextureTarget(Texture.Dimension dimension)
        {
            switch (dimension)
            {
                case Texture.Dimension.DIM_2D:
                case Texture.Dimension.DIM_2D_NPOT:
                    return (int)TextureTarget.Texture2D;
                case Texture.Dimension.DIM_2D_RECT:
                    return (int)TextureTarget.TextureRectangle;
                case Texture.Dimension.DIM_CUBE_MAP:
                case Texture.Dimension.DIM_CUBE_MAP_NPOT:
                    return (int)TextureTarget.TextureCubeMap;
                default:
                    return (int)TextureTarget.Texture2D;
            }
        }

        public void TexImage2D(int target, int level, TextureFormat format, int width, int height, int border, byte[] data)
        {
            if (target >= (int)TextureTarget.TextureCubeMap && target < (int)TextureTarget.TextureCubeMap + 6)
            {
                target = (int)TextureTarget.TextureCubeMapPositiveX + (target - (int)TextureTarget.TextureCubeMap);
            }
            TextureTarget tt = (TextureTarget)target;
            PixelInternalFormat internalFormat = (PixelInternalFormat)GLTextureFormatMap.internalFormat(format);
            PixelFormat pixelFormat = (PixelFormat)GLTextureFormatMap.baseFormat(format);
            PixelType pixelType = (PixelType)GLTextureFormatMap.type(format);

            GL.TexImage2D(tt, level, internalFormat, width, height, border, pixelFormat, pixelType, data);
        }

        public void CompressedTexImage2D(int target, TextureFormat format, int width, int height, List<List<byte[]>> mipMaps)
        {
            TextureTarget tt = (TextureTarget)target;
            InternalFormat internalFormat;
            if (format == TextureFormat.RGB_DXT1)
            {
                internalFormat = InternalFormat.CompressedRgbS3tcDxt1Ext;
            }
            else if (format == TextureFormat.RGBA_DXT1)
            {
                internalFormat = InternalFormat.CompressedRgbaS3tcDxt1Ext;
            }
            else if (format == TextureFormat.RGBA_DXT3)
            {
                internalFormat = InternalFormat.CompressedRgbaS3tcDxt3Ext;
            }
            else if (format == TextureFormat.RGBA_DXT5)
            {
                internalFormat = InternalFormat.CompressedRgbaS3tcDxt5Ext;
            }
            else
            {
                throw new NotSupportedException("The texture format is not compressed.");
            }
            int levelWidth = width;
            int levelHeight = height;
            for (int level = 0; level < mipMaps.Count; ++level)
            {
                List<byte[]> faces = mipMaps[level];
                for (int face = 0; face < faces.Count; ++face)
                {
                    TextureTarget faceTarget = faces.Count == 6
                        ? (TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + face)
                        : tt;
                    byte[] data = faces[face];
                    GL.CompressedTexImage2D(faceTarget, level, internalFormat, levelWidth, levelHeight, 0, data.Length, data);
                }
                levelWidth = Math.Max(1, levelWidth / 2);
                levelHeight = Math.Max(1, levelHeight / 2);
            }
        }

        public void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, TextureFormat format, byte[] data)
        {
            TextureTarget tt = (TextureTarget)target;
            PixelFormat pixelFormat = (PixelFormat)GLTextureFormatMap.baseFormat(format);
            PixelType pixelType = (PixelType)GLTextureFormatMap.type(format);

            GL.TexSubImage2D(tt, level, xoffset, yoffset, width, height, pixelFormat, pixelType, data);
        }

        public void GenerateMipmap(int target)
        {
            GL.GenerateMipmap((GenerateMipmapTarget)target);
        }

        public void TexParameter(int target, Texture.WrapMode wrapS, Texture.WrapMode wrapT, Texture.InterpolateMode minFilter, Texture.InterpolateMode magFilter, float maxAnisotropy)
        {
            TextureTarget tt = (TextureTarget)target;

            TextureWrapMode wrapSMode = wrapS == Texture.WrapMode.CLAMP ? TextureWrapMode.ClampToEdge :
                                        wrapS == Texture.WrapMode.TRANSPARENT_BORDER ? TextureWrapMode.ClampToBorder :
                                        TextureWrapMode.Repeat;
            TextureWrapMode wrapTMode = wrapT == Texture.WrapMode.CLAMP ? TextureWrapMode.ClampToEdge :
                                        wrapT == Texture.WrapMode.TRANSPARENT_BORDER ? TextureWrapMode.ClampToBorder :
                                        TextureWrapMode.Repeat;

            GL.TexParameter(tt, TextureParameterName.TextureWrapS, (int)wrapSMode);
            GL.TexParameter(tt, TextureParameterName.TextureWrapT, (int)wrapTMode);

            TextureMinFilter minFilterMode;
            TextureMagFilter magFilterMode;

            switch (minFilter)
            {
                case Texture.InterpolateMode.TRILINEAR_MIPMAP:
                    minFilterMode = TextureMinFilter.LinearMipmapLinear;
                    break;
                case Texture.InterpolateMode.BILINEAR_MIPMAP:
                    minFilterMode = TextureMinFilter.LinearMipmapNearest;
                    break;
                case Texture.InterpolateMode.NEAREST_MIPMAP:
                    minFilterMode = TextureMinFilter.NearestMipmapNearest;
                    break;
                case Texture.InterpolateMode.BILINEAR_NO_MIPMAP:
                    minFilterMode = TextureMinFilter.Linear;
                    break;
                case Texture.InterpolateMode.NEAREST_NO_MIPMAP:
                case Texture.InterpolateMode.NO_INTERPOLATION:
                    minFilterMode = TextureMinFilter.Nearest;
                    break;
                default:
                    minFilterMode = TextureMinFilter.LinearMipmapLinear;
                    break;
            }

            switch (magFilter)
            {
                case Texture.InterpolateMode.TRILINEAR_MIPMAP:
                case Texture.InterpolateMode.BILINEAR_MIPMAP:
                case Texture.InterpolateMode.BILINEAR_NO_MIPMAP:
                    magFilterMode = TextureMagFilter.Linear;
                    break;
                default:
                    magFilterMode = TextureMagFilter.Nearest;
                    break;
            }

            GL.TexParameter(tt, TextureParameterName.TextureMinFilter, (int)minFilterMode);
            GL.TexParameter(tt, TextureParameterName.TextureMagFilter, (int)magFilterMode);

            if (maxAnisotropy > 1.0f)
            {
                GL.TexParameter(tt, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, maxAnisotropy);
            }
        }

        public void SetTextureDepthCompare(int target, Texture.DepthReadMode mode)
        {
            TextureTarget tt = (TextureTarget)target;
            if (mode == Texture.DepthReadMode.DEPTH_NORMAL)
            {
                GL.TexParameter(tt, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);
                return;
            }
            GL.TexParameter(tt, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRToTexture);
            GL.TexParameter(tt, TextureParameterName.TextureCompareFunc, mode == Texture.DepthReadMode.DEPTH_LEQUAL ? 515 : 516);
        }

        public void ConfigureShadowMap(int textureUnit, Matrix4 lightMVP, Matrix4 cameraToWorldMatrixInverse, int textureID, Texture.DepthReadMode mode)
        {
            bindTextureToUnit(textureUnit, textureID, TextureTarget.Texture2D);
            SetTextureDepthCompare((int)TextureTarget.Texture2D, mode);

            Matrix4 bias = new Matrix4(
                0.5f, 0.0f, 0.0f, 0.5f,
                0.0f, 0.5f, 0.0f, 0.5f,
                0.0f, 0.0f, 0.5f, 0.5f - 0.000001f,
                0.0f, 0.0f, 0.0f, 1.0f);
            Matrix4 coordinateMatrix = bias * lightMVP;

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            float[] modelview = new float[16];
            for (int r = 0; r < 4; ++r)
                for (int c = 0; c < 4; ++c)
                    modelview[c * 4 + r] = cameraToWorldMatrixInverse.elt(r, c);
            GL.LoadMatrix(modelview);

            GL.TexGen(TextureCoordName.S, TextureGenParameter.TextureGenMode, (int)TextureGenMode.EyeLinear);
            GL.TexGen(TextureCoordName.S, TextureGenParameter.EyePlane, new float[] { coordinateMatrix.elt(0, 0), coordinateMatrix.elt(0, 1), coordinateMatrix.elt(0, 2), coordinateMatrix.elt(0, 3) });
            GL.TexGen(TextureCoordName.T, TextureGenParameter.TextureGenMode, (int)TextureGenMode.EyeLinear);
            GL.TexGen(TextureCoordName.T, TextureGenParameter.EyePlane, new float[] { coordinateMatrix.elt(1, 0), coordinateMatrix.elt(1, 1), coordinateMatrix.elt(1, 2), coordinateMatrix.elt(1, 3) });
            GL.TexGen(TextureCoordName.R, TextureGenParameter.TextureGenMode, (int)TextureGenMode.EyeLinear);
            GL.TexGen(TextureCoordName.R, TextureGenParameter.EyePlane, new float[] { coordinateMatrix.elt(2, 0), coordinateMatrix.elt(2, 1), coordinateMatrix.elt(2, 2), coordinateMatrix.elt(2, 3) });
            GL.TexGen(TextureCoordName.Q, TextureGenParameter.TextureGenMode, (int)TextureGenMode.EyeLinear);
            GL.TexGen(TextureCoordName.Q, TextureGenParameter.EyePlane, new float[] { coordinateMatrix.elt(3, 0), coordinateMatrix.elt(3, 1), coordinateMatrix.elt(3, 2), coordinateMatrix.elt(3, 3) });
            GL.Enable(EnableCap.TextureGenS);
            GL.Enable(EnableCap.TextureGenT);
            GL.Enable(EnableCap.TextureGenR);
            GL.Enable(EnableCap.TextureGenQ);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);

            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
        }

        public void DisableShadowMap(int textureUnit)
        {
            setActiveTextureUnit(textureUnit);
            GL.Disable(EnableCap.TextureGenS);
            GL.Disable(EnableCap.TextureGenT);
            GL.Disable(EnableCap.TextureGenR);
            GL.Disable(EnableCap.TextureGenQ);
        }

        public void CopyTexImage2D(int target, int level, int x, int y, int width, int height, int border)
        {
            TextureTarget tt = (TextureTarget)target;
            GL.CopyTexImage2D(tt, level, InternalFormat.Rgba, x, y, width, height, border);
        }

        public void CopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height)
        {
            TextureTarget tt = (TextureTarget)target;
            GL.CopyTexSubImage2D(tt, level, xoffset, yoffset, x, y, width, height);
        }

        public void GetTexImage(int target, int level, TextureFormat format, byte[] data)
        {
            TextureTarget tt = (TextureTarget)target;
            PixelFormat pixelFormat = (PixelFormat)GLTextureFormatMap.baseFormat(format);
            PixelType pixelType = (PixelType)GLTextureFormatMap.type(format);
            GL.GetTexImage(tt, level, pixelFormat, pixelType, data);
        }

        // Shader management
        public bool SupportsShaderLanguage(GraphicsShaderLanguage language)
        {
            return language == GraphicsShaderLanguage.GLSL;
        }

        public int CreateShaderProgram(GraphicsShaderLanguage language, string vertexSource, string fragmentSource)
        {
            if (language != GraphicsShaderLanguage.GLSL)
            {
                throw new NotSupportedException("The OpenGL backend supports GLSL shader programs only.");
            }
            ShaderProgramInfo info = CompileShaderProgram(language, vertexSource, fragmentSource);
            if (!info.Success)
            {
                throw new InvalidOperationException(info.Messages);
            }
            return info.ProgramID;
        }

        public ShaderProgramInfo CompileShaderProgram(GraphicsShaderLanguage language, string vertexSource, string fragmentSource)
        {
            if (language != GraphicsShaderLanguage.GLSL)
            {
                throw new NotSupportedException("The OpenGL backend supports GLSL shader programs only.");
            }
            ShaderProgramInfo info = new ShaderProgramInfo();
            info.Language = language;
            info.ProgramID = GL.CreateProgram();

            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vertexSource);
            GL.CompileShader(vs);
            int status;
            GL.GetShader(vs, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                info.VertexErrors = GL.GetShaderInfoLog(vs) ?? "";
            }
            else
            {
                GL.AttachShader(info.ProgramID, vs);
            }

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fragmentSource);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                info.PixelErrors = GL.GetShaderInfoLog(fs) ?? "";
            }
            else
            {
                GL.AttachShader(info.ProgramID, fs);
            }

            if (info.VertexErrors.Length == 0 && info.PixelErrors.Length == 0)
            {
                GL.LinkProgram(info.ProgramID);
                GL.GetProgram(info.ProgramID, GetProgramParameterName.LinkStatus, out status);
                if (status == 0)
                {
                    info.LinkErrors = GL.GetProgramInfoLog(info.ProgramID) ?? "";
                }
                else
                {
                    ReflectProgram(info);
                }
            }

            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
            info.Success = info.VertexErrors.Length == 0 && info.PixelErrors.Length == 0 && info.LinkErrors.Length == 0;
            info.Messages = info.VertexErrors + info.PixelErrors + info.LinkErrors;
            return info;
        }

        private void ReflectProgram(ShaderProgramInfo info)
        {
            int count;
            GL.GetProgram(info.ProgramID, GetProgramParameterName.ActiveUniforms, out count);
            int nextTextureUnit = 0;
            for (int i = 0; i < count; ++i)
            {
                int size;
                ActiveUniformType type;
                string name = GL.GetActiveUniform(info.ProgramID, i, out size, out type);
                ShaderUniformInfo uniform = new ShaderUniformInfo();
                uniform.Name = name;
                uniform.Size = size;
                uniform.Type = translateUniformType(type);
                uniform.Location = GL.GetUniformLocation(info.ProgramID, name);
                if (isSamplerType(uniform.Type))
                {
                    uniform.TextureUnit = nextTextureUnit++;
                }
                info.Uniforms.Add(uniform);
            }

            GL.GetProgram(info.ProgramID, GetProgramParameterName.ActiveAttributes, out count);
            for (int i = 0; i < count; ++i)
            {
                int size;
                ActiveAttribType type;
                string name = GL.GetActiveAttrib(info.ProgramID, i, out size, out type);
                ShaderAttributeInfo attribute = new ShaderAttributeInfo();
                attribute.Name = name;
                attribute.Location = GL.GetAttribLocation(info.ProgramID, name);
                attribute.ComponentCount = translateAttributeComponentCount(type);
                info.Attributes.Add(attribute);
            }
        }

        private static bool isSamplerType(ShaderProgramType type)
        {
            return type == ShaderProgramType.Sampler1D || type == ShaderProgramType.Sampler2D ||
                   type == ShaderProgramType.Sampler3D || type == ShaderProgramType.SamplerCube ||
                   type == ShaderProgramType.SamplerRectangle;
        }

        private static ShaderProgramType translateUniformType(ActiveUniformType type)
        {
            switch (type)
            {
                case ActiveUniformType.Float: return ShaderProgramType.Float;
                case ActiveUniformType.FloatVec2: return ShaderProgramType.Float2;
                case ActiveUniformType.FloatVec3: return ShaderProgramType.Float3;
                case ActiveUniformType.FloatVec4: return ShaderProgramType.Float4;
                case ActiveUniformType.Int: return ShaderProgramType.Int;
                case ActiveUniformType.IntVec2: return ShaderProgramType.Int2;
                case ActiveUniformType.IntVec3: return ShaderProgramType.Int3;
                case ActiveUniformType.IntVec4: return ShaderProgramType.Int4;
                case ActiveUniformType.Bool: return ShaderProgramType.Bool;
                case ActiveUniformType.BoolVec2: return ShaderProgramType.Bool2;
                case ActiveUniformType.BoolVec3: return ShaderProgramType.Bool3;
                case ActiveUniformType.BoolVec4: return ShaderProgramType.Bool4;
                case ActiveUniformType.FloatMat3: return ShaderProgramType.Matrix3;
                case ActiveUniformType.FloatMat4: return ShaderProgramType.Matrix4;
                case ActiveUniformType.Sampler1D: return ShaderProgramType.Sampler1D;
                case ActiveUniformType.Sampler2D: return ShaderProgramType.Sampler2D;
                case ActiveUniformType.Sampler3D: return ShaderProgramType.Sampler3D;
                case ActiveUniformType.SamplerCube: return ShaderProgramType.SamplerCube;
                case ActiveUniformType.Sampler2DRect: return ShaderProgramType.SamplerRectangle;
                default: throw new NotSupportedException("Unsupported active uniform type: " + type);
            }
        }

        private static int translateAttributeComponentCount(ActiveAttribType type)
        {
            switch (type)
            {
                case ActiveAttribType.Float: return 1;
                case ActiveAttribType.FloatVec2: return 2;
                case ActiveAttribType.FloatVec3: return 3;
                case ActiveAttribType.FloatVec4: return 4;
                case ActiveAttribType.Int: return 1;
                case ActiveAttribType.IntVec2: return 2;
                case ActiveAttribType.IntVec3: return 3;
                case ActiveAttribType.IntVec4: return 4;
                default: return 4;
            }
        }

        public int CreateShaderProgram(string vertexSource, string fragmentSource)
        {
            int program = GL.CreateProgram();

            int vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vertexSource);
            GL.CompileShader(vs);
            CheckShaderCompile(vs, "Vertex");
            GL.AttachShader(program, vs);

            int fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fragmentSource);
            GL.CompileShader(fs);
            CheckShaderCompile(fs, "Fragment");
            GL.AttachShader(program, fs);

            GL.LinkProgram(program);
            CheckProgramLink(program);

            GL.DeleteShader(vs);
            GL.DeleteShader(fs);

            return program;
        }

        private void CheckShaderCompile(int shader, string type)
        {
            int status;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                string log = GL.GetShaderInfoLog(shader);
                throw new Exception(type + " shader compilation failed: " + log);
            }
        }

        private void CheckProgramLink(int program)
        {
            int status;
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                string log = GL.GetProgramInfoLog(program);
                throw new Exception("Program linking failed: " + log);
            }
        }

        public void DeleteShaderProgram(int programID)
        {
            if (programID > 0)
            {
                GL.DeleteProgram(programID);
            }
        }

        public void UseShaderProgram(int programID)
        {
            if (programID != currentProgram)
            {
                GL.UseProgram(programID);
                currentProgram = programID;
            }
        }

        public int GetUniformLocation(int programID, string name)
        {
            return GL.GetUniformLocation(programID, name);
        }

        public int GetAttribLocation(int programID, string name)
        {
            return GL.GetAttribLocation(programID, name);
        }

        public void UniformMatrix4fv(int location, Matrix4 value)
        {
            // Match the original GLG3D byte order: the array is stored column-major
            // (m[c*4+r] = value[r][c]) and uploaded with transpose = GL_FALSE.
            float[] m = new float[16];
            for (int c = 0; c < 4; ++c)
                for (int r = 0; r < 4; ++r)
                    m[c * 4 + r] = value.elt(r, c);
            GL.UniformMatrix4(location, 1, false, m);
        }

        public void UniformMatrix3fv(int location, Matrix3 value)
        {
            // See UniformMatrix4fv for the byte-order convention.
            float[] m = new float[9];
            for (int c = 0; c < 3; ++c)
                for (int r = 0; r < 3; ++r)
                    m[c * 3 + r] = value.elt(r, c);
            GL.UniformMatrix3(location, 1, false, m);
        }

        public void Uniform4fv(int location, Vector4 value)
        {
            GL.Uniform4(location, value.x, value.y, value.z, value.w);
        }

        public void Uniform3fv(int location, Vector3 value)
        {
            GL.Uniform3(location, value.x, value.y, value.z);
        }

        public void Uniform2fv(int location, Vector2 value)
        {
            GL.Uniform2(location, value.x, value.y);
        }

        public void Uniform1f(int location, float value)
        {
            GL.Uniform1(location, value);
        }

        public void Uniform1i(int location, int value)
        {
            GL.Uniform1(location, value);
        }

        public void UniformTexture(int location, int textureUnit)
        {
            GL.Uniform1(location, textureUnit);
        }

        public void SetShaderTexture(int programID, int textureUnit, int textureID, Texture.Dimension dimension)
        {
            BindTexture(textureUnit, textureID, dimension);
        }

        // Abstraction over NVIDIA/ARB extensions
        public int GenAssemblyProgram(int unit)
        {
            int id;
            GL.Arb.GenProgram(1, out id);
            return id;
        }

        public void DeleteAssemblyProgram(int program)
        {
            if (program > 0)
            {
                GL.Arb.DeleteProgram(program);
            }
        }

        public void BindAssemblyProgram(int unit, int program)
        {
            GL.Arb.BindProgram(toAssemblyProgramTarget(unit), program);
        }

        public void LoadAssemblyProgram(int unit, int program, string code)
        {
            byte[] bytes = global::System.Text.Encoding.ASCII.GetBytes(code);
            GL.Arb.ProgramString(toAssemblyProgramTarget(unit), (All)0x8875, bytes.Length, bytes);
        }

        public void SetAssemblyProgramLocalParameter(int unit, int slot, Vector4 value)
        {
            GL.Arb.ProgramLocalParameter4(toAssemblyProgramTarget(unit), slot, value.x, value.y, value.z, value.w);
        }

        public void GetAssemblyProgramError(int unit, out int pos, out string msg)
        {
            pos = GL.GetInteger((GetPName)0x864B);
            msg = GL.GetString((StringName)0x8874) ?? "";
        }

        public void EnableAssemblyProgram(int unit, bool enabled)
        {
            if (enabled)
            {
                GL.Enable((EnableCap)unit);
            }
            else
            {
                GL.Disable((EnableCap)unit);
            }
        }

        private static AssemblyProgramTargetArb toAssemblyProgramTarget(int unit)
        {
            if (unit == 0x8620)
            {
                return AssemblyProgramTargetArb.VertexProgram;
            }
            if (unit == 0x8804)
            {
                return AssemblyProgramTargetArb.FragmentProgram;
            }
            throw new ArgumentException("Unsupported assembly program unit 0x" + unit.ToString("X4") + ".");
        }

        // Vertex Array / VBO management
        public int CreateVertexArray()
        {
            int vao = GL.GenVertexArray();
            return vao;
        }

        public void DeleteVertexArray(int vaoID)
        {
            if (vaoID > 0)
            {
                GL.DeleteVertexArray(vaoID);
            }
        }

        public void BindVertexArray(int vaoID)
        {
            if (vaoID != currentVertexArray)
            {
                GL.BindVertexArray(vaoID);
                currentVertexArray = vaoID;
            }
        }

        public int CreateBuffer()
        {
            int buffer = GL.GenBuffer();
            return buffer;
        }

        public void DeleteBuffer(int bufferID)
        {
            if (bufferID > 0)
            {
                // The GL context lives as long as the host control; once the
                // window is closed and the control disposed the entry points
                // still resolve but fault.  Deleting an already-orphaned buffer
                // name cannot be observed, so the call is skipped, matching the
                // guard OpenGLWindow.Present already applies.
                if (window != null && !window.Disposed)
                {
                    GL.DeleteBuffer(bufferID);
                }
            }
        }

        public void BindBuffer(int target, int bufferID)
        {
            BufferTarget bt = (BufferTarget)target;
            GL.BindBuffer(bt, bufferID);
        }

        public void BufferData(int target, int size, byte[] data, VAR.Usage usage)
        {
            BufferTarget bt = (BufferTarget)target;
            BufferUsageHint hint = usage == VAR.Usage.STATIC ? BufferUsageHint.StaticDraw :
                                   usage == VAR.Usage.DYNAMIC ? BufferUsageHint.DynamicDraw :
                                   BufferUsageHint.StreamDraw;
            GL.BufferData(bt, new IntPtr(size), data, hint);
        }

        public void BufferSubData(int target, int offset, int size, byte[] data)
        {
            BufferTarget bt = (BufferTarget)target;
            GL.BufferSubData(bt, new IntPtr(offset), new IntPtr(size), data);
        }

        public void VertexAttribPointer(int index, int size, int type, bool normalized, int stride, int offset)
        {
            VertexAttribPointerType vt = (VertexAttribPointerType)type;
            GL.VertexAttribPointer(index, size, vt, normalized, stride, offset);
        }

        public void EnableVertexAttribArray(int index)
        {
            GL.EnableVertexAttribArray(index);
        }

        public void DisableVertexAttribArray(int index)
        {
            GL.DisableVertexAttribArray(index);
        }

        public void SetVertexAttribArray(int attribNum, VAR v, bool normalize)
        {
            if (v == null)
            {
                return;
            }
            int buffer = getVARBuffer(v);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            GL.VertexAttribPointer(attribNum, vertexComponentCount(v.getType()), VertexAttribPointerType.Float, normalize, v.getStride(), IntPtr.Zero);
            GL.EnableVertexAttribArray(attribNum);
        }

        private static int vertexComponentCount(VAR.Type type)
        {
            switch (type)
            {
                case VAR.Type.VERTEX:
                case VAR.Type.NORMAL:
                case VAR.Type.TEXCOORD0:
                case VAR.Type.TEXCOORD1:
                case VAR.Type.TEXCOORD2:
                case VAR.Type.TEXCOORD3:
                case VAR.Type.TEXCOORD4:
                case VAR.Type.TEXCOORD5:
                case VAR.Type.TEXCOORD6:
                case VAR.Type.TEXCOORD7:
                    return 3;
                case VAR.Type.COLOR:
                    return 4;
                case VAR.Type.INDEX:
                    return 1;
                default:
                    return 4;
            }
        }

        // Framebuffer management
        public int CreateFramebuffer()
        {
            int fbo = GL.GenFramebuffer();
            return fbo;
        }

        public void DeleteFramebuffer(int framebufferID)
        {
            if (framebufferID > 0)
            {
                GL.DeleteFramebuffer(framebufferID);
            }
            framebufferHasColorAttachment.Remove(framebufferID);
        }

        public void BindFramebuffer(int framebufferID)
        {
            if (framebufferID != currentFramebuffer)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferID);
                currentFramebuffer = framebufferID;
            }
        }

        public int GetBoundFramebuffer()
        {
            return currentFramebuffer;
        }

        public void FramebufferTexture2D(int target, int attachment, int textarget, int textureID, int level)
        {
            FramebufferTarget ft = (FramebufferTarget)target;
            FramebufferAttachment fa = (FramebufferAttachment)attachment;
            TextureTarget tt = (TextureTarget)textarget;
            GL.FramebufferTexture2D(ft, fa, tt, textureID, level);
        }

        public void AttachFramebufferTexture(int framebufferID, Framebuffer.AttachmentPoint attachment, int textureID)
        {
            AttachFramebufferTexture(framebufferID, attachment, textureID, 0, -1);
        }

        public void AttachFramebufferTexture(int framebufferID, Framebuffer.AttachmentPoint attachment, int textureID, int mipLevel, int cubeFace)
        {
            BindFramebuffer(framebufferID);
            FramebufferAttachment glAttachment = (FramebufferAttachment)(int)attachment;
            TextureTarget textureTarget = cubeFace >= 0 ? (TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + cubeFace) : TextureTarget.Texture2D;
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, glAttachment, textureTarget, textureID, mipLevel);
            updateFramebufferBuffers(framebufferID, attachment);
        }

        public void AttachFramebufferRenderbuffer(int framebufferID, Framebuffer.AttachmentPoint attachment, int renderbufferID)
        {
            BindFramebuffer(framebufferID);
            FramebufferAttachment glAttachment = (FramebufferAttachment)(int)attachment;
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, glAttachment, RenderbufferTarget.Renderbuffer, renderbufferID);
            updateFramebufferBuffers(framebufferID, attachment);
        }

        private readonly Dictionary<int, bool> framebufferHasColorAttachment = new Dictionary<int, bool>();

        private void updateFramebufferBuffers(int framebufferID, Framebuffer.AttachmentPoint attachment)
        {
            bool hasColor;
            if (!framebufferHasColorAttachment.TryGetValue(framebufferID, out hasColor))
            {
                hasColor = false;
            }

            if ((attachment == Framebuffer.AttachmentPoint.Color0) ||
                (attachment == Framebuffer.AttachmentPoint.Color1) ||
                (attachment == Framebuffer.AttachmentPoint.Color2) ||
                (attachment == Framebuffer.AttachmentPoint.Color3) ||
                (attachment == Framebuffer.AttachmentPoint.Color4) ||
                (attachment == Framebuffer.AttachmentPoint.Color5) ||
                (attachment == Framebuffer.AttachmentPoint.Color6) ||
                (attachment == Framebuffer.AttachmentPoint.Color7))
            {
                hasColor = true;
            }

            framebufferHasColorAttachment[framebufferID] = hasColor;

            if (hasColor)
            {
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
            }
            else
            {
                GL.DrawBuffer(DrawBufferMode.None);
                GL.ReadBuffer(ReadBufferMode.None);
            }
        }

        public void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, int renderbufferID)
        {
            FramebufferTarget ft = (FramebufferTarget)target;
            FramebufferAttachment fa = (FramebufferAttachment)attachment;
            RenderbufferTarget rt = (RenderbufferTarget)renderbuffertarget;
            GL.FramebufferRenderbuffer(ft, fa, rt, renderbufferID);
        }

        public bool CheckFramebufferStatus()
        {
            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            return status == FramebufferErrorCode.FramebufferComplete;
        }

        // Renderbuffer management
        public int CreateRenderbuffer()
        {
            int rbo = GL.GenRenderbuffer();
            return rbo;
        }

        public void DeleteRenderbuffer(int renderbufferID)
        {
            if (renderbufferID > 0)
            {
                GL.DeleteRenderbuffer(renderbufferID);
            }
        }

        public void BindRenderbuffer(int renderbufferID)
        {
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderbufferID);
        }

        public void RenderbufferStorage(int renderbufferID, TextureFormat format, int width, int height)
        {
            BindRenderbuffer(renderbufferID);
            RenderbufferStorage internalFormat = (RenderbufferStorage)GLTextureFormatMap.internalFormat(format);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, internalFormat, width, height);
        }

        // Draw calls
        public void DrawArrays(RenderDevice.Primitive primitive, int first, int count)
        {
            PrimitiveType pt = ConvertPrimitive(primitive);
            GL.DrawArrays(pt, first, count);
        }

        public void DrawElements(RenderDevice.Primitive primitive, int count, int type, int offset)
        {
            PrimitiveType pt = ConvertPrimitive(primitive);
            DrawElementsType dt = (DrawElementsType)type;
            GL.DrawElements(pt, count, dt, offset);
        }

        public void DrawElementsInstanced(RenderDevice.Primitive primitive, int count, int type, int offset, int instanceCount)
        {
            PrimitiveType pt = ConvertPrimitive(primitive);
            DrawElementsType dt = (DrawElementsType)type;
            GL.DrawElementsInstanced(pt, count, dt, (IntPtr)offset, instanceCount);
        }

        private readonly Dictionary<VAR, int> varBuffers = new Dictionary<VAR, int>();
        private readonly Dictionary<VAR, int> varBufferVersions = new Dictionary<VAR, int>();

        public static int bufferDataCalls;

        private VAR clientIndexVAR;
        private int clientIndexVARCapacity;
        private uint[] clientIndexValues;

        private int getVARBuffer(VAR varray)
        {
            int bufferID;
            if (!varBuffers.TryGetValue(varray, out bufferID))
            {
                bufferID = CreateBuffer();
                varBuffers.Add(varray, bufferID);
                varBufferVersions.Add(varray, -1);
            }

            int version = varray.getDataVersion();
            if (varBufferVersions[varray] != version)
            {
                global::System.Threading.Interlocked.Increment(ref bufferDataCalls);
                byte[] data = varray.getData();
                if (data == null)
                {
                    data = new byte[0];
                }
                BufferTarget target = (varray.getType() == VAR.Type.INDEX) ? BufferTarget.ElementArrayBuffer : BufferTarget.ArrayBuffer;
                GL.BindBuffer(target, bufferID);
                GL.BufferData(target, new IntPtr(data.Length), data,
                              varray.getUsage() == VAR.Usage.STATIC ? BufferUsageHint.StaticDraw :
                              varray.getUsage() == VAR.Usage.DYNAMIC ? BufferUsageHint.DynamicDraw :
                              BufferUsageHint.StreamDraw);
                varBufferVersions[varray] = version;
            }

            return bufferID;
        }

        public void DrawVAR(RenderDevice.Primitive primitive, int numIndices, VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, VAR indexArray)
        {
            if (vertexArray == null || numIndices <= 0)
            {
                return;
            }

            PrimitiveType pt = ConvertPrimitive(primitive);

            if (currentProgram != 0)
            {
                DrawVARWithProgram(primitive, numIndices, pt, vertexArray, normalArray, colorArray, texCoordArray, indexArray);
                return;
            }

            // Legacy fixed-function path: client-state arrays.
            bool hasNormals;
            bool hasColors;
            bool hasTexCoords;
            bindFixedFunctionArrays(vertexArray, normalArray, colorArray, texCoordArray, out hasNormals, out hasColors, out hasTexCoords);

            if (indexArray != null)
            {
                int indexBuffer = getVARBuffer(indexArray);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
                GL.DrawElements(pt, numIndices, drawElementsTypeFor(indexArray.getFormat()), IntPtr.Zero);
            }
            else
            {
                GL.DrawArrays(pt, 0, numIndices);
            }

            disableFixedFunctionArrays(hasNormals, hasColors, hasTexCoords);
        }

        /**
          Draws indexed geometry with client-side indices supplied by the
          caller.  The original RenderDevice passed the caller's index array
          straight to glDrawElements instead of staging it through a VAR, so
          repeating the same indices cost neither a copy nor an upload.
        */
        public void DrawVAR(RenderDevice.Primitive primitive, int numIndices, VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, uint[] index)
        {
            if (vertexArray == null || numIndices <= 0 || index == null)
            {
                return;
            }

            PrimitiveType pt = ConvertPrimitive(primitive);

            if (currentProgram != 0)
            {
                // The programmable path draws from a bound element buffer, so the
                // client indices are staged into the immediate index VAR.
                VAR staged = getClientIndexVAR(index, numIndices);
                DrawVARWithProgram(primitive, numIndices, pt, vertexArray, normalArray, colorArray, texCoordArray, staged);
                return;
            }

            bool hasNormals;
            bool hasColors;
            bool hasTexCoords;
            bindFixedFunctionArrays(vertexArray, normalArray, colorArray, texCoordArray, out hasNormals, out hasColors, out hasTexCoords);

            if (numIndices > index.Length)
            {
                numIndices = index.Length;
            }
            GL.DrawElements(pt, numIndices, DrawElementsType.UnsignedInt, index);

            disableFixedFunctionArrays(hasNormals, hasColors, hasTexCoords);
        }

        private VAR getClientIndexVAR(uint[] index, int numIndices)
        {
            if (clientIndexValues == null || clientIndexValues.Length < numIndices)
            {
                clientIndexValues = new uint[numIndices];
            }
            Array.Copy(index, clientIndexValues, numIndices);
            if (clientIndexVAR == null || clientIndexVARCapacity < numIndices)
            {
                clientIndexVARCapacity = numIndices;
                clientIndexVAR = VAR.create(VAR.Type.INDEX, clientIndexVARCapacity, VAR.Usage.STREAM);
            }
            clientIndexVAR.setData(clientIndexValues);
            return clientIndexVAR;
        }

        private void bindFixedFunctionArrays(VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray,
                                             out bool hasNormals, out bool hasColors, out bool hasTexCoords)
        {
            int vertexBuffer = getVARBuffer(vertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.VertexPointer(3, vertexPointerTypeFor(vertexArray.getFormat()), vertexArray.getStride(), IntPtr.Zero);
            GL.EnableClientState(ArrayCap.VertexArray);

            hasNormals = (normalArray != null);
            if (hasNormals)
            {
                int normalBuffer = getVARBuffer(normalArray);
                GL.BindBuffer(BufferTarget.ArrayBuffer, normalBuffer);
                GL.NormalPointer(normalPointerTypeFor(normalArray.getFormat()), normalArray.getStride(), IntPtr.Zero);
                GL.EnableClientState(ArrayCap.NormalArray);
            }

            hasColors = (colorArray != null);
            if (hasColors)
            {
                int colorBuffer = getVARBuffer(colorArray);
                GL.BindBuffer(BufferTarget.ArrayBuffer, colorBuffer);
                int colorComponents = colorArray.getComponentCount();
                if (colorComponents <= 0)
                {
                    colorComponents = 4;
                }
                GL.ColorPointer(colorComponents, colorPointerTypeFor(colorArray.getFormat()), colorArray.getStride(), IntPtr.Zero);
                GL.EnableClientState(ArrayCap.ColorArray);
            }

            hasTexCoords = (texCoordArray != null);
            if (hasTexCoords)
            {
                int texCoordBuffer = getVARBuffer(texCoordArray);
                GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordBuffer);
                int texCoordComponents = texCoordArray.getComponentCount();
                if (texCoordComponents <= 0)
                {
                    texCoordComponents = 2;
                }
                GL.TexCoordPointer(texCoordComponents, texCoordPointerTypeFor(texCoordArray.getFormat()), texCoordArray.getStride(), IntPtr.Zero);
                GL.EnableClientState(ArrayCap.TextureCoordArray);
            }
        }

        private void disableFixedFunctionArrays(bool hasNormals, bool hasColors, bool hasTexCoords)
        {
            GL.DisableClientState(ArrayCap.VertexArray);
            if (hasNormals)
            {
                GL.DisableClientState(ArrayCap.NormalArray);
            }
            if (hasColors)
            {
                GL.DisableClientState(ArrayCap.ColorArray);
            }
            if (hasTexCoords)
            {
                GL.DisableClientState(ArrayCap.TextureCoordArray);
            }
        }

        private static VertexPointerType vertexPointerTypeFor(VAR.Format format)
        {
            switch (format)
            {
                case VAR.Format.SHORT: return VertexPointerType.Short;
                case VAR.Format.INT: return VertexPointerType.Int;
                case VAR.Format.DOUBLE: return VertexPointerType.Double;
                default: return VertexPointerType.Float;
            }
        }

        private static NormalPointerType normalPointerTypeFor(VAR.Format format)
        {
            switch (format)
            {
                case VAR.Format.BYTE: return NormalPointerType.Byte;
                case VAR.Format.SHORT: return NormalPointerType.Short;
                case VAR.Format.INT: return NormalPointerType.Int;
                case VAR.Format.DOUBLE: return NormalPointerType.Double;
                default: return NormalPointerType.Float;
            }
        }

        private static TexCoordPointerType texCoordPointerTypeFor(VAR.Format format)
        {
            switch (format)
            {
                case VAR.Format.SHORT: return TexCoordPointerType.Short;
                case VAR.Format.INT: return TexCoordPointerType.Int;
                case VAR.Format.DOUBLE: return TexCoordPointerType.Double;
                default: return TexCoordPointerType.Float;
            }
        }

        private static ColorPointerType colorPointerTypeFor(VAR.Format format)
        {
            switch (format)
            {
                case VAR.Format.BYTE: return ColorPointerType.Byte;
                case VAR.Format.UNSIGNED_BYTE: return ColorPointerType.UnsignedByte;
                case VAR.Format.SHORT: return ColorPointerType.Short;
                case VAR.Format.UNSIGNED_SHORT: return ColorPointerType.UnsignedShort;
                case VAR.Format.INT: return ColorPointerType.Int;
                case VAR.Format.UNSIGNED_INT: return ColorPointerType.UnsignedInt;
                case VAR.Format.DOUBLE: return ColorPointerType.Double;
                default: return ColorPointerType.Float;
            }
        }

        private static DrawElementsType drawElementsTypeFor(VAR.Format format)
        {
            switch (format)
            {
                case VAR.Format.UNSIGNED_BYTE: return DrawElementsType.UnsignedByte;
                case VAR.Format.UNSIGNED_SHORT: return DrawElementsType.UnsignedShort;
                default: return DrawElementsType.UnsignedInt;
            }
        }

        private void DrawVARWithProgram(RenderDevice.Primitive primitive, int numIndices, PrimitiveType pt,
            VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, VAR indexArray)
        {
            List<int> enabledAttributes = new List<int>();

            bindShaderAttribute(vertexArray, "position", enabledAttributes);
            bindShaderAttribute(normalArray, "normal", enabledAttributes);
            bindShaderAttribute(colorArray, "color", enabledAttributes);
            bindShaderAttribute(texCoordArray, "texCoord0", enabledAttributes);

            if (indexArray != null)
            {
                int indexBuffer = getVARBuffer(indexArray);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
                GL.DrawElements(pt, numIndices, DrawElementsType.UnsignedInt, IntPtr.Zero);
            }
            else
            {
                GL.DrawArrays(pt, 0, numIndices);
            }

            foreach (int location in enabledAttributes)
            {
                GL.DisableVertexAttribArray(location);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        private void bindShaderAttribute(VAR array, string name, List<int> enabledAttributes)
        {
            if (array == null || string.IsNullOrEmpty(name))
            {
                return;
            }
            int location = GL.GetAttribLocation(currentProgram, name);
            if (location < 0)
            {
                return;
            }
            int buffer = getVARBuffer(array);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffer);
            int components = (array.getType() == VAR.Type.COLOR) ? 4 : 3;
            GL.VertexAttribPointer(location, components, VertexAttribPointerType.Float, false, array.getStride(), IntPtr.Zero);
            GL.EnableVertexAttribArray(location);
            enabledAttributes.Add(location);
        }

        private PrimitiveType ConvertPrimitive(RenderDevice.Primitive p)
        {
            switch (p)
            {
                case RenderDevice.Primitive.LINES: return PrimitiveType.Lines;
                case RenderDevice.Primitive.LINE_STRIP: return PrimitiveType.LineStrip;
                case RenderDevice.Primitive.TRIANGLES: return PrimitiveType.Triangles;
                case RenderDevice.Primitive.TRIANGLE_STRIP: return PrimitiveType.TriangleStrip;
                case RenderDevice.Primitive.TRIANGLE_FAN: return PrimitiveType.TriangleFan;
                case RenderDevice.Primitive.QUADS: return PrimitiveType.Quads;
                case RenderDevice.Primitive.QUAD_STRIP: return PrimitiveType.QuadStrip;
                case RenderDevice.Primitive.POINTS: return PrimitiveType.Points;
                default: return PrimitiveType.Triangles;
            }
        }

        public void BeginPrimitive(RenderDevice.Primitive primitive)
        {
            GL.Begin(ConvertPrimitive(primitive));
        }

        public void SendVertex(float x, float y, float z)
        {
            GL.Vertex3(x, y, z);
        }

        public void SendVertex(float x, float y, float z, float w)
        {
            GL.Vertex4(x, y, z, w);
        }

        public void SendNormal(float x, float y, float z)
        {
            GL.Normal3(x, y, z);
        }

        public void SendColor(float r, float g, float b, float a)
        {
            GL.Color4(r, g, b, a);
        }

        public void SendTexCoord(float s, float t, int textureUnit)
        {
            setActiveTextureUnit(textureUnit);
            GL.TexCoord2(s, t);
        }

        public void EndPrimitive()
        {
            GL.End();
        }

        // State management
        public void SetDepthTest(RenderDevice.DepthTest test)
        {
            if (test == RenderDevice.DepthTest.DEPTH_CURRENT) return;

            EnableCap depthTest = EnableCap.DepthTest;
            switch (test)
            {
                case RenderDevice.DepthTest.DEPTH_GREATER: GL.DepthFunc(DepthFunction.Greater); break;
                case RenderDevice.DepthTest.DEPTH_LESS: GL.DepthFunc(DepthFunction.Less); break;
                case RenderDevice.DepthTest.DEPTH_GEQUAL: GL.DepthFunc(DepthFunction.Gequal); break;
                case RenderDevice.DepthTest.DEPTH_LEQUAL: GL.DepthFunc(DepthFunction.Lequal); break;
                case RenderDevice.DepthTest.DEPTH_NOTEQUAL: GL.DepthFunc(DepthFunction.Notequal); break;
                case RenderDevice.DepthTest.DEPTH_EQUAL: GL.DepthFunc(DepthFunction.Equal); break;
                case RenderDevice.DepthTest.DEPTH_ALWAYS_PASS: GL.DepthFunc(DepthFunction.Always); break;
                case RenderDevice.DepthTest.DEPTH_NEVER_PASS: GL.DepthFunc(DepthFunction.Never); break;
            }
        }

        public void SetStencilTest(RenderDevice.StencilTest test, int reference)
        {
            if (test == RenderDevice.StencilTest.STENCIL_CURRENT) return;

            GL.Enable(EnableCap.StencilTest);
            switch (test)
            {
                case RenderDevice.StencilTest.STENCIL_GREATER: GL.StencilFunc(StencilFunction.Greater, reference, ~0); break;
                case RenderDevice.StencilTest.STENCIL_LESS: GL.StencilFunc(StencilFunction.Less, reference, ~0); break;
                case RenderDevice.StencilTest.STENCIL_GEQUAL: GL.StencilFunc(StencilFunction.Gequal, reference, ~0); break;
                case RenderDevice.StencilTest.STENCIL_LEQUAL: GL.StencilFunc(StencilFunction.Lequal, reference, ~0); break;
                case RenderDevice.StencilTest.STENCIL_NOTEQUAL: GL.StencilFunc(StencilFunction.Notequal, reference, ~0); break;
                case RenderDevice.StencilTest.STENCIL_EQUAL: GL.StencilFunc(StencilFunction.Equal, reference, ~0); break;
                case RenderDevice.StencilTest.STENCIL_ALWAYS_PASS: GL.StencilFunc(StencilFunction.Always, reference, ~0); break;
                case RenderDevice.StencilTest.STENCIL_NEVER_PASS: GL.StencilFunc(StencilFunction.Never, reference, ~0); break;
            }
        }

        public void SetAlphaTest(RenderDevice.AlphaTest test, double reference)
        {
            if (test == RenderDevice.AlphaTest.ALPHA_CURRENT) return;

            switch (test)
            {
                case RenderDevice.AlphaTest.ALPHA_GREATER: GL.AlphaFunc(AlphaFunction.Greater, (float)reference); break;
                case RenderDevice.AlphaTest.ALPHA_LESS: GL.AlphaFunc(AlphaFunction.Less, (float)reference); break;
                case RenderDevice.AlphaTest.ALPHA_GEQUAL: GL.AlphaFunc(AlphaFunction.Gequal, (float)reference); break;
                case RenderDevice.AlphaTest.ALPHA_LEQUAL: GL.AlphaFunc(AlphaFunction.Lequal, (float)reference); break;
                case RenderDevice.AlphaTest.ALPHA_NOTEQUAL: GL.AlphaFunc(AlphaFunction.Notequal, (float)reference); break;
                case RenderDevice.AlphaTest.ALPHA_EQUAL: GL.AlphaFunc(AlphaFunction.Equal, (float)reference); break;
                case RenderDevice.AlphaTest.ALPHA_ALWAYS_PASS: GL.AlphaFunc(AlphaFunction.Always, (float)reference); break;
                case RenderDevice.AlphaTest.ALPHA_NEVER_PASS: GL.AlphaFunc(AlphaFunction.Never, (float)reference); break;
            }

            GL.Enable(EnableCap.AlphaTest);
        }

        public void SetRenderMode(RenderDevice.RenderMode mode)
        {
            if (mode == RenderDevice.RenderMode.RENDER_CURRENT) return;

            switch (mode)
            {
                case RenderDevice.RenderMode.RENDER_SOLID: GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill); break;
                case RenderDevice.RenderMode.RENDER_WIREFRAME: GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line); break;
                case RenderDevice.RenderMode.RENDER_POINTS: GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point); break;
            }
        }

        public void SetCullFace(RenderDevice.CullFace face)
        {
            if (face == RenderDevice.CullFace.CULL_CURRENT) return;

            switch (face)
            {
                case RenderDevice.CullFace.CULL_FRONT: GL.CullFace(CullFaceMode.Front); GL.Enable(EnableCap.CullFace); break;
                case RenderDevice.CullFace.CULL_BACK: GL.CullFace(CullFaceMode.Back); GL.Enable(EnableCap.CullFace); break;
                case RenderDevice.CullFace.CULL_NONE: GL.Disable(EnableCap.CullFace); break;
            }
        }

        public void SetBlendFunc(RenderDevice.BlendFunc src, RenderDevice.BlendFunc dst, RenderDevice.BlendEq eq)
        {
            if (src != RenderDevice.BlendFunc.BLEND_CURRENT) blendSrc = src;
            if (dst != RenderDevice.BlendFunc.BLEND_CURRENT) blendDst = dst;
            if (eq != RenderDevice.BlendEq.BLENDEQ_CURRENT) blendEq = eq;

            BlendingFactor srcFactor = ConvertBlendFunc(blendSrc);
            BlendingFactor dstFactor = ConvertBlendFuncDest(blendDst);

            if ((blendDst == RenderDevice.BlendFunc.BLEND_ZERO) &&
                (blendSrc == RenderDevice.BlendFunc.BLEND_ONE) &&
                ((blendEq == RenderDevice.BlendEq.BLENDEQ_ADD) || (blendEq == RenderDevice.BlendEq.BLENDEQ_SUBTRACT))) {
                GL.Disable(EnableCap.Blend);
            } else {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(srcFactor, dstFactor);

                BlendEquationMode eqMode = ConvertBlendEq(blendEq);
                GL.BlendEquation(eqMode);
            }
        }

        private BlendingFactor ConvertBlendFunc(RenderDevice.BlendFunc bf)
        {
            switch (bf)
            {
                case RenderDevice.BlendFunc.BLEND_SRC_ALPHA: return BlendingFactor.SrcAlpha;
                case RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA: return BlendingFactor.OneMinusSrcAlpha;
                case RenderDevice.BlendFunc.BLEND_ONE: return BlendingFactor.One;
                case RenderDevice.BlendFunc.BLEND_ZERO: return BlendingFactor.Zero;
                case RenderDevice.BlendFunc.BLEND_SRC_COLOR: return BlendingFactor.SrcColor;
                case RenderDevice.BlendFunc.BLEND_DST_COLOR: return BlendingFactor.DstColor;
                case RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_COLOR: return BlendingFactor.OneMinusSrcColor;
                default: return BlendingFactor.One;
            }
        }

        private BlendingFactor ConvertBlendFuncDest(RenderDevice.BlendFunc bf)
        {
            switch (bf)
            {
                case RenderDevice.BlendFunc.BLEND_SRC_ALPHA: return BlendingFactor.SrcAlpha;
                case RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA: return BlendingFactor.OneMinusSrcAlpha;
                case RenderDevice.BlendFunc.BLEND_ONE: return BlendingFactor.One;
                case RenderDevice.BlendFunc.BLEND_ZERO: return BlendingFactor.Zero;
                case RenderDevice.BlendFunc.BLEND_SRC_COLOR: return BlendingFactor.SrcColor;
                case RenderDevice.BlendFunc.BLEND_DST_COLOR: return BlendingFactor.DstColor;
                case RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_COLOR: return BlendingFactor.OneMinusSrcColor;
                default: return BlendingFactor.Zero;
            }
        }

        private BlendEquationMode ConvertBlendEq(RenderDevice.BlendEq eq)
        {
            switch (eq)
            {
                case RenderDevice.BlendEq.BLENDEQ_MIN: return BlendEquationMode.Min;
                case RenderDevice.BlendEq.BLENDEQ_MAX: return BlendEquationMode.Max;
                case RenderDevice.BlendEq.BLENDEQ_ADD: return BlendEquationMode.FuncAdd;
                case RenderDevice.BlendEq.BLENDEQ_SUBTRACT: return BlendEquationMode.FuncSubtract;
                case RenderDevice.BlendEq.BLENDEQ_REVERSE_SUBTRACT: return BlendEquationMode.FuncReverseSubtract;
                default: return BlendEquationMode.FuncAdd;
            }
        }

        public void SetColorWrite(bool enable)
        {
            colorWriteEnabled = enable;
            applyColorWriteMask();
        }

        public void SetAlphaWrite(bool enable)
        {
            alphaWriteEnabled = enable;
            applyColorWriteMask();
        }

        private void applyColorWriteMask()
        {
            GL.ColorMask(colorWriteEnabled, colorWriteEnabled, colorWriteEnabled, alphaWriteEnabled);
        }

        public void SetDepthWrite(bool enable)
        {
            GL.DepthMask(enable);
        }

        public void SetShadeMode(RenderDevice.ShadeMode mode)
        {
            if (mode == RenderDevice.ShadeMode.SHADE_CURRENT) return;
            GL.ShadeModel(mode == RenderDevice.ShadeMode.SHADE_FLAT ? ShadingModel.Flat : ShadingModel.Smooth);
        }

        public void SetStencilOp(RenderDevice.StencilOp fail, RenderDevice.StencilOp zfail, RenderDevice.StencilOp zpass,
                                RenderDevice.StencilOp backFail, RenderDevice.StencilOp backZfail, RenderDevice.StencilOp backZpass)
        {
            StencilOp sfail = ConvertStencilOp(fail);
            StencilOp szfail = ConvertStencilOp(zfail);
            StencilOp szpass = ConvertStencilOp(zpass);
            StencilOp sbackFail = ConvertStencilOp(backFail);
            StencilOp sbackZfail = ConvertStencilOp(backZfail);
            StencilOp sbackZpass = ConvertStencilOp(backZpass);

            GL.StencilOpSeparate(StencilFace.Front, sfail, szfail, szpass);
            GL.StencilOpSeparate(StencilFace.Back, sbackFail, sbackZfail, sbackZpass);
        }

        private StencilOp ConvertStencilOp(RenderDevice.StencilOp op)
        {
            switch (op)
            {
                case RenderDevice.StencilOp.STENCIL_INCR_WRAP: return StencilOp.IncrWrap;
                case RenderDevice.StencilOp.STENCIL_DECR_WRAP: return StencilOp.DecrWrap;
                case RenderDevice.StencilOp.STENCIL_KEEP: return StencilOp.Keep;
                case RenderDevice.StencilOp.STENCIL_INCR: return StencilOp.Incr;
                case RenderDevice.StencilOp.STENCIL_DECR: return StencilOp.Decr;
                case RenderDevice.StencilOp.STENCIL_REPLACE: return StencilOp.Replace;
                case RenderDevice.StencilOp.STENCIL_ZERO: return StencilOp.Zero;
                case RenderDevice.StencilOp.STENCIL_INVERT: return StencilOp.Invert;
                default: return StencilOp.Keep;
            }
        }

        public void SetPolygonOffset(double offset)
        {
            float units = (float)offset;
            float slope = offset < 0 ? -1.0f : (offset > 0 ? 1.0f : 0.0f);
            GL.PolygonOffset(units, slope);
            if (offset != 0)
            {
                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.Enable(EnableCap.PolygonOffsetLine);
                GL.Enable(EnableCap.PolygonOffsetPoint);
            }
            else
            {
                GL.Disable(EnableCap.PolygonOffsetFill);
                GL.Disable(EnableCap.PolygonOffsetLine);
                GL.Disable(EnableCap.PolygonOffsetPoint);
            }
        }

        public void SetLineWidth(double width)
        {
            GL.LineWidth((float)width);
        }

        public void SetPointSize(double size)
        {
            GL.PointSize((float)size);
        }

        public void SetScissor(Rect2D rect, bool enable)
        {
            if (enable)
            {
                GL.Enable(EnableCap.ScissorTest);
                GL.Scissor((int)rect.x, (int)rect.y, (int)rect.width(), (int)rect.height());
            }
            else
            {
                GL.Disable(EnableCap.ScissorTest);
            }
        }

        public void SetViewport(Rect2D viewport)
        {
            GL.Viewport((int)viewport.x, (int)viewport.y, (int)viewport.width(), (int)viewport.height());
        }

        public void SetDepthRange(double near, double far)
        {
            GL.DepthRange(near, far);
        }

        public void SetColorClearValue(Color4 color)
        {
            clearValue = color;
            GL.ClearColor(color.r, color.g, color.b, color.a);
        }

        public void SetDepthClearValue(double depth)
        {
            GL.ClearDepth(depth);
        }

        public void SetStencilClearValue(int stencil)
        {
            GL.ClearStencil(stencil);
        }

        // Texture matrix
        public void SetTextureMatrix(int textureUnit, Matrix4 matrix)
        {
            setActiveTextureUnit(textureUnit);
            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();
            fillMatrixBuffer(matrix);
            GL.LoadMatrix(matrixBuffer);
            GL.MatrixMode(MatrixMode.Modelview);
        }

        public void SetTextureCombineMode(int textureUnit, RenderDevice.CombineMode mode)
        {
            if (mode == RenderDevice.CombineMode.TEX_CURRENT) return;

            setActiveTextureUnit(textureUnit);

            bool hasAdd = SupportsExtension("GL_EXT_texture_env_add");
            bool hasCombine = SupportsExtension("GL_ARB_texture_env_combine");
            bool hasDot3 = SupportsExtension("GL_ARB_texture_env_dot3");

            int envMode;
            switch (mode)
            {
                case RenderDevice.CombineMode.TEX_REPLACE: envMode = (int)TextureEnvMode.Replace; break;
                case RenderDevice.CombineMode.TEX_BLEND: envMode = (int)TextureEnvMode.Blend; break;
                case RenderDevice.CombineMode.TEX_MODULATE: envMode = (int)TextureEnvMode.Modulate; break;
                case RenderDevice.CombineMode.TEX_INTERPOLATE: envMode = (int)TextureEnvMode.Decal; break;
                case RenderDevice.CombineMode.TEX_ADD: envMode = hasCombine ? (int)TextureEnvMode.Add : (int)TextureEnvMode.Blend; break;
                case RenderDevice.CombineMode.TEX_SUBTRACT: envMode = hasAdd ? 0x84E7 : (int)TextureEnvMode.Blend; break;
                case RenderDevice.CombineMode.TEX_ADD_SIGNED: envMode = hasAdd ? 0x84E8 : (int)TextureEnvMode.Blend; break;
                case RenderDevice.CombineMode.TEX_DOT3_RGB: envMode = hasDot3 ? 0x86AE : (int)TextureEnvMode.Blend; break;
                case RenderDevice.CombineMode.TEX_DOT3_RGBA: envMode = hasDot3 ? 0x86AF : (int)TextureEnvMode.Blend; break;
                default: throw new ArgumentOutOfRangeException("mode");
            }

            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, envMode);
        }

        public void SetTextureLODBias(int textureUnit, float bias)
        {
            setActiveTextureUnit(textureUnit);
            int target;
            if (!textureTargets.TryGetValue(textureUnit, out target))
            {
                target = (int)TextureTarget.Texture2D;
            }
            GL.TexParameter((TextureTarget)target, TextureParameterName.TextureLodBias, bias);
        }

        public void ConfigureCubeMapTexGen(int textureUnit, CubeMapTexGenMode mode)
        {
            setActiveTextureUnit(textureUnit);

            if (mode == CubeMapTexGenMode.TEXGEN_OFF)
            {
                GL.Disable(EnableCap.TextureGenS);
                GL.Disable(EnableCap.TextureGenT);
                GL.Disable(EnableCap.TextureGenR);
                return;
            }

            int genMode = (mode == CubeMapTexGenMode.TEXGEN_REFLECTION_MAP)
                ? (int)TextureGenMode.ReflectionMap
                : (int)TextureGenMode.NormalMap;

            GL.TexGen(TextureCoordName.S, TextureGenParameter.TextureGenMode, genMode);
            GL.TexGen(TextureCoordName.T, TextureGenParameter.TextureGenMode, genMode);
            GL.TexGen(TextureCoordName.R, TextureGenParameter.TextureGenMode, genMode);
            GL.Enable(EnableCap.TextureGenS);
            GL.Enable(EnableCap.TextureGenT);
            GL.Enable(EnableCap.TextureGenR);
        }

        public void SetDrawBuffer(RenderDevice.Buffer drawBuffer)
        {
            switch (drawBuffer)
            {
                case RenderDevice.Buffer.BUFFER_CURRENT: return;
                case RenderDevice.Buffer.BUFFER_NONE: GL.DrawBuffer(DrawBufferMode.None); break;
                case RenderDevice.Buffer.BUFFER_FRONT_LEFT: GL.DrawBuffer(DrawBufferMode.FrontLeft); break;
                case RenderDevice.Buffer.BUFFER_FRONT_RIGHT: GL.DrawBuffer(DrawBufferMode.FrontRight); break;
                case RenderDevice.Buffer.BUFFER_BACK_LEFT: GL.DrawBuffer(DrawBufferMode.BackLeft); break;
                case RenderDevice.Buffer.BUFFER_BACK_RIGHT: GL.DrawBuffer(DrawBufferMode.BackRight); break;
                case RenderDevice.Buffer.BUFFER_FRONT: GL.DrawBuffer(DrawBufferMode.Front); break;
                case RenderDevice.Buffer.BUFFER_BACK: GL.DrawBuffer(DrawBufferMode.Back); break;
                case RenderDevice.Buffer.BUFFER_LEFT: GL.DrawBuffer(DrawBufferMode.Left); break;
                case RenderDevice.Buffer.BUFFER_RIGHT: GL.DrawBuffer(DrawBufferMode.Right); break;
                case RenderDevice.Buffer.BUFFER_FRONT_AND_BACK: GL.DrawBuffer(DrawBufferMode.FrontAndBack); break;
                case RenderDevice.Buffer.BUFFER_COLOR0:
                case RenderDevice.Buffer.BUFFER_COLOR1:
                case RenderDevice.Buffer.BUFFER_COLOR2:
                case RenderDevice.Buffer.BUFFER_COLOR3:
                case RenderDevice.Buffer.BUFFER_COLOR4:
                case RenderDevice.Buffer.BUFFER_COLOR5:
                case RenderDevice.Buffer.BUFFER_COLOR6:
                case RenderDevice.Buffer.BUFFER_COLOR7:
                case RenderDevice.Buffer.BUFFER_COLOR8:
                case RenderDevice.Buffer.BUFFER_COLOR9:
                case RenderDevice.Buffer.BUFFER_COLOR10:
                case RenderDevice.Buffer.BUFFER_COLOR11:
                case RenderDevice.Buffer.BUFFER_COLOR12:
                case RenderDevice.Buffer.BUFFER_COLOR13:
                case RenderDevice.Buffer.BUFFER_COLOR14:
                case RenderDevice.Buffer.BUFFER_COLOR15:
                    GL.DrawBuffer(DrawBufferMode.ColorAttachment0 + (drawBuffer - RenderDevice.Buffer.BUFFER_COLOR0));
                    break;
                default:
                    throw new NotSupportedException("The draw buffer is not supported by the OpenGL backend.");
            }
        }

        public void SetProjectionMatrix(Matrix4 matrix)
        {
            GL.MatrixMode(MatrixMode.Projection);
            fillMatrixBuffer(matrix);
            GL.LoadMatrix(matrixBuffer);
            GL.MatrixMode(MatrixMode.Modelview);
        }

        public void SetModelViewMatrix(Matrix4 matrix)
        {
            GL.MatrixMode(MatrixMode.Modelview);
            fillMatrixBuffer(matrix);
            GL.LoadMatrix(matrixBuffer);
        }

        public void SetCameraToWorldMatrix(Matrix4 matrix)
        {
        }

        private void fillMatrixBuffer(Matrix4 matrix)
        {
            for (int r = 0; r < 4; ++r)
                for (int c = 0; c < 4; ++c)
                    matrixBuffer[c * 4 + r] = matrix.elt(r, c); // Transpose for OpenGL
        }

        // Lighting
        public void SetLight(int index, GLight light, bool enabled)
        {
            LightName glLight = LightName.Light0 + index;
            if (enabled)
            {
                GL.Enable((EnableCap)((int)glLight));
                Color3 lightColor = light.color;
                lightPositionBuffer[0] = light.position.x;
                lightPositionBuffer[1] = light.position.y;
                lightPositionBuffer[2] = light.position.z;
                lightPositionBuffer[3] = light.position.w;
                GL.Light(glLight, LightParameter.Position, lightPositionBuffer);

                lightDirectionBuffer[0] = light.spotDirection.x;
                lightDirectionBuffer[1] = light.spotDirection.y;
                lightDirectionBuffer[2] = light.spotDirection.z;
                GL.Light(glLight, LightParameter.SpotDirection, lightDirectionBuffer);
                GL.Light(glLight, LightParameter.SpotCutoff, (float)light.spotCutoff);
                GL.Light(glLight, LightParameter.ConstantAttenuation, (float)light.attenuation[0]);
                GL.Light(glLight, LightParameter.LinearAttenuation, (float)light.attenuation[1]);
                GL.Light(glLight, LightParameter.QuadraticAttenuation, (float)light.attenuation[2]);

                if (light.diffuse)
                {
                    lightColorBuffer[0] = lightColor.r;
                    lightColorBuffer[1] = lightColor.g;
                    lightColorBuffer[2] = lightColor.b;
                    lightColorBuffer[3] = 1.0f;
                }
                else
                {
                    lightColorBuffer[0] = 0.0f;
                    lightColorBuffer[1] = 0.0f;
                    lightColorBuffer[2] = 0.0f;
                    lightColorBuffer[3] = 1.0f;
                }
                GL.Light(glLight, LightParameter.Diffuse, lightColorBuffer);

                if (light.specular)
                {
                    lightColorBuffer[0] = lightColor.r;
                    lightColorBuffer[1] = lightColor.g;
                    lightColorBuffer[2] = lightColor.b;
                    lightColorBuffer[3] = 1.0f;
                }
                else
                {
                    lightColorBuffer[0] = 0.0f;
                    lightColorBuffer[1] = 0.0f;
                    lightColorBuffer[2] = 0.0f;
                    lightColorBuffer[3] = 1.0f;
                }
                GL.Light(glLight, LightParameter.Specular, lightColorBuffer);

                lightColorBuffer[0] = 0.0f;
                lightColorBuffer[1] = 0.0f;
                lightColorBuffer[2] = 0.0f;
                lightColorBuffer[3] = 1.0f;
                GL.Light(glLight, LightParameter.Ambient, lightColorBuffer);
            }
            else
            {
                GL.Disable((EnableCap)((int)glLight));
            }
        }

        public void SetAmbientLight(Color4 color)
        {
            lightColorBuffer[0] = color.r;
            lightColorBuffer[1] = color.g;
            lightColorBuffer[2] = color.b;
            lightColorBuffer[3] = color.a;
            GL.LightModel(LightModelParameter.LightModelAmbient, lightColorBuffer);
        }

        public void SetLightingEnabled(bool enabled)
        {
            if (enabled) GL.Enable(EnableCap.Lighting); else GL.Disable(EnableCap.Lighting);
        }

        public void SetTwoSidedLighting(bool enabled)
        {
            GL.LightModel(LightModelParameter.LightModelTwoSide, enabled ? 1.0f : 0.0f);
        }

        public void SetSpecular(Color3 color)
        {
            lightColorBuffer[0] = color.r;
            lightColorBuffer[1] = color.g;
            lightColorBuffer[2] = color.b;
            lightColorBuffer[3] = 1.0f;
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Specular, lightColorBuffer);
        }

        public void SetShininess(float shininess)
        {
            GL.Material(MaterialFace.FrontAndBack, MaterialParameter.Shininess, shininess);
        }

        // Milestones / queries
        public int CreateQuery()
        {
            int query = GL.GenQuery();
            return query;
        }

        public void DeleteQuery(int queryID)
        {
            if (queryID > 0) GL.DeleteQuery(queryID);
        }

        public void BeginQuery(int queryID)
        {
            GL.BeginQuery(QueryTarget.TimeElapsed, queryID);
        }

        public void EndQuery(int queryID)
        {
            GL.EndQuery(QueryTarget.TimeElapsed);
        }

        public bool GetQueryResult(int queryID, out uint result)
        {
            int available;
            GL.GetQueryObject(queryID, GetQueryObjectParam.QueryResultAvailable, out available);
            if (available != 0)
            {
                int res;
                GL.GetQueryObject(queryID, GetQueryObjectParam.QueryResult, out res);
                result = (uint)res;
                return true;
            }
            result = 0;
            return false;
        }

        public int CreateMilestone()
        {
            int id = nextMilestoneId++;
            milestones.Add(id, IntPtr.Zero);
            return id;
        }

        public void DeleteMilestone(int milestoneID)
        {
            IntPtr sync;
            if (milestones.TryGetValue(milestoneID, out sync))
            {
                if (sync != IntPtr.Zero)
                {
                    GL.DeleteSync(sync);
                }
                milestones.Remove(milestoneID);
            }
        }

        public void SetMilestone(int milestoneID)
        {
            IntPtr sync;
            if (!milestones.TryGetValue(milestoneID, out sync))
            {
                throw new ArgumentOutOfRangeException("milestoneID");
            }
            if (sync != IntPtr.Zero)
            {
                GL.DeleteSync(sync);
            }
            if (SupportsExtension("GL_ARB_sync"))
            {
                sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            }
            else
            {
                GL.Finish();
                sync = IntPtr.Zero;
            }
            milestones[milestoneID] = sync;
        }

        public void WaitForMilestone(int milestoneID)
        {
            IntPtr sync;
            if (!milestones.TryGetValue(milestoneID, out sync))
            {
                throw new ArgumentOutOfRangeException("milestoneID");
            }
            if (sync == IntPtr.Zero)
            {
                return;
            }
            WaitSyncStatus status = GL.ClientWaitSync(sync, ClientWaitSyncFlags.SyncFlushCommandsBit, 1000000000UL);
            if (status == WaitSyncStatus.TimeoutExpired || status == WaitSyncStatus.WaitFailed)
            {
                throw new TimeoutException("The GPU milestone did not complete within the timeout.");
            }
            GL.DeleteSync(sync);
            milestones[milestoneID] = IntPtr.Zero;
        }

        // Capabilities
        private void checkCubeMapBugs()
        {
            if (!SupportsExtension("GL_EXT_texture_cube_map") && !SupportsExtension("GL_ARB_texture_cube_map"))
            {
                OpenGLCapabilities.BugMultiTexCoord3fvARB = false;
                OpenGLCapabilities.BugNormalMapTexGen = false;
                return;
            }

            int[] viewport = new int[4];
            GL.GetInteger(GetPName.Viewport, viewport);
            if (viewport[2] < 60 || viewport[3] < 6)
            {
                return;
            }

            TextureTarget[] faces = new TextureTarget[] {
                TextureTarget.TextureCubeMapPositiveX,
                TextureTarget.TextureCubeMapNegativeX,
                TextureTarget.TextureCubeMapPositiveY,
                TextureTarget.TextureCubeMapNegativeY,
                TextureTarget.TextureCubeMapPositiveZ,
                TextureTarget.TextureCubeMapNegativeZ
            };
            byte[] color = new byte[6];
            float[] corner = new float[] {
                1.0f, -1.0f, 1.0f, 1.0f, -1.0f, -1.0f, 1.0f, 1.0f, -1.0f, 1.0f, 1.0f, 1.0f,
                -1.0f, 1.0f, 1.0f, -1.0f, 1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, 1.0f,
                1.0f, 1.0f, 1.0f, 1.0f, 1.0f, -1.0f, -1.0f, 1.0f, -1.0f, -1.0f, 1.0f, 1.0f,
                1.0f, -1.0f, 1.0f, -1.0f, 1.0f, 1.0f, -1.0f, -1.0f, 1.0f, 1.0f, -1.0f, -1.0f,
                -1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, -1.0f, -1.0f,
                -1.0f, -1.0f, 1.0f, -1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, -1.0f, -1.0f
            };

            GL.PushAttrib(AttribMask.AllAttribBits);
            int texture = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, texture);
            GL.Enable(EnableCap.TextureCubeMap);
            for (int face = 0; face < faces.Length; ++face)
            {
                color[face] = (byte)(face * 40);
                byte[] pixels = new byte[16 * 16 * 4];
                for (int i = 0; i < pixels.Length; ++i)
                {
                    pixels[i] = color[face];
                }
                GL.TexImage2D(faces[face], 0, PixelInternalFormat.Rgba, 16, 16, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
            }
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Clamp);
            GL.DrawBuffer(DrawBufferMode.Back);
            GL.ReadBuffer(ReadBufferMode.Back);
            GL.ClearColor(0.0f, 1.0f, 1.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.CullFace);
            GL.Color3(1.0f, 1.0f, 1.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(viewport[0], viewport[0] + viewport[2], viewport[1] + viewport[3], viewport[1], -1.0, 10.0);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Texture);
            GL.LoadIdentity();
            GL.Disable(EnableCap.TextureGenS);
            GL.Disable(EnableCap.TextureGenT);
            GL.Disable(EnableCap.TextureGenR);

            for (int pass = 0; pass < 2; ++pass)
            {
                if (pass == 1)
                {
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.TexGen(TextureCoordName.S, TextureGenParameter.TextureGenMode, (int)TextureGenMode.NormalMap);
                    GL.TexGen(TextureCoordName.T, TextureGenParameter.TextureGenMode, (int)TextureGenMode.NormalMap);
                    GL.TexGen(TextureCoordName.R, TextureGenParameter.TextureGenMode, (int)TextureGenMode.NormalMap);
                    GL.Enable(EnableCap.TextureGenS);
                    GL.Enable(EnableCap.TextureGenT);
                    GL.Enable(EnableCap.TextureGenR);
                }

                GL.Begin(PrimitiveType.Quads);
                for (int face = 0; face < faces.Length; ++face)
                {
                    float size = 10.0f;
                    int index = face * 12;
                    GL.MultiTexCoord3(TextureUnit.Texture0, corner[index], corner[index + 1], corner[index + 2]);
                    GL.Normal3(corner[index], corner[index + 1], corner[index + 2]);
                    GL.Vertex4(face * size, 0.0f, -1.0f, 1.0f);
                    index += 3;
                    GL.MultiTexCoord3(TextureUnit.Texture0, corner[index], corner[index + 1], corner[index + 2]);
                    GL.Normal3(corner[index], corner[index + 1], corner[index + 2]);
                    GL.Vertex4(face * size, size, -1.0f, 1.0f);
                    index += 3;
                    GL.MultiTexCoord3(TextureUnit.Texture0, corner[index], corner[index + 1], corner[index + 2]);
                    GL.Normal3(corner[index], corner[index + 1], corner[index + 2]);
                    GL.Vertex4((face + 1) * size, size, -1.0f, 1.0f);
                    index += 3;
                    GL.MultiTexCoord3(TextureUnit.Texture0, corner[index], corner[index + 1], corner[index + 2]);
                    GL.Normal3(corner[index], corner[index + 1], corner[index + 2]);
                    GL.Vertex4((face + 1) * size, 0.0f, -1.0f, 1.0f);
                }
                GL.End();

                byte[] readback = new byte[60 * 4];
                GL.ReadPixels(0, viewport[3] - 5, 60, 1, PixelFormat.Rgba, PixelType.UnsignedByte, readback);
                bool textureBug = false;
                for (int face = 0; face < faces.Length; ++face)
                {
                    if (readback[face * 10 * 4 + 5 * 4] != color[face])
                    {
                        textureBug = true;
                        break;
                    }
                }
                if (pass == 0)
                {
                    OpenGLCapabilities.BugMultiTexCoord3fvARB = textureBug;
                }
                else
                {
                    OpenGLCapabilities.BugNormalMapTexGen = textureBug;
                }
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            }

            GL.PopAttrib();
            GL.DeleteTexture(texture);
            activeTextureUnit = -1;
        }

        private bool checkRedBlueMipmapSwap()
        {
            int previousUnit = activeTextureUnit < 0 ? 0 : activeTextureUnit;
            int previousTexture = GL.GetInteger(GetPName.TextureBinding2D);
            int texture = GL.GenTexture();
            byte[] pixels = new byte[4 * 4 * 3];
            for (int i = 0; i < pixels.Length; i += 3)
            {
                pixels[i] = 255;
            }
            GL.ActiveTexture(TextureUnit.Texture0 + previousUnit);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb8, 4, 4, 0, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
            if (SupportsExtension("GL_SGIS_generate_mipmap"))
            {
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }
            GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
            bool bug = pixels[0] != 255 || pixels[1] != 0 || pixels[2] != 0;
            GL.DeleteTexture(texture);
            GL.BindTexture(TextureTarget.Texture2D, previousTexture);
            activeTextureUnit = previousUnit;
            return bug;
        }

        public bool SupportsExtension(string extension)
        {
            return extension != null && OpenGLCapabilities.Extensions.Contains(extension);
        }

        public bool SupportsTextureFormat(TextureFormat format)
        {
            if (format == null || format == TextureFormat.AUTO)
            {
                return false;
            }
            bool supported;
            if (supportedTextureFormats.TryGetValue(format, out supported))
            {
                return supported;
            }

            int previousUnit = activeTextureUnit < 0 ? 0 : activeTextureUnit;
            int previousTexture = GL.GetInteger(GetPName.TextureBinding2D);
            int texture = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0 + previousUnit);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            while (GL.GetError() != ErrorCode.NoError)
            {
            }
            try
            {
                PixelInternalFormat internalFormat = (PixelInternalFormat)GLTextureFormatMap.internalFormat(format);
                PixelFormat pixelFormat = (PixelFormat)GLTextureFormatMap.baseFormat(format);
                GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, 8, 8, 0, pixelFormat, PixelType.UnsignedByte, new byte[8 * 8 * 4]);
                supported = GL.GetError() == ErrorCode.NoError;
            }
            finally
            {
                GL.DeleteTexture(texture);
                GL.BindTexture(TextureTarget.Texture2D, previousTexture);
                activeTextureUnit = previousUnit;
            }
            supportedTextureFormats.Add(format, supported);
            return supported;
        }

        public bool SupportsTwoSidedStencil()
        {
            return SupportsExtension("GL_EXT_stencil_two_side") || SupportsExtension("GL_ATI_separate_stencil");
        }

        public bool SupportsTextureRectangle()
        {
            return SupportsExtension("GL_EXT_texture_rectangle") || SupportsExtension("GL_ARB_texture_rectangle");
        }

        public bool SupportsVertexProgram()
        {
            return SupportsExtension("GL_ARB_vertex_shader") || SupportsExtension("GL_ARB_vertex_program");
        }

        public bool SupportsVertexProgramNV2()
        {
            return SupportsExtension("GL_NV_vertex_program2");
        }

        public bool SupportsPixelProgram()
        {
            return SupportsExtension("GL_ARB_fragment_shader") || SupportsExtension("GL_ARB_fragment_program");
        }

        public bool SupportsVertexBufferObject()
        {
            return SupportsExtension("GL_ARB_vertex_buffer_object");
        }

        // Screenshot
        public void ReadPixels(int x, int y, int width, int height, byte[] data)
        {
            GL.ReadPixels(x, y, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        }

        public void ReadDepth(int x, int y, int width, int height, float[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException("width", "The readback region must have a positive size.");
            }
            if (data.Length < width * height)
            {
                throw new ArgumentException("The destination array is too small for the requested region.", "data");
            }

            GL.ReadPixels(x, y, width, height, PixelFormat.DepthComponent, PixelType.Float, data);
        }

        // DescribeSystem support
        public string GetRenderer()
        {
            return caps.Renderer.Length > 0 ? caps.Renderer : (GL.GetString(StringName.Renderer) ?? string.Empty);
        }

        public string GetVendor()
        {
            return caps.Vendor.Length > 0 ? caps.Vendor : (GL.GetString(StringName.Vendor) ?? string.Empty);
        }

        public string GetDriverVersion()
        {
            if (caps.DriverVersion.Length > 0)
            {
                return caps.DriverVersion;
            }
            string vendor = GetVendor();
            if (vendor == "NVIDIA Corporation")
            {
                return getWindowsDriverVersion("nv4_disp.dll");
            }
            if (vendor == "ATI Technologies Inc.")
            {
                return getWindowsDriverVersion("ati2dvag.dll");
            }
            if (vendor == "Brian Paul")
            {
                string glVersion = GetGLVersion();
                int index = glVersion.LastIndexOf(' ');
                return index < 0 ? "Unknown (bad MESA driver string)" : glVersion.Substring(index + 1);
            }
            return "Unknown (Unknown vendor)";
        }

        private static string getWindowsDriverVersion(string filename)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), filename);
            if (!File.Exists(path))
            {
                return "Unknown (Can't find driver)";
            }
            FileVersionInfo version = FileVersionInfo.GetVersionInfo(path);
            if (version.FileMajorPart == 0 && version.FileMinorPart == 0 && version.FileBuildPart == 0 && version.FilePrivatePart == 0)
            {
                return "Unknown (No information)";
            }
            return string.Format("{0}.{1}.{2}.{3}", version.FileMajorPart, version.FileMinorPart, version.FileBuildPart, version.FilePrivatePart);
        }

        public string GetGLVersion()
        {
            return caps.Version.Length > 0 ? caps.Version : (GL.GetString(StringName.Version) ?? string.Empty);
        }

        public int GetMaxTextures()
        {
            return caps.MaxTextures;
        }

        public int GetMaxTextureCoords()
        {
            return caps.MaxTextureCoords;
        }

        public int GetMaxTextureUnits()
        {
            return caps.MaxTextureUnits;
        }

        public int GetMaxTextureSize()
        {
            return caps.MaxTextureSize;
        }

        public int GetMaxColorAttachments()
        {
            return caps.MaxColorAttachments;
        }

        public bool HasBug_glMultiTexCoord3fvARB() { return caps.HasBug_glMultiTexCoord3fvARB; }
        public bool HasBug_normalMapTexGen() { return caps.HasBug_normalMapTexGen; }
        public bool HasBug_redBlueMipmapSwap() { return caps.HasBug_redBlueMipmapSwap; }
        public bool HasBug_mipmapGeneration() { return caps.HasBug_mipmapGeneration; }
        public bool HasBug_slowVBO() { return caps.HasBug_slowVBO; }
    }
}