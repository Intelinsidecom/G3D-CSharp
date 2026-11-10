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

        private sealed class VarDrawState
        {
            public VAR VertexArray;
            public VAR NormalArray;
            public VAR ColorArray;
            public VAR TexCoordArray;
            public VAR IndexArray;
            public int VertexVersion = -1;
            public int NormalVersion = -1;
            public int ColorVersion = -1;
            public int TexCoordVersion = -1;
            public int IndexVersion = -1;
            public SharpDX.Direct3D11.Buffer VertexBuffer;
            public SharpDX.Direct3D11.Buffer IndexBuffer;
            public int NumVertices;
            public int NumIndices;
            public bool Indexed;
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
        private readonly Dictionary<VAR, VarDrawState> varDrawStates = new Dictionary<VAR, VarDrawState>();

        private VAR clientIndexVAR;
        private int clientIndexVARCapacity;
        private readonly Dictionary<int, Direct3D11Milestone> milestones = new Dictionary<int, Direct3D11Milestone>();
        private int nextTextureId = 1;
        private int nextFramebufferId = 1;
        private int nextBufferId = 1;
        private int nextRenderbufferId = 1;
        private int nextProgramId = 1;
        private int nextMilestoneId = 1;
        private int currentProgramID;
        private int currentFramebuffer;
        private int drawBufferIndex;
        private VAR[] vertexAttribArrays = new VAR[16];
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
        private Matrix4 cameraToWorld = Matrix4.identity();
        // The inverse of the camera-to-world matrix changes only when the camera
        // changes, so it is computed once per camera update instead of once per
        // draw (a 4x4 adjugate inverse dominates the draw submission cost).
        private Matrix4 cameraToWorldInverseCached = Matrix4.identity();
        private bool cameraToWorldInverseValid = true;
        private Matrix4 lightMvp = Matrix4.identity();
        private Matrix4[] textureMatrices = new Matrix4[G3D.GLCaps.G3D_MAX_TEXTURE_UNITS];
        private Color4 ambient = new Color4(0.1f, 0.1f, 0.1f, 1.0f);
        private Color3 specular = Color3.white();
        private float shininess = 32.0f;
        private bool lightingEnabled = true;
        private bool twoSidedLighting;
        private RenderDevice.AlphaTest alphaTest = RenderDevice.AlphaTest.ALPHA_CURRENT;
        private double alphaTestReference;
        private RenderDevice.CombineMode combineMode = RenderDevice.CombineMode.TEX_CURRENT;
        private GLight[] lights = new GLight[8];
        private bool[] lightEnabled = new bool[8];
        private readonly Dictionary<int, int> boundTextures = new Dictionary<int, int>();
        private readonly Dictionary<int, List<byte[]>> textureLevelData = new Dictionary<int, List<byte[]>>();
        private readonly Dictionary<int, TextureSamplerParameters> textureSamplerParameters = new Dictionary<int, TextureSamplerParameters>();
        private readonly float[] textureLodBias = new float[G3D.GLCaps.G3D_MAX_TEXTURE_UNITS];
        private CubeMapTexGenMode[] cubeTexGenModes = new CubeMapTexGenMode[G3D.GLCaps.G3D_MAX_TEXTURE_UNITS];
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
        // Rotating pools of dynamic buffers.  Mapping a dynamic buffer whose
        // memory a previous frame is still reading costs about 0.1ms on some
        // drivers, so the fixed-function constant and immediate-mode vertex
        // buffers rotate through a small pool (four in-flight draws).
        private readonly SharpDX.Direct3D11.Buffer[] constantsBuffers = new SharpDX.Direct3D11.Buffer[4];
        private readonly SharpDX.Direct3D11.Buffer[] immediateVertexBuffers = new SharpDX.Direct3D11.Buffer[4];
        private int fixedFunctionBufferIndex;
        private const int FixedFunctionBufferCount = 4;
        private void advanceFixedFunctionBuffers() { fixedFunctionBufferIndex = (fixedFunctionBufferIndex + 1) % FixedFunctionBufferCount; }
        private SharpDX.Direct3D11.Buffer constantsBuffer
        {
            get { return constantsBuffers[fixedFunctionBufferIndex]; }
            set { constantsBuffers[fixedFunctionBufferIndex] = value; }
        }
        private SharpDX.Direct3D11.Buffer constantsReadback;
        private SharpDX.Direct3D11.Buffer immediateVertexBuffer
        {
            get { return immediateVertexBuffers[fixedFunctionBufferIndex]; }
            set { immediateVertexBuffers[fixedFunctionBufferIndex] = value; }
        }
        // Scratch buffers reused across draws so the draw path allocates nothing
        // per call.
        private byte[] immediatePacked;
        private readonly float[] valueScratch = new float[16];
        private readonly float[] constantsPacked = new float[244];
        private SharpDX.Direct3D11.Buffer[] attrBuffers = new SharpDX.Direct3D11.Buffer[4];
        private SharpDX.Direct3D11.Buffer attrIndexBuffer;
        private int attrIndexBufferSize;
        private int fixedTextureUnit = -1;
        private int shadowTextureUnit = -1;
        private bool shadowEnabled;
        private DepthStencilState depthState;
        private BlendState blendState;
        private RasterizerState rasterizerState;
        private RenderDevice.RenderMode renderModeState = RenderDevice.RenderMode.RENDER_SOLID;
        private RenderDevice.CullFace cullFaceState = RenderDevice.CullFace.CULL_BACK;
        private double polygonOffset;
        private bool colorWrite = true;
        private bool alphaWrite = true;
        private bool depthWrite = true;
        private RenderDevice.DepthTest depthTest = RenderDevice.DepthTest.DEPTH_LESS;
        private float viewportHeight = 1.0f;
        private RenderDevice.BlendFunc blendSrc = RenderDevice.BlendFunc.BLEND_ONE;
        private RenderDevice.BlendFunc blendDst = RenderDevice.BlendFunc.BLEND_ZERO;
        private RenderDevice.BlendEq blendEquation = RenderDevice.BlendEq.BLENDEQ_ADD;

        public Direct3D11Backend()
        {
            caps = new BackendCaps();
            for (int i = 0; i < lights.Length; ++i) lights[i] = new GLight();
            for (int i = 0; i < textureMatrices.Length; ++i) textureMatrices[i] = Matrix4.identity();
        }

        public GraphicsBackendKind Kind { get { return GraphicsBackendKind.Direct3D11; } }
        public string Description { get { return string.Format("Direct3D 11 {0}", caps.Version); } }
        public BackendCaps Caps { get { return caps; } }

        public static bool EnableDebugLayer { get; set; }

        public bool DebugLayerActive { get; private set; }

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
            DeviceCreationFlags creationFlags = DeviceCreationFlags.BgraSupport;
            if (EnableDebugLayer)
            {
                creationFlags |= DeviceCreationFlags.Debug;
            }
            try
            {
                Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, creationFlags, description, out device, out swapChain);
                DebugLayerActive = (creationFlags & DeviceCreationFlags.Debug) == DeviceCreationFlags.Debug;
            }
            catch (SharpDXException)
            {
                if ((creationFlags & DeviceCreationFlags.Debug) == 0)
                {
                    throw;
                }
                DebugLayerActive = false;
                Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport, description, out device, out swapChain);
            }
            context = device.ImmediateContext;
            CreateRenderTarget();
            CreateFixedFunctionPipeline();
            caps.MaxTextureSize = 16384;
            caps.MaxTextureUnits = G3D.GLCaps.G3D_MAX_TEXTURE_UNITS;
            caps.MaxTextures = caps.MaxTextureUnits;
            caps.MaxTextureCoords = caps.MaxTextureUnits;
            caps.MaxLights = 1;
            caps.MaxColorAttachments = 1;
            caps.SupportsShadows = true;
            caps.SupportsCubeMaps = true;
            caps.SupportsStencil = true;
            caps.SupportsTwoSidedStencil = true;
            caps.SupportsVertexProgram = true;
            caps.SupportsVertexProgramNV2 = false;
            caps.SupportsPixelProgram = true;
            caps.SupportsVertexBufferObject = true;
            caps.SupportsTextureRectangle = false;
            caps.SupportsAlphaTest = true;
            caps.SupportsTextureCombineModes = true;
            caps.SupportsTextureLODBias = true;
            caps.SupportsGenericVertexAttributes = false;
            caps.SupportsStereoDrawBuffers = false;
            caps.SupportsReflectionMaps = true;
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

        public string[] DrainDeviceMessages()
        {
            if (device == null)
            {
                return new string[0];
            }
            SharpDX.Direct3D11.InfoQueue infoQueue = device.QueryInterfaceOrNull<SharpDX.Direct3D11.InfoQueue>();
            if (infoQueue == null)
            {
                return new string[0];
            }
            try
            {
                List<string> messages = new List<string>();
                long count = infoQueue.NumStoredMessages;
                for (long i = 0; i < count; ++i)
                {
                    SharpDX.Direct3D11.Message message = infoQueue.GetMessage(i);
                    messages.Add(string.Format("{0}: {1}", message.Severity, message.Description));
                }
                infoQueue.ClearStoredMessages();
                return messages.ToArray();
            }
            finally
            {
                infoQueue.Dispose();
            }
        }

        public byte[] ReadFixedFunctionConstants()
        {
            if (device == null || context == null || constantsBuffer == null)
            {
                return null;
            }
            int size = constantsBuffer.Description.SizeInBytes;
            if (constantsReadback == null || constantsReadback.Description.SizeInBytes != size)
            {
                if (constantsReadback != null) constantsReadback.Dispose();
                constantsReadback = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
                {
                    SizeInBytes = size,
                    Usage = ResourceUsage.Staging,
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read
                });
            }
            context.CopyResource(constantsBuffer, constantsReadback);
            DataBox box = context.MapSubresource(constantsReadback, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
            try
            {
                byte[] data = new byte[size];
                Marshal.Copy(box.DataPointer, data, 0, size);
                return data;
            }
            finally
            {
                context.UnmapSubresource(constantsReadback, 0);
            }
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
            TextureFormat.screenDepthBitsProvider = delegate { return 24; };
            TextureFormat.screenStencilBitsProvider = delegate { return 8; };
            using (Texture2D backBuffer = swapChain.GetBackBuffer<Texture2D>(0))
            {
                renderTargetView = new RenderTargetView(device, backBuffer);
                int width = backBuffer.Description.Width;
                int height = backBuffer.Description.Height;
                Direct3D11Texture depth = new Direct3D11Texture();
                depth.Width = width;
                depth.Height = height;
                depth.IsDepth = true;
                depth.Resource = new Texture2D(device, new Texture2DDescription {
                    Width = width, Height = height, MipLevels = 1,
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
            for (int b = 0; b < FixedFunctionBufferCount; ++b)
            {
                constantsBuffers[b] = new SharpDX.Direct3D11.Buffer(device, new BufferDescription { SizeInBytes = 976, Usage = ResourceUsage.Dynamic, BindFlags = BindFlags.ConstantBuffer, CpuAccessFlags = CpuAccessFlags.Write });
                immediateVertexBuffers[b] = new SharpDX.Direct3D11.Buffer(device, new BufferDescription { SizeInBytes = 1024 * 64, Usage = ResourceUsage.Dynamic, BindFlags = BindFlags.VertexBuffer, CpuAccessFlags = CpuAccessFlags.Write });
            }
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
            foreach (VarDrawState drawState in varDrawStates.Values)
            {
                if (drawState.VertexBuffer != null) drawState.VertexBuffer.Dispose();
                if (drawState.IndexBuffer != null) drawState.IndexBuffer.Dispose();
            }
            varDrawStates.Clear();
            textures.Clear(); framebuffers.Clear(); buffers.Clear(); programs.Clear();
            if (fixedInputLayout != null) fixedInputLayout.Dispose();
            if (fixedVertexShader != null) fixedVertexShader.Dispose();
            if (fixedPixelShader != null) fixedPixelShader.Dispose();
            for (int b = 0; b < FixedFunctionBufferCount; ++b)
            {
                if (constantsBuffers[b] != null) constantsBuffers[b].Dispose();
                if (immediateVertexBuffers[b] != null) immediateVertexBuffers[b].Dispose();
            }
            if (constantsReadback != null) constantsReadback.Dispose();
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
            DxgiFormat dx = t.IsDepth ? DxgiFormat.R32_Typeless : TextureFormatMap.ToDXGI(format);
            t.Resource = new Texture2D(device, new Texture2DDescription { Width = width, Height = height, MipLevels = t.IsDepth ? 1 : mipLevelCount(width, height), ArraySize = 1, Format = dx, SampleDescription = new SampleDescription(1, 0), Usage = ResourceUsage.Default, BindFlags = t.IsDepth ? (BindFlags.DepthStencil | BindFlags.ShaderResource) : (BindFlags.ShaderResource | BindFlags.RenderTarget) });
            if (t.IsDepth)
            {
                t.DepthView = new DepthStencilView(device, t.Resource, new DepthStencilViewDescription
                {
                    Format = DxgiFormat.D32_Float,
                    Dimension = DepthStencilViewDimension.Texture2D,
                    Texture2D = new DepthStencilViewDescription.Texture2DResource { MipSlice = 0 }
                });
                t.ComparisonView = new ShaderResourceView(device, t.Resource, new ShaderResourceViewDescription
                {
                    Format = DxgiFormat.R32_Float,
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D = new ShaderResourceViewDescription.Texture2DResource { MostDetailedMip = 0, MipLevels = 1 }
                });
                t.ComparisonSampler = CreateSampler(depthReadMode, true);
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
                if (textureUnit == 0) fixedTextureUnit = -1;
                return;
            }
            Direct3D11Texture t; if (!textures.TryGetValue(textureID, out t)) return;
            if (dimension == Texture.Dimension.DIM_CUBE_MAP || dimension == Texture.Dimension.DIM_CUBE_MAP_NPOT)
            {
                t.IsCube = true;
            }
            ShaderResourceView view = t.IsDepth ? t.ComparisonView : t.ShaderView;
            if (view == null)
            {
                throw new NotSupportedException(
                    "G3D.Direct3D11: the texture cannot be sampled because it has no shader resource view.");
            }
            context.VertexShader.SetShaderResource(textureUnit, view);
            context.PixelShader.SetShaderResource(textureUnit, view);
            if (t.IsDepth)
            {
                if (t.ComparisonSampler == null) t.ComparisonSampler = CreateSampler(t.DepthMode, true);
                context.VertexShader.SetSampler(textureUnit, t.ComparisonSampler);
                context.PixelShader.SetSampler(textureUnit, t.ComparisonSampler);
            }
            else if (t.Sampler != null)
            {
                context.VertexShader.SetSampler(textureUnit, t.Sampler);
                context.PixelShader.SetSampler(textureUnit, t.Sampler);
            }
            boundTextures[textureUnit] = textureID;
            currentTextureID = textureID;
            if (textureUnit == 0) fixedTextureUnit = 0;
        }
        public int GetTextureTarget(Texture.Dimension dimension)
        {
            if (dimension == Texture.Dimension.DIM_CUBE_MAP || dimension == Texture.Dimension.DIM_CUBE_MAP_NPOT)
            {
                return 0x8513;
            }
            return 0x0DE1;
        }
        public void ConfigureCubeMapTexGen(int textureUnit, CubeMapTexGenMode mode)
        {
            // G3D uses fixed-function GL_NORMAL_MAP / GL_REFLECTION_MAP texgen for cube maps.
            // The fixed-function HLSL pipeline reproduces GL_NORMAL_MAP: the object-space normal
            // is transformed by the unit's texture matrix and used as the cube lookup direction.
            if (textureUnit < 0 || textureUnit >= cubeTexGenModes.Length)
            {
                throw new ArgumentOutOfRangeException("textureUnit");
            }
            cubeTexGenModes[textureUnit] = mode;
        }
        public void TexImage2D(int target, int level, TextureFormat format, int width, int height, int border, byte[] data)
        {
            Direct3D11Texture texture;
            if (!textures.TryGetValue(currentTextureID, out texture) || texture.Resource == null)
            {
                throw new InvalidOperationException("No texture is bound for upload.");
            }
            if (data == null || texture.IsDepth) return;
            bool cube = target >= 0x8513 && target < 0x8513 + 6;
            int face = 0;
            if (cube)
            {
                face = target - 0x8513;
                texture.IsCube = true;
                ensureCubeResource(texture);
            }
            byte[] converted = ConvertTextureData(format, data, width, height);
            int mipLevels = texture.Resource.Description.MipLevels;
            if (level >= 0 && level < mipLevels)
            {
                int subresource = cube ? (face * mipLevels + level) : level;
                uploadTextureLevel(converted, texture.Resource, subresource, width);
            }
            recordTextureLevel(texture.Id, level, face, converted);
        }

        /**
          Uploads one texture level with an explicit row pitch.  UpdateSubresource
          without a DataBox derives the pitch from the resource's base width, which
          is wrong for mip levels smaller than the base level.
          */
        private void uploadTextureLevel(byte[] data, Texture2D resource, int subresource, int levelWidth)
        {
            int bytes = bytesPerPixel(resource.Description.Format);
            IntPtr pointer = Marshal.AllocHGlobal(data.Length);
            try
            {
                Marshal.Copy(data, 0, pointer, data.Length);
                DataBox box = new DataBox(pointer, Math.Max(1, levelWidth) * bytes, data.Length);
                context.UpdateSubresource(box, resource, subresource);
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }
        private void ensureCubeResource(Direct3D11Texture texture)
        {
            Texture2DDescription description = texture.Resource.Description;
            if (description.OptionFlags == ResourceOptionFlags.TextureCube && description.ArraySize == 6)
            {
                return;
            }
            if (texture.ShaderView != null) { texture.ShaderView.Dispose(); texture.ShaderView = null; }
            if (texture.ColorView != null) { texture.ColorView.Dispose(); texture.ColorView = null; }
            texture.Resource.Dispose();
            description.ArraySize = 6;
            description.OptionFlags = ResourceOptionFlags.TextureCube;
            description.BindFlags = BindFlags.ShaderResource;
            description.Usage = ResourceUsage.Default;
            texture.Resource = new Texture2D(device, description);
            texture.ShaderView = new ShaderResourceView(device, texture.Resource, new ShaderResourceViewDescription
            {
                Format = description.Format,
                Dimension = ShaderResourceViewDimension.TextureCube,
                TextureCube = new ShaderResourceViewDescription.TextureCubeResource { MostDetailedMip = 0, MipLevels = -1 }
            });
        }
        private void recordTextureLevel(int textureID, int level, byte[] data)
        {
            recordTextureLevel(textureID, level, 0, data);
        }
        private void recordTextureLevel(int textureID, int level, int face, byte[] data)
        {
            List<byte[]> levels;
            if (!textureLevelData.TryGetValue(textureID, out levels))
            {
                levels = new List<byte[]>();
                textureLevelData[textureID] = levels;
            }
            Direct3D11Texture texture;
            bool cube = textures.TryGetValue(textureID, out texture) && texture.IsCube;
            int index = cube ? (level * 6 + face) : level;
            while (levels.Count <= index)
            {
                levels.Add(null);
            }
            levels[index] = data;
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
            Texture2DDescription description = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = mipCount,
                ArraySize = faceCount,
                Format = TextureFormatMap.ToDXGI(format),
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget
            };
            if (faceCount == 6)
            {
                description.OptionFlags = ResourceOptionFlags.TextureCube;
                description.BindFlags = BindFlags.ShaderResource;
            }
            texture.Resource = new Texture2D(device, description);
            texture.IsCube = faceCount == 6;
            ShaderResourceViewDescription shaderViewDescription = new ShaderResourceViewDescription
            {
                Format = TextureFormatMap.ToDXGI(format),
                Dimension = faceCount == 6 ? ShaderResourceViewDimension.TextureCube : ShaderResourceViewDimension.Texture2D
            };
            if (faceCount == 6)
            {
                shaderViewDescription.TextureCube = new ShaderResourceViewDescription.TextureCubeResource { MostDetailedMip = 0, MipLevels = -1 };
            }
            texture.ShaderView = new ShaderResourceView(device, texture.Resource, shaderViewDescription);
            if (faceCount != 6)
            {
                texture.ColorView = new RenderTargetView(device, texture.Resource);
            }
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
            if (texture.IsCube)
            {
                int cubeBytes = bytesPerPixel(texture.Resource.Description.Format);
                int cubeMipLevels = texture.Resource.Description.MipLevels;
                List<byte[]> cubeLevels;
                if (!textureLevelData.TryGetValue(texture.Id, out cubeLevels))
                {
                    cubeLevels = new List<byte[]>();
                    textureLevelData[texture.Id] = cubeLevels;
                }
                for (int face = 0; face < 6; ++face)
                {
                    byte[] previousFace = cubeLevels.Count > face ? cubeLevels[face] : null;
                    if (previousFace == null)
                    {
                        previousFace = new byte[texture.Width * texture.Height * cubeBytes];
                        recordTextureLevel(texture.Id, 0, face, previousFace);
                    }
                    int faceWidth = texture.Width;
                    int faceHeight = texture.Height;
                    for (int level = 1; level < cubeMipLevels; ++level)
                    {
                        int recordedIndex = level * 6 + face;
                        byte[] destination;
                        if (cubeLevels.Count > recordedIndex && cubeLevels[recordedIndex] != null)
                        {
                            destination = cubeLevels[recordedIndex];
                        }
                        else
                        {
                            destination = downsample(previousFace, faceWidth, faceHeight, cubeBytes);
                        }
                        uploadTextureLevel(destination, texture.Resource, face * cubeMipLevels + level, Math.Max(1, texture.Width >> level));
                        recordTextureLevel(texture.Id, level, face, destination);
                        previousFace = destination;
                        faceWidth = Math.Max(1, faceWidth / 2);
                        faceHeight = Math.Max(1, faceHeight / 2);
                    }
                }
                return;
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
                uploadTextureLevel(destination, texture.Resource, level, Math.Max(1, texture.Width >> level));
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
            bool mipmaps = usesMipmaps(parameters.MinFilter);
            d.MinimumLod = 0.0f;
            d.MaximumLod = mipmaps ? float.MaxValue : 0.0f;
            if (mipmaps && parameters.MaxAnisotropy > 1.0f)
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
        private static bool usesMipmaps(Texture.InterpolateMode mode)
        {
            return mode == Texture.InterpolateMode.NEAREST_MIPMAP ||
                   mode == Texture.InterpolateMode.BILINEAR_MIPMAP ||
                   mode == Texture.InterpolateMode.TRILINEAR_MIPMAP;
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
            Direct3D11Texture texture;
            if (!textures.TryGetValue(textureID, out texture))
            {
                throw new InvalidOperationException("G3D.Direct3D11: the shadow map texture is not allocated.");
            }
            if (texture.ComparisonView == null)
            {
                throw new InvalidOperationException("G3D.Direct3D11: the shadow map texture cannot be sampled.");
            }
            if (texture.ComparisonSampler == null)
            {
                texture.ComparisonSampler = CreateSampler(mode, true);
            }
            lightMvp = lightMVP;
            context.VertexShader.SetShaderResource(textureUnit, texture.ComparisonView);
            context.PixelShader.SetShaderResource(textureUnit, texture.ComparisonView);
            context.VertexShader.SetSampler(textureUnit, texture.ComparisonSampler);
            context.PixelShader.SetSampler(textureUnit, texture.ComparisonSampler);
            boundTextures[textureUnit] = textureID;
            shadowTextureUnit = textureUnit;
            shadowEnabled = true;
        }
        private SamplerState CreateSampler(Texture.DepthReadMode mode, bool comparison)
        {
            SamplerStateDescription d = SamplerStateDescription.Default();
            d.AddressU = TextureAddressMode.Clamp; d.AddressV = TextureAddressMode.Clamp; d.AddressW = TextureAddressMode.Clamp;
            d.Filter = comparison ? Filter.ComparisonMinMagMipLinear : Filter.MinMagMipLinear;
            d.ComparisonFunction = comparison ? (mode == Texture.DepthReadMode.DEPTH_GEQUAL ? Comparison.GreaterEqual : Comparison.LessEqual) : Comparison.Never;
            return new SamplerState(device, d);
        }
        public void DisableShadowMap(int textureUnit)
        {
            if (shadowTextureUnit == textureUnit)
            {
                shadowTextureUnit = -1;
                shadowEnabled = false;
            }
            if (fixedTextureUnit == textureUnit) fixedTextureUnit = -1;
        }
        public void CopyTexImage2D(int target, int level, int x, int y, int width, int height, int border)
        {
            CopyTexSubImage2D(target, level, 0, 0, x, y, width, height);
        }
        public void CopyTexSubImage2D(int target, int level, int xoffset, int yoffset, int x, int y, int width, int height)
        {
            Direct3D11Texture texture;
            if (!textures.TryGetValue(currentTextureID, out texture) || texture.Resource == null)
            {
                throw new InvalidOperationException("No texture is bound for the copy.");
            }
            if (level < 0 || level >= texture.Resource.Description.MipLevels)
            {
                return;
            }
            Texture2D source = activeColorView != null ? activeColorView.ResourceAs<Texture2D>() : null;
            if (source == null)
            {
                throw new NotSupportedException("G3D.Direct3D11: the active render target cannot be copied into a texture.");
            }
            if (source.Description.Format != texture.Resource.Description.Format)
            {
                throw new NotSupportedException(
                    "G3D.Direct3D11: copying the render target into a texture requires the source and destination formats to match.");
            }
            int sourceHeight = source.Description.Height;
            int sourceY = sourceHeight - y - height;
            if (sourceY < 0 || x < 0 || x + width > source.Description.Width || y + height > sourceHeight)
            {
                return;
            }
            int destinationHeight = texture.Resource.Description.Height >> level;
            int destinationY = destinationHeight - yoffset - height;
            if (destinationY < 0 || xoffset < 0 || xoffset + width > (texture.Resource.Description.Width >> level))
            {
                return;
            }
            ResourceRegion sourceRegion = new ResourceRegion
            {
                Left = x,
                Top = sourceY,
                Right = x + width,
                Bottom = sourceY + height,
                Front = 0,
                Back = 1
            };
            context.CopySubresourceRegion(source, 0, sourceRegion, texture.Resource, level, xoffset, destinationY, 0);
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
                context.CopyResource(texture.Resource, staging);
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

        // Abstraction over NVIDIA/ARB extensions
        public int GenAssemblyProgram(int unit)
        {
            throw new NotSupportedException("The Direct3D 11 backend does not support legacy ARB/NVIDIA assembly GPU programs.");
        }

        public void DeleteAssemblyProgram(int program)
        {
            throw new NotSupportedException("The Direct3D 11 backend does not support legacy ARB/NVIDIA assembly GPU programs.");
        }

        public void BindAssemblyProgram(int unit, int program)
        {
            throw new NotSupportedException("The Direct3D 11 backend does not support legacy ARB/NVIDIA assembly GPU programs.");
        }

        public void LoadAssemblyProgram(int unit, int program, string code)
        {
            throw new NotSupportedException("The Direct3D 11 backend does not support legacy ARB/NVIDIA assembly GPU programs.");
        }

        public void SetAssemblyProgramLocalParameter(int unit, int slot, Vector4 value)
        {
            throw new NotSupportedException("The Direct3D 11 backend does not support legacy ARB/NVIDIA assembly GPU programs.");
        }

        public void GetAssemblyProgramError(int unit, out int pos, out string msg)
        {
            throw new NotSupportedException("The Direct3D 11 backend does not support legacy ARB/NVIDIA assembly GPU programs.");
        }

        public void EnableAssemblyProgram(int unit, bool enabled)
        {
            throw new NotSupportedException("The Direct3D 11 backend does not support legacy ARB/NVIDIA assembly GPU programs.");
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
            if (framebufferID == 0)
            {
                activeDepthView = defaultDepthView;
                activeDepthResource = defaultDepthResource;
                activeColorView = renderTargetView;
                context.OutputMerger.SetRenderTargets(activeDepthView, renderTargetView);
                return;
            }
            Direct3D11Framebuffer f;
            if (framebuffers.TryGetValue(framebufferID, out f))
            {
                activeDepthView = f.Depth;
                activeDepthResource = f.DepthResource;
                activeColorView = f.Color;
                unbindConflictingShaderResources(f.DepthResource);
                applyRenderTargets(f);
            }
        }
        private Texture2D dummyRenderTargetResource;
        private RenderTargetView dummyRenderTargetRTV;

        /**
          Returns a small color render target the size of the framebuffer's depth
          attachment, so a depth-only framebuffer still has a render target bound.
          The texture is never sampled and the caller disables color writes.
          */
        private RenderTargetView dummyRenderTargetView(Direct3D11Framebuffer f)
        {
            // The render target must match the depth-stencil view's dimensions; a
            // smaller one limits rasterization to its own size.
            int width = activeDepthResource != null ? activeDepthResource.Description.Width : 1;
            int height = activeDepthResource != null ? activeDepthResource.Description.Height : 1;
            if (width < 1) width = 1;
            if (height < 1) height = 1;
            if (dummyRenderTargetResource == null
                || dummyRenderTargetResource.Description.Width != width
                || dummyRenderTargetResource.Description.Height != height)
            {
                if (dummyRenderTargetRTV != null) { dummyRenderTargetRTV.Dispose(); dummyRenderTargetRTV = null; }
                if (dummyRenderTargetResource != null) { dummyRenderTargetResource.Dispose(); dummyRenderTargetResource = null; }
                dummyRenderTargetResource = new Texture2D(device, new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = DxgiFormat.R8G8B8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget
                });
                dummyRenderTargetRTV = new RenderTargetView(device, dummyRenderTargetResource);
            }
            return dummyRenderTargetRTV;
        }

        private void unbindConflictingShaderResources(Texture2D depthResource)
        {
            if (depthResource == null) return;
            List<int> conflicting = new List<int>();
            foreach (KeyValuePair<int, int> entry in boundTextures)
            {
                Direct3D11Texture texture;
                if (!textures.TryGetValue(entry.Value, out texture)) continue;
                if (texture.Resource == depthResource) conflicting.Add(entry.Key);
            }
            foreach (int unit in conflicting)
            {
                context.VertexShader.SetShaderResource(unit, null);
                context.PixelShader.SetShaderResource(unit, null);
            }
        }
        private void applyRenderTargets(Direct3D11Framebuffer f)
        {
            List<RenderTargetView> views = new List<RenderTargetView>();
            bool selected = drawBufferIndex > 0 && drawBufferIndex < f.Colors.Length && f.Colors[drawBufferIndex] != null;
            if (selected)
            {
                views.Add(f.Colors[drawBufferIndex]);
            }
            for (int i = 0; i < f.Colors.Length; ++i)
            {
                if (selected && i == drawBufferIndex) continue;
                if (f.Colors[i] != null) views.Add(f.Colors[i]);
            }
            if (drawBufferIndex < 0 || views.Count == 0)
            {
                // Depth-only render target.  Binding zero render targets makes the
                // output merger keep whatever render target was bound before, so the
                // render state mismatches the depth-stencil view and the rasterizer
                // skips most fragments (shadow maps came out mostly empty).  Bind a
                // matching dummy color target alongside the depth-stencil view; the
                // application disables color writes or never reads the target, so it
                // has no visible effect.
                context.OutputMerger.SetRenderTargets(activeDepthView, dummyRenderTargetView(f));
                return;
            }
            context.OutputMerger.SetRenderTargets(activeDepthView, views.ToArray());
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
            if (attachment == Framebuffer.AttachmentPoint.Depth) { f.Depth = t.DepthView; f.DepthResource = t.Resource as Texture2D; }
            else
            {
                int index = colorAttachmentIndex(attachment);
                if (index >= 0 && index < f.Colors.Length)
                {
                    f.Colors[index] = t.ColorView;
                    if (index == 0) f.Color = t.ColorView;
                }
            }
            f.Width = t.Width; f.Height = t.Height;
        }
        public void AttachFramebufferRenderbuffer(int framebufferID, Framebuffer.AttachmentPoint attachment, int renderbufferID)
        {
            Direct3D11Framebuffer f; Direct3D11Renderbuffer r; if (!framebuffers.TryGetValue(framebufferID, out f) || !renderbuffers.TryGetValue(renderbufferID, out r)) return;
            if (attachment == Framebuffer.AttachmentPoint.Depth) { f.Depth = r.DepthView; f.DepthResource = r.Resource; }
            else
            {
                int index = colorAttachmentIndex(attachment);
                if (index >= 0 && index < f.Colors.Length)
                {
                    f.Colors[index] = r.ColorView;
                    if (index == 0) f.Color = r.ColorView;
                }
            }
            f.Width = r.Width; f.Height = r.Height;
        }
        private static int colorAttachmentIndex(Framebuffer.AttachmentPoint attachment)
        {
            switch (attachment)
            {
                case Framebuffer.AttachmentPoint.Color0: return 0;
                case Framebuffer.AttachmentPoint.Color1: return 1;
                case Framebuffer.AttachmentPoint.Color2: return 2;
                case Framebuffer.AttachmentPoint.Color3: return 3;
                case Framebuffer.AttachmentPoint.Color4: return 4;
                case Framebuffer.AttachmentPoint.Color5: return 5;
                case Framebuffer.AttachmentPoint.Color6: return 6;
                case Framebuffer.AttachmentPoint.Color7: return 7;
                default: return -1;
            }
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
                BindFlags = TextureFormatMap.IsDepth(format) ? BindFlags.DepthStencil : BindFlags.RenderTarget
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
            advanceFixedFunctionBuffers();
            int stride = 64; int bytes = vertices.Count * stride;
            if (bytes > immediateVertexBuffer.Description.SizeInBytes) { immediateVertexBuffer.Dispose(); immediateVertexBuffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription { SizeInBytes = Math.Max(bytes, 1024), Usage = ResourceUsage.Dynamic, BindFlags = BindFlags.VertexBuffer, CpuAccessFlags = CpuAccessFlags.Write }); }
            if (immediatePacked == null || immediatePacked.Length < bytes) { immediatePacked = new byte[bytes]; }
            byte[] data = immediatePacked; int at = 0;
            foreach (ImmediateVertex v in vertices)
            {
                float[] values = valueScratch;
                values[0] = v.X; values[1] = v.Y; values[2] = v.Z;
                values[3] = v.Nx; values[4] = v.Ny; values[5] = v.Nz;
                values[6] = 0.0f; values[7] = 0.0f;
                values[8] = v.R; values[9] = v.G; values[10] = v.B; values[11] = v.A;
                values[12] = v.S; values[13] = v.T; values[14] = v.U; values[15] = v.V;
                global::System.Buffer.BlockCopy(values, 0, data, at, 64);
                at += 64;
            }
            DataBox box = context.MapSubresource(immediateVertexBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None); Marshal.Copy(data, 0, box.DataPointer, bytes); context.UnmapSubresource(immediateVertexBuffer, 0);
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(immediateVertexBuffer, stride, 0));
            context.InputAssembler.InputLayout = fixedInputLayout;
            context.VertexShader.Set(fixedVertexShader);
            context.PixelShader.Set(fixedPixelShader);
            // UpdateConstants binds the constant buffers after mapping them, so
            // they stay unbound when the Map happens.
            UpdateConstants();
            context.InputAssembler.PrimitiveTopology = TopologyFor(primitive);
            context.Draw(vertices.Count, 0);
            unbindFixedFunctionResources();
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

        /**
          Direct3D 11 cannot draw from client memory, so the caller's indices are
          staged into an index buffer.  The OpenGL backend passes them straight
          to glDrawElements as the original RenderDevice did.
        */
        public void DrawVAR(RenderDevice.Primitive primitive, int numIndices, VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, uint[] index)
        {
            if (vertexArray == null || numIndices <= 0 || index == null) return;

            if (numIndices > index.Length)
            {
                numIndices = index.Length;
            }

            if (clientIndexVAR == null || clientIndexVARCapacity < numIndices)
            {
                clientIndexVARCapacity = Math.Max(numIndices, 64);
                clientIndexVAR = VAR.create(VAR.Type.INDEX, clientIndexVARCapacity, VAR.Usage.STREAM);
            }

            uint[] staged = new uint[numIndices];
            Array.Copy(index, staged, numIndices);
            clientIndexVAR.setData(staged);
            DrawVAR(primitive, numIndices, vertexArray, normalArray, colorArray, texCoordArray, clientIndexVAR);
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

        private static bool isStandardSemantic(string semanticName)
        {
            if (semanticName == null) return false;
            string upper = semanticName.ToUpperInvariant();
            return upper == "POSITION" || upper == "NORMAL" || upper == "COLOR" || upper == "TEXCOORD0";
        }

        private VAR varForAttribute(Direct3D11VertexAttribute attribute, VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray)
        {
            if (attribute.SemanticName == null) return varForAttributeSlot(attribute.Slot, vertexArray, normalArray, colorArray, texCoordArray);
            string upper = attribute.SemanticName.ToUpperInvariant();
            if (upper == "POSITION") return vertexArray;
            if (upper == "NORMAL") return normalArray;
            if (upper == "COLOR") return colorArray;
            if (upper == "TEXCOORD0") return texCoordArray;
            return null;
        }

        private void drawVarWithProgram(RenderDevice.Primitive primitive, int numIndices, Direct3D11Program program,
            VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, VAR indexArray)
        {
            context.VertexShader.Set(program.Vertex);
            context.PixelShader.Set(program.Pixel);
            context.InputAssembler.InputLayout = program.Layout;
            foreach (Direct3D11ConstantBuffer programBuffer in program.ConstantBuffers)
            {
                if (programBuffer.IsVertexStage)
                {
                    context.VertexShader.SetConstantBuffer(programBuffer.Slot, programBuffer.Buffer);
                }
                else
                {
                    context.PixelShader.SetConstantBuffer(programBuffer.Slot, programBuffer.Buffer);
                }
            }

            List<int> drawIndices = buildDrawIndices(numIndices, vertexArray, indexArray);

            foreach (Direct3D11VertexAttribute attr in program.Attributes)
            {
                VAR array = varForAttribute(attr, vertexArray, normalArray, colorArray, texCoordArray);
                if (array == null) continue;
                byte[] data = array.getData();
                if (data == null || data.Length == 0) continue;
                ensureVertexBuffer(ref attrBuffers[attr.Slot], data.Length);
                uploadVertexBuffer(attrBuffers[attr.Slot], data);
                context.InputAssembler.SetVertexBuffers(attr.Slot, new VertexBufferBinding(attrBuffers[attr.Slot], array.getStride(), 0));
            }

            // Generic vertex attributes fill the shader inputs that are not one of
            // the standard semantics, in ascending attribute number order.
            int genericIndex = 0;
            foreach (Direct3D11VertexAttribute attr in program.Attributes)
            {
                if (isStandardSemantic(attr.SemanticName)) continue;
                VAR array = genericIndex < vertexAttribArrays.Length ? vertexAttribArrays[genericIndex] : null;
                ++genericIndex;
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
            if (vertexArray == null || numIndices <= 0)
            {
                return;
            }
            advanceFixedFunctionBuffers();

            VarDrawState drawState = getVarDrawState(vertexArray, normalArray, colorArray, texCoordArray, indexArray);
            if (drawState == null || drawState.NumVertices == 0)
            {
                return;
            }

            int count = drawState.Indexed
                ? Math.Min(numIndices, drawState.NumIndices)
                : Math.Min(numIndices, drawState.NumVertices);
            if (count <= 0)
            {
                return;
            }

            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(drawState.VertexBuffer, 64, 0));
            context.InputAssembler.InputLayout = fixedInputLayout;
            context.VertexShader.Set(fixedVertexShader);
            context.PixelShader.Set(fixedPixelShader);
            // UpdateConstants binds the constant buffers after mapping them, so
            // they stay unbound when the Map happens.
            UpdateConstants();
            context.InputAssembler.PrimitiveTopology = TopologyFor(primitive);
            if (drawState.Indexed)
            {
                context.InputAssembler.SetIndexBuffer(drawState.IndexBuffer, DxgiFormat.R32_UInt, 0);
                context.DrawIndexed(count, 0, 0);
            }
            else
            {
                context.Draw(count, 0);
            }
            unbindFixedFunctionResources();
        }
        /**
          Detaches the dynamic fixed-function vertex and constant buffers from
          the pipeline.  The D3D11 runtime flushes pending GPU work whenever a
          mapped dynamic buffer is still bound to a pipeline stage, so the
          buffers are unbound after each draw to keep the next Map cheap.
        */
        private void unbindFixedFunctionResources()
        {
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding((SharpDX.Direct3D11.Buffer)null, 0, 0));
            context.VertexShader.SetConstantBuffer(0, (SharpDX.Direct3D11.Buffer)null);
            context.PixelShader.SetConstantBuffer(0, (SharpDX.Direct3D11.Buffer)null);
        }
        private static int varVersion(VAR array)
        {
            return (array != null) ? array.getDataVersion() : -1;
        }
        private VarDrawState getVarDrawState(VAR vertexArray, VAR normalArray, VAR colorArray, VAR texCoordArray, VAR indexArray)
        {
            VarDrawState drawState;
            if (!varDrawStates.TryGetValue(vertexArray, out drawState))
            {
                drawState = new VarDrawState();
                varDrawStates.Add(vertexArray, drawState);
            }

            if (drawState.VertexArray != vertexArray ||
                drawState.NormalArray != normalArray ||
                drawState.ColorArray != colorArray ||
                drawState.TexCoordArray != texCoordArray ||
                drawState.IndexArray != indexArray ||
                drawState.VertexVersion != vertexArray.getDataVersion() ||
                drawState.NormalVersion != varVersion(normalArray) ||
                drawState.ColorVersion != varVersion(colorArray) ||
                drawState.TexCoordVersion != varVersion(texCoordArray) ||
                drawState.IndexVersion != varVersion(indexArray))
            {
                drawState.VertexArray = vertexArray;
                drawState.NormalArray = normalArray;
                drawState.ColorArray = colorArray;
                drawState.TexCoordArray = texCoordArray;
                drawState.IndexArray = indexArray;
                drawState.VertexVersion = vertexArray.getDataVersion();
                drawState.NormalVersion = varVersion(normalArray);
                drawState.ColorVersion = varVersion(colorArray);
                drawState.TexCoordVersion = varVersion(texCoordArray);
                drawState.IndexVersion = varVersion(indexArray);
                rebuildVarDrawState(drawState);
            }

            return drawState;
        }
        private void rebuildVarDrawState(VarDrawState drawState)
        {
            drawState.NumVertices = 0;
            drawState.NumIndices = 0;
            drawState.Indexed = false;

            VAR vertexArray = drawState.VertexArray;
            byte[] vertexData = vertexArray.getData();
            int vertexStride = vertexArray.getStride();
            if (vertexData == null || vertexStride < 12)
            {
                return;
            }
            int numVertices = Math.Min(vertexArray.getCount(), vertexData.Length / vertexStride);
            if (numVertices <= 0)
            {
                return;
            }

            byte[] normalData = (drawState.NormalArray != null) ? drawState.NormalArray.getData() : null;
            int normalStride = (normalData != null) ? drawState.NormalArray.getStride() : 0;
            bool useNormals = normalData != null && normalStride >= 12 && drawState.NormalArray.getFormat() == VAR.Format.FLOAT;

            byte[] colorData = (drawState.ColorArray != null) ? drawState.ColorArray.getData() : null;
            int colorStride = (colorData != null) ? drawState.ColorArray.getStride() : 0;
            int colorComponents = (drawState.ColorArray != null) ? drawState.ColorArray.getComponentCount() : 0;
            bool useColors = colorData != null && colorStride >= 12 && colorComponents >= 3 &&
                             drawState.ColorArray.getFormat() == VAR.Format.FLOAT;

            byte[] texCoordData = (drawState.TexCoordArray != null) ? drawState.TexCoordArray.getData() : null;
            int texCoordStride = (texCoordData != null) ? drawState.TexCoordArray.getStride() : 0;
            int texCoordComponents = (drawState.TexCoordArray != null) ? drawState.TexCoordArray.getComponentCount() : 0;
            bool useTexCoords = texCoordData != null && texCoordStride >= 8 && texCoordComponents >= 2 &&
                                drawState.TexCoordArray.getFormat() == VAR.Format.FLOAT;

            float[] values = new float[numVertices * 16];
            for (int i = 0; i < numVertices; ++i)
            {
                int at = i * 16;
                int vat = i * vertexStride;
                values[at + 0] = BitConverter.ToSingle(vertexData, vat);
                values[at + 1] = BitConverter.ToSingle(vertexData, vat + 4);
                values[at + 2] = BitConverter.ToSingle(vertexData, vat + 8);
                if (useNormals)
                {
                    int nat = i * normalStride;
                    values[at + 3] = BitConverter.ToSingle(normalData, nat);
                    values[at + 4] = BitConverter.ToSingle(normalData, nat + 4);
                    values[at + 5] = BitConverter.ToSingle(normalData, nat + 8);
                }
                values[at + 8] = currentColor.r;
                values[at + 9] = currentColor.g;
                values[at + 10] = currentColor.b;
                values[at + 11] = currentColor.a;
                if (useColors)
                {
                    int cat = i * colorStride;
                    values[at + 8] = BitConverter.ToSingle(colorData, cat);
                    values[at + 9] = BitConverter.ToSingle(colorData, cat + 4);
                    values[at + 10] = BitConverter.ToSingle(colorData, cat + 8);
                    values[at + 11] = (colorComponents >= 4 && colorStride >= 16)
                        ? BitConverter.ToSingle(colorData, cat + 12)
                        : 1.0f;
                }
                values[at + 12] = currentTexCoord.x;
                values[at + 13] = currentTexCoord.y;
                values[at + 14] = currentTexCoord.z;
                values[at + 15] = currentTexCoord.w;
                if (useTexCoords)
                {
                    int tat = i * texCoordStride;
                    values[at + 12] = BitConverter.ToSingle(texCoordData, tat);
                    values[at + 13] = BitConverter.ToSingle(texCoordData, tat + 4);
                }
            }

            byte[] bytes = new byte[values.Length * 4];
            global::System.Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            if (drawState.VertexBuffer == null || drawState.VertexBuffer.Description.SizeInBytes < bytes.Length)
            {
                if (drawState.VertexBuffer != null) drawState.VertexBuffer.Dispose();
                drawState.VertexBuffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
                {
                    SizeInBytes = Math.Max(bytes.Length, 256),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.VertexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write
                });
            }
            uploadVertexBuffer(drawState.VertexBuffer, bytes);
            drawState.NumVertices = numVertices;

            if (drawState.IndexArray == null)
            {
                return;
            }

            byte[] indexData = drawState.IndexArray.getData();
            if (indexData == null || indexData.Length < 4)
            {
                return;
            }
            int indexCount = Math.Min(drawState.IndexArray.getCount(), indexData.Length / 4);
            if (indexCount <= 0)
            {
                return;
            }
            uint[] indices = new uint[indexCount];
            for (int i = 0; i < indexCount; ++i)
            {
                indices[i] = (uint)BitConverter.ToInt32(indexData, i * 4);
            }
            byte[] indexBytes = new byte[indexCount * 4];
            global::System.Buffer.BlockCopy(indices, 0, indexBytes, 0, indexBytes.Length);
            if (drawState.IndexBuffer == null || drawState.IndexBuffer.Description.SizeInBytes < indexBytes.Length)
            {
                if (drawState.IndexBuffer != null) drawState.IndexBuffer.Dispose();
                drawState.IndexBuffer = new SharpDX.Direct3D11.Buffer(device, new BufferDescription
                {
                    SizeInBytes = Math.Max(indexBytes.Length, 256),
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.IndexBuffer,
                    CpuAccessFlags = CpuAccessFlags.Write
                });
            }
            uploadVertexBuffer(drawState.IndexBuffer, indexBytes);
            drawState.Indexed = true;
            drawState.NumIndices = indexCount;
        }

        private void UpdateConstants()
        {
            // modelView = cameraToWorldInverse * objectToWorld, so the object-to-world
            // transform is reconstructed here where both matrices are current.  It
            // cannot be cached in SetModelViewMatrix because RenderDevice applies the
            // model view before the camera-to-world matrix.
            Matrix4 objectToWorldCurrent = cameraToWorld * modelView;
            Matrix4 mvp = ClipSpace.Multiply(ClipSpace.ToDirect3D(projection), modelView);
            Matrix4 light = ClipSpace.ToDirect3D(lightMvp);
            float[] data = constantsPacked; int at = 0;
            for (int r = 0; r < 4; ++r) for (int c = 0; c < 4; ++c) data[at++] = mvp.elt(r, c);
            for (int r = 0; r < 4; ++r) for (int c = 0; c < 4; ++c) data[at++] = light.elt(r, c);
            for (int r = 0; r < 4; ++r) for (int c = 0; c < 4; ++c) data[at++] = textureMatrices[0].elt(r, c);
            for (int r = 0; r < 4; ++r) for (int c = 0; c < 4; ++c) data[at++] = modelView.elt(r, c);
            for (int r = 0; r < 4; ++r) for (int c = 0; c < 4; ++c) data[at++] = objectToWorldCurrent.elt(r, c);

            Matrix3 normalMatrix = new Matrix3(
                modelView.elt(0, 0), modelView.elt(0, 1), modelView.elt(0, 2),
                modelView.elt(1, 0), modelView.elt(1, 1), modelView.elt(1, 2),
                modelView.elt(2, 0), modelView.elt(2, 1), modelView.elt(2, 2));
            normalMatrix = normalMatrix.inverse().transpose();
            for (int r = 0; r < 3; ++r)
            {
                data[at++] = normalMatrix.elt(r, 0);
                data[at++] = normalMatrix.elt(r, 1);
                data[at++] = normalMatrix.elt(r, 2);
                data[at++] = 0.0f;
            }

            data[at++] = ambient.r; data[at++] = ambient.g; data[at++] = ambient.b; data[at++] = ambient.a;

            data[at++] = specular.r; data[at++] = specular.g; data[at++] = specular.b; data[at++] = 1.0f;

            data[at++] = shininess;
            data[at++] = twoSidedLighting ? 1.0f : 0.0f;
            data[at++] = lightingEnabled ? 1.0f : 0.0f;
            data[at++] = fixedTextureUnit >= 0 ? 1.0f : 0.0f;

            int lightMask = 0;
            for (int i = 0; i < lights.Length; ++i)
            {
                if (lightEnabled[i]) lightMask |= (1 << i);
            }
            data[at++] = 0.0f; data[at++] = 0.0f; data[at++] = 0.0f; data[at++] = (float)lightMask;

            data[at++] = shadowEnabled ? 1.0f : 0.0f; data[at++] = 0.0f; data[at++] = 0.0f; data[at++] = 0.0f;

            data[at++] = CombineModeIndex(combineMode);
            data[at++] = AlphaTestIndex(alphaTest);
            data[at++] = (float)alphaTestReference;
            data[at++] = updateCubeMapBinding() ? 1.0f : 0.0f;

            if (!cameraToWorldInverseValid) { cameraToWorldInverseCached = cameraToWorld.inverse(); cameraToWorldInverseValid = true; }
            Matrix4 cameraToWorldInverse = cameraToWorldInverseCached;
            for (int i = 0; i < lights.Length; ++i)
            {
                GLight lightValue = lights[i] ?? new GLight();
                // Disabled lights contribute nothing, so their transform is skipped.
                Vector4 eyePosition = lightEnabled[i] ? (cameraToWorldInverse * lightValue.position) : new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                data[at++] = eyePosition.x; data[at++] = eyePosition.y; data[at++] = eyePosition.z; data[at++] = lightValue.position.w;
            }
            for (int i = 0; i < lights.Length; ++i)
            {
                GLight lightValue = lights[i] ?? new GLight();
                Vector3 eyeDirection = lightEnabled[i] ? (cameraToWorldInverse * new Vector4(lightValue.spotDirection, 0.0f)).xyz() : new Vector3(0.0f, 0.0f, 0.0f);
                data[at++] = eyeDirection.x; data[at++] = eyeDirection.y; data[at++] = eyeDirection.z; data[at++] = (float)lightValue.spotCutoff;
            }
            for (int i = 0; i < lights.Length; ++i)
            {
                GLight lightValue = lights[i] ?? new GLight();
                Color3 color = lightValue.color;
                float flags = (lightValue.diffuse ? 1.0f : 0.0f) + (lightValue.specular ? 2.0f : 0.0f);
                data[at++] = color.r; data[at++] = color.g; data[at++] = color.b; data[at++] = flags;
            }
            for (int i = 0; i < lights.Length; ++i)
            {
                GLight lightValue = lights[i] ?? new GLight();
                data[at++] = (float)lightValue.attenuation[0];
                data[at++] = (float)lightValue.attenuation[1];
                data[at++] = (float)lightValue.attenuation[2];
                data[at++] = 0.0f;
            }

            {
                // Map first: the buffer was unbound from the pipeline after the
                // previous draw, and mapping a bound dynamic buffer makes the
                // D3D11 runtime flush pending GPU work.
                SharpDX.DataBox box = context.MapSubresource(constantsBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                try
                {
                    Marshal.Copy(data, 0, box.DataPointer, data.Length);
                }
                finally
                {
                    context.UnmapSubresource(constantsBuffer, 0);
                }
            }
            // Bind after uploading so the next Map sees an unbound buffer.
            context.VertexShader.SetConstantBuffer(0, constantsBuffer);
            context.PixelShader.SetConstantBuffer(0, constantsBuffer);
        }
        /**
          Binds the unit-0 cube map to the fixed-function cube slot (t2/s2) when
          cube texgen is active, mirroring OpenGL's GL_NORMAL_MAP texgen. Returns
          whether the fixed-function pixel shader should sample the cube map.
          */
        private bool updateCubeMapBinding()
        {
            if (cubeTexGenModes.Length == 0 || cubeTexGenModes[0] == CubeMapTexGenMode.TEXGEN_OFF)
            {
                return false;
            }
            int textureID;
            if (!boundTextures.TryGetValue(0, out textureID))
            {
                return false;
            }
            Direct3D11Texture texture;
            if (!textures.TryGetValue(textureID, out texture) || !texture.IsCube || texture.ShaderView == null)
            {
                return false;
            }
            context.PixelShader.SetShaderResource(2, texture.ShaderView);
            if (texture.Sampler != null)
            {
                context.PixelShader.SetSampler(2, texture.Sampler);
            }
            else
            {
                context.PixelShader.SetSampler(2, getDefaultSampler());
            }
            return true;
        }

        private static int CombineModeIndex(RenderDevice.CombineMode mode)
        {
            switch (mode)
            {
                case RenderDevice.CombineMode.TEX_REPLACE: return 0;
                case RenderDevice.CombineMode.TEX_BLEND: return 1;
                case RenderDevice.CombineMode.TEX_INTERPOLATE: return 2;
                case RenderDevice.CombineMode.TEX_ADD: return 3;
                case RenderDevice.CombineMode.TEX_MODULATE: return 4;
                case RenderDevice.CombineMode.TEX_ADD_SIGNED: return 5;
                case RenderDevice.CombineMode.TEX_SUBTRACT: return 6;
                case RenderDevice.CombineMode.TEX_DOT3_RGB: return 7;
                case RenderDevice.CombineMode.TEX_DOT3_RGBA: return 8;
                default: return 4;
            }
        }

        private static int AlphaTestIndex(RenderDevice.AlphaTest test)
        {
            switch (test)
            {
                case RenderDevice.AlphaTest.ALPHA_GREATER: return 1;
                case RenderDevice.AlphaTest.ALPHA_LESS: return 2;
                case RenderDevice.AlphaTest.ALPHA_GEQUAL: return 3;
                case RenderDevice.AlphaTest.ALPHA_LEQUAL: return 4;
                case RenderDevice.AlphaTest.ALPHA_NOTEQUAL: return 5;
                case RenderDevice.AlphaTest.ALPHA_EQUAL: return 6;
                case RenderDevice.AlphaTest.ALPHA_ALWAYS_PASS: return 7;
                case RenderDevice.AlphaTest.ALPHA_NEVER_PASS: return 8;
                default: return 0;
            }
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
            if (mode == RenderDevice.RenderMode.RENDER_CURRENT) return;
            renderModeState = mode;
            applyRasterizerState();
        }
        public void SetCullFace(RenderDevice.CullFace face)
        {
            if (face == RenderDevice.CullFace.CULL_CURRENT) return;
            cullFaceState = face;
            applyRasterizerState();
        }
        private void applyRasterizerState()
        {
            if (rasterizerState != null) rasterizerState.Dispose();
            RasterizerStateDescription d = RasterizerStateDescription.Default();
            d.FillMode = renderModeState == RenderDevice.RenderMode.RENDER_WIREFRAME ? FillMode.Wireframe : FillMode.Solid;
            d.CullMode = cullFaceState == RenderDevice.CullFace.CULL_FRONT ? CullMode.Front : cullFaceState == RenderDevice.CullFace.CULL_BACK ? CullMode.Back : CullMode.None;
            if (polygonOffset != 0)
            {
                d.SlopeScaledDepthBias = (float)polygonOffset;
                d.DepthBias = polygonOffset < 0 ? -1 : 1;
                d.DepthBiasClamp = 0.0f;
            }
            d.IsFrontCounterClockwise = true;
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
            alphaTest = test;
            alphaTestReference = reference;
        }
        public void SetTextureCombineMode(int textureUnit, RenderDevice.CombineMode mode)
        {
            if (textureUnit == fixedTextureUnit) combineMode = mode;
        }
        public void SetVertexAttribArray(int attribNum, VAR v, bool normalize)
        {
            if (attribNum < 0 || attribNum >= vertexAttribArrays.Length)
            {
                throw new ArgumentOutOfRangeException("attribNum");
            }
            vertexAttribArrays[attribNum] = v;
        }
        public void SetDrawBuffer(RenderDevice.Buffer drawBuffer)
        {
            if (drawBuffer == RenderDevice.Buffer.BUFFER_CURRENT) return;
            switch (drawBuffer)
            {
                case RenderDevice.Buffer.BUFFER_NONE:
                    drawBufferIndex = -1;
                    break;
                case RenderDevice.Buffer.BUFFER_COLOR0:
                case RenderDevice.Buffer.BUFFER_COLOR1:
                case RenderDevice.Buffer.BUFFER_COLOR2:
                case RenderDevice.Buffer.BUFFER_COLOR3:
                case RenderDevice.Buffer.BUFFER_COLOR4:
                case RenderDevice.Buffer.BUFFER_COLOR5:
                case RenderDevice.Buffer.BUFFER_COLOR6:
                case RenderDevice.Buffer.BUFFER_COLOR7:
                    drawBufferIndex = (int)drawBuffer - (int)RenderDevice.Buffer.BUFFER_COLOR0;
                    break;
                case RenderDevice.Buffer.BUFFER_BACK:
                case RenderDevice.Buffer.BUFFER_BACK_LEFT:
                case RenderDevice.Buffer.BUFFER_BACK_RIGHT:
                    drawBufferIndex = 0;
                    break;
                case RenderDevice.Buffer.BUFFER_FRONT:
                case RenderDevice.Buffer.BUFFER_FRONT_LEFT:
                case RenderDevice.Buffer.BUFFER_FRONT_RIGHT:
                case RenderDevice.Buffer.BUFFER_LEFT:
                case RenderDevice.Buffer.BUFFER_RIGHT:
                case RenderDevice.Buffer.BUFFER_FRONT_AND_BACK:
                    throw new NotSupportedException("G3D.Direct3D11: stereo draw buffers are not supported by the Direct3D 11 backend.");
                default:
                    throw new NotSupportedException("G3D.Direct3D11: the requested draw buffer is not supported by the Direct3D 11 backend.");
            }
            Direct3D11Framebuffer f;
            if (framebuffers.TryGetValue(currentFramebuffer, out f))
            {
                applyRenderTargets(f);
            }
            else
            {
                context.OutputMerger.SetRenderTargets(activeDepthView, drawBufferIndex < 0 ? (RenderTargetView)null : renderTargetView);
            }
        }
        public void SetColorWrite(bool enable) { colorWrite = enable; UpdateBlendState(blendSrc, blendDst, blendEquation); }
        public void SetAlphaWrite(bool enable) { alphaWrite = enable; UpdateBlendState(blendSrc, blendDst, blendEquation); }
        public void SetDepthWrite(bool enable) { depthWrite = enable; SetDepthTest(RenderDevice.DepthTest.DEPTH_CURRENT); }
        public void SetShadeMode(RenderDevice.ShadeMode mode) { }
        public void SetStencilOp(RenderDevice.StencilOp fail, RenderDevice.StencilOp zfail, RenderDevice.StencilOp zpass, RenderDevice.StencilOp backFail, RenderDevice.StencilOp backZfail, RenderDevice.StencilOp backZpass) { }
        public void SetPolygonOffset(double offset)
        {
            polygonOffset = offset;
            applyRasterizerState();
        }
        public void SetLineWidth(double width) { }
        public void SetPointSize(double size) { }
        public void SetScissor(Rect2D rect, bool enable) { }
        public void SetViewport(Rect2D viewport) { viewportHeight = Math.Max(1.0f, viewport.height()); context.Rasterizer.SetViewport(viewport.x, viewport.y, viewport.width(), viewport.height(), 0.0f, 1.0f); }
        public void SetDepthRange(double near, double far) { }
        public void SetColorClearValue(Color4 color) { clearValue = color; }
        public void SetDepthClearValue(double depth) { depthClearValue = depth; }
        public void SetStencilClearValue(int stencil) { stencilClearValue = stencil; }
        public void SetTextureMatrix(int textureUnit, Matrix4 matrix) { if (textureUnit >= 0 && textureUnit < textureMatrices.Length) textureMatrices[textureUnit] = matrix; }
        public void SetProjectionMatrix(Matrix4 matrix) { projection = matrix; }
        public void SetModelViewMatrix(Matrix4 matrix) { modelView = matrix; }
        public void SetCameraToWorldMatrix(Matrix4 matrix)
        {
            // RenderDevice re-sends the camera matrix with every object-to-world
            // update, so only recompute the cached inverse when it really changes.
            if (cameraToWorldInverseValid && matrix == cameraToWorld) return;
            cameraToWorld = matrix;
            cameraToWorldInverseValid = false;
        }
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
            context.CopyResource(milestone.Source, milestone.Readback);
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
            Texture2D source = activeColorView.ResourceAs<Texture2D>();
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
                context.CopyResource(source, staging);
                DataBox box = context.MapSubresource(staging, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                bool swapChannels = source.Description.Format == DxgiFormat.B8G8R8A8_UNorm;
                try
                {
                    for (int row = 0; row < height; ++row)
                    {
                        int sourceRow = source.Description.Height - 1 - (y + row);
                        IntPtr rowPointer = new IntPtr(box.DataPointer.ToInt64() + sourceRow * box.RowPitch + x * 4);
                        Marshal.Copy(rowPointer, data, row * width * 4, width * 4);
                        if (swapChannels)
                        {
                            for (int i = 0; i < width; ++i)
                            {
                                int at = row * width * 4 + i * 4;
                                byte blue = data[at];
                                data[at] = data[at + 2];
                                data[at + 2] = blue;
                            }
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
            DxgiFormat stagingFormat;
            int bytesPerPixel;
            int depthMode;
            switch (format)
            {
                case DxgiFormat.D32_Float:
                case DxgiFormat.R32_Float:
                    stagingFormat = format;
                    bytesPerPixel = 4;
                    depthMode = 0;
                    break;
                case DxgiFormat.R32_Typeless:
                    stagingFormat = DxgiFormat.R32_Float;
                    bytesPerPixel = 4;
                    depthMode = 0;
                    break;
                case DxgiFormat.D24_UNorm_S8_UInt:
                    stagingFormat = format;
                    bytesPerPixel = 4;
                    depthMode = 1;
                    break;
                case DxgiFormat.D16_UNorm:
                    stagingFormat = format;
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
                Format = stagingFormat,
                SampleDescription = source.Description.SampleDescription,
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read
            });
            try
            {
                context.CopyResource(source, staging);
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





