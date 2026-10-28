using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX;
using Device = SharpDX.Direct3D11.Device;
using DxgiFormat = SharpDX.DXGI.Format;

namespace G3D.Direct3D11
{
    public sealed class Direct3D11Backend : IGraphicsBackend
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct ImmediateVertex
        {
            public float X, Y, Z;
            public float Nx, Ny, Nz;
            public float Pad0, Pad1;
            public float R, G, B, A;
            public float S, T, U, V;
        }

        private sealed class TextureSamplerParameters
        {
            public Texture.WrapMode WrapS = Texture.WrapMode.TILE;
            public Texture.WrapMode WrapT = Texture.WrapMode.TILE;
            public Texture.InterpolateMode MinFilter = Texture.InterpolateMode.TRILINEAR_MIPMAP;
            public Texture.InterpolateMode MagFilter = Texture.InterpolateMode.TRILINEAR_MIPMAP;
            public float MaxAnisotropy = 1.0f;
            public float LodBias = 0.0f;
        }

        private Direct3D11Window window;
        private Device device;
        private DeviceContext context;
        private SwapChain swapChain;
        private RenderTargetView renderTargetView;
        private DepthStencilView defaultDepthView;
        private Texture2D defaultDepthResource;
        private BackendCaps caps;
        private readonly Dictionary<int, Direct3D11Texture> textures = new Dictionary<int, Direct3D11Texture>();
        private readonly Dictionary<int, Direct3D11Framebuffer> framebuffers = new Dictionary<int, Direct3D11Framebuffer>();
        private readonly Dictionary<int, Direct3D11Buffer> buffers = new Dictionary<int, Direct3D11Buffer>();
        private readonly Dictionary<int, Direct3D11Renderbuffer> renderbuffers = new Dictionary<int, Direct3D11Renderbuffer>();
        private readonly Dictionary<int, Direct3D11Program> programs = new Dictionary<int, Direct3D11Program>();
        private readonly Dictionary<int, Direct3D11Milestone> milestones = new Dictionary<int, Direct3D11Milestone>();
        private int nextTextureId = 1;
        private int nextFramebufferId = 1;
        private int nextBufferId = 1;
        private int nextRenderbufferId = 1;
        private int nextProgramId = 1;
        private int nextMilestoneId = 1;
        private int currentProgramID;
        private int currentFramebuffer;
        private int currentBoundBuffer;
        private int currentBufferTarget;
        private DepthStencilView activeDepthView;
        private Texture2D activeDepthResource;
        private RenderTargetView activeColorView;
        private Color4 clearValue = new Color4(0.10f, 0.20f, 0.30f, 1.0f);
        private double depthClearValue = 1.0;
        private int stencilClearValue;
        private Matrix4 projection = Matrix4.identity();
        private Matrix4 modelView = Matrix4.identity();
        private Matrix4 lightMvp = Matrix4.identity();
        private Matrix4 textureMatrix = Matrix4.identity();
        private Color4 ambient = new Color4(0.1f, 0.1f, 0.1f, 1.0f);
        private Color3 specular = Color3.white();
        private float shininess = 32.0f;
        private bool lightingEnabled = true;
        private bool twoSidedLighting;
        private GLight[] lights = new GLight[8];
        private bool[] lightEnabled = new bool[8];
        private readonly Dictionary<int, int> boundTextures = new Dictionary<int, int>();
        private readonly Dictionary<int, List<byte[]>> textureLevelData = new Dictionary<int, List<byte[]>>();
        private readonly Dictionary<int, TextureSamplerParameters> textureSamplerParameters = new Dictionary<int, TextureSamplerParameters>();
        private readonly float[] textureLodBias = new float[G3D.GLCaps.G3D_MAX_TEXTURE_UNITS];
        private int currentTextureID;
        private readonly List<ImmediateVertex> immediateVertices = new List<ImmediateVertex>();
        private ImmediateVertex currentVertex;
        private Color4 currentColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        private Vector3 currentNormal = Vector3.zero();
        private Vector4 currentTexCoord = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        private RenderDevice.Primitive immediatePrimitive;
        private VertexShader fixedVertexShader;
        private PixelShader fixedPixelShader;
        private InputLayout fixedInputLayout;
        private SharpDX.Direct3D11.Buffer constantsBuffer;
        private SharpDX.Direct3D11.Buffer immediateVertexBuffer;
        private SharpDX.Direct3D11.Buffer[] attrBuffers = new SharpDX.Direct3D11.Buffer[4];
        private SharpDX.Direct3D11.Buffer attrIndexBuffer;
        private int attrIndexBufferSize;
        private int fixedTextureUnit = -1;
        private DepthStencilState depthState;
        private BlendState blendState;
        private RasterizerState rasterizerState;
        private bool colorWrite = true;
        private bool alphaWrite = true;
        private bool depthWrite = true;
        private RenderDevice.DepthTest depthTest = RenderDevice.DepthTest.DEPTH_LESS;
        private float viewportHeight = 1.0f;
        private float targetHeight = 1.0f;
        private RenderDevice.BlendFunc blendSrc = RenderDevice.BlendFunc.BLEND_ONE;
        private RenderDevice.BlendFunc blendDst = RenderDevice.BlendFunc.BLEND_ZERO;
        private RenderDevice.BlendEq blendEquation = RenderDevice.BlendEq.BLENDEQ_ADD;

        public Direct3D11Backend()
        {
            caps = new BackendCaps();
            for (int i = 0; i < lights.Length; ++i) lights[i] = new GLight();
        }

        public GraphicsBackendKind Kind { get { return GraphicsBackendKind.Direct3D11; } }
        public string Description { get { return string.Format("Direct3D 11 {0}", caps.Version); } }
        public BackendCaps Caps { get { return caps; } }

        public void Initialize(IGraphicsWindow graphicsWindow)
        {
            window = (Direct3D11Window)graphicsWindow;
            int width = Math.Max(1, window.Width);
            int height = Math.Max(1, window.Height);
            SwapChainDescription description = new SwapChainDescription();
            description.BufferCount = 1;
            description.ModeDescription = new ModeDescription(width, height, new Rational(0, 1), DxgiFormat.R8G8B8A8_UNorm);
            description.IsWindowed = true;
            description.OutputHandle = window.NativeHandle;
            description.SampleDescription = new SampleDescription(1, 0);
            description.SwapEffect = SwapEffect.Discard;
            description.Usage = Usage.RenderTargetOutput;
            Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport, description, out device, out swapChain);
            context = device.ImmediateContext;
            CreateRenderTarget();
            CreateFixedFunctionPipeline();
            caps.MaxTextureSize = 16384;
            caps.MaxTextureUnits = G3D.GLCaps.G3D_MAX_TEXTURE_UNITS;
            caps.MaxTextures = caps.MaxTextureUnits;
            caps.MaxTextureCoords = caps.MaxTextureUnits;
            caps.MaxLights = 1;
            caps.MaxColorAttachments = 1;
            caps.SupportsShadows = false;
            caps.SupportsCubeMaps = true;
            caps.SupportsStencil = true;
            caps.SupportsTwoSidedStencil = true;
            caps.SupportsVertexProgram = true;
            caps.SupportsVertexProgramNV2 = false;
            caps.SupportsPixelProgram = true;
            caps.SupportsVertexBufferObject = true;
            caps.SupportsTextureRectangle = false;
            caps.SupportsAlphaTest = false;
            caps.SupportsTextureCombineModes = false;
            caps.SupportsTextureLODBias = true;
            caps.SupportsGenericVertexAttributes = false;
            caps.SupportsStereoDrawBuffers = false;
            caps.SupportsReflectionMaps = false;
            caps.HasBug_glMultiTexCoord3fvARB = false;
            caps.HasBug_normalMapTexGen = false;
            caps.HasBug_redBlueMipmapSwap = false;
            caps.HasBug_mipmapGeneration = false;
            caps.HasBug_slowVBO = false;
            caps.Renderer = getAdapterDescription();
            caps.Vendor = "Microsoft";
            caps.Version = "Direct3D 11, Feature Level " + featureLevelName(device.FeatureLevel);
            uint directXVersion = DXCaps.version();
            caps.DriverVersion = directXVersion == 0
                ? "Direct3D 11 (DirectX version unknown)"
                : string.Format("Direct3D 11 (DirectX {0}.{1})", directXVersion / 100, directXVersion % 100);
            caps.VideoMemorySize = DXCaps.videoMemorySize();
            caps.Extensions = new List<string>();
            caps.ShaderLanguages = new List<GraphicsShaderLanguage>();
            caps.ShaderLanguages.Add(GraphicsShaderLanguage.HLSL);
            G3D.GLCaps.setBackend(this);
        }

        private static string getAdapterDescription()
        {
            try
            {
                using (Factory1 factory = new Factory1())
                {
                    using (Adapter1 adapter = factory.GetAdapter1(0))
                    {
                        if (adapter == null)
                        {
                            return string.Empty;
                        }
                        return adapter.Description.Description ?? string.Empty;
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string featureLevelName(FeatureLevel level)
        {
            switch (level)
            {
                case FeatureLevel.Level_9_1: return "9.1";
                case FeatureLevel.Level_9_2: return "9.2";
                case FeatureLevel.Level_9_3: return "9.3";
                case FeatureLevel.Level_10_0: return "10.0";
                case FeatureLevel.Level_10_1: return "10.1";
                case FeatureLevel.Level_11_0: return "11.0";
                case FeatureLevel.Level_11_1: return "11.1";
                case FeatureLevel.Level_12_0: return "12.0";
                case FeatureLevel.Level_12_1: return "12.1";
                default: return level.ToString();
            }
        }

        private void CreateRenderTarget()
        {
            using (Texture2D backBuffer = swapChain.GetBackBuffer<Texture2D>(0)) renderTargetView = new RenderTargetView(device, backBuffer);
            Direct3D11Texture depth = new Direct3D11Texture();
            depth.Width = window.Width;
            depth.Height = window.Height;
            targetHeight = Math.Max(1.0f, window.Height);
            depth.IsDepth = true;
            depth.Resource = new Texture2D(device, new Texture2DDescription {
                Width = Math.Max(1, window.Width), Height = Math.Max(1, window.Height), MipLevels = 1,
                ArraySize = 1, Format = DxgiFormat.D32_Float, SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default, BindFlags = BindFlags.DepthStencil
            });
            depth.DepthView = new DepthStencilView(device, depth.Resource);
            defaultDepthView = depth.DepthView;
            defaultDepthResource = depth.Resource;
            activeDepthView = defaultDepthView;
            activeDepthResource = defaultDepthResource;
            activeColorView = renderTargetView;
            context.OutputMerger.SetRenderTargets(activeDepthView, activeColorView);
        }

        private void CreateFixedFunctionPipeline()
        {
            CompilationResult vs = compileShaderSource(FixedFunctionShaders.VertexSource, "main", "vs_4_0");
            CompilationResult fs = compileShaderSource(FixedFunctionShaders.FragmentSource, "main", "ps_4_1");
            if (vs.HasErrors || fs.HasErrors) throw new InvalidOperationException("Fixed-function shader compilation failed: " + vs.Message + fs.Message);
            fixedVertexShader = new VertexShader(device, vs.Bytecode);
            fixedPixelShader = new PixelShader(device, fs.Bytecode);
            InputElement[] elements = new InputElement[] {
                new InputElement("POSITION", 0, DxgiFormat.R32G32B32_Float, 0, 0),
                new InputElement("NORMAL", 0, DxgiFormat.R32G32B32_Float, 12, 0),
                new InputElement("COLOR", 0, DxgiFormat.R32G32B32A32_Float, 32, 0),
                new InputElement("TEXCOORD", 0, DxgiFormat.R32G32B32A32_Float, 48, 0)
            };
            fixedInputLayout = new InputLayout(device, vs.Bytecode, elements);
            constantsBuffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription { SizeInBytes = 320, Usage = ResourceUsage.Dynamic, BindFlags = BindFlags.ConstantBuffer, CpuAccessFlags = CpuAccessFlags.Write });
            immediateVertexBuffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription { SizeInBytes = 1024 * 64, Usage = ResourceUsage.Dynamic, BindFlags = BindFlags.VertexBuffer, CpuAccessFlags = CpuAccessFlags.Write });
            context.InputAssembler.InputLayout = fixedInputLayout;
            context.VertexShader.Set(fixedVertexShader);
            context.PixelShader.Set(fixedPixelShader);
            context.VertexShader.SetConstantBuffer(0, constantsBuffer);
            context.PixelShader.SetConstantBuffer(0, constantsBuffer);
        }

        public void BeginFrame() { }
        public void EndFrame() { if (swapChain != null) swapChain.Present(1, PresentFlags.None); }
        public void Clear(bool color, bool depth, bool stencil)
        {
            if (color && activeColorView != null) context.ClearRenderTargetView(activeColorView, new RawColor4(clearValue.r, clearValue.g, clearValue.b, clearValue.a));
            if ((depth || stencil) && activeDepthView != null)
            {
                DepthStencilClearFlags flags = DepthStencilClearFlags.Depth;
                if (stencil) flags |= DepthStencilClearFlags.Stencil;
                context.ClearDepthStencilView(activeDepthView, flags, (float)depthClearValue, (byte)stencilClearValue);
            }
        }
        public void Resize(int width, int height)
        {
            if (swapChain == null) return;
            width = Math.Max(1, width); height = Math.Max(1, height);
            context.OutputMerger.SetRenderTargets((RenderTargetView)null);
            if (renderTargetView != null) { renderTargetView.Dispose(); renderTargetView = null; }
            swapChain.ResizeBuffers(1, width, height, DxgiFormat.R8G8B8A8_UNorm, SwapChainFlags.None);
            CreateRenderTarget();
        }
        public void Dispose()
        {
            foreach (Direct3D11Texture t in textures.Values) t.Dispose();
            foreach (Direct3D11Framebuffer f in framebuffers.Values) f.Dispose();
            foreach (Direct3D11Buffer b in buffers.Values) b.Dispose();
            foreach (Direct3D11Program p in programs.Values) p.Dispose();
            textures.Clear(); framebuffers.Clear(); buffers.Clear(); programs.Clear();
            if (fixedInputLayout != null) fixedInputLayout.Dispose();
            if (fixedVertexShader != null) fixedVertexShader.Dispose();
            if (fixedPixelShader != null) fixedPixelShader.Dispose();
            if (constantsBuffer != null) constantsBuffer.Dispose();
            if (immediateVertexBuffer != null) immediateVertexBuffer.Dispose();
            foreach (SharpDX.Direct3D11.Buffer b in attrBuffers) if (b != null) b.Dispose();
            if (attrIndexBuffer != null) attrIndexBuffer.Dispose();
            if (defaultDepthView != null) defaultDepthView.Dispose();
            if (renderTargetView != null) renderTargetView.Dispose();
            if (swapChain != null) swapChain.Dispose();
            if (context != null) context.Dispose();
            if (device != null) device.Dispose();
        }

        public int CreateTexture() { return CreateTexture(TextureFormat.RGBA8, 1, 1, Texture.DepthReadMode.DEPTH_NORMAL); }
        public int CreateTexture(TextureFormat format, int width, int height, Texture.DepthReadMode depthReadMode)
        {
            if (format == null || format == TextureFormat.AUTO) format = TextureFormat.RGBA8;
            width = Math.Max(1, width); height = Math.Max(1, height);
            Direct3D11Texture t = new Direct3D11Texture();
            t.Id = nextTextureId++; t.Format = format; t.Width = width; t.Height = height; t.DepthMode = depthReadMode; t.IsDepth = format.getIsDepth();
            DxgiFormat dx = t.IsDepth ? DxgiFormat.D32_Float : TextureFormatMap.ToDXGI(format);
            t.Resource = new Texture2D(device, new Texture2DDescription { Width = width, Height = height, MipLevels = t.IsDepth ? 1 : mipLevelCount(width, height), ArraySize = 1, Format = dx, SampleDescription = new SampleDescription(1, 0), Usage = ResourceUsage.Default, BindFlags = t.IsDepth ? BindFlags.DepthStencil : (BindFlags.ShaderResource | BindFlags.RenderTarget) });
            if (t.IsDepth)
            {
                t.DepthView = new DepthStencilView(device, t.Resource);
            }
            else
            {
                t.ShaderView = new ShaderResourceView(device, t.Resource);
                t.ColorView = new RenderTargetView(device, t.Resource);
            }
            textures.Add(t.Id, t); return t.Id;
        }
        private static int mipLevelCount(int width, int height)
        {
            int largest = Math.Max(width, height);
            int levels = 1;
            while (largest > 1)
            {
                largest = largest / 2;
                levels++;
            }
            return levels;
        }
        public void DeleteTexture(int textureID) { Direct3D11Texture t; if (textures.TryGetValue(textureID, out t)) { t.Dispose(); textures.Remove(textureID); textureLevelData.Remove(textureID); textureSamplerParameters.Remove(textureID); if (currentTextureID == textureID) currentTextureID = 0; } }
        public void BindTexture(int textureUnit, int textureID, Texture.Dimension dimension)
        {
            if (textureID == 0)
            {
                context.VertexShader.SetShaderResource(textureUnit, null);
                context.PixelShader.SetShaderResource(textureUnit, null);
                boundTextures.Remove(textureUnit);
                currentTextureID = 0;
                return;
            }
            Direct3D11Texture t; if (!textures.TryGetValue(textureID, out t)) return;
            ShaderResourceView view = t.IsDepth ? t.ComparisonView : t.ShaderView;
            if (view == null)
            {
                throw new NotSupportedException(
                    "G3D.Direct3D11: a depth texture cannot be sampled because Direct3D 11 does not allow " +
                    "a resource to be bound for both depth-stencil and shader-resource use.");
            }
            context.VertexShader.SetShaderResource(textureUnit, view);
            context.PixelShader.SetShaderResource(textureUnit, view);
            if (t.Sampler != null)
            {
                context.VertexShader.SetSampler(textureUnit, t.Sampler);
                context.PixelShader.SetSampler(textureUnit, t.Sampler);
            }
            boundTextures[textureUnit] = textureID;
            currentTextureID = textureID;
        }
        public int GetTextureTarget(Texture.Dimension dimension) { return 0x0DE1; }
        public void TexImage2D(int target, int level, TextureFormat format, int width, int height, int border, byte[] data)
        {
            Direct3D11Texture texture;
            if (!textures.TryGetValue(currentTextureID, out texture) || texture.Resource == null)
            {
                throw new InvalidOperationException("No texture is bound for upload.");
            }
            if (data == null || texture.IsDepth) return;
            byte[] converted = ConvertTextureData(format, data, width, height);
            if (level >= 0 && level < texture.Resource.Description.MipLevels)
            {
                context.UpdateSubresource(converted, texture.Resource, level, 0, 0, null);
            }
            recordTextureLevel(texture.Id, level, converted);
        }
        private void recordTextureLevel(int textureID, int level, byte[] data)
        {
            List<byte[]> levels;
            if (!textureLevelData.TryGetValue(textureID, out levels))
            {
                levels = new List<byte[]>();
                textureLevelData[textureID] = levels;
            }
            while (levels.Count <= level)
            {
                levels.Add(null);
            }
            levels[level] = data;
        }
        public void CompressedTexImage2D(int target, TextureFormat format, int width, int height, List<List<byte[]>> mipMaps)
        {
            Direct3D11Texture texture = null;
            foreach (Direct3D11Texture candidate in textures.Values)
            {
                if (candidate.Width == width && candidate.Height == height && candidate.Format == format)
                {
                    texture = candidate;
                    break;
                }
            }
            if (texture == null)
            {
                throw new InvalidOperationException("The compressed texture was not allocated.");
            }
            int faceCount = mipMaps[0].Count;
            int mipCount = mipMaps.Count;
            if (texture.ShaderView != null) { texture.ShaderView.Dispose(); texture.ShaderView = null; }
            if (texture.ColorView != null) { texture.ColorView.Dispose(); texture.ColorView = null; }
            texture.Resource.Dispose();
            texture.Resource = new Texture2D(device, new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = mipCount,
                ArraySize = faceCount,
                Format = TextureFormatMap.ToDXGI(format),
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget
            });
            texture.ShaderView = new ShaderResourceView(device, texture.Resource, new ShaderResourceViewDescription
            {
                Format = TextureFormatMap.ToDXGI(format),
                Dimension = faceCount == 6 ? ShaderResourceViewDimension.TextureCube : ShaderResourceViewDimension.Texture2D
            });
            texture.ColorView = new RenderTargetView(device, texture.Resource);
            for (int level = 0; level < mipCount; ++level)
            {
                for (int face = 0; face < faceCount; ++face)
                {
                    context.UpdateSubresource(mipMaps[level][face], texture.Resource, level + face * mipCount, 0, 0, null);
                }
            }
        }

        private static byte[] ConvertTextureData(TextureFormat format, byte[] data, int width, int height)
        {
            if (format != TextureFormat.RGB8 || data == null) return data;
            byte[] converted = new byte[width * height * 4];
            for (int i = 0; i < width * height; ++i) { int source = i * 3; int destination = i * 4; converted[destination] = data[source]; converted[destination + 1] = data[source + 1]; converted[destination + 2] = data[source + 2]; converted[destination + 3] = 255; }
            return converted;
        }
        public void TexSubImage2D(int target, int level, int xoffset, int yoffset, int width, int height, TextureFormat format, byte[] data)
        {
            Direct3D11Texture texture;
            if (!textures.TryGetValue(currentTextureID, out texture) || texture.Resource == null)
            {
                throw new InvalidOperationException("No texture is bound for update.");
            }
            if (data == null || texture.IsDepth) return;
            byte[] converted = ConvertTextureData(format, data, width, height);
            int bytes = bytesPerPixel(texture.Resource.Description.Format);
            IntPtr source = Marshal.AllocHGlobal(converted.Length);
            try
            {
                Marshal.Copy(converted, 0, source, converted.Length);
                DataBox box = new DataBox(source, converted.Length, width * bytes);
                ResourceRegion region = new ResourceRegion(xoffset, yoffset, 0, width, height, 1);
                context.UpdateSubresource(box, texture.Resource, level, region);
            }
            finally
            {
                Marshal.FreeHGlobal(source);
            }
        }
        public void GenerateMipmap(int target)
        {
            Direct3D11Texture texture;
            if (!textures.TryGetValue(currentTextureID, out texture) || texture.Resource == null)
            {
                throw new InvalidOperationException("No texture is bound for mipmap generation.");
            }
            if (texture.IsDepth)
            {
                throw new NotSupportedException("G3D.Direct3D11: mipmap generation is not supported for depth textures.");
            }
            if (isCompressedFormat(texture.Resource.Description.Format))
            {
                throw new NotSupportedException("G3D.Direct3D11: mipmap generation is not supported for block compressed textures.");
            }
            List<byte[]> levels;
            if (!textureLevelData.TryGetValue(texture.Id, out levels))
            {
                levels = new List<byte[]>();
                textureLevelData[texture.Id] = levels;
            }
            int bytes = bytesPerPixel(texture.Resource.Description.Format);
            int mipLevels = texture.Resource.Description.MipLevels;
            byte[] previous = levels.Count > 0 ? levels[0] : null;
            if (previous == null)
            {
                previous = new byte[texture.Width * texture.Height * bytes];
                recordTextureLevel(texture.Id, 0, previous);
            }
            int width = texture.Width;
            int height = texture.Height;
            for (int level = 1; level < mipLevels; ++level)
            {
                byte[] destination;
                if (levels.Count > level && levels[level] != null)
                {
                    destination = levels[level];
                }
                else
                {
                    destination = downsample(previous, width, height, bytes);
                }
                context.UpdateSubresource(destination, texture.Resource, level, 0, 0, null);
                recordTextureLevel(texture.Id, level, destination);
                previous = destination;
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
            }
        }
        private static byte[] downsample(byte[] source, int width, int height, int bytes)
        {
            int destinationWidth = Math.Max(1, width / 2);
            int destinationHeight = Math.Max(1, height / 2);
            byte[] destination = new byte[destinationWidth * destinationHeight * bytes];
            for (int y = 0; y < destinationHeight; ++y)
            {
                for (int x = 0; x < destinationWidth; ++x)
                {
                    for (int c = 0; c < bytes; ++c)
                    {
                        int sum = 0;
                        int count = 0;
                        for (int dy = 0; dy < 2; ++dy)
                        {
                            for (int dx = 0; dx < 2; ++dx)
                            {
                                int sx = Math.Min(width - 1, x * 2 + dx);
                                int sy = Math.Min(height - 1, y * 2 + dy);
                                sum += source[(sy * width + sx) * bytes + c];
                                ++count;
                            }
                        }
                        destination[(y * destinationWidth + x) * bytes + c] = (byte)(sum / count);
                    }
                }
            }
            return destination;
        }
        private static bool isCompressedFormat(DxgiFormat format)
        {
            return format == DxgiFormat.BC1_UNorm || format == DxgiFormat.BC1_UNorm_SRgb ||
                   format == DxgiFormat.BC2_UNorm || format == DxgiFormat.BC2_UNorm_SRgb ||
                   format == DxgiFormat.BC3_UNorm || format == DxgiFormat.BC3_UNorm_SRgb ||
                   format == DxgiFormat.BC4_UNorm || format == DxgiFormat.BC4_SNorm ||
                   format == DxgiFormat.BC5_UNorm || format == DxgiFormat.BC5_SNorm;
        }
        private static int bytesPerPixel(DxgiFormat format)
        {
            switch (format)
            {
                case DxgiFormat.R8_UNorm: return 1;
                case DxgiFormat.R8G8_UNorm: return 2;
                case DxgiFormat.R16_UNorm: return 2;
                case DxgiFormat.R16G16_UNorm: return 4;
                case DxgiFormat.R32_Float: return 4;
                default: return 4;
            }
        }
        public void TexParameter(int target, Texture.WrapMode wrapS, Texture.WrapMode wrapT, Texture.InterpolateMode minFilter, Texture.InterpolateMode magFilter, float maxAnisotropy)
        {
            Direct3D11Texture texture;
            if (!textures.TryGetValue(currentTextureID, out texture))
            {
                throw new InvalidOperationException("No texture is bound for parameter update.");
            }
            TextureSamplerParameters parameters;
            if (!textureSamplerParameters.TryGetValue(texture.Id, out parameters))
            {
                parameters = new TextureSamplerParameters();
                textureSamplerParameters[texture.Id] = parameters;
            }
            parameters.WrapS = wrapS;
            parameters.WrapT = wrapT;
            parameters.MinFilter = minFilter;
            parameters.MagFilter = magFilter;
            parameters.MaxAnisotropy = maxAnisotropy;
            if (texture.Sampler != null) { texture.Sampler.Dispose(); texture.Sampler = null; }
            texture.Sampler = createSampler(parameters);
            foreach (KeyValuePair<int, int> entry in boundTextures)
            {
                if (entry.Value == texture.Id)
                {
                    context.VertexShader.SetSampler(entry.Key, texture.Sampler);
                    context.PixelShader.SetSampler(entry.Key, texture.Sampler);
                }
            }
        }
        public void SetTextureLODBias(int textureUnit, float bias)
        {
            if (textureUnit < 0 || textureUnit >= textureLodBias.Length)
            {
                throw new ArgumentOutOfRangeException("textureUnit");
            }
            textureLodBias[textureUnit] = bias;
            int textureID;
            if (!boundTextures.TryGetValue(textureUnit, out textureID)) return;
            Direct3D11Texture texture;
            if (!textures.TryGetValue(textureID, out texture)) return;
            TextureSamplerParameters parameters;
            if (!textureSamplerParameters.TryGetValue(textureID, out parameters))
            {
                parameters = new TextureSamplerParameters();
                textureSamplerParameters[textureID] = parameters;
            }
            parameters.LodBias = bias;
            if (texture.Sampler != null) { texture.Sampler.Dispose(); texture.Sampler = null; }
            texture.Sampler = createSampler(parameters);
            context.VertexShader.SetSampler(textureUnit, texture.Sampler);
            context.PixelShader.SetSampler(textureUnit, texture.Sampler);
        }
        private SamplerState createSampler(TextureSamplerParameters parameters)
        {
            SamplerStateDescription d = SamplerStateDescription.Default();
            d.AddressU = wrapAddressMode(parameters.WrapS);
            d.AddressV = wrapAddressMode(parameters.WrapT);
            d.AddressW = TextureAddressMode.Clamp;
            d.ComparisonFunction = Comparison.Never;
            d.MipLodBias = parameters.LodBias;
            d.MinimumLod = 0.0f;
            d.MaximumLod = float.MaxValue;
            if (parameters.MaxAnisotropy > 1.0f)
            {
                d.Filter = Filter.Anisotropic;
                d.MaximumAnisotropy = (int)parameters.MaxAnisotropy;
            }
            else
            {
                d.Filter = filterFor(parameters.MinFilter, parameters.MagFilter);
            }
            return new SamplerState(device, d);
        }
        private static TextureAddressMode wrapAddressMode(Texture.WrapMode wrap)
        {
            switch (wrap)
            {
                case Texture.WrapMode.CLAMP: return TextureAddressMode.Clamp;
                case Texture.WrapMode.TRANSPARENT_BORDER: return TextureAddressMode.Border;
                default: return TextureAddressMode.Wrap;
            }
        }
        private static Filter filterFor(Texture.InterpolateMode minFilter, Texture.InterpolateMode magFilter)
        {
            bool nearestMag = magFilter == Texture.InterpolateMode.NEAREST_NO_MIPMAP ||
                              magFilter == Texture.InterpolateMode.NO_INTERPOLATION ||
                              magFilter == Texture.InterpolateMode.NEAREST_MIPMAP;
            bool nearestMin = minFilter == Texture.InterpolateMode.NEAREST_NO_MIPMAP ||
                              minFilter == Texture.InterpolateMode.NO_INTERPOLATION ||
                              minFilter == Texture.InterpolateMode.NEAREST_MIPMAP;
            bool linearMip = minFilter == Texture.InterpolateMode.BILINEAR_MIPMAP ||
                             minFilter == Texture.InterpolateMode.TRILINEAR_MIPMAP;
            if (nearestMin)
            {
                if (nearestMag)
                {
                    return linearMip ? Filter.MinMagPointMipLinear : Filter.MinMagMipPoint;
                }
                return linearMip ? Filter.MinPointMagMipLinear : Filter.MinPointMagLinearMipPoint;
            }
            if (nearestMag)
            {
                return linearMip ? Filter.MinLinearMagPointMipLinear : Filter.MinLinearMagMipPoint;
            }
            return linearMip ? Filter.MinMagMipLinear : Filter.MinMagLinearMipPoint;
        }
        public void SetTextureDepthCompare(int target, Texture.DepthReadMode mode)
        {
            foreach (Direct3D11Texture t in textures.Values) if (t.IsDepth) { t.DepthMode = mode; t.ComparisonSampler = CreateSampler(mode, true); }
        }
        public void ConfigureShadowMap(int textureUnit, Matrix4 lightMVP, Matrix4 cameraToWorldMatrixInverse, int textureID, Texture.DepthReadMode mode)
        {
            throw new NotSupportedException(
                "G3D.Direct3D11: shadow maps are not supported because Direct3D 11 does not allow a depth " +
                "texture to be sampled; the shadow map would have to be rendered into a sampled float texture.");
        }
        private SamplerState CreateSampler(Texture.DepthReadMode mode, bool comparison)
        {
            SamplerStateDescription d = SamplerStateDescription.Default();
            d.AddressU = TextureAddressMode.Clamp; d.AddressV = TextureAddressMode.Clamp; d.AddressW = TextureAddressMode.Clamp;
            d.Filter = comparison ? Filter.ComparisonMinMagMipLinear : Filter.MinMagMipLinear;
            d.ComparisonFunction = comparison ? (mode == Texture.DepthReadMode.DEPTH_GEQUAL ? Comparison.GreaterEqual : Comparison.LessEqual) : Comparison.Never;
            return new SamplerState(device, d);
        }
        public void DisableShadowMap(int textureUnit) { if (fixedTextureUnit == textureUnit) fixedTextureUnit = -1; }
        public void CopyTexImage2D(int target, int level, int x, int y, int width, int height, int border)
        {
            throw new NotSupportedException("G3D.Direct3D11: copying the render target into a texture is not implemented by the Direct3D 11 backend.");
        }
        public void CopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height)
        {
            throw new NotSupportedException("G3D.Direct3D11: copying the render target into a texture is not implemented by the Direct3D 11 backend.");
        }
        public void GetTexImage(int target, int level, TextureFormat format, byte[] data)
        {
            Direct3D11Texture texture;
            if (!textures.TryGetValue(currentTextureID, out texture) || texture.Resource == null)
            {
                throw new InvalidOperationException("No texture is bound for readback.");
            }
            if (data == null) return;
            int bytes = bytesPerPixel(texture.Resource.Description.Format);
            int width = Math.Max(1, texture.Width >> level);
            int height = Math.Max(1, texture.Height >> level);
            Texture2D staging = new Texture2D(device, new Texture2DDescription
            {
                Width = texture.Width,
                Height = texture.Height,
                MipLevels = texture.Resource.Description.MipLevels,
                ArraySize = 1,
                Format = texture.Resource.Description.Format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read
            });
            try
            {
                context.CopyResource(staging, texture.Resource);
                DataBox box = context.MapSubresource(staging, level, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                try
                {
                    byte[] full = new byte[width * height * bytes];
                    for (int row = 0; row < height; ++row)
                    {
                        IntPtr rowPointer = new IntPtr(box.DataPointer.ToInt64() + row * box.RowPitch);
                        Marshal.Copy(rowPointer, full, row * width * bytes, width * bytes);
                    }
                    if (format == TextureFormat.RGB8 && bytes == 4)
                    {
                        for (int i = 0; i < width * height; ++i)
                        {
                            data[i * 3] = full[i * 4];
                            data[i * 3 + 1] = full[i * 4 + 1];
                            data[i * 3 + 2] = full[i * 4 + 2];
                        }
                    }
                    else
                    {
                        Array.Copy(full, 0, data, 0, Math.Min(full.Length, data.Length));
                    }
                }
                finally
                {
                    context.UnmapSubresource(staging, level);
                }
            }
            finally
            {
                staging.Dispose();
            }
        }

        public bool SupportsShaderLanguage(GraphicsShaderLanguage language)
        {
            return language == GraphicsShaderLanguage.HLSL;
        }

        private static CompilationResult compileShaderSource(string source, string entryPoint, string profile)
        {
            IntPtr text = Marshal.StringToHGlobalAnsi(source);
            try
            {
                return ShaderBytecode.Compile(text, source.Length, entryPoint, profile, ShaderFlags.None, EffectFlags.None, null, null, null, SecondaryDataFlags.None, null);
            }
            finally
            {
                Marshal.FreeHGlobal(text);
            }
        }

        public int CreateShaderProgram(string vertexSource, string fragmentSource) { throw new NotSupportedException("G3D.Direct3D11: GLSL shader programs are not supported by the Direct3D 11 backend."); }

        public int CreateShaderProgram(GraphicsShaderLanguage language, string vertexSource, string fragmentSource)
        {
            ShaderProgramInfo info = CompileShaderProgram(language, vertexSource, fragmentSource);
            if (!info.Success)
            {
                throw new InvalidOperationException(info.Messages);
            }
            return info.ProgramID;
        }

        public ShaderProgramInfo CompileShaderProgram(GraphicsShaderLanguage language, string vertexSource, string fragmentSource)
        {
            if (language == GraphicsShaderLanguage.GLSL)
            {
                string vertexHlsl;
                string pixelHlsl;
                GlslToHlslTranslator.translatePair(vertexSource, fragmentSource, out vertexHlsl, out pixelHlsl);
                vertexSource = vertexHlsl;
                fragmentSource = pixelHlsl;
                language = GraphicsShaderLanguage.HLSL;
            }
            if (language != GraphicsShaderLanguage.HLSL)
            {
                throw new NotSupportedException("The Direct3D 11 backend supports GLSL and HLSL shader programs only.");
            }

            CompilationResult vertexResult;
            CompilationResult pixelResult;
            try
            {
                vertexResult = compileShaderSource(vertexSource, "main", "vs_4_0");
                pixelResult = compileShaderSource(fragmentSource, "main", "ps_4_0");
            }
            catch (Exception exception)
            {
                ShaderProgramInfo failure = new ShaderProgramInfo();
                failure.Language = language;
                failure.Success = false;
                failure.Messages = exception.Message;
                return failure;
            }

            if (vertexResult.HasErrors || pixelResult.HasErrors)
            {
                ShaderProgramInfo failure = new ShaderProgramInfo();
                failure.Language = language;
                failure.Success = false;
                failure.VertexErrors = vertexResult.HasErrors ? vertexResult.Message : "";
                failure.PixelErrors = pixelResult.HasErrors ? pixelResult.Message : "";
                failure.Messages = failure.VertexErrors + failure.PixelErrors;
                return failure;
            }

            Direct3D11Program program = new Direct3D11Program();
            program.Id = nextProgramId++;
            program.Vertex = new VertexShader(device, vertexResult.Bytecode);
            program.Pixel = new PixelShader(device, pixelResult.Bytecode);
            programs.Add(program.Id, program);

            ShaderProgramInfo result = new ShaderProgramInfo();
            result.Language = language;
            result.ProgramID = program.Id;
            result.Success = true;

            reflectStage(program, vertexResult.Bytecode, true, result);
            reflectStage(program, pixelResult.Bytecode, false, result);
            HlslProgramCompiler.scanSamplers(vertexSource, result);
            HlslProgramCompiler.scanSamplers(fragmentSource, result);
            return result;
        }

        private void reflectStage(Direct3D11Program program, ShaderBytecode bytecode, bool isVertexStage, ShaderProgramInfo info)
        {
            ShaderReflection reflection = new ShaderReflection(bytecode.Data);
            for (int bufferIndex = 0; ; ++bufferIndex)
            {
                ConstantBuffer constantBuffer;
                try
                {
                    constantBuffer = reflection.GetConstantBuffer(bufferIndex);
                }
                catch (Exception)
                {
                    break;
                }
                if (constantBuffer == null)
                {
                    break;
                }

                ConstantBufferDescription description = constantBuffer.Description;
                int size = Math.Max(description.Size, 16);
                Direct3D11ConstantBuffer backendBuffer = new Direct3D11ConstantBuffer();
                backendBuffer.Name = description.Name;
                backendBuffer.Slot = parseConstantBufferSlot(description.Name, bufferIndex);
                backendBuffer.Size = size;
                backendBuffer.IsVertexStage = isVertexStage;
                backendBuffer.Data = new byte[size];
                backendBuffer.Buffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
                {
                    SizeInBytes = size,
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ConstantBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write
                });
                program.ConstantBuffers.Add(backendBuffer);

                if (isVertexStage)
                {
                    context.VertexShader.SetConstantBuffer(backendBuffer.Slot, backendBuffer.Buffer);
                }
                else
                {
                    context.PixelShader.SetConstantBuffer(backendBuffer.Slot, backendBuffer.Buffer);
                }

                for (int variableIndex = 0; variableIndex < description.VariableCount; ++variableIndex)
                {
                    ShaderReflectionVariable variable = constantBuffer.GetVariable(variableIndex);
                    ShaderVariableDescription variableDescription = variable.Description;
                    ShaderTypeDescription typeDescription = variable.GetVariableType().Description;

                    Direct3D11UniformBinding binding = null;
                    foreach (Direct3D11UniformBinding candidate in program.Uniforms)
                    {
                        if (candidate.Name == variableDescription.Name)
                        {
                            binding = candidate;
                            break;
                        }
                    }
                    if (binding == null)
                    {
                        binding = new Direct3D11UniformBinding();
                        binding.Name = variableDescription.Name;
                        binding.Size = variableDescription.Size;
                        program.Uniforms.Add(binding);

                        ShaderUniformInfo uniformInfo = new ShaderUniformInfo();
                        uniformInfo.Name = variableDescription.Name;
                        uniformInfo.Type = translateShaderType(typeDescription);
                        uniformInfo.Size = typeDescription.ElementCount;
                        uniformInfo.Location = program.Uniforms.Count - 1;
                        uniformInfo.ConstantBuffer = description.Name;
                        uniformInfo.ByteOffset = variableDescription.StartOffset;
                        uniformInfo.Register = backendBuffer.Slot;
                        info.Uniforms.Add(uniformInfo);
                    }
                    // Each stage has its own constant buffer, and a uniform shared by
                    // both stages can live at a different offset in each one.  Record
                    // the offset per buffer so the bytes land at the right place.
                    binding.Slots.Add(new Direct3D11UniformSlot { Buffer = backendBuffer, ByteOffset = variableDescription.StartOffset });
                }
            }

            if (isVertexStage)
            {
                reflectAttributes(program, reflection, bytecode);
            }
        }

        private void reflectAttributes(Direct3D11Program program, ShaderReflection reflection, ShaderBytecode bytecode)
        {
            List<InputElement> elements = new List<InputElement>();
            for (int i = 0; ; ++i)
            {
                ShaderParameterDescription parameter;
                try
                {
                    parameter = reflection.GetInputParameterDescription(i);
                }
                catch (Exception)
                {
                    break;
                }
                if (string.IsNullOrEmpty(parameter.SemanticName))
                {
                    break;
                }

                int slot = slotForSemantic(parameter.SemanticName, parameter.SemanticIndex);
                SharpDX.DXGI.Format format = formatForMask((int)parameter.UsageMask);
                elements.Add(new InputElement(parameter.SemanticName, parameter.SemanticIndex, format, slot, 0, InputClassification.PerVertexData, 0));
                program.Attributes.Add(new Direct3D11VertexAttribute { SemanticName = parameter.SemanticName, SemanticIndex = parameter.SemanticIndex, Slot = slot, Format = format });
            }

            if (elements.Count > 0)
            {
                program.Layout = new InputLayout(device, bytecode, elements.ToArray());
            }
        }

        private static int slotForSemantic(string name, int index)
        {
            switch (name)
            {
                case "POSITION": return 0;
                case "NORMAL": return 1;
                case "COLOR": return 2;
                case "TEXCOORD": return 3 + index;
                default: return 3 + index;
            }
        }

        private static SharpDX.DXGI.Format formatForMask(int mask)
        {
            int count = ((mask & 1) != 0 ? 1 : 0) + ((mask & 2) != 0 ? 1 : 0)
                      + ((mask & 4) != 0 ? 1 : 0) + ((mask & 8) != 0 ? 1 : 0);
            switch (count)
            {
                case 1: return SharpDX.DXGI.Format.R32_Float;
                case 2: return SharpDX.DXGI.Format.R32G32_Float;
                case 3: return SharpDX.DXGI.Format.R32G32B32_Float;
                case 4: return SharpDX.DXGI.Format.R32G32B32A32_Float;
                default: return SharpDX.DXGI.Format.R32G32B32A32_Float;
            }
        }

        private static ShaderProgramType translateSamplerType(ShaderResourceViewDimension dimension)
        {
            switch (dimension)
            {
                case ShaderResourceViewDimension.Texture1D: return ShaderProgramType.Sampler1D;
                case ShaderResourceViewDimension.Texture2D: return ShaderProgramType.Sampler2D;
                case ShaderResourceViewDimension.Texture3D: return ShaderProgramType.Sampler3D;
                case ShaderResourceViewDimension.TextureCube: return ShaderProgramType.SamplerCube;
                default: return ShaderProgramType.Sampler2D;
            }
        }

        private static int parseConstantBufferSlot(string name, int fallback)
        {
            if (!string.IsNullOrEmpty(name) && name.Length > 1 && (name[0] == 'b' || name[0] == 'B'))
            {
                int slot;
                if (int.TryParse(name.Substring(1), out slot))
                {
                    return slot;
                }
            }
            return fallback;
        }

        private static ShaderProgramType translateShaderType(ShaderTypeDescription description)
        {
            if (description.Class == ShaderVariableClass.Scalar)
            {
                if (description.Type == ShaderVariableType.Float) { return ShaderProgramType.Float; }
                if (description.Type == ShaderVariableType.Int || description.Type == ShaderVariableType.UInt) { return ShaderProgramType.Int; }
                return ShaderProgramType.Float;
            }
            if (description.Class == ShaderVariableClass.Vector)
            {
                if (description.Type == ShaderVariableType.Float)
                {
                    if (description.RowCount == 2) { return ShaderProgramType.Float2; }
                    if (description.RowCount == 3) { return ShaderProgramType.Float3; }
                    return ShaderProgramType.Float4;
                }
                if (description.Type == ShaderVariableType.Int || description.Type == ShaderVariableType.UInt)
                {
                    if (description.RowCount == 2) { return ShaderProgramType.Int2; }
                    if (description.RowCount == 3) { return ShaderProgramType.Int3; }
                    return ShaderProgramType.Int4;
                }
                return ShaderProgramType.Float4;
            }
            if (description.Class == ShaderVariableClass.MatrixRows || description.Class == ShaderVariableClass.MatrixColumns)
            {
                return description.RowCount == 3 ? ShaderProgramType.Matrix3 : ShaderProgramType.Matrix4;
            }
            return ShaderProgramType.Float4;
        }

        public void SetShaderTexture(int programID, int textureUnit, int textureID, Texture.Dimension dimension)
        {
            BindTexture(textureUnit, textureID, dimension);
            if (textureID != 0)
            {
                Direct3D11Texture texture;
                if (textures.TryGetValue(textureID, out texture) && texture.Sampler != null)
                {
                    context.PixelShader.SetSampler(textureUnit, texture.Sampler);
                    context.VertexShader.SetSampler(textureUnit, texture.Sampler);
                }
                else
                {
                    context.PixelShader.SetSampler(textureUnit, getDefaultSampler());
                }
            }
        }

        private SamplerState defaultSampler;

        private SamplerState getDefaultSampler()
        {
            if (defaultSampler == null)
            {
                SamplerStateDescription description = new SamplerStateDescription();
                description.Filter = Filter.MinMagMipLinear;
                description.AddressU = TextureAddressMode.Wrap;
                description.AddressV = TextureAddressMode.Wrap;
                description.AddressW = TextureAddressMode.Wrap;
                description.ComparisonFunction = Comparison.Never;
                description.MinimumLod = 0;
                description.MaximumLod = float.MaxValue;
                defaultSampler = new SamplerState(device, description);
            }
            return defaultSampler;
        }
        public void DeleteShaderProgram(int programID)
        {
            Direct3D11Program program;
            if (programs.TryGetValue(programID, out program))
            {
                programs.Remove(programID);
                program.Dispose();
            }
        }

        public void UseShaderProgram(int programID)
        {
            Direct3D11Program program;
            if (!programs.TryGetValue(programID, out program))
            {
                throw new ArgumentOutOfRangeException("programID");
            }
            context.VertexShader.Set(program.Vertex);
            context.PixelShader.Set(program.Pixel);
            context.InputAssembler.InputLayout = program.Layout;
            currentProgramID = programID;
        }

        public int GetUniformLocation(int programID, string name)
        {
            Direct3D11Program program;
            if (!programs.TryGetValue(programID, out program))
            {
                throw new ArgumentOutOfRangeException("programID");
            }
            for (int i = 0; i < program.Uniforms.Count; ++i)
            {
                if (program.Uniforms[i].Name == name)
                {
                    return i;
                }
            }
            return -1;
        }
        public int GetAttribLocation(int programID, string name) { return -1; }

        public void UniformMatrix4fv(int location, Matrix4 value)
        {
            writeUniform(location, new float[] { value.m00, value.m01, value.m02, value.m03, value.m10, value.m11, value.m12, value.m13, value.m20, value.m21, value.m22, value.m23, value.m30, value.m31, value.m32, value.m33 });
        }
        public void UniformMatrix3fv(int location, Matrix3 value)
        {
            // The HLSL cbuffer member is declared row_major float3x3.  HLSL packs
            // each row of a row_major matrix into its own 16-byte register, so the
            // 9 floats must be written with a 16-byte row stride rather than
            // contiguously.
            float[] packed = new float[12];
            packed[0] = value.m00; packed[1] = value.m01; packed[2] = value.m02;
            packed[4] = value.m10; packed[5] = value.m11; packed[6] = value.m12;
            packed[8] = value.m20; packed[9] = value.m21; packed[10] = value.m22;
            writeUniform(location, packed);
        }
        public void Uniform4fv(int location, Vector4 value)
        {
            writeUniform(location, new float[] { value.x, value.y, value.z, value.w });
        }
        public void Uniform3fv(int location, Vector3 value)
        {
            writeUniform(location, new float[] { value.x, value.y, value.z });
        }
        public void Uniform2fv(int location, Vector2 value)
        {
            writeUniform(location, new float[] { value.x, value.y });
        }
        public void Uniform1f(int location, float value)
        {
            writeUniform(location, new float[] { value });
        }
        public void Uniform1i(int location, int value)
        {
            writeUniform(location, new int[] { value });
        }
        public void UniformTexture(int location, int textureUnit)
        {
            if (textureUnit < 0)
            {
                throw new ArgumentOutOfRangeException("textureUnit");
            }
        }

        private void writeUniform(int location, float[] values)
        {
            byte[] bytes = new byte[4 * values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                byte[] component = BitConverter.GetBytes(values[i]);
                Array.Copy(component, 0, bytes, 4 * i, 4);
            }
            writeUniformBytes(location, bytes);
        }

        private void writeUniform(int location, int[] values)
        {
            byte[] bytes = new byte[4 * values.Length];
            for (int i = 0; i < values.Length; ++i)
            {
                byte[] component = BitConverter.GetBytes(values[i]);
                Array.Copy(component, 0, bytes, 4 * i, 4);
            }
            writeUniformBytes(location, bytes);
        }

        private void writeUniformBytes(int location, byte[] source)
        {
            Direct3D11Program program;
            if (!programs.TryGetValue(currentProgramID, out program))
            {
                throw new InvalidOperationException("No shader program is currently bound.");
            }
            if (location < 0 || location >= program.Uniforms.Count)
            {
                throw new ArgumentOutOfRangeException("location");
            }
            Direct3D11UniformBinding uniform = program.Uniforms[location];
            foreach (Direct3D11UniformSlot slot in uniform.Slots)
            {
                Array.Copy(source, 0, slot.Buffer.Data, slot.ByteOffset, source.Length);
                updateConstantBuffer(slot.Buffer);
            }
        }

        private void updateConstantBuffer(Direct3D11ConstantBuffer constantBuffer)
        {
            DataBox box = context.MapSubresource(constantBuffer.Buffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
            try
            {
                Marshal.Copy(constantBuffer.Data, 0, box.DataPointer, constantBuffer.Data.Length);
            }
            finally
            {
                context.UnmapSubresource(constantBuffer.Buffer, 0);
            }
        }

        public int CreateVertexArray() { return 0; }
        public void DeleteVertexArray(int vaoID) { }
        public void BindVertexArray(int vaoID) { }
        public int CreateBuffer() { int id = nextBufferId++; buffers.Add(id, new Direct3D11Buffer { Id = id }); return id; }
        public void DeleteBuffer(int bufferID) { Direct3D11Buffer b; if (buffers.TryGetValue(bufferID, out b)) { b.Dispose(); buffers.Remove(bufferID); } }
        public void BindBuffer(int target, int bufferID)
        {
            currentBoundBuffer = bufferID;
            currentBufferTarget = target;
        }
        public void BufferData(int target, int size, byte[] data, VAR.Usage usage)
        {
            Direct3D11Buffer buffer;
            if (!buffers.TryGetValue(currentBoundBuffer, out buffer))
            {
                throw new InvalidOperationException("No buffer is bound.");
            }
            if (buffer.Resource != null)
            {
                buffer.Resource.Dispose();
            }
            bool isVertex = target == 0x8892;
            bool dynamic = usage != VAR.Usage.STATIC;
            buffer.IsVertex = isVertex;
            buffer.Resource = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
            {
                SizeInBytes = Math.Max(1, size),
                Usage = dynamic ? ResourceUsage.Dynamic : ResourceUsage.Default,
                BindFlags = isVertex ? BindFlags.VertexBuffer : BindFlags.IndexBuffer,
                CpuAccessFlags = dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None
            });
            if (data != null && data.Length > 0)
            {
                if (dynamic)
                {
                    DataStream stream;
                    DataBox box = context.MapSubresource(buffer.Resource, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
                    Marshal.Copy(data, 0, box.DataPointer, Math.Min(size, data.Length));
                    context.UnmapSubresource(buffer.Resource, 0);
                }
                else
                {
                    context.UpdateSubresource(data, buffer.Resource, 0, 0, 0, null);
                }
            }
        }
        public void BufferSubData(int target, int offset, int size, byte[] data)
        {
            Direct3D11Buffer buffer;
            if (!buffers.TryGetValue(currentBoundBuffer, out buffer))
            {
                throw new InvalidOperationException("No buffer is bound.");
            }
            if (buffer.Resource == null)
            {
                throw new InvalidOperationException("The buffer has no storage.");
            }
            DataStream stream;
            context.MapSubresource(buffer.Resource, 0, MapMode.Write, SharpDX.Direct3D11.MapFlags.None, out stream);
            stream.Position = offset;
            Marshal.Copy(data, 0, stream.PositionPointer, Math.Min(size, data.Length));
            context.UnmapSubresource(buffer.Resource, 0);
        }
        public void VertexAttribPointer(int index, int size, int type, bool normalized, int stride, int offset) { }
        public void EnableVertexAttribArray(int index) { }
        public void DisableVertexAttribArray(int index) { }

        public int CreateFramebuffer() { int id = nextFramebufferId++; framebuffers.Add(id, new Direct3D11Framebuffer { Id = id }); return id; }
        public void DeleteFramebuffer(int framebufferID) { Direct3D11Framebuffer f; if (framebuffers.TryGetValue(framebufferID, out f)) { f.Dispose(); framebuffers.Remove(framebufferID); } }
        public void BindFramebuffer(int framebufferID)
        {
            currentFramebuffer = framebufferID;
            if (framebufferID == 0) { activeDepthView = defaultDepthView; activeDepthResource = defaultDepthResource; activeColorView = renderTargetView; context.OutputMerger.SetRenderTargets(activeDepthView, activeColorView); return; }
            Direct3D11Framebuffer f; if (framebuffers.TryGetValue(framebufferID, out f)) { activeDepthView = f.Depth; activeDepthResource = f.DepthResource; activeColorView = f.Color; context.OutputMerger.SetRenderTargets(activeDepthView, activeColorView); }
        }
        public int GetBoundFramebuffer() { return currentFramebuffer; }
        public void FramebufferTexture2D(int target, int attachment, int textarget, int textureID, int level) { AttachFramebufferTexture(currentFramebuffer, attachment == 1 ? Framebuffer.AttachmentPoint.Depth : Framebuffer.AttachmentPoint.Color0, textureID); }
        public void FramebufferRenderbuffer(int target, int attachment, int renderbuffertarget, int renderbufferID) { }
        public void AttachFramebufferTexture(int framebufferID, Framebuffer.AttachmentPoint attachment, int textureID)
        {
            AttachFramebufferTexture(framebufferID, attachment, textureID, 0, -1);
        }
        public void AttachFramebufferTexture(int framebufferID, Framebuffer.AttachmentPoint attachment, int textureID, int mipLevel, int cubeFace)
        {
            Direct3D11Framebuffer f; Direct3D11Texture t; if (!framebuffers.TryGetValue(framebufferID, out f) || !textures.TryGetValue(textureID, out t)) return;
            if (attachment == Framebuffer.AttachmentPoint.Depth) { f.Depth = t.DepthView; f.DepthResource = t.Resource as Texture2D; } else f.Color = t.ColorView;
        }
        public void AttachFramebufferRenderbuffer(int framebufferID, Framebuffer.AttachmentPoint attachment, int renderbufferID)
        {
            Direct3D11Framebuffer f; Direct3D11Renderbuffer r; if (!framebuffers.TryGetValue(framebufferID, out f) || !renderbuffers.TryGetValue(renderbufferID, out r)) return;
            if (attachment == Framebuffer.AttachmentPoint.Depth) { f.Depth = r.DepthView; f.DepthResource = r.Resource; } else f.Color = r.ColorView;
        }
        public bool CheckFramebufferStatus() { return true; }
        public int CreateRenderbuffer()
        {
            int id = nextRenderbufferId++;
            renderbuffers.Add(id, new Direct3D11Renderbuffer { Id = id });
            return id;
        }
        public void DeleteRenderbuffer(int renderbufferID)
        {
            Direct3D11Renderbuffer renderbuffer;
            if (renderbuffers.TryGetValue(renderbufferID, out renderbuffer))
            {
                renderbuffer.Dispose();
                renderbuffers.Remove(renderbufferID);
            }
        }
        public void BindRenderbuffer(int renderbufferID) { }
        public void RenderbufferStorage(int renderbufferID, TextureFormat format, int width, int height)
        {
            Direct3D11Renderbuffer renderbuffer;
            if (!renderbuffers.TryGetValue(renderbufferID, out renderbuffer))
            {
                throw new ArgumentOutOfRangeException("renderbufferID");
            }
            renderbuffer.Dispose();
            renderbuffer.Width = width;
            renderbuffer.Height = height;
            renderbuffer.Format = format;
            DxgiFormat dxgiFormat = TextureFormatMap.ToDXGI(format);
            renderbuffer.Resource = new Texture2D(device, new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = dxgiFormat,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget | BindFlags.DepthStencil
            });
            if (TextureFormatMap.IsDepth(format))
            {
                renderbuffer.DepthView = new DepthStencilView(device, renderbuffer.Resource);
            }
            else
            {
                renderbuffer.ColorView = new RenderTargetView(device, renderbuffer.Resource);
            }
        }

        public void BeginPrimitive(RenderDevice.Primitive primitive) { immediatePrimitive = primitive; immediateVertices.Clear(); currentVertex = new ImmediateVertex(); currentVertex.R = currentColor.r; currentVertex.G = currentColor.g; currentVertex.B = currentColor.b; currentVertex.A = currentColor.a; currentVertex.Nx = currentNormal.x; currentVertex.Ny = currentNormal.y; currentVertex.Nz = currentNormal.z; currentVertex.S = currentTexCoord.x; currentVertex.T = currentTexCoord.y; currentVertex.U = currentTexCoord.z; currentVertex.V = currentTexCoord.w; }
        public void SendVertex(float x, float y, float z) { currentVertex.X = x; currentVertex.Y = y; currentVertex.Z = z; immediateVertices.Add(currentVertex); currentVertex.Nx = currentNormal.x; currentVertex.Ny = currentNormal.y; currentVertex.Nz = currentNormal.z; currentVertex.R = currentColor.r; currentVertex.G = currentColor.g; currentVertex.B = currentColor.b; currentVertex.A = currentColor.a; currentVertex.S = currentTexCoord.x; currentVertex.T = currentTexCoord.y; currentVertex.U = currentTexCoord.z; currentVertex.V = currentTexCoord.w; }
        public void SendVertex(float x, float y, float z, float w) { SendVertex(x, y, z); }
        public void SendNormal(float x, float y, float z) { currentNormal = new Vector3(x, y, z); currentVertex.Nx = x; currentVertex.Ny = y; currentVertex.Nz = z; }
        public void SendColor(float r, float g, float b, float a) { currentColor = new Color4(r, g, b, a); currentVertex.R = r; currentVertex.G = g; currentVertex.B = b; currentVertex.A = a; }
        public void SendTexCoord(float s, float t, int textureUnit) { currentTexCoord = new Vector4(s, t, 0.0f, 1.0f); currentVertex.S = s; currentVertex.T = t; currentVertex.U = 0.0f; currentVertex.V = 1.0f; }
        public void EndPrimitive()
        {
            if (immediateVertices.Count == 0) return;
            List<ImmediateVertex> vertices = immediateVertices;
            if (immediatePrimitive == RenderDevice.Primitive.QUADS)
            {
                vertices = new List<ImmediateVertex>();
                for (int i = 0; i + 3 < immediateVertices.Count; i += 4)
                {
                    vertices.Add(immediateVertices[i]); vertices.Add(immediateVertices[i + 1]); vertices.Add(immediateVertices[i + 2]);
                    vertices.Add(immediateVertices[i]); vertices.Add(immediateVertices[i + 2]); vertices.Add(immediateVertices[i + 3]);
                }
            }
            else if (immediatePrimitive == RenderDevice.Primitive.QUAD_STRIP)
            {
                vertices = new List<ImmediateVertex>();
                for (int i = 0; i + 3 < immediateVertices.Count; i += 2)
                {
                    vertices.Add(immediateVertices[i]); vertices.Add(immediateVertices[i + 1]); vertices.Add(immediateVertices[i + 2]);
                    vertices.Add(immediateVertices[i + 2]); vertices.Add(immediateVertices[i + 1]); vertices.Add(immediateVertices[i + 3]);
                }
            }
            else if (immediatePrimitive == RenderDevice.Primitive.TRIANGLE_FAN)
            {
                vertices = new List<ImmediateVertex>();
                for (int i = 1; i + 1 < immediateVertices.Count; ++i)
                {
                    vertices.Add(immediateVertices[0]); vertices.Add(immediateVertices[i]); vertices.Add(immediateVertices[i + 1]);
                }
            }
            drawVertexList(vertices, immediatePrimitive);
        }

        private void drawVertexList(List<ImmediateVertex> vertices, RenderDevice.Primitive primitive)
        {
            if (vertices.Count == 0) return;
            int stride = 64; int bytes = vertices.Count * stride;
            if (bytes > immediateVertexBuffer.Description.SizeInBytes) { immediateVertexBuffer.Dispose(); immediateVertexBuffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription { SizeInBytes = Math.Max(bytes, 1024), Usage = ResourceUsage.Dynamic, BindFlags = BindFlags.VertexBuffer, CpuAccessFlags = CpuAccessFlags.Write }); }
            byte[] data = new byte[bytes]; int at = 0;
            foreach (ImmediateVertex v in vertices)
            {
                float[] values = new float[] { v.X, v.Y, v.Z, v.Nx, v.Ny, v.Nz, 0.0f, 0.0f, v.R, v.G, v.B, v.A, v.S, v.T, v.U, v.V };
                foreach (float value in values) { byte[] bytesValue = BitConverter.GetBytes(value); Array.Copy(bytesValue, 0, data, at, 4); at += 4; }
            }
            DataBox box = context.MapSubresource(immediateVertexBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None); Marshal.Copy(data, 0, box.DataPointer, bytes); context.UnmapSubresource(immediateVertexBuffer, 0);
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(immediateVertexBuffer, stride, 0));
            UpdateConstants();
            context.InputAssembler.PrimitiveTopology = TopologyFor(primitive);
            context.Draw(vertices.Count, 0);
        }

        public void DrawVAR(RenderDevice.Primitive primitive, int numIndices, VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, VAR indexArray)
        {
            if (vertexArray == null || numIndices <= 0) return;

            Direct3D11Program program = null;
            bool useProgramPath =
                currentProgramID != 0 &&
                programs.TryGetValue(currentProgramID, out program) &&
                program != null &&
                program.Layout != null;

            if (useProgramPath)
            {
                drawVarWithProgram(primitive, numIndices, program, vertexArray, normalArray, colorArray, texCoordArray, indexArray);
                return;
            }

            // Fixed-function / no-program path: CPU-expand into the immediate vertex buffer.
            drawVarFixedFunction(primitive, numIndices, vertexArray, normalArray, colorArray, texCoordArray, indexArray);
        }

        private VAR varForAttributeSlot(int slot, VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray)
        {
            switch (slot)
            {
                case 0: return vertexArray;
                case 1: return normalArray;
                case 2: return colorArray;
                default: return texCoordArray;
            }
        }

        private void drawVarWithProgram(RenderDevice.Primitive primitive, int numIndices, Direct3D11Program program,
            VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, VAR indexArray)
        {
            List<int> drawIndices = buildDrawIndices(numIndices, vertexArray, indexArray);

            foreach (Direct3D11VertexAttribute attr in program.Attributes)
            {
                VAR array = varForAttributeSlot(attr.Slot, vertexArray, normalArray, colorArray, texCoordArray);
                if (array == null) continue;
                byte[] data = array.getData();
                if (data == null || data.Length == 0) continue;
                ensureVertexBuffer(ref attrBuffers[attr.Slot], data.Length);
                uploadVertexBuffer(attrBuffers[attr.Slot], data);
                context.InputAssembler.SetVertexBuffers(attr.Slot, new VertexBufferBinding(attrBuffers[attr.Slot], array.getStride(), 0));
            }

            context.InputAssembler.PrimitiveTopology = TopologyFor(primitive);
            if (indexArray != null && drawIndices.Count > 0)
            {
                byte[] indexBytes = new byte[drawIndices.Count * 4];
                for (int i = 0; i < drawIndices.Count; ++i)
                {
                    byte[] component = BitConverter.GetBytes(drawIndices[i]);
                    Array.Copy(component, 0, indexBytes, 4 * i, 4);
                }
                ensureIndexBuffer(ref attrIndexBuffer, indexBytes.Length);
                uploadIndexBuffer(attrIndexBuffer, indexBytes);
                context.InputAssembler.SetIndexBuffer(attrIndexBuffer, DxgiFormat.R32_UInt, 0);
                context.DrawIndexed(drawIndices.Count, 0, 0);
            }
            else
            {
                context.Draw(drawIndices.Count, 0);
            }
        }

        private List<int> buildDrawIndices(int numIndices, VAR vertexArray, VAR indexArray)
        {
            List<int> drawIndices = new List<int>();
            if (indexArray != null && indexArray.getData() != null)
            {
                byte[] indexData = indexArray.getData();
                int count = Math.Min(numIndices, indexArray.getCount());
                for (int i = 0; i < count; ++i)
                {
                    drawIndices.Add(BitConverter.ToInt32(indexData, i * 4));
                }
            }
            else
            {
                int count = Math.Min(numIndices, vertexArray.getCount());
                for (int i = 0; i < count; ++i)
                {
                    drawIndices.Add(i);
                }
            }
            return drawIndices;
        }

        private void ensureVertexBuffer(ref SharpDX.Direct3D11.Buffer buffer, int neededBytes)
        {
            if (buffer == null || buffer.Description.SizeInBytes < neededBytes)
            {
                if (buffer != null) buffer.Dispose();
                buffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
                {
                    SizeInBytes = Math.Max(neededBytes, 256),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write
                });
            }
        }

        private void uploadVertexBuffer(SharpDX.Direct3D11.Buffer buffer, byte[] data)
        {
            DataBox box = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
            Marshal.Copy(data, 0, box.DataPointer, data.Length);
            context.UnmapSubresource(buffer, 0);
        }

        private void ensureIndexBuffer(ref SharpDX.Direct3D11.Buffer buffer, int neededBytes)
        {
            if (buffer == null || buffer.Description.SizeInBytes < neededBytes)
            {
                if (buffer != null) buffer.Dispose();
                buffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
                {
                    SizeInBytes = Math.Max(neededBytes, 256),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.IndexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write
                });
                attrIndexBufferSize = neededBytes;
            }
        }

        private void uploadIndexBuffer(SharpDX.Direct3D11.Buffer buffer, byte[] data)
        {
            DataBox box = context.MapSubresource(buffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
            Marshal.Copy(data, 0, box.DataPointer, data.Length);
            context.UnmapSubresource(buffer, 0);
        }

        private void drawVarFixedFunction(RenderDevice.Primitive primitive, int numIndices, VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, VAR indexArray)
        {
            // Color and texture-coordinate arrays are not part of the fixed-function
            // immediate vertex layout; they only take effect when a shader program
            // binds them through drawVarWithProgram.
            byte[] vertexData = vertexArray.getData();
            byte[] normalData = (normalArray != null) ? normalArray.getData() : null;
            if (vertexData == null) return;
            int vertexStride = vertexArray.getStride();
            int normalStride = (normalArray != null) ? normalArray.getStride() : 0;

            List<int> drawIndices = buildDrawIndices(numIndices, vertexArray, indexArray);

            List<ImmediateVertex> vertices = new List<ImmediateVertex>(drawIndices.Count);
            foreach (int index in drawIndices)
            {
                ImmediateVertex v = new ImmediateVertex();
                int at = index * vertexStride;
                v.X = BitConverter.ToSingle(vertexData, at);
                v.Y = BitConverter.ToSingle(vertexData, at + 4);
                v.Z = BitConverter.ToSingle(vertexData, at + 8);
                if (normalData != null)
                {
                    int nat = index * normalStride;
                    v.Nx = BitConverter.ToSingle(normalData, nat);
                    v.Ny = BitConverter.ToSingle(normalData, nat + 4);
                    v.Nz = BitConverter.ToSingle(normalData, nat + 8);
                }
                v.R = currentColor.r; v.G = currentColor.g; v.B = currentColor.b; v.A = currentColor.a;
                v.S = currentTexCoord.x; v.T = currentTexCoord.y; v.U = currentTexCoord.z; v.V = currentTexCoord.w;
                vertices.Add(v);
            }

            drawVertexList(vertices, primitive);
        }

        private void UpdateConstants()
        {
            Matrix4 mvp = ClipSpace.Multiply(ClipSpace.ToDirect3D(projection), modelView);
            Matrix4 light = ClipSpace.ToDirect3D(lightMvp);
            float[] data = new float[80]; int at = 0;
            for (int r = 0; r < 4; ++r) for (int c = 0; c < 4; ++c) data[at++] = mvp.elt(r, c);
            for (int r = 0; r < 4; ++r) for (int c = 0; c < 4; ++c) data[at++] = light.elt(r, c);
            data[at++] = lights[0].position.x; data[at++] = lights[0].position.y; data[at++] = lights[0].position.z; data[at++] = 0;
            Color3 lc = lights[0].color; data[at++] = lc.r; data[at++] = lc.g; data[at++] = lc.b; data[at++] = 1;
            data[at++] = ambient.r; data[at++] = ambient.g; data[at++] = ambient.b; data[at++] = ambient.a;
            data[at++] = 1; data[at++] = 1; data[at++] = 1; data[at++] = shininess;
            data[at++] = fixedTextureUnit >= 0 ? 1.0f : 0.0f; data[at++] = 0.0f; data[at++] = 0.0f; data[at++] = 0.0f;
            for (int r = 0; r < 4; ++r) for (int c = 0; c < 4; ++c) data[at++] = textureMatrix.elt(r, c);
            context.UpdateSubresource(data, constantsBuffer, 0, 0, 0, null);
        }
        private PrimitiveTopology TopologyFor(RenderDevice.Primitive p)
        {
            switch (p) { case RenderDevice.Primitive.LINES: return PrimitiveTopology.LineList; case RenderDevice.Primitive.LINE_STRIP: return PrimitiveTopology.LineStrip; case RenderDevice.Primitive.TRIANGLES: return PrimitiveTopology.TriangleList; case RenderDevice.Primitive.TRIANGLE_STRIP: return PrimitiveTopology.TriangleStrip; case RenderDevice.Primitive.TRIANGLE_FAN: return PrimitiveTopology.TriangleList; case RenderDevice.Primitive.QUADS: return PrimitiveTopology.TriangleList; case RenderDevice.Primitive.QUAD_STRIP: return PrimitiveTopology.TriangleStrip; default: return PrimitiveTopology.PointList; }
        }
        public void DrawArrays(RenderDevice.Primitive primitive, int first, int count) { context.InputAssembler.PrimitiveTopology = TopologyFor(primitive); context.Draw(count, first); }
        public void DrawElements(RenderDevice.Primitive primitive, int count, int type, int offset) { }
        public void DrawElementsInstanced(RenderDevice.Primitive primitive, int count, int type, int offset, int instanceCount) { }

        public void SetDepthTest(RenderDevice.DepthTest test)
        {
            if (test != RenderDevice.DepthTest.DEPTH_CURRENT) depthTest = test;
            if (depthState != null) depthState.Dispose();
            DepthStencilStateDescription d = DepthStencilStateDescription.Default();
            d.IsDepthEnabled = true;
            d.DepthComparison = DepthComparison(depthTest);
            d.DepthWriteMask = depthWrite ? DepthWriteMask.All : DepthWriteMask.Zero;
            depthState = new DepthStencilState(device, d);
            context.OutputMerger.SetDepthStencilState(depthState, 0);
        }
        private Comparison DepthComparison(RenderDevice.DepthTest test)
        {
            switch (test) { case RenderDevice.DepthTest.DEPTH_GREATER: return Comparison.Greater; case RenderDevice.DepthTest.DEPTH_LESS: return Comparison.Less; case RenderDevice.DepthTest.DEPTH_GEQUAL: return Comparison.GreaterEqual; case RenderDevice.DepthTest.DEPTH_LEQUAL: return Comparison.LessEqual; case RenderDevice.DepthTest.DEPTH_NOTEQUAL: return Comparison.NotEqual; case RenderDevice.DepthTest.DEPTH_EQUAL: return Comparison.Equal; case RenderDevice.DepthTest.DEPTH_ALWAYS_PASS: return Comparison.Always; default: return Comparison.Never; }
        }
        public void SetStencilTest(RenderDevice.StencilTest test, int reference)
        {
            if (depthState != null) depthState.Dispose();
            DepthStencilStateDescription d = DepthStencilStateDescription.Default();
            d.IsDepthEnabled = true;
            d.DepthComparison = DepthComparison(depthTest);
            d.DepthWriteMask = depthWrite ? DepthWriteMask.All : DepthWriteMask.Zero;
            d.IsStencilEnabled = test != RenderDevice.StencilTest.STENCIL_CURRENT;
            d.StencilReadMask = 0xFF; d.StencilWriteMask = 0xFF;
            d.FrontFace.Comparison = StencilComparison(test); d.BackFace.Comparison = d.FrontFace.Comparison;
            depthState = new DepthStencilState(device, d); context.OutputMerger.SetDepthStencilState(depthState, reference);
        }
        private Comparison StencilComparison(RenderDevice.StencilTest test)
        {
            switch (test) { case RenderDevice.StencilTest.STENCIL_GREATER: return Comparison.Greater; case RenderDevice.StencilTest.STENCIL_LESS: return Comparison.Less; case RenderDevice.StencilTest.STENCIL_GEQUAL: return Comparison.GreaterEqual; case RenderDevice.StencilTest.STENCIL_LEQUAL: return Comparison.LessEqual; case RenderDevice.StencilTest.STENCIL_NOTEQUAL: return Comparison.NotEqual; case RenderDevice.StencilTest.STENCIL_EQUAL: return Comparison.Equal; case RenderDevice.StencilTest.STENCIL_ALWAYS_PASS: return Comparison.Always; default: return Comparison.Never; }
        }
        public void SetRenderMode(RenderDevice.RenderMode mode)
        {
            if (rasterizerState != null) rasterizerState.Dispose();
            RasterizerStateDescription d = RasterizerStateDescription.Default();
            d.IsFrontCounterClockwise = true;
            d.FillMode = mode == RenderDevice.RenderMode.RENDER_WIREFRAME ? FillMode.Wireframe : FillMode.Solid;
            rasterizerState = new RasterizerState(device, d); context.Rasterizer.State = rasterizerState;
        }
        public void SetCullFace(RenderDevice.CullFace face)
        {
            if (rasterizerState != null) rasterizerState.Dispose();
            RasterizerStateDescription d = RasterizerStateDescription.Default(); d.IsFrontCounterClockwise = true;
            d.CullMode = face == RenderDevice.CullFace.CULL_FRONT ? CullMode.Front : face == RenderDevice.CullFace.CULL_BACK ? CullMode.Back : CullMode.None;
            rasterizerState = new RasterizerState(device, d); context.Rasterizer.State = rasterizerState;
        }
        public void SetBlendFunc(RenderDevice.BlendFunc src, RenderDevice.BlendFunc dst, RenderDevice.BlendEq eq)
        {
            if (src != RenderDevice.BlendFunc.BLEND_CURRENT) blendSrc = src;
            if (dst != RenderDevice.BlendFunc.BLEND_CURRENT) blendDst = dst;
            if (eq != RenderDevice.BlendEq.BLENDEQ_CURRENT) blendEquation = eq;
            UpdateBlendState(blendSrc, blendDst, blendEquation);
        }
        private void UpdateBlendState(RenderDevice.BlendFunc src, RenderDevice.BlendFunc dst, RenderDevice.BlendEq eq)
        {
            if (blendState != null) blendState.Dispose();
            BlendStateDescription d = BlendStateDescription.Default();
            RenderTargetBlendDescription target = new RenderTargetBlendDescription();
            target.IsBlendEnabled = src != RenderDevice.BlendFunc.BLEND_ONE || dst != RenderDevice.BlendFunc.BLEND_ZERO || eq != RenderDevice.BlendEq.BLENDEQ_ADD;
            target.SourceBlend = BlendOptionFor(src); target.DestinationBlend = BlendOptionFor(dst);
            target.BlendOperation = BlendOperationFor(eq); target.SourceAlphaBlend = target.SourceBlend; target.DestinationAlphaBlend = target.DestinationBlend; target.AlphaBlendOperation = target.BlendOperation;
            target.RenderTargetWriteMask = colorWrite ? (alphaWrite ? ColorWriteMaskFlags.All : ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue) : (ColorWriteMaskFlags)0;
            d.RenderTarget[0] = target;
            blendState = new BlendState(device, d); context.OutputMerger.SetBlendState(blendState, null, 0xFFFFFFFF);
        }
        private BlendOption BlendOptionFor(RenderDevice.BlendFunc f)
        {
            switch (f) { case RenderDevice.BlendFunc.BLEND_ZERO: return BlendOption.Zero; case RenderDevice.BlendFunc.BLEND_ONE: return BlendOption.One; case RenderDevice.BlendFunc.BLEND_SRC_ALPHA: return BlendOption.SourceAlpha; case RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA: return BlendOption.InverseSourceAlpha; case RenderDevice.BlendFunc.BLEND_SRC_COLOR: return BlendOption.SourceColor; case RenderDevice.BlendFunc.BLEND_DST_COLOR: return BlendOption.DestinationColor; case RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_COLOR: return BlendOption.InverseSourceColor; default: return BlendOption.One; }
        }
        private BlendOperation BlendOperationFor(RenderDevice.BlendEq e)
        {
            switch (e) { case RenderDevice.BlendEq.BLENDEQ_MIN: return BlendOperation.Minimum; case RenderDevice.BlendEq.BLENDEQ_MAX: return BlendOperation.Maximum; case RenderDevice.BlendEq.BLENDEQ_SUBTRACT: return BlendOperation.Subtract; case RenderDevice.BlendEq.BLENDEQ_REVERSE_SUBTRACT: return BlendOperation.ReverseSubtract; default: return BlendOperation.Add; }
        }
        public void SetAlphaTest(RenderDevice.AlphaTest test, double reference)
        {
            if (test == RenderDevice.AlphaTest.ALPHA_CURRENT) return;
            throw new NotSupportedException("G3D.Direct3D11: the fixed-function alpha test requires the alpha-test shader permutation, which the Direct3D 11 backend does not implement.");
        }
        public void SetTextureCombineMode(int textureUnit, RenderDevice.CombineMode mode)
        {
            if (mode == RenderDevice.CombineMode.TEX_CURRENT) return;
            if (mode != RenderDevice.CombineMode.TEX_MODULATE)
            {
                throw new NotSupportedException("G3D.Direct3D11: the fixed-function texture environment supports TEX_MODULATE only.");
            }
        }
        public void SetVertexAttribArray(int attribNum, VAR v, bool normalize)
        {
            throw new NotSupportedException("G3D.Direct3D11: generic vertex attributes are not supported by the Direct3D 11 backend; use the POSITION, NORMAL, COLOR and TEXCOORD semantics.");
        }
        public void SetDrawBuffer(RenderDevice.Buffer drawBuffer)
        {
            if (drawBuffer == RenderDevice.Buffer.BUFFER_CURRENT) return;
            throw new NotSupportedException("G3D.Direct3D11: stereo and multiple render target draw buffers are not supported by the Direct3D 11 backend.");
        }
        public void SetColorWrite(bool enable) { colorWrite = enable; UpdateBlendState(blendSrc, blendDst, blendEquation); }
        public void SetAlphaWrite(bool enable) { alphaWrite = enable; UpdateBlendState(blendSrc, blendDst, blendEquation); }
        public void SetDepthWrite(bool enable) { depthWrite = enable; SetDepthTest(RenderDevice.DepthTest.DEPTH_CURRENT); }
        public void SetShadeMode(RenderDevice.ShadeMode mode) { }
        public void SetStencilOp(RenderDevice.StencilOp fail, RenderDevice.StencilOp zfail, RenderDevice.StencilOp zpass, RenderDevice.StencilOp backFail, RenderDevice.StencilOp backZfail, RenderDevice.StencilOp backZpass) { }
        public void SetPolygonOffset(double offset) { }
        public void SetLineWidth(double width) { }
        public void SetPointSize(double size) { }
        public void SetScissor(Rect2D rect, bool enable) { }
        public void SetViewport(Rect2D viewport) { viewportHeight = Math.Max(1.0f, viewport.height()); float y = Math.Max(0.0f, targetHeight - viewport.y - viewport.height()); context.Rasterizer.SetViewport(viewport.x, y, viewport.width(), viewport.height(), 0.0f, 1.0f); }
        public void SetDepthRange(double near, double far) { }
        public void SetColorClearValue(Color4 color) { clearValue = color; }
        public void SetDepthClearValue(double depth) { depthClearValue = depth; }
        public void SetStencilClearValue(int stencil) { stencilClearValue = stencil; }
        public void SetTextureMatrix(int textureUnit, Matrix4 matrix) { textureMatrix = matrix; }
        public void SetProjectionMatrix(Matrix4 matrix) { projection = matrix; }
        public void SetModelViewMatrix(Matrix4 matrix) { modelView = matrix; }
        public void SetLight(int index, GLight light, bool enabled) { if (index >= 0 && index < lights.Length) { lights[index] = new GLight(light); lightEnabled[index] = enabled; } }
        public void SetAmbientLight(Color4 color) { ambient = color; }
        public void SetLightingEnabled(bool enabled) { lightingEnabled = enabled; }
        public void SetTwoSidedLighting(bool enabled) { twoSidedLighting = enabled; }
        public void SetSpecular(Color3 color) { specular = color; }
        public void SetShininess(float value) { shininess = value; }
        public int CreateQuery() { return 0; }
        public void DeleteQuery(int queryID) { }
        public void BeginQuery(int queryID) { }
        public void EndQuery(int queryID) { }
        public bool GetQueryResult(int queryID, out uint result) { result = 0; return true; }

        public int CreateMilestone()
        {
            int id = nextMilestoneId++;
            Direct3D11Milestone milestone = new Direct3D11Milestone();
            milestone.Id = id;
            milestone.Source = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
            {
                SizeInBytes = 4,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.None
            });
            milestone.Readback = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
            {
                SizeInBytes = 4,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read
            });
            milestones.Add(id, milestone);
            return id;
        }

        public void DeleteMilestone(int milestoneID)
        {
            Direct3D11Milestone milestone;
            if (milestones.TryGetValue(milestoneID, out milestone))
            {
                milestone.Dispose();
                milestones.Remove(milestoneID);
            }
        }

        public void SetMilestone(int milestoneID)
        {
            Direct3D11Milestone milestone;
            if (!milestones.TryGetValue(milestoneID, out milestone))
            {
                throw new ArgumentOutOfRangeException("milestoneID");
            }
            context.CopyResource(milestone.Readback, milestone.Source);
        }

        public void WaitForMilestone(int milestoneID)
        {
            Direct3D11Milestone milestone;
            if (!milestones.TryGetValue(milestoneID, out milestone))
            {
                throw new ArgumentOutOfRangeException("milestoneID");
            }
            DataStream stream;
            DataBox box = context.MapSubresource(milestone.Readback, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out stream);
            context.UnmapSubresource(milestone.Readback, 0);
        }
        public bool SupportsExtension(string extension) { return false; }
        public bool SupportsTextureFormat(TextureFormat format)
        {
            if (format == null || format == TextureFormat.AUTO)
            {
                return false;
            }
            try
            {
                TextureFormatMap.ToDXGI(format);
                return true;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }
        public bool SupportsTwoSidedStencil() { return caps.SupportsTwoSidedStencil; }
        public bool SupportsTextureRectangle() { return caps.SupportsTextureRectangle; }
        public bool SupportsVertexProgram() { return caps.SupportsVertexProgram; }
        public bool SupportsVertexProgramNV2() { return caps.SupportsVertexProgramNV2; }
        public bool SupportsPixelProgram() { return caps.SupportsPixelProgram; }
        public bool SupportsVertexBufferObject() { return caps.SupportsVertexBufferObject; }
        public bool HasBug_glMultiTexCoord3fvARB() { return caps.HasBug_glMultiTexCoord3fvARB; }
        public bool HasBug_normalMapTexGen() { return caps.HasBug_normalMapTexGen; }
        public bool HasBug_redBlueMipmapSwap() { return caps.HasBug_redBlueMipmapSwap; }
        public bool HasBug_mipmapGeneration() { return caps.HasBug_mipmapGeneration; }
        public bool HasBug_slowVBO() { return caps.HasBug_slowVBO; }
        public string GetRenderer() { return caps.Renderer; }
        public string GetVendor() { return caps.Vendor; }
        public string GetDriverVersion() { return caps.DriverVersion; }
        public string GetGLVersion() { return caps.Version; }
        public int GetMaxTextures() { return caps.MaxTextures; }
        public int GetMaxTextureCoords() { return caps.MaxTextureCoords; }
        public int GetMaxTextureUnits() { return caps.MaxTextureUnits; }
        public int GetMaxTextureSize() { return caps.MaxTextureSize; }
        public int GetMaxColorAttachments() { return caps.MaxColorAttachments; }
        public void ReadPixels(int x, int y, int width, int height, byte[] data)
        {
            if (activeColorView == null)
            {
                throw new InvalidOperationException("There is no active render target to read from.");
            }
            Texture2D source = activeColorView.Resource as Texture2D;
            if (source == null)
            {
                throw new NotSupportedException("G3D.Direct3D11: the active render target cannot be copied to the CPU.");
            }
            if (data == null || width <= 0 || height <= 0) return;
            Texture2D staging = new Texture2D(device, new Texture2DDescription
            {
                Width = source.Description.Width,
                Height = source.Description.Height,
                MipLevels = source.Description.MipLevels,
                ArraySize = 1,
                Format = source.Description.Format,
                SampleDescription = source.Description.SampleDescription,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read
            });
            try
            {
                context.CopyResource(staging, source);
                DataBox box = context.MapSubresource(staging, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                try
                {
                    for (int row = 0; row < height; ++row)
                    {
                        int sourceRow = source.Description.Height - 1 - (y + row);
                        IntPtr rowPointer = new IntPtr(box.DataPointer.ToInt64() + sourceRow * box.RowPitch + x * 4);
                        Marshal.Copy(rowPointer, data, row * width * 4, width * 4);
                    }
                }
                finally
                {
                    context.UnmapSubresource(staging, 0);
                }
            }
            finally
            {
                staging.Dispose();
            }
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
            if (activeDepthView == null)
            {
                throw new InvalidOperationException("There is no active depth buffer to read from.");
            }

            Texture2D source = activeDepthResource;
            if (source == null)
            {
                throw new NotSupportedException("G3D.Direct3D11: the active depth buffer cannot be copied to the CPU.");
            }

            DxgiFormat format = source.Description.Format;
            int bytesPerPixel;
            int depthMode;
            switch (format)
            {
                case DxgiFormat.D32_Float:
                case DxgiFormat.R32_Float:
                    bytesPerPixel = 4;
                    depthMode = 0;
                    break;
                case DxgiFormat.D24_UNorm_S8_UInt:
                    bytesPerPixel = 4;
                    depthMode = 1;
                    break;
                case DxgiFormat.D16_UNorm:
                    bytesPerPixel = 2;
                    depthMode = 2;
                    break;
                default:
                    throw new NotSupportedException(
                        "G3D.Direct3D11: reading a depth buffer with format " + format + " is not supported.");
            }

            Texture2D staging = new Texture2D(device, new Texture2DDescription
            {
                Width = source.Description.Width,
                Height = source.Description.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = format,
                SampleDescription = source.Description.SampleDescription,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read
            });
            try
            {
                context.CopyResource(staging, source);
                DataBox box = context.MapSubresource(staging, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                try
                {
                    byte[] rowBytes = new byte[width * bytesPerPixel];
                    for (int row = 0; row < height; ++row)
                    {
                        int sourceRow = source.Description.Height - 1 - (y + row);
                        IntPtr rowPointer = new IntPtr(box.DataPointer.ToInt64() + sourceRow * box.RowPitch + x * bytesPerPixel);
                        Marshal.Copy(rowPointer, rowBytes, 0, rowBytes.Length);

                        for (int i = 0; i < width; ++i)
                        {
                            float value;
                            if (depthMode == 0)
                            {
                                value = BitConverter.ToSingle(rowBytes, i * 4);
                            }
                            else if (depthMode == 1)
                            {
                                int raw = rowBytes[i * 4] | (rowBytes[i * 4 + 1] << 8) | (rowBytes[i * 4 + 2] << 16);
                                value = raw / 16777215.0f;
                            }
                            else
                            {
                                int raw = rowBytes[i * 2] | (rowBytes[i * 2 + 1] << 8);
                                value = raw / 65535.0f;
                            }

                            data[row * width + i] = value;
                        }
                    }
                }
                finally
                {
                    context.UnmapSubresource(staging, 0);
                }
            }
            finally
            {
                staging.Dispose();
            }
        }
    }
}
