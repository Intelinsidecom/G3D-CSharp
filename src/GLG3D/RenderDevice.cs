/**
  @file RenderDevice.h

  Graphics hardware abstraction layer (wrapper for OpenGL).

  You can freely mix OpenGL calls with RenderDevice, just make sure you put
  the state back the way you found it or you will confuse RenderDevice.

  @maintainer Morgan McGuire, morgan@graphics3d.com
  @created 2001-05-29
  @edited  2006-03-04

  Copyright 2001-2006, Morgan McGuire
*/

using System;
using System.Collections.Generic;
using G3D;

namespace G3D
{

    /**
      You must call RenderDevice::init() before using the RenderDevice.

      Rendering interface that abstracts OpenGL.  OpenGL is a basically
      good API with some rough spots.  Three of these are addressed by
      RenderDevice.  First, OpenGL state management is both tricky and
      potentially slow.  Second, OpenGL functions are difficult to use
      because many extensions have led to an evolutionary rather than
      designed API.  For type safety, new enums are introduced for values
      instead of the traditional OpenGL GLenum's, which are just ints.
      Third, OpenGL initialization is complicated.  This interface
      simplifies it significantly.

      <P> NICEST line and point smoothing is enabled by default (however,
      you need to set your alpha blending mode to see it).

      <P> glEnable(GL_NORMALIZE) is set by default.  glEnable(GL_COLOR_MATERIAL)
          is enabled by default.  You may be able to get a slight speed increase
          by disabling GL_NORMALIZE or using GL_SCALE_NORMAL instead.

      <P> For stereo rendering, set <CODE>GWindow::Settings::stereo = true</CODE>
          and use RenderDevice::setDrawBuffer to switch which eye is being rendered.  Only
          use RenderDevice::beginFrame/RenderDevice::endFrame once per frame,
          but do clear both buffers separately.

      <P> The only OpenGL calls <B>NOT</B> abstracted by RenderDevice are
          fog and texture coordinate generation.  For everything else, use
          RenderDevice.

      <P>
      Example
       <PRE>
        RenderDevice renderDevice = new RenderDevice();
        renderDevice->init(GWindow::Settings());
       </PRE>

      <P>
      Example 2 (textured quad)
      <PRE>
        RenderDevice* renderDevice = new RenderDevice();
        renderDevice->init(640, 480);

        TextureRef sprite = new Texture("Grass Texture", "image.jpg");

        renderDevice->beginFrame();
        renderDevice->pushState();
        renderDevice->clear(true, true, true);
        renderDevice->setCullFace(RenderDevice::CULL_NONE);
        renderDevice->setProjectionMatrix(Matrix4::perspectiveProjection(-.2, .2, -.15, .15, .2, 200));
        renderDevice->setTexture(0, sprite);
        renderDevice->setColor(Color3::white());
        renderDevice->beginPrimitive(RenderDevice::QUADS);
            renderDevice->setTexCoord(0,  Vector2(0, 1));
            renderDevice->sendVertex(Vector3(-3, -3, -5));

            renderDevice->setTexCoord(0,  Vector2(1, 1));
            renderDevice->sendVertex(Vector3( 3, -3, -5));

            renderDevice->setTexCoord(0,  Vector2(1, 0));
            renderDevice->sendVertex(Vector3( 3,  3, -5));

            renderDevice->setTexCoord(0,  Vector2(0, 0));
            renderDevice->sendVertex(Vector3(-3,  3, -5));
        renderDevice->endPrimitive();
        renderDevice->popState();

        renderDevice->endFrame();

        while (true);

        renderDevice->cleanup();
        </PRE>

      <P>

      G3D::RenderDevice supports "X_CURRENT" as an option for most settings.

      <P>
     <B>Stereo Rendering</B>
     You can render in stereo (on a stereo capable card) by rendering twice,
     once for each eye's buffer:

     <pre>
       void doGraphics() {
           glDrawBuffer(GL_BACK_LEFT);
           for (int count = 0; count < 2; ++count) {
              ... (put your normal rendering code here)
              glDrawBuffer(GL_BACK_RIGHT);
           }
       }
     </pre>

     Only flip the buffers once; that is, call renderDevice->swapBuffers
     once per frame (not once per eye).  If you use G3D::GApp, it takes
     care of this for you.

     <B>Multiple displays</B>
     If you are using multiple synchronized displays (e.g. the CAVE),
     see:
     http://www.nvidia.com/object/IO_10753.html
     and
     http://www.cs.unc.edu/Research/stc/FAQs/nVidia/FrameLock-V1.0C.pdf

     GLCaps loads the relevant extensions for you, but you must make
     the synchronizing calls yourself (typically, immediately before
     you call swap buffers).

     <P>
     <B>Raw OpenGL Calls</B>
     RenderDevice allows you to mix your own OpenGL calls with RenderDevice calls.
     It assumes that you restored the OpenGL state after your OpenGL calls, however.
     It is not safe to mix arbitrary OpenGL calls with Shaders, however.  The
     G3D::Shader API supports more features than OpenGL shaders and does not work
     well with low-level OpenGL.  You may find that the "wrong" shader is bound
     when you execute OpenGL calls.
     */
    public sealed class RenderDevice : IDisposable
    {
        public enum Primitive
        {
            LINES = 0,
            LINE_STRIP = 1,
            TRIANGLES = 2,
            TRIANGLE_STRIP = 3,
            TRIANGLE_FAN = 4,
            QUADS = 5,
            QUAD_STRIP = 6,
            POINTS = 7
        }

        /** RENDER_CURRENT = preserve whatever the render mode is currently set to.  */
        public enum RenderMode
        {
            RENDER_SOLID,
            RENDER_WIREFRAME,
            RENDER_POINTS,
            RENDER_CURRENT
        }

        public const int MAX_LIGHTS = 8;

        // Private fields
        private IGraphicsBackend backend;
        private IGraphicsWindow _window;
        private Log debugLog;
        private uint generation;
        private int stencilBits;
        private int depthBits;
        private double lightSaturation;
        private int beginEndFrame;
        private bool _swapBuffersAutomatically;
        private bool swapGLBuffersPending;
        private double lastTime;
        private Primitive currentPrimitive;
        private int currentPrimitiveVertexCount;
        private VARArea currentVARArea;
        private int triangleCount;
        private double emwaFrameRate;
        private double emwaTriangleCount;
        private double emwaTriangleRate;
        private string cardDescription;
        private uint mDebugNumMajorOpenGLStateChanges;
        private uint mDebugNumMinorOpenGLStateChanges;
        private uint mDebugNumMajorStateChanges;
        private uint mDebugNumMinorStateChanges;
        private uint mDebugPushStateCalls;
        private bool inPrimitive;
        private bool inShader;
        private bool inIndexedPrimitive;
        private int _numTextureUnits;
        private int _numTextures;
        private int _numTextureCoords;
        private bool _initialized;
        private bool cleanedup;
        private double brightScale;

        /**
          For performance, we don't actually unbind a texture when
          turning off a texture unit, we just disable it.  If it
          is enabled with the same texture, we've saved a swap.
         */
        private uint[] currentlyBoundTexture = new uint[GLCaps.G3D_MAX_TEXTURE_UNITS];

        /**
          Cache of values supplied to supportsTextureFormat.
          Works on pointers since there is no way for users
          to construct their own TextureFormats.
         */
        private Dictionary<TextureFormat, bool> supportedTextureFormatCache =
            new Dictionary<TextureFormat, bool>(TextureFormatReferenceComparer.Instance);

        private int immediateIndexVARCapacity;

        private bool deleteWindow;

        private sealed class TextureFormatReferenceComparer : IEqualityComparer<TextureFormat>
        {
            public static readonly TextureFormatReferenceComparer Instance = new TextureFormatReferenceComparer();

            public bool Equals(TextureFormat x, TextureFormat y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(TextureFormat obj)
            {
                return global::System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        // State management
        private RenderState state;
        private List<RenderState> stateStack;
        private Stack<RenderState> statePool;
        private VAR immediateIndexVAR;

        // Debug settings
        public DebugSettings debugSettings = new DebugSettings();

        // Public nested classes
        public sealed class DebugSettings
        {
            public int primitiveCutoff = -1;
            public bool forceWireframe = false;

            public enum Pipeline
            {
                PIPELINE_NONE,
                PIPELINE_VERTICES_ONLY,
                PIPELINE_FULL
            }

            public Pipeline pipeline = Pipeline.PIPELINE_FULL;

            public enum Reveal
            {
                REVEAL_COLOR,
                REVEAL_DEPTH,
                REVEAL_STENCIL,
                REVEAL_ALPHA,
                REVEAL_OVERDRAW
            }

            public Reveal reveal = Reveal.REVEAL_COLOR;
        }

        public sealed class StatusInfo
        {
            public uint minorStateChangeCount;
            public uint minorOpenGLStateChangeCount;
            public uint majorStateChangeCount;
            public uint majorOpenGLStateChangeCount;
            public uint pushStateCount;
            public uint primitiveCount;
            public uint triangleCount;
            public double swapbuffersTime;
            public float frameRate;
            public float smoothFrameRate;
            public float triangleRate;
        }

        // State change counters
        public uint debugNumMajorOpenGLStateChanges() { return mDebugNumMajorOpenGLStateChanges; }
        public uint debugNumMinorOpenGLStateChanges() { return mDebugNumMinorOpenGLStateChanges; }
        public uint debugNumMajorStateChanges() { return mDebugNumMajorStateChanges; }
        public uint debugNumMinorStateChanges() { return mDebugNumMinorStateChanges; }
        public uint debugNumPushStateCalls() { return mDebugPushStateCalls; }

        /** Allows the UserInput to find the RenderDevice 
            @deprecated */
        public static RenderDevice lastRenderDeviceCreated;

        /**
          State change to RenderDevice.
          Use to update the state change statistics when raw OpenGL calls are made. */
        public void majStateChange(int inc = 1)
        {
            mDebugNumMajorStateChanges += (uint)inc;
        }

        /** 
          State change to RenderDevice.
         Use to update the state change statistics when raw OpenGL calls are made. */
        public void minStateChange(int inc = 1)
        {
            mDebugNumMinorStateChanges += (uint)inc;
        }

        /** 
          State change to OpenGL (possibly because of a state change to RenderDevice).
          Use to update the state change statistics when raw OpenGL calls are made. */
        public void majGLStateChange(int inc = 1)
        {
            mDebugNumMajorOpenGLStateChanges += (uint)inc;
        }

        /** 
          State change to OpenGL (possibly because of a state change to RenderDevice).
          Use to update the state change statistics when raw OpenGL calls are made. */
        public void minGLStateChange(int inc = 1)
        {
            mDebugNumMinorOpenGLStateChanges += (uint)inc;
        }

        private void recordMajorStateChange()
        {
            mDebugNumMajorStateChanges++;
            mDebugNumMajorOpenGLStateChanges++;
        }

        // Enums
        public enum DepthTest
        {
            DEPTH_GREATER, DEPTH_LESS, DEPTH_GEQUAL,
            DEPTH_LEQUAL, DEPTH_NOTEQUAL, DEPTH_EQUAL,
            DEPTH_ALWAYS_PASS, DEPTH_NEVER_PASS, DEPTH_CURRENT
        }

        public enum AlphaTest
        {
            ALPHA_GREATER, ALPHA_LESS, ALPHA_GEQUAL,
            ALPHA_LEQUAL, ALPHA_NOTEQUAL, ALPHA_EQUAL,
            ALPHA_ALWAYS_PASS, ALPHA_NEVER_PASS, ALPHA_CURRENT
        }

        public enum StencilTest
        {
            STENCIL_GREATER, STENCIL_LESS, STENCIL_GEQUAL,
            STENCIL_LEQUAL, STENCIL_NOTEQUAL, STENCIL_EQUAL,
            STENCIL_ALWAYS_PASS, STENCIL_NEVER_PASS, STENCIL_CURRENT
        }

        public enum BlendFunc
        {
            BLEND_SRC_ALPHA, BLEND_ONE_MINUS_SRC_ALPHA, BLEND_ONE,
            BLEND_ZERO, BLEND_SRC_COLOR, BLEND_DST_COLOR,
            BLEND_ONE_MINUS_SRC_COLOR, BLEND_CURRENT
        }

        public enum BlendEq
        {
            BLENDEQ_MIN, BLENDEQ_MAX, BLENDEQ_ADD,
            BLENDEQ_SUBTRACT, BLENDEQ_REVERSE_SUBTRACT,
            BLENDEQ_CURRENT
        }

        public enum StencilOp
        {
            STENCIL_INCR_WRAP, STENCIL_DECR_WRAP,
            STENCIL_KEEP, STENCIL_INCR, STENCIL_DECR,
            STENCIL_REPLACE, STENCIL_ZERO, STENCIL_INVERT,
            STENCILOP_CURRENT
        }

        public enum CullFace
        {
            CULL_FRONT, CULL_BACK, CULL_NONE,
            CULL_CURRENT
        }

        public enum ShadeMode
        {
            SHADE_FLAT, SHADE_SMOOTH, SHADE_CURRENT
        }

        public enum CombineMode
        {
            TEX_REPLACE, TEX_BLEND, TEX_INTERPOLATE, TEX_ADD, TEX_MODULATE,
            TEX_ADD_SIGNED, TEX_SUBTRACT, TEX_DOT3_RGB, TEX_DOT3_RGBA,
            TEX_CURRENT
        }

        private const int MAX_BUFFER_SIZE = 27;

        public enum Buffer
        {
            BUFFER_NONE, BUFFER_FRONT_LEFT, BUFFER_FRONT_RIGHT, BUFFER_BACK_LEFT,
            BUFFER_BACK_RIGHT, BUFFER_FRONT, BUFFER_BACK, BUFFER_LEFT, BUFFER_RIGHT,
            BUFFER_FRONT_AND_BACK, BUFFER_CURRENT,
            BUFFER_COLOR0, BUFFER_COLOR1, BUFFER_COLOR2, BUFFER_COLOR3,
            BUFFER_COLOR4, BUFFER_COLOR5, BUFFER_COLOR6, BUFFER_COLOR7,
            BUFFER_COLOR8, BUFFER_COLOR9, BUFFER_COLOR10, BUFFER_COLOR11,
            BUFFER_COLOR12, BUFFER_COLOR13, BUFFER_COLOR14, BUFFER_COLOR15
        }

        private uint[] BufferToGL = new uint[MAX_BUFFER_SIZE];

        // RenderState class
        public sealed class RenderState
        {
            public sealed class TextureUnit
            {
                public Vector4 texCoord;
                public Texture texture;
                public bool shadowMapEnabled;
                public Matrix4 shadowLightMVP = Matrix4.identity();
                public float[] textureMatrix = new float[16];
                public CombineMode combineMode;
                public float LODBias;

                public TextureUnit()
                {
                    texCoord = new Vector4(0, 0, 0, 1);
                    texture = null;
                    for (int i = 0; i < 16; ++i) textureMatrix[i] = (i % 5 == 0) ? 1.0f : 0.0f;
                    combineMode = CombineMode.TEX_MODULATE;
                    LODBias = 0.0f;
                }

                public bool Equals(TextureUnit other)
                {
                    if (other == null) return false;
                    return
                        (texCoord == other.texCoord) &&
                        (texture == other.texture) &&
                        (combineMode == other.combineMode) &&
                        (LODBias == other.LODBias) &&
                        textureMatricesEqual(textureMatrix, other.textureMatrix);
                }
            }

            public sealed class Lights
            {
                public GLight[] light = new GLight[MAX_LIGHTS];
                public bool[] lightEnabled = new bool[MAX_LIGHTS];
                public bool twoSidedLighting;
                public Color4 ambient;
                public bool lighting;
                public bool changed;

                public Lights()
                {
                    for (int i = 0; i < MAX_LIGHTS; ++i)
                    {
                        light[i] = new GLight();
                        lightEnabled[i] = false;
                    }
                    ambient = new Color4(0, 0, 0, 1);
                    lighting = false;
                    changed = true;
                }

                public bool Equals(Lights other)
                {
                    if (other == null) return false;

                    for (int L = 0; L < MAX_LIGHTS; ++L)
                    {
                        if ((lightEnabled[L] != other.lightEnabled[L]) ||
                            (lightEnabled[L] && (light[L] != other.light[L])))
                        {
                            return false;
                        }
                    }

                    Debug.debugAssertM(changed == false,
                        "Should never enter lighting comparison " +
                        "when lighting has not changed.");

                    return
                        (lighting == other.lighting) &&
                        (ambient == other.ambient) &&
                        (twoSidedLighting == other.twoSidedLighting);
                }
            }

            public sealed class Stencil
            {
                public StencilTest stencilTest;
                public int stencilReference;
                public int stencilClear;
                public StencilOp frontStencilFail;
                public StencilOp frontStencilZFail;
                public StencilOp frontStencilZPass;
                public StencilOp backStencilFail;
                public StencilOp backStencilZFail;
                public StencilOp backStencilZPass;

                public bool Equals(Stencil other)
                {
                    if (other == null) return false;
                    return
                        (stencilTest == other.stencilTest) &&
                        (stencilReference == other.stencilReference) &&
                        (stencilClear == other.stencilClear) &&
                        (frontStencilFail == other.frontStencilFail) &&
                        (frontStencilZFail == other.frontStencilZFail) &&
                        (frontStencilZPass == other.frontStencilZPass) &&
                        (backStencilFail == other.backStencilFail) &&
                        (backStencilZFail == other.backStencilZFail) &&
                        (backStencilZPass == other.backStencilZPass);
                }
            }

            public sealed class Matrices
            {
                public CoordinateFrame objectToWorldMatrix;
                public CoordinateFrame cameraToWorldMatrix;
                public CoordinateFrame cameraToWorldMatrixInverse;
                public Matrix4 projectionMatrix;
                public bool changed;

                public Matrices()
                {
                    objectToWorldMatrix = CoordinateFrame.identity();
                    cameraToWorldMatrix = CoordinateFrame.identity();
                    cameraToWorldMatrixInverse = CoordinateFrame.identity();
                    projectionMatrix = Matrix4.identity();
                    changed = true;
                }

                public bool Equals(Matrices other)
                {
                    if (other == null) return false;
                    return
                        (objectToWorldMatrix == other.objectToWorldMatrix) &&
                        (cameraToWorldMatrix == other.cameraToWorldMatrix) &&
                        (projectionMatrix == other.projectionMatrix);
                }
            }

            public Lights lights = new Lights();
            public Rect2D viewport;
            public Rect2D clip2D;
            public bool useClip2D;
            public bool depthWrite;
            public bool colorWrite;
            public bool alphaWrite;
            public Buffer drawBuffer;
            public Framebuffer framebuffer;
            public DepthTest depthTest;
            public AlphaTest alphaTest;
            public double alphaReference;
            public double depthClear;
            public Color4 colorClear;
            public CullFace cullFace;
            public Stencil stencil = new Stencil();
            public BlendFunc srcBlendFunc;
            public BlendFunc dstBlendFunc;
            public BlendEq blendEq;
            public ShadeMode shadeMode;
            public double polygonOffset;
            public RenderMode renderMode;
            public Color3 specular;
            public double shininess;
            public double lowDepthRange;
            public double highDepthRange;
            public VertexAndPixelShader vertexAndPixelShader;
            public ObjectShader objectShader;
            public Shader shader;
            public VertexProgram vertexProgram;
            public PixelProgram pixelProgram;
            public double lineWidth;
            public double pointSize;
            public Color4 color;
            public Vector3 normal;
            public int highestTextureUnitThatChanged;
            public TextureUnit[] textureUnit;
            public Matrices matrices = new Matrices();
            public VAR vertexArray;
            public VAR normalArray;
            public VAR colorArray;
            public VAR[] texCoordArray;
            public VAR indexArray;
            public VAR[] vertexAttribArray;

            public RenderState(int width = 1, int height = 1, int maxTextureUnits = GLCaps.G3D_MAX_TEXTURE_UNITS)
            {
                viewport = new Rect2D(0, 0, width, height);
                clip2D = new Rect2D(0, 0, width, height);
                useClip2D = false;
                depthWrite = true;
                colorWrite = true;
                alphaWrite = false;
                drawBuffer = Buffer.BUFFER_BACK;
                framebuffer = null;
                depthTest = DepthTest.DEPTH_LEQUAL;
                alphaTest = AlphaTest.ALPHA_ALWAYS_PASS;
                alphaReference = 0.0;
                depthClear = 1.0;
                colorClear = new Color4(0, 0, 0, 1);
                cullFace = CullFace.CULL_BACK;
                stencil.stencilTest = StencilTest.STENCIL_ALWAYS_PASS;
                stencil.stencilReference = 0;
                stencil.stencilClear = 0;
                stencil.frontStencilFail = StencilOp.STENCIL_KEEP;
                stencil.frontStencilZFail = StencilOp.STENCIL_KEEP;
                stencil.frontStencilZPass = StencilOp.STENCIL_KEEP;
                stencil.backStencilFail = StencilOp.STENCIL_KEEP;
                stencil.backStencilZFail = StencilOp.STENCIL_KEEP;
                stencil.backStencilZPass = StencilOp.STENCIL_KEEP;
                srcBlendFunc = BlendFunc.BLEND_ONE;
                dstBlendFunc = BlendFunc.BLEND_ZERO;
                blendEq = BlendEq.BLENDEQ_ADD;
                shadeMode = ShadeMode.SHADE_FLAT;
                polygonOffset = 0.0;
                renderMode = RenderMode.RENDER_SOLID;
                specular = new Color3(0.8f, 0.8f, 0.8f);
                shininess = 15.0;
                lowDepthRange = 0.0;
                highDepthRange = 1.0;
                vertexAndPixelShader = null;
                objectShader = null;
                shader = null;
                vertexProgram = null;
                pixelProgram = null;
                lineWidth = 1.0;
                pointSize = 1.0;
                color = new Color4(1, 1, 1, 1);
                normal = new Vector3(0, 0, 0);
                highestTextureUnitThatChanged = maxTextureUnits;
                textureUnit = new TextureUnit[GLCaps.G3D_MAX_TEXTURE_UNITS];
                for (int i = 0; i < textureUnit.Length; ++i)
                {
                    textureUnit[i] = new TextureUnit();
                }
                texCoordArray = new VAR[GLCaps.G3D_MAX_TEXTURE_UNITS];
                vertexAttribArray = new VAR[16];

                lights.twoSidedLighting = false;
                lights.ambient = new Color4(0.25f, 0.25f, 0.25f, 1.0f);
                lights.lighting = false;
                for (int i = 0; i < MAX_LIGHTS; ++i)
                {
                    lights.lightEnabled[i] = false;
                }

                double aspect = (double)viewport.width() / viewport.height();
                matrices.projectionMatrix = Matrix4.perspectiveProjection(
                    (float)-aspect, (float)aspect, -1.0f, 1.0f, 0.1f, 100.0f);
            }

            public void touchedTextureUnit(int u)
            {
                highestTextureUnitThatChanged = Math.Max(highestTextureUnitThatChanged, u);
            }

            /**
              Copies the contents of another state into this one, reusing the
              existing nested objects so that state snapshots can be pooled.
             */
            public void copyFrom(RenderState src)
            {
                viewport = src.viewport;
                clip2D = src.clip2D;
                useClip2D = src.useClip2D;
                depthWrite = src.depthWrite;
                colorWrite = src.colorWrite;
                alphaWrite = src.alphaWrite;
                drawBuffer = src.drawBuffer;
                framebuffer = src.framebuffer;
                depthTest = src.depthTest;
                alphaTest = src.alphaTest;
                alphaReference = src.alphaReference;
                depthClear = src.depthClear;
                colorClear = src.colorClear;
                cullFace = src.cullFace;
                stencil.stencilTest = src.stencil.stencilTest;
                stencil.stencilReference = src.stencil.stencilReference;
                stencil.stencilClear = src.stencil.stencilClear;
                stencil.frontStencilFail = src.stencil.frontStencilFail;
                stencil.frontStencilZFail = src.stencil.frontStencilZFail;
                stencil.frontStencilZPass = src.stencil.frontStencilZPass;
                stencil.backStencilFail = src.stencil.backStencilFail;
                stencil.backStencilZFail = src.stencil.backStencilZFail;
                stencil.backStencilZPass = src.stencil.backStencilZPass;
                srcBlendFunc = src.srcBlendFunc;
                dstBlendFunc = src.dstBlendFunc;
                blendEq = src.blendEq;
                shadeMode = src.shadeMode;
                polygonOffset = src.polygonOffset;
                renderMode = src.renderMode;
                specular = src.specular;
                shininess = src.shininess;
                lowDepthRange = src.lowDepthRange;
                highDepthRange = src.highDepthRange;
                vertexAndPixelShader = src.vertexAndPixelShader;
                objectShader = src.objectShader;
                shader = src.shader;
                vertexProgram = src.vertexProgram;
                pixelProgram = src.pixelProgram;
                lineWidth = src.lineWidth;
                pointSize = src.pointSize;
                color = src.color;
                normal = src.normal;
                highestTextureUnitThatChanged = src.highestTextureUnitThatChanged;
                vertexArray = src.vertexArray;
                normalArray = src.normalArray;
                indexArray = src.indexArray;
                colorArray = src.colorArray;

                for (int i = 0; i < texCoordArray.Length; ++i)
                {
                    texCoordArray[i] = (i < src.texCoordArray.Length) ? src.texCoordArray[i] : null;
                }

                for (int i = 0; i < vertexAttribArray.Length; ++i)
                {
                    vertexAttribArray[i] = (i < src.vertexAttribArray.Length) ? src.vertexAttribArray[i] : null;
                }

                for (int i = 0; i < MAX_LIGHTS; ++i)
                {
                    if (src.lights.light[i] == null)
                    {
                        lights.light[i] = new GLight();
                    }
                    else
                    {
                        lights.light[i] = new GLight(src.lights.light[i]);
                    }
                    lights.lightEnabled[i] = src.lights.lightEnabled[i];
                }
                lights.twoSidedLighting = src.lights.twoSidedLighting;
                lights.ambient = src.lights.ambient;
                lights.lighting = src.lights.lighting;
                lights.changed = src.lights.changed;

                matrices.objectToWorldMatrix = src.matrices.objectToWorldMatrix;
                matrices.cameraToWorldMatrix = src.matrices.cameraToWorldMatrix;
                matrices.cameraToWorldMatrixInverse = src.matrices.cameraToWorldMatrixInverse;
                matrices.projectionMatrix = src.matrices.projectionMatrix;
                matrices.changed = src.matrices.changed;

                for (int i = 0; i < textureUnit.Length; ++i)
                {
                    RenderState.TextureUnit dstUnit = textureUnit[i];
                    RenderState.TextureUnit srcUnit = src.textureUnit[i];
                    if (dstUnit == null)
                    {
                        dstUnit = new RenderState.TextureUnit();
                        textureUnit[i] = dstUnit;
                    }
                    dstUnit.texCoord = srcUnit.texCoord;
                    dstUnit.texture = srcUnit.texture;
                    dstUnit.shadowMapEnabled = srcUnit.shadowMapEnabled;
                    dstUnit.shadowLightMVP = srcUnit.shadowLightMVP;
                    dstUnit.combineMode = srcUnit.combineMode;
                    dstUnit.LODBias = srcUnit.LODBias;
                    Array.Copy(srcUnit.textureMatrix, dstUnit.textureMatrix, 16);
                }
            }

            public bool Equals(RenderState other)
            {
                if (other == null) return false;

                if (viewport != other.viewport ||
                    clip2D != other.clip2D ||
                    useClip2D != other.useClip2D ||
                    depthWrite != other.depthWrite ||
                    colorWrite != other.colorWrite ||
                    alphaWrite != other.alphaWrite ||
                    drawBuffer != other.drawBuffer ||
                    !ReferenceEquals(framebuffer, other.framebuffer) ||
                    depthTest != other.depthTest ||
                    alphaTest != other.alphaTest ||
                    alphaReference != other.alphaReference ||
                    depthClear != other.depthClear ||
                    colorClear != other.colorClear ||
                    cullFace != other.cullFace ||
                    !stencil.Equals(other.stencil) ||
                    srcBlendFunc != other.srcBlendFunc ||
                    dstBlendFunc != other.dstBlendFunc ||
                    blendEq != other.blendEq ||
                    shadeMode != other.shadeMode ||
                    polygonOffset != other.polygonOffset ||
                    renderMode != other.renderMode ||
                    specular != other.specular ||
                    shininess != other.shininess ||
                    lowDepthRange != other.lowDepthRange ||
                    highDepthRange != other.highDepthRange ||
                    !ReferenceEquals(vertexAndPixelShader, other.vertexAndPixelShader) ||
                    !ReferenceEquals(objectShader, other.objectShader) ||
                    !ReferenceEquals(shader, other.shader) ||
                    !ReferenceEquals(vertexProgram, other.vertexProgram) ||
                    !ReferenceEquals(pixelProgram, other.pixelProgram) ||
                    lineWidth != other.lineWidth ||
                    pointSize != other.pointSize ||
                    color != other.color ||
                    normal != other.normal ||
                    !matrices.Equals(other.matrices))
                {
                    return false;
                }

                for (int i = 0; i < MAX_LIGHTS; ++i)
                {
                    if (lights.lightEnabled[i] != other.lights.lightEnabled[i])
                    {
                        return false;
                    }
                    if (lights.lightEnabled[i] && (lights.light[i] != other.lights.light[i]))
                    {
                        return false;
                    }
                }

                if (lights.lighting != other.lights.lighting ||
                    lights.ambient != other.lights.ambient ||
                    lights.twoSidedLighting != other.lights.twoSidedLighting)
                {
                    return false;
                }

                for (int i = 0; i < textureUnit.Length; ++i)
                {
                    if (!textureUnit[i].Equals(other.textureUnit[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public RenderDevice()
        {
            backend = null;
            _window = null;
            debugLog = null;
            generation = 0;
            stencilBits = 0;
            depthBits = 0;
            lightSaturation = 1.0;
            beginEndFrame = 0;
            _swapBuffersAutomatically = true;
            swapGLBuffersPending = false;
            lastTime = System.getTick();
            currentPrimitive = Primitive.TRIANGLES;
            currentPrimitiveVertexCount = 0;
            currentVARArea = null;
            triangleCount = 0;
            emwaFrameRate = 0.0;
            emwaTriangleCount = 0.0;
            emwaTriangleRate = 0.0;
            cardDescription = "";
            mDebugNumMajorOpenGLStateChanges = 0;
            mDebugNumMinorOpenGLStateChanges = 0;
            mDebugNumMajorStateChanges = 0;
            mDebugNumMinorStateChanges = 0;
            mDebugPushStateCalls = 0;
            inPrimitive = false;
            inShader = false;
            inIndexedPrimitive = false;
            _numTextureUnits = 0;
            _numTextures = 0;
            _numTextureCoords = 0;
            _initialized = false;
            cleanedup = false;
            brightScale = 1.0;
            immediateIndexVARCapacity = 0;
            deleteWindow = false;
            for (int i = 0; i < GLCaps.G3D_MAX_TEXTURE_UNITS; ++i)
            {
                currentlyBoundTexture[i] = 0;
            }
            state = new RenderState();
            stateStack = new List<RenderState>();
            statePool = new Stack<RenderState>();
            lastRenderDeviceCreated = this;
        }

        public void Dispose()
        {
            cleanup();
        }

        /**
          Prints a human-readable description of this machine
          to the text output stream.
        */
public void describeSystem(TextOutput t)
        {
            // GPU section
            t.writeSymbol("GPU");
            t.writeSymbol("{");
            t.writeNewline();
            t.pushIndent();

            string renderer;
            string vendor;
            string driver;
            string version;
            int textures;
            int texCoords;
            int texUnits;

            if (backend == null)
            {
                renderer = "(no graphics backend)";
                vendor = "(no graphics backend)";
                driver = "(no graphics backend)";
                version = "(no graphics backend)";
                textures = 0;
                texCoords = 0;
                texUnits = 0;
            }
            else
            {
                renderer = backend.GetRenderer();
                vendor = backend.GetVendor();
                driver = backend.GetDriverVersion();
                version = backend.GetGLVersion();
                textures = backend.GetMaxTextures();
                texCoords = backend.GetMaxTextureCoords();
                texUnits = backend.GetMaxTextureUnits();
            }

            t.writeSymbol("Chipset");
            t.writeSymbol("=");
            t.writeString(renderer);
            t.writeNewline();

            t.writeSymbol("Vendor");
            t.writeSymbol("=");
            t.writeString(vendor);
            t.writeNewline();

            t.writeSymbol("Driver");
            t.writeSymbol("=");
            t.writeString(driver);
            t.writeNewline();

            t.writeSymbol("OpenGL version");
            t.writeSymbol("=");
            t.writeString(version);
            t.writeNewline();

            t.writeSymbol("Textures");
            t.writeSymbol("=");
            t.writeNumber(textures);
            t.writeNewline();

            t.writeSymbol("Texture coordinates");
            t.writeSymbol("=");
            t.writeNumber(texCoords);
            t.writeNewline();

            t.writeSymbol("Texture units");
            t.writeSymbol("=");
            t.writeNumber(texUnits);
            t.writeNewline();

            t.popIndent();
            t.writeSymbol("}");
            t.writeNewline();
            t.writeNewline();

            // Window section
            GWindow w = _window as GWindow;
            if (w != null)
            {
                GWindow.Settings settings = w.getSettings();

                t.writeSymbol("Window");
                t.writeSymbol("{");
                t.writeNewline();
                t.pushIndent();

                t.writeSymbol("API");
                t.writeSymbol("=");
                t.writeString(w.getAPIName());
                t.writeNewline();

                t.writeSymbol("Version");
                t.writeSymbol("=");
                t.writeString(w.getAPIVersion());
                t.writeNewline();
                t.writeNewline();

                t.writeSymbol("In focus");
                t.writeSymbol("=");
                t.writeSymbol(w.hasFocus() ? "Yes" : "No");
                t.writeNewline();

                t.writeSymbol("Centered");
                t.writeSymbol("=");
                t.writeSymbol(settings.centered ? "Yes" : "No");
                t.writeNewline();

                t.writeSymbol("Framed");
                t.writeSymbol("=");
                t.writeSymbol(settings.framed ? "Yes" : "No");
                t.writeNewline();

                t.writeSymbol("Visible");
                t.writeSymbol("=");
                t.writeSymbol(settings.visible ? "Yes" : "No");
                t.writeNewline();

                t.writeSymbol("Resizable");
                t.writeSymbol("=");
                t.writeSymbol(settings.resizable ? "Yes" : "No");
                t.writeNewline();

                t.writeSymbol("Full screen");
                t.writeSymbol("=");
                t.writeSymbol(settings.fullScreen ? "Yes" : "No");
                t.writeNewline();

                t.writeSymbol("Top");
                t.writeSymbol("=");
                t.writeNumber(settings.y);
                t.writeNewline();

                t.writeSymbol("Left");
                t.writeSymbol("=");
                t.writeNumber(settings.x);
                t.writeNewline();

                t.writeSymbol("Width");
                t.writeSymbol("=");
                t.writeNumber(settings.width);
                t.writeNewline();

                t.writeSymbol("Height");
                t.writeSymbol("=");
                t.writeNumber(settings.height);
                t.writeNewline();

                t.writeSymbol("Refresh rate");
                t.writeSymbol("=");
                t.writeNumber(settings.refreshRate);
                t.writeNewline();
                t.writeNewline();

                t.writeSymbol("Alpha bits");
                t.writeSymbol("=");
                t.writeNumber(settings.alphaBits);
                t.writeNewline();

                t.writeSymbol("Red bits");
                t.writeSymbol("=");
                t.writeNumber(settings.rgbBits);
                t.writeNewline();

                t.writeSymbol("Green bits");
                t.writeSymbol("=");
                t.writeNumber(settings.rgbBits);
                t.writeNewline();

                t.writeSymbol("Blue bits");
                t.writeSymbol("=");
                t.writeNumber(settings.rgbBits);
                t.writeNewline();

                t.writeSymbol("Depth bits");
                t.writeSymbol("=");
                t.writeNumber(settings.depthBits);
                t.writeNewline();

                t.writeSymbol("Stencil bits");
                t.writeSymbol("=");
                t.writeNumber(settings.stencilBits);
                t.writeNewline();

                t.writeSymbol("Asynchronous");
                t.writeSymbol("=");
                t.writeSymbol(settings.asychronous ? "Yes" : "No");
                t.writeNewline();

                t.writeSymbol("Stereo");
                t.writeSymbol("=");
                t.writeSymbol(settings.stereo ? "Yes" : "No");
                t.writeNewline();

                t.writeSymbol("FSAA samples");
                t.writeSymbol("=");
                t.writeNumber(settings.fsaaSamples);
                t.writeNewline();

                t.popIndent();
                t.writeSymbol("}");
                t.writeNewline();
                t.writeNewline();
            }
        }

        public void describeSystem(ref string s)
        {
            TextOutput t = new TextOutput(new TextOutput.Options());
            describeSystem(t);
            s = t.commitString();
        }

        /**
          Checkmarks all RenderDevice state (anything that can be set
          using RenderDevice methods).
        */
        public void pushState()
        {
            Debug.debugAssert(!inPrimitive);

            mDebugPushStateCalls++;

            // Push a copy of the current state; the current state remains the
            // working copy so that changes to it are not visible to the
            // pushed state.
            stateStack.Add(cloneState(state));

            // Record that that the lights and matrices are unchanged since the previous state.
            // This allows popState to restore the lighting environment efficiently.

            state.lights.changed = false;
            state.matrices.changed = false;
            state.highestTextureUnitThatChanged = -1;
        }

        private RenderState cloneState(RenderState src)
        {
            RenderState dst;
            if (statePool.Count > 0)
            {
                dst = statePool.Pop();
            }
            else
            {
                dst = new RenderState(1, 1, GLCaps.G3D_MAX_TEXTURE_UNITS);
            }
            dst.copyFrom(src);
            return dst;
        }

        /**
          Sets all state to a clean rendering environment.
        */
        public void resetState()
        {
            Debug.debugAssertM(stateStack.Count == 0,
                "Cannot call setVideoMode between pushState and popState");
            Debug.debugAssertM(beginEndFrame == 0,
                "Cannot call setVideoMode between beginFrame and endFrame");

            stateStack.Clear();
            statePool.Clear();

            state = new RenderState(width(), height(),
                Math.Max(_numTextures, _numTextureCoords) - 1);

            if (backend == null)
            {
                return;
            }

            backend.SetColorClearValue(state.colorClear);
            backend.SetDepthClearValue(state.depthClear);
            backend.SetStencilClearValue(state.stencil.stencilClear);
            backend.SetViewport(state.viewport);
            backend.SetScissor(state.clip2D, false);
            backend.SetDepthTest(state.depthTest);
            backend.SetDepthWrite(state.depthWrite);
            backend.SetColorWrite(state.colorWrite);
            backend.SetAlphaWrite(state.alphaWrite);
            backend.SetCullFace(state.cullFace);
            backend.SetShadeMode(state.shadeMode);
            backend.SetBlendFunc(state.srcBlendFunc, state.dstBlendFunc, state.blendEq);
            backend.SetRenderMode(state.renderMode);
            backend.SetPolygonOffset(state.polygonOffset);
            backend.SetLineWidth(state.lineWidth);
            backend.SetPointSize(state.pointSize);
            backend.SetDepthRange(state.lowDepthRange, state.highDepthRange);
            backend.SetLightingEnabled(state.lights.lighting);
            backend.SetTwoSidedLighting(state.lights.twoSidedLighting);
            backend.SetAmbientLight(state.lights.ambient);
            backend.SetSpecular(state.specular);
            backend.SetShininess((float)state.shininess);
            backend.SetProjectionMatrix(state.matrices.projectionMatrix);
            backend.SetModelViewMatrix(
                (state.matrices.cameraToWorldMatrixInverse * state.matrices.objectToWorldMatrix).toMatrix4());

            for (int i = 0; i < MAX_LIGHTS; ++i)
            {
                backend.SetLight(i, new GLight(), false);
                currentlyBoundTexture[i] = 0;
            }

            for (int i = 0; i < state.textureUnit.Length; ++i)
            {
                state.textureUnit[i] = new RenderState.TextureUnit();
                backend.BindTexture(i, 0, Texture.Dimension.DIM_2D);
                backend.SetTextureMatrix(i, matrixFromArray(state.textureUnit[i].textureMatrix));
                backend.SetTextureCombineMode(i, state.textureUnit[i].combineMode);
                backend.SetTextureLODBias(i, state.textureUnit[i].LODBias);
            }
        }

        /**
          Restores all state to whatever was pushed previously.  Push and
          pop must be used in matching pairs.
        */
        public void popState()
        {
            Debug.debugAssert(!inPrimitive);
            Debug.debugAssertM(stateStack.Count > 0,
                "More calls to RenderDevice::pushState() than RenderDevice::popState().");

            RenderState newState = stateStack[stateStack.Count - 1];
            stateStack.RemoveAt(stateStack.Count - 1);

            setState(newState);
        }

        /**
          Applies a previously pushed state to the render device, forwarding
          every change to the graphics backend.
        */
        private void setState(RenderState newState)
        {
            // The state change checks inside the individual
            // methods will (for the most part) minimize
            // the state changes so we can set all of the
            // new state explicitly.

            // Set framebuffer first, since it can affect the viewport
            if (!ReferenceEquals(state.framebuffer, newState.framebuffer))
            {
                setFramebuffer(newState.framebuffer);

                // Intentionally corrupt the viewport, forcing renderdevice to reset it
                state.viewport = Rect2D.xywh(-1, -1, -1, -1);
            }

            setViewport(newState.viewport);

            if (newState.useClip2D)
            {
                enableClip2D(newState.clip2D);
            }
            else
            {
                disableClip2D();
            }

            if (newState.depthWrite)
            {
                enableDepthWrite();
            }
            else
            {
                disableDepthWrite();
            }

            if (newState.colorWrite)
            {
                enableColorWrite();
            }
            else
            {
                disableColorWrite();
            }

            if (newState.alphaWrite)
            {
                enableAlphaWrite();
            }
            else
            {
                disableAlphaWrite();
            }

            setDrawBuffer(newState.drawBuffer);

            setShadeMode(newState.shadeMode);
            setDepthTest(newState.depthTest);

            if (!newState.stencil.Equals(state.stencil))
            {
                setStencilConstant(newState.stencil.stencilReference);
                setStencilTest(newState.stencil.stencilTest);
                setStencilOp(
                    newState.stencil.frontStencilFail, newState.stencil.frontStencilZFail, newState.stencil.frontStencilZPass,
                    newState.stencil.backStencilFail, newState.stencil.backStencilZFail, newState.stencil.backStencilZPass);
                setStencilClearValue(newState.stencil.stencilClear);
            }

            setDepthClearValue(newState.depthClear);

            setColorClearValue(newState.colorClear);

            setAlphaTest(newState.alphaTest, newState.alphaReference);

            setBlendFunc(newState.srcBlendFunc, newState.dstBlendFunc, newState.blendEq);

            setRenderMode(newState.renderMode);
            setPolygonOffset(newState.polygonOffset);
            setLineWidth(newState.lineWidth);
            setPointSize(newState.pointSize);

            setSpecularCoefficient(newState.specular);
            setShininess((float)newState.shininess);

            if (state.lights.changed)
            {
                if (newState.lights.lighting)
                {
                    enableLighting();
                }
                else
                {
                    disableLighting();
                }

                if (newState.lights.twoSidedLighting)
                {
                    enableTwoSidedLighting();
                }
                else
                {
                    disableTwoSidedLighting();
                }

                setAmbientLightColor(newState.lights.ambient);

                for (int i = 0; i < MAX_LIGHTS; ++i)
                {
                    if (newState.lights.lightEnabled[i])
                    {
                        setLight(i, newState.lights.light[i]);
                    }
                    else
                    {
                        setLight(i, (object)null);
                    }
                }
            }

            setColor(newState.color);
            setNormal(newState.normal);

            for (int u = state.highestTextureUnitThatChanged; u >= 0; --u)
            {
                bool wasShadowMapEnabled = state.textureUnit[u].shadowMapEnabled;
                Matrix4 wasShadowLightMVP = state.textureUnit[u].shadowLightMVP;
                Texture wasShadowTexture = state.textureUnit[u].texture;

                if (!newState.textureUnit[u].Equals(state.textureUnit[u]))
                {
                    if (u < (int)numTextures())
                    {
                        setTexture((uint)u, newState.textureUnit[u].texture);

                        if (u < (int)numTextureUnits())
                        {
                            setTextureCombineMode((uint)u, newState.textureUnit[u].combineMode);
                            setTextureMatrix((uint)u, newState.textureUnit[u].textureMatrix);
                            setTextureLODBias((uint)u, newState.textureUnit[u].LODBias);
                        }
                    }
                    setTexCoord((uint)u, newState.textureUnit[u].texCoord);
                }

                state.textureUnit[u].shadowMapEnabled = newState.textureUnit[u].shadowMapEnabled;
                state.textureUnit[u].shadowLightMVP = newState.textureUnit[u].shadowLightMVP;

                applyShadowMapState(u, wasShadowMapEnabled, wasShadowLightMVP, wasShadowTexture);
            }

            setCullFace(newState.cullFace);

            setDepthRange(newState.lowDepthRange, newState.highDepthRange);

            if (state.matrices.changed)
            {
                if (newState.matrices.cameraToWorldMatrix != state.matrices.cameraToWorldMatrix)
                {
                    setCameraToWorldMatrix(newState.matrices.cameraToWorldMatrix);
                }

                if (newState.matrices.objectToWorldMatrix != state.matrices.objectToWorldMatrix)
                {
                    setObjectToWorldMatrix(newState.matrices.objectToWorldMatrix);
                }

                setProjectionMatrix(newState.matrices.projectionMatrix);
            }

            setVertexAndPixelShader(newState.vertexAndPixelShader);
            setObjectShader(newState.objectShader);
            setShader(newState.shader);

            if (supportsVertexProgram())
            {
                setVertexProgram(newState.vertexProgram);
            }

            if (supportsPixelProgram())
            {
                setPixelProgram(newState.pixelProgram);
            }

            // Adopt the popped state's deltas relative the state that it replaced.
            state.highestTextureUnitThatChanged = newState.highestTextureUnitThatChanged;
            state.matrices.changed = newState.matrices.changed;
            state.lights.changed = newState.lights.changed;
        }

        private static bool rectsEqual(Rect2D a, Rect2D b)
        {
            return a.x0() == b.x0() && a.y0() == b.y0() &&
                   a.x1() == b.x1() && a.y1() == b.y1();
        }

        private static bool colorsEqual(Color4 a, Color4 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        private static bool lightsEqual(GLight a, GLight b)
        {
            return a == b;
        }

        private static bool textureMatricesEqual(float[] a, float[] b)
        {
            for (int i = 0; i < 16; ++i)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static Matrix4 matrixFromArray(float[] values)
        {
            Matrix4 m = Matrix4.identity();
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    m.setElt(r, c, values[r * 4 + c]);
                }
            }
            return m;
        }

        /**
          Restores the shadow map configuration of a texture unit when the
          texture unit state changes.
         */
        private void applyShadowMapState(int unit, bool wasEnabled, Matrix4 wasLightMVP, Texture wasTexture)
        {
            if (backend == null)
            {
                return;
            }

            RenderState.TextureUnit restoredUnit = state.textureUnit[unit];

            if (wasEnabled != restoredUnit.shadowMapEnabled ||
                wasLightMVP != restoredUnit.shadowLightMVP ||
                wasTexture != restoredUnit.texture)
            {
                if (restoredUnit.shadowMapEnabled && restoredUnit.texture != null)
                {
                    backend.ConfigureShadowMap(unit, restoredUnit.shadowLightMVP,
                                               state.matrices.cameraToWorldMatrixInverse.toMatrix4(),
                                               restoredUnit.texture.getBackendTextureID(backend),
                                               restoredUnit.texture.depthReadMode());
                }
                else
                {
                    backend.DisableShadowMap(unit);
                }
            }
        }

        /** To clear the alpha portion of the color buffer, remember to
            enable alpha write */
        public void clear(bool clearColor, bool clearDepth, bool clearStencil)
        {
            Debug.debugAssert(!inPrimitive);

            if (backend == null)
            {
                return;
            }

            majStateChange();
            majGLStateChange();

            backend.SetColorClearValue(state.colorClear);
            backend.SetDepthClearValue(state.depthClear);
            backend.SetStencilClearValue(state.stencil.stencilClear);

            // The color and depth buffers are only cleared if writes are enabled
            bool oldColorWrite = state.colorWrite;
            bool oldDepthWrite = state.depthWrite;
            if (clearColor && !oldColorWrite)
            {
                state.colorWrite = true;
                backend.SetColorWrite(true);
            }
            if (clearDepth && !oldDepthWrite)
            {
                state.depthWrite = true;
                backend.SetDepthWrite(true);
            }

            backend.Clear(clearColor, clearDepth, clearStencil);

            if (clearColor && !oldColorWrite)
            {
                state.colorWrite = oldColorWrite;
                backend.SetColorWrite(oldColorWrite);
            }
            if (clearDepth && !oldDepthWrite)
            {
                state.depthWrite = oldDepthWrite;
                backend.SetDepthWrite(oldDepthWrite);
            }
        }

        public void setClearColor(Color4 c)
        {
            state.colorClear = c;
            if (backend != null)
            {
                backend.SetColorClearValue(c);
            }
        }

        public void setClearColor(Color3 c)
        {
            setClearColor(new Color4(c, 1.0f));
        }

        public void setClearDepth(double d)
        {
            state.depthClear = d;
            if (backend != null)
            {
                backend.SetDepthClearValue(d);
            }
        }

        /** Clears color, depth, and stencil. */
        public void clear()
        {
            clear(true, true, true);
        }

        public void setDepthTest(DepthTest test)
        {
            Debug.debugAssert(!inPrimitive);

            minStateChange();

            if (test == DepthTest.DEPTH_CURRENT)
            {
                return;
            }

            if (state.depthTest != test)
            {
                state.depthTest = test;
                if (backend != null)
                {
                    backend.SetDepthTest(test);
                }
                minGLStateChange();
            }
        }

        public void setStencilTest(StencilTest test)
        {
            minStateChange();

            if (test == StencilTest.STENCIL_CURRENT)
            {
                return;
            }

            Debug.debugAssert(!inPrimitive);

            if (state.stencil.stencilTest != test)
            {
                state.stencil.stencilTest = test;
                if (backend != null)
                {
                    backend.SetStencilTest(test, state.stencil.stencilReference);
                }
                minGLStateChange();
            }
        }

        public void setRenderMode(RenderMode mode)
        {
            minStateChange();

            if (mode == RenderMode.RENDER_CURRENT)
            {
                return;
            }

            if (state.renderMode != mode)
            {
                state.renderMode = mode;
                if (backend != null)
                {
                    backend.SetRenderMode(mode);
                }
                minGLStateChange();
            }
        }

        public RenderMode renderMode()
        {
            return state.renderMode;
        }

        public void setStencilConstant(int reference)
        {
            minStateChange();

            Debug.debugAssert(!inPrimitive);

            if (state.stencil.stencilReference != reference)
            {
                state.stencil.stencilReference = reference;
                if (backend != null)
                {
                    backend.SetStencilTest(state.stencil.stencilTest, reference);
                }
                minStateChange();
            }
        }

        public void setAlphaTest(AlphaTest test, double reference)
        {
            Debug.debugAssert(!inPrimitive);

            minStateChange();

            if (test == AlphaTest.ALPHA_CURRENT)
            {
                return;
            }

            if ((state.alphaTest != test) || (state.alphaReference != reference))
            {
                state.alphaTest = test;
                state.alphaReference = reference;
                if (backend != null)
                {
                    if (test == AlphaTest.ALPHA_ALWAYS_PASS && !backend.Caps.SupportsAlphaTest)
                    {
                        minGLStateChange();
                    }
                    else
                    {
                        backend.SetAlphaTest(test, reference);
                        minGLStateChange();
                    }
                }
            }
        }

        public AlphaTest alphaTest()
        {
            return state.alphaTest;
        }

        public double alphaTestReference()
        {
            return state.alphaReference;
        }

        public void setDrawBuffer(Buffer drawBuffer)
        {
            minStateChange();

            if (drawBuffer == Buffer.BUFFER_CURRENT) return;

            if (state.framebuffer == null)
            {
                Debug.alwaysAssertM((drawBuffer >= Buffer.BUFFER_NONE) && (drawBuffer <= Buffer.BUFFER_CURRENT),
                    "Drawing to a color buffer is only supported by application-created framebuffers!");
            }
            else
            {
                Debug.alwaysAssertM((drawBuffer >= Buffer.BUFFER_CURRENT) && (drawBuffer <= Buffer.BUFFER_COLOR15),
                    "Visible buffers cannot be specified for application-created framebuffers!");
            }

            if (drawBuffer != state.drawBuffer)
            {
                state.drawBuffer = drawBuffer;
                if (backend != null)
                {
                    backend.SetDrawBuffer(drawBuffer);
                }
                minGLStateChange();
            }
        }

        public Buffer drawBuffer()
        {
            return state.drawBuffer;
        }

        public void setDepthRange(double low, double high)
        {
            majStateChange();

            if ((state.lowDepthRange != low) || (state.highDepthRange != high))
            {
                state.lowDepthRange = low;
                state.highDepthRange = high;
                if (backend != null)
                {
                    backend.SetDepthRange(low, high);
                }
                minGLStateChange();
            }
        }

        public void setColorWrite(bool b)
        {
            if (b)
            {
                enableColorWrite();
            }
            else
            {
                disableColorWrite();
            }
        }

        public bool colorWrite()
        {
            return state.colorWrite;
        }

        /** @deprecated Use setColorWrite */
        public void enableColorWrite()
        {
            Debug.debugAssert(!inPrimitive);
            minStateChange();
            if (!state.colorWrite)
            {
                state.colorWrite = true;
                if (backend != null)
                {
                    backend.SetColorWrite(true);
                    backend.SetAlphaWrite(state.alphaWrite);
                }
                minGLStateChange();
            }
        }

        /** @deprecated Use setColorWrite */
        public void disableColorWrite()
        {
            Debug.debugAssert(!inPrimitive);
            minStateChange();
            if (state.colorWrite)
            {
                state.colorWrite = false;
                if (backend != null)
                {
                    backend.SetColorWrite(false);
                    backend.SetAlphaWrite(state.alphaWrite);
                }
                minGLStateChange();
            }
        }

        public void setAlphaWrite(bool b)
        {
            if (b)
            {
                enableAlphaWrite();
            }
            else
            {
                disableAlphaWrite();
            }
        }

        public bool alphaWrite()
        {
            return state.alphaWrite;
        }

        /** @deprecated Use setAlphaWrite */
        public void enableAlphaWrite()
        {
            Debug.debugAssert(!inPrimitive);
            minStateChange();
            if (!state.alphaWrite)
            {
                state.alphaWrite = true;
                if (backend != null)
                {
                    backend.SetAlphaWrite(true);
                }
                minGLStateChange();
            }
        }

        /** @deprecated Use setAlphaWrite */
        public void disableAlphaWrite()
        {
            Debug.debugAssert(!inPrimitive);
            minStateChange();
            if (state.alphaWrite)
            {
                state.alphaWrite = false;
                if (backend != null)
                {
                    backend.SetAlphaWrite(false);
                }
                minGLStateChange();
            }
        }

        public void setDepthWrite(bool b)
        {
            if (b)
            {
                enableDepthWrite();
            }
            else
            {
                disableDepthWrite();
            }
        }

        public bool depthWrite()
        {
            return state.depthWrite;
        }

        /** @deprecated Use setDepthWrite */
        public void enableDepthWrite()
        {
            Debug.debugAssert(!inPrimitive);
            minStateChange();
            if (!state.depthWrite)
            {
                state.depthWrite = true;
                if (backend != null)
                {
                    backend.SetDepthWrite(true);
                }
                minGLStateChange();
            }
        }

        /** @deprecated Use setDepthWrite */
        public void disableDepthWrite()
        {
            Debug.debugAssert(!inPrimitive);
            minStateChange();
            if (state.depthWrite)
            {
                state.depthWrite = false;
                if (backend != null)
                {
                    backend.SetDepthWrite(false);
                }
                minGLStateChange();
            }
        }

        public void setShadeMode(ShadeMode s)
        {
            minStateChange();
            if (state.shadeMode != s)
            {
                state.shadeMode = s;
                if (backend != null)
                {
                    backend.SetShadeMode(s);
                }
                minGLStateChange();
            }
        }

        public void setStencilOp(
            StencilOp fail,
            StencilOp zfail,
            StencilOp zpass)
        {
            Debug.debugAssert(!inPrimitive);

            setStencilOp(fail, zfail, zpass, fail, zfail, zpass);
        }

        public void setStencilOp(
            StencilOp frontStencilFail,
            StencilOp frontZFail,
            StencilOp frontZPass,
            StencilOp backStencilFail,
            StencilOp backZFail,
            StencilOp backZPass)
        {
            minStateChange();

            if (frontStencilFail == StencilOp.STENCILOP_CURRENT)
            {
                frontStencilFail = state.stencil.frontStencilFail;
            }

            if (frontZFail == StencilOp.STENCILOP_CURRENT)
            {
                frontZFail = state.stencil.frontStencilZFail;
            }

            if (frontZPass == StencilOp.STENCILOP_CURRENT)
            {
                frontZPass = state.stencil.frontStencilZPass;
            }

            if (backStencilFail == StencilOp.STENCILOP_CURRENT)
            {
                backStencilFail = state.stencil.backStencilFail;
            }

            if (backZFail == StencilOp.STENCILOP_CURRENT)
            {
                backZFail = state.stencil.backStencilZFail;
            }

            if (backZPass == StencilOp.STENCILOP_CURRENT)
            {
                backZPass = state.stencil.backStencilZPass;
            }

            if ((frontStencilFail != state.stencil.frontStencilFail) ||
                (frontZFail != state.stencil.frontStencilZFail) ||
                (frontZPass != state.stencil.frontStencilZPass) ||
                (supportsTwoSidedStencil() &&
                ((backStencilFail != state.stencil.backStencilFail) ||
                 (backZFail != state.stencil.backStencilZFail) ||
                 (backZPass != state.stencil.backStencilZPass))))
            {
                if (backend != null)
                {
                    backend.SetStencilOp(frontStencilFail, frontZFail, frontZPass,
                                         backStencilFail, backZFail, backZPass);
                }
                minGLStateChange();
            }

            state.stencil.frontStencilFail = frontStencilFail;
            state.stencil.frontStencilZFail = frontZFail;
            state.stencil.frontStencilZPass = frontZPass;
            state.stencil.backStencilFail = backStencilFail;
            state.stencil.backStencilZFail = backZFail;
            state.stencil.backStencilZPass = backZPass;
        }

        public void setBlendFunc(
            BlendFunc src,
            BlendFunc dst,
            BlendEq eq = BlendEq.BLENDEQ_ADD)
        {
            Debug.debugAssert(!inPrimitive);

            minStateChange();
            if (src == BlendFunc.BLEND_CURRENT)
            {
                src = state.srcBlendFunc;
            }

            if (dst == BlendFunc.BLEND_CURRENT)
            {
                dst = state.dstBlendFunc;
            }

            if (eq == BlendEq.BLENDEQ_CURRENT)
            {
                eq = state.blendEq;
            }

            if (state.srcBlendFunc != src || state.dstBlendFunc != dst || state.blendEq != eq)
            {
                state.srcBlendFunc = src;
                state.dstBlendFunc = dst;
                state.blendEq = eq;
                if (backend != null)
                {
                    backend.SetBlendFunc(src, dst, eq);
                }
                minGLStateChange();
            }
        }

        public void enableClip2D(Rect2D clip)
        {
            minStateChange();
            minGLStateChange();
            state.clip2D = clip;

            if (backend != null)
            {
                backend.SetScissor(clip, true);
            }

            state.useClip2D = true;
        }

        public void disableClip2D()
        {
            minStateChange();
            if (state.useClip2D)
            {
                minGLStateChange();
                if (backend != null)
                {
                    backend.SetScissor(state.clip2D, false);
                }
            }
            state.useClip2D = false;
        }

        public void setLineWidth(double width)
        {
            Debug.debugAssert(!inPrimitive);
            minStateChange();
            if (state.lineWidth != width)
            {
                state.lineWidth = width;
                if (backend != null)
                {
                    backend.SetLineWidth(width);
                }
                minGLStateChange();
            }
        }

        public void setPointSize(double diameter)
        {
            Debug.debugAssert(!inPrimitive);
            minStateChange();
            if (state.pointSize != diameter)
            {
                state.pointSize = diameter;
                if (backend != null)
                {
                    backend.SetPointSize(diameter);
                }
                minGLStateChange();
            }
        }

        public void setObjectToWorldMatrix(CoordinateFrame cFrame)
        {
            minStateChange();
            Debug.debugAssert(!inPrimitive);

            // No test to see if it is already equal; this is called frequently and is
            // usually different.
            state.matrices.changed = true;
            state.matrices.objectToWorldMatrix = cFrame;
            applyModelViewMatrix();
            minGLStateChange();
        }

        public CoordinateFrame getObjectToWorldMatrix()
        {
            return state.matrices.objectToWorldMatrix;
        }

        public void setCameraToWorldMatrix(CoordinateFrame cFrame)
        {
            Debug.debugAssert(!inPrimitive);
            majStateChange();
            majGLStateChange();

            state.matrices.changed = true;
            state.matrices.cameraToWorldMatrix = cFrame;
            state.matrices.cameraToWorldMatrixInverse = cFrame.inverse();
            applyModelViewMatrix();
        }

        public CoordinateFrame getCameraToWorldMatrix()
        {
            return state.matrices.cameraToWorldMatrix;
        }

        public Matrix4 getProjectionMatrix()
        {
            return state.matrices.projectionMatrix;
        }

        public CoordinateFrame getModelViewMatrix()
        {
            return state.matrices.cameraToWorldMatrixInverse * state.matrices.objectToWorldMatrix;
        }

        public Matrix4 getModelViewProjectionMatrix()
        {
            return state.matrices.projectionMatrix * getModelViewMatrix();
        }

        public void setProjectionMatrix(Matrix4 P)
        {
            minStateChange();
            if (state.matrices.projectionMatrix != P)
            {
                state.matrices.projectionMatrix = P;
                state.matrices.changed = true;
                if (backend != null)
                {
                    backend.SetProjectionMatrix(P);
                }
                minGLStateChange();
            }
        }

        private void applyModelViewMatrix()
        {
            if (backend != null)
            {
                backend.SetModelViewMatrix(
                    (state.matrices.cameraToWorldMatrixInverse * state.matrices.objectToWorldMatrix).toMatrix4());
            }
        }

        public void setTextureMatrix(uint textureUnit, float[] m)
        {
            Debug.debugAssert(!inPrimitive);
            if (backend != null)
            {
                Debug.debugAssertM((int)textureUnit < (int)numTextureUnits(),
                    string.Format("Attempted to access texture unit {0} on a device with {1} units.",
                                  textureUnit, numTextureUnits()));
            }

            bool changed = false;
            for (int i = 0; i < 16; ++i)
            {
                if (state.textureUnit[textureUnit].textureMatrix[i] != m[i])
                {
                    changed = true;
                    break;
                }
            }

            if (changed)
            {
                forceSetTextureMatrix(textureUnit, m);
            }
        }

        private void forceSetTextureMatrix(uint textureUnit, float[] m)
        {
            minStateChange();
            minGLStateChange();

            state.touchedTextureUnit((int)textureUnit);
            Array.Copy(m, state.textureUnit[textureUnit].textureMatrix, 16);

            if (backend != null)
            {
                backend.SetTextureMatrix((int)textureUnit, matrixFromArray(state.textureUnit[textureUnit].textureMatrix));
            }
        }

        public void setTextureMatrix(uint textureUnit, double[] m)
        {
            Debug.debugAssert(!inPrimitive);
            if (backend != null)
            {
                Debug.debugAssertM((int)textureUnit < (int)numTextureUnits(),
                    string.Format("Attempted to access texture unit {0} on a device with {1} units.",
                                  textureUnit, numTextureUnits()));
            }

            float[] f = new float[16];
            for (int i = 0; i < 16; ++i)
            {
                f[i] = (float)m[i];
            }

            forceSetTextureMatrix(textureUnit, f);
        }

        public void setTextureMatrix(uint textureUnit, Matrix4 m)
        {
            float[] fm = new float[16];
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    fm[r * 4 + c] = m.elt(r, c);
                }
            }
            setTextureMatrix(textureUnit, fm);
        }

        public void setTextureMatrix(uint textureUnit, CoordinateFrame c)
        {
            float[] m = new float[16];
            for (int r = 0; r < 3; ++r)
            {
                for (int i = 0; i < 3; ++i)
                {
                    m[r * 4 + i] = c.rotation.elt(r, i);
                }
                m[r * 4 + 3] = c.translation[r];
            }
            m[12] = 0;
            m[13] = 0;
            m[14] = 0;
            m[15] = 1;

            setTextureMatrix(textureUnit, m);
        }

        public void setTextureLODBias(uint unit, float bias)
        {
            Debug.debugAssert(!inPrimitive);
            if (backend != null)
            {
                Debug.debugAssertM((int)unit < (int)numTextureUnits(),
                    string.Format("Attempted to access texture unit {0} on a device with {1} units.",
                                  unit, numTextureUnits()));
            }

            minStateChange();
            if (state.textureUnit[unit].LODBias != bias)
            {
                state.touchedTextureUnit((int)unit);
                state.textureUnit[unit].LODBias = bias;
                if (backend != null)
                {
                    backend.SetTextureLODBias((int)unit, bias);
                }
                minGLStateChange();
            }
        }

        public Matrix4 getTextureMatrix(uint textureUnit)
        {
            if (backend != null)
            {
                Debug.debugAssertM((int)textureUnit < (int)numTextureUnits(),
                    string.Format("Attempted to access texture unit {0} on a device with {1} units.",
                                  textureUnit, numTextureUnits()));
            }

            float[] M = state.textureUnit[textureUnit].textureMatrix;

            Matrix4 result = new Matrix4();
            for (int r = 0; r < 4; ++r)
            {
                for (int c = 0; c < 4; ++c)
                {
                    result.setElt(r, c, M[c * 4 + r]);
                }
            }
            return result;
        }

        public void setTextureCombineMode(uint textureUnit, CombineMode texCombine)
        {
            minStateChange();

            if (texCombine == CombineMode.TEX_CURRENT)
            {
                return;
            }

            if (backend != null)
            {
                Debug.debugAssertM((int)textureUnit < (int)numTextureUnits(),
                    string.Format("Attempted to access texture unit {0} on a device with {1} units.",
                                  textureUnit, numTextureUnits()));
            }

            if (state.textureUnit[textureUnit].combineMode != texCombine)
            {
                state.touchedTextureUnit((int)textureUnit);
                state.textureUnit[textureUnit].combineMode = texCombine;
                if (backend != null)
                {
                    backend.SetTextureCombineMode((int)textureUnit, texCombine);
                }
                minGLStateChange();
            }
        }

        public void resetTextureUnit(uint textureUnit)
        {
            if (backend != null)
            {
                Debug.debugAssertM((int)textureUnit < (int)numTextureUnits(),
                    string.Format("Attempted to access texture unit {0} on a device with {1} units.",
                                  textureUnit, numTextureUnits()));
            }

            RenderState newState = cloneState(state);
            state.textureUnit[textureUnit] = new RenderState.TextureUnit();
            state.touchedTextureUnit((int)textureUnit);
            setState(newState);
        }

        public void setPolygonOffset(double offset)
        {
            Debug.debugAssert(!inPrimitive);

            minStateChange();
            if (state.polygonOffset != offset)
            {
                state.polygonOffset = offset;
                if (backend != null)
                {
                    backend.SetPolygonOffset(offset);
                }
                minGLStateChange();
            }
        }

        public void setColor(Color4 color)
        {
            minStateChange();
            state.color = color;
            if (backend != null)
            {
                backend.SendColor(color.r, color.g, color.b, color.a);
            }
            minGLStateChange();
        }

        public void setColor(Color3 color)
        {
            setColor(new Color4(color, 1.0f));
        }

        public void setNormal(Vector3 normal)
        {
            minStateChange();
            state.normal = normal;
            if (backend != null)
            {
                backend.SendNormal(normal.x, normal.y, normal.z);
            }
            minGLStateChange();
        }

        public void setTexCoord(uint textureUnit, Vector4 texCoord)
        {
            minStateChange();
            state.textureUnit[textureUnit].texCoord = texCoord;
            if (backend != null)
            {
                backend.SendTexCoord(texCoord.x, texCoord.y, (int)textureUnit);
            }
            minGLStateChange();
        }

        public void setTexCoord(uint textureUnit, Vector3 texCoord)
        {
            setTexCoord(textureUnit, new Vector4(texCoord.x, texCoord.y, texCoord.z, 1.0f));
        }

        public void setTexCoord(uint textureUnit, Vector3int16 texCoord)
        {
            setTexCoord(textureUnit, new Vector4(texCoord.x, texCoord.y, texCoord.z, 1.0f));
        }

        public void setTexCoord(uint textureUnit, Vector2 texCoord)
        {
            setTexCoord(textureUnit, new Vector4(texCoord.x, texCoord.y, 0.0f, 1.0f));
        }

        public void setTexCoord(uint textureUnit, Vector2int16 texCoord)
        {
            setTexCoord(textureUnit, new Vector4(texCoord.x, texCoord.y, 0.0f, 1.0f));
        }

        public void setTexCoord(uint textureUnit, double texCoord)
        {
            setTexCoord(textureUnit, new Vector4((float)texCoord, 0.0f, 0.0f, 1.0f));
        }

        public void setCullFace(CullFace f)
        {
            minStateChange();
            if (f == CullFace.CULL_CURRENT)
            {
                return;
            }
            if (state.cullFace != f)
            {
                state.cullFace = f;
                if (backend != null)
                {
                    backend.SetCullFace(f);
                }
                minGLStateChange();
            }
        }

        public double getBrightScale()
        {
            return brightScale;
        }

        public void setViewport(Rect2D v)
        {
            minStateChange();
            if (state.viewport != v)
            {
                state.viewport = v;
                if (backend != null)
                {
                    backend.SetViewport(v);
                }
                minGLStateChange();
            }
        }

        public Rect2D getViewport()
        {
            return state.viewport;
        }

        public void sendVertex(Vector2 vertex)
        {
            sendVertex(new Vector3(vertex.x, vertex.y, 0.0f));
        }

        public void sendVertex(Vector3 vertex)
        {
            if (backend != null)
            {
                backend.SendVertex(vertex.x, vertex.y, vertex.z);
            }
            currentPrimitiveVertexCount++;
        }

        public void sendVertex(Vector4 vertex)
        {
            if (backend != null)
            {
                backend.SendVertex(vertex.x, vertex.y, vertex.z, vertex.w);
            }
            currentPrimitiveVertexCount++;
        }

        public void setProjectionAndCameraMatrix(GCamera camera)
        {
            double pixelAspect = state.viewport.width() / state.viewport.height();

            double y = -camera.getNearPlaneZ() * Math.Tan(camera.getFieldOfView() / 2.0);
            double x = y * pixelAspect;

            float r = (float)x;
            float l = (float)-x;
            float t = (float)y;
            float b = (float)-y;
            float n = (float)-camera.getNearPlaneZ();
            float f = (float)-camera.getFarPlaneZ();

            setProjectionMatrix(Matrix4.perspectiveProjection(l, r, b, t, n, f));
            setCameraToWorldMatrix(camera.getCoordinateFrame());
        }

        public void beginPrimitive(Primitive p)
        {
            Debug.debugAssertM(!inPrimitive, "Already inside a primitive");
            Debug.debugAssertM(currentFramebufferComplete(), "Bound Framebuffer Incomplete!");

            beforePrimitive();
            runObjectShader();

            inPrimitive = true;
            currentPrimitiveVertexCount = 0;
            currentPrimitive = p;

            if (backend != null)
            {
                backend.BeginPrimitive(p);
            }
        }

        public void endPrimitive()
        {
            Debug.debugAssertM(inPrimitive, "Call to endPrimitive() without matching beginPrimitive()");

            mDebugNumMinorStateChanges += (uint)currentPrimitiveVertexCount;
            mDebugNumMinorOpenGLStateChanges += (uint)currentPrimitiveVertexCount;
            triangleCount += countTriangles(currentPrimitive, currentPrimitiveVertexCount);

            if (backend != null)
            {
                backend.EndPrimitive();
            }

            inPrimitive = false;

            afterPrimitive();
        }

        /**
          Called immediately before a primitive group is rendered.
         */
        public void beforePrimitive()
        {
            if (state.shader != null)
            {
                Debug.debugAssert(!inShader);
                inShader = true;
                state.shader.beforePrimitive(this);
                inShader = false;
            }
        }

        /**
          Called immediately after a primitive group is rendered.
         */
        public void afterPrimitive()
        {
            if (state.shader != null)
            {
                Debug.debugAssert(!inShader);
                inShader = true;
                state.shader.afterPrimitive(this);
                inShader = false;
            }
        }

        /**
          Runs the object shader, if one is bound.
         */
        private void runObjectShader()
        {
            if (state.objectShader != null)
            {
                state.objectShader.run(this);
            }
        }

        private int countTriangles(Primitive primitive, int numVertices)
        {
            switch (primitive)
            {
                case Primitive.LINES: return numVertices / 2;
                case Primitive.LINE_STRIP: return numVertices - 1;
                case Primitive.TRIANGLES: return numVertices / 3;
                case Primitive.TRIANGLE_STRIP: return numVertices - 2;
                case Primitive.TRIANGLE_FAN: return numVertices - 2;
                case Primitive.QUADS: return (numVertices / 4) * 2;
                case Primitive.QUAD_STRIP: return ((numVertices / 2) - 1) * 2;
                case Primitive.POINTS: return numVertices;
                default: return 0;
            }
        }

        public void beginIndexedPrimitives()
        {
            Debug.debugAssert(!inPrimitive);
            Debug.debugAssert(!inIndexedPrimitive);

            inIndexedPrimitive = true;
        }

        public void endIndexedPrimitives()
        {
            Debug.debugAssert(!inPrimitive);
            Debug.debugAssert(inIndexedPrimitive);

            inIndexedPrimitive = false;
            currentVARArea = null;
        }

        /**
          All vertex arrays used within a single begin/endIndexedPrimitives
          block must share the same VARArea.
         */
        private void setVARAreaFromVAR(VAR v)
        {
            Debug.debugAssert(inIndexedPrimitive);
            Debug.debugAssert(!inPrimitive);

            if (v == null || v.getArea() == null)
            {
                return;
            }

            Debug.alwaysAssertM(currentVARArea == null || (v.getArea() == currentVARArea),
                "All vertex arrays used within a single begin/endIndexedPrimitive block must share the same VARArea.");

            majStateChange();

            if (v.getArea() != currentVARArea)
            {
                currentVARArea = v.getArea();
                majGLStateChange();
            }
        }

        public void setVertexArray(VAR v)
        {
            setVARAreaFromVAR(v);
            state.vertexArray = v;
        }

        public void setNormalArray(VAR v)
        {
            setVARAreaFromVAR(v);
            state.normalArray = v;
        }

        public void setIndexArray(VAR v)
        {
            state.indexArray = v;
        }

        public void setColorArray(VAR v)
        {
            setVARAreaFromVAR(v);
            state.colorArray = v;
        }

        public void setTexCoordArray(uint unit, VAR v)
        {
            Debug.debugAssertM(unit < (uint)state.texCoordArray.Length,
                string.Format("Attempted to access texture coordinate array {0} on a device with {1} coordinate arrays.",
                              unit, state.texCoordArray.Length));

            setVARAreaFromVAR(v);
            state.texCoordArray[unit] = v;
        }

        public GWindow window()
        {
            return _window as GWindow;
        }

        /**
          Returns the window that this render device is rendering to, or null
          if the render device was not created from a GWindow.
         */
        public GWindow getWindow()
        {
            return _window as GWindow;
        }

        public void setVertexAttribArray(uint attribNum, VAR v, bool normalize)
        {
            if (attribNum >= (uint)state.vertexAttribArray.Length)
            {
                throw new ArgumentOutOfRangeException("attribNum",
                    string.Format("Vertex attribute arrays are limited to {0} entries.",
                                  state.vertexAttribArray.Length));
            }

            setVARAreaFromVAR(v);
            state.vertexAttribArray[attribNum] = v;

            if (backend != null)
            {
                backend.SetVertexAttribArray((int)attribNum, v, normalize);
            }
        }

        /**
          Draws indexed primitives using the currently set vertex, normal,
          and index arrays.
         */
        public void sendIndices(Primitive primitive, int numIndices)
        {
            Debug.debugAssertM(currentFramebufferComplete(), "Incomplete Framebuffer");

            if (backend == null || state.vertexArray == null)
            {
                return;
            }

            beforePrimitive();
            runObjectShader();

            VAR texCoordArray = (state.texCoordArray.Length > 0) ? state.texCoordArray[0] : null;
            backend.DrawVAR(primitive, numIndices, state.vertexArray, state.normalArray,
                            state.colorArray, texCoordArray, state.indexArray);

            afterPrimitive();

            triangleCount += countTriangles(primitive, numIndices);
        }

        /**
          Draws primitives using the vertices of the currently set vertex
          array in sequential order.
         */
        public void sendSequentialIndices(Primitive primitive, int numVertices)
        {
            Debug.debugAssertM(currentFramebufferComplete(), "Incomplete Framebuffer");

            if (backend == null || state.vertexArray == null)
            {
                return;
            }

            beforePrimitive();
            runObjectShader();

            VAR texCoordArray = (state.texCoordArray.Length > 0) ? state.texCoordArray[0] : null;
            backend.DrawVAR(primitive, numVertices, state.vertexArray, state.normalArray,
                            state.colorArray, texCoordArray, null);

            afterPrimitive();

            triangleCount += countTriangles(primitive, numVertices);
        }

        public void sendIndices<T>(Primitive primitive, int numIndices, T[] index) where T : struct
        {
            Debug.debugAssertM(currentFramebufferComplete(), "Incomplete Framebuffer");

            if (backend == null || state.vertexArray == null)
            {
                return;
            }

            if (index == null)
            {
                throw new ArgumentNullException("index");
            }

            // The graphics backends draw 32 bit indices, so the indices are
            // converted before they are uploaded.
            uint[] values = new uint[index.Length];
            for (int i = 0; i < index.Length; ++i)
            {
                values[i] = indexToUInt(index[i]);
            }

            if (immediateIndexVAR == null || immediateIndexVARCapacity < values.Length)
            {
                immediateIndexVARCapacity = Math.Max(values.Length, 64);
                immediateIndexVAR = VAR.create(VAR.Type.INDEX, immediateIndexVARCapacity, VAR.Usage.STREAM);
            }

            immediateIndexVAR.setData(values);
            state.indexArray = immediateIndexVAR;
            sendIndices(primitive, Math.Min(numIndices, values.Length));
        }

        /**
          Converts an index of 8, 16, or 32 bits to the 32 bit representation
          that the graphics backends use.
         */
        private static uint indexToUInt<T>(T index) where T : struct
        {
            object value = index;
            if (value is uint)
            {
                return (uint)value;
            }
            if (value is int)
            {
                return (uint)(int)value;
            }
            if (value is ushort)
            {
                return (ushort)value;
            }
            if (value is short)
            {
                return (uint)(short)value;
            }
            if (value is byte)
            {
                return (byte)value;
            }
            if (value is sbyte)
            {
                return (uint)(sbyte)value;
            }
            throw new ArgumentException("Indices must be either 8, 16, or 32 bits each.", "index");
        }

        public void sendIndices<T>(Primitive primitive, Array<T> index) where T : struct
        {
            T[] values = new T[index.size()];
            for (int i = 0; i < values.Length; ++i)
            {
                values[i] = index[i];
            }
            sendIndices(primitive, values.Length, values);
        }

        public void setStencilClearValue(int s)
        {
            state.stencil.stencilClear = s;
        }

        public void setDepthClearValue(double d)
        {
            state.depthClear = d;
        }

        public void setColorClearValue(Color4 c)
        {
            state.colorClear = c;
        }

        public void setTexture(uint textureUnit, Texture texture)
        {
            Debug.debugAssertM(!inPrimitive, "Can't change textures while rendering a primitive.");
            if (backend != null)
            {
                Debug.debugAssertM((int)textureUnit < (int)numTextures(),
                    string.Format("Attempted to access texture {0} on a device with {1} textures.",
                                  textureUnit, numTextures()));
            }

            if (textureUnit >= (uint)state.textureUnit.Length)
            {
                throw new ArgumentOutOfRangeException("textureUnit",
                    string.Format("Texture units are limited to {0} entries.", state.textureUnit.Length));
            }

            majStateChange();

            if (state.textureUnit[textureUnit].texture == texture)
            {
                return;
            }

            majGLStateChange();

            state.textureUnit[textureUnit].texture = texture;
            state.touchedTextureUnit((int)textureUnit);
            bindTextureUnit((int)textureUnit);
        }

        /**
          Uploads the texture to the backend if needed and binds it to the
          given texture unit.
         */
        private void bindTextureUnit(int textureUnit)
        {
            if (backend == null || textureUnit < 0 || textureUnit >= state.textureUnit.Length)
            {
                return;
            }

            Texture texture = state.textureUnit[textureUnit].texture;
            if (texture == null)
            {
                backend.BindTexture(textureUnit, 0, Texture.Dimension.DIM_2D);
                currentlyBoundTexture[textureUnit] = 0;
                return;
            }

            int id = texture.getBackendTextureID(backend);
            if (id == 0)
            {
                id = allocateTexture(texture);
            }
            backend.BindTexture(textureUnit, id, texture.getDimension());
            currentlyBoundTexture[textureUnit] = (uint)id;
        }

        public int allocateTexture(Texture texture)
        {
            if (backend == null)
            {
                throw new InvalidOperationException("No graphics backend is attached.");
            }
            if (texture == null)
            {
                throw new ArgumentNullException("texture");
            }
            int id = texture.getBackendTextureID(backend);
            if (id != 0)
            {
                return id;
            }
            id = backend.CreateTexture(texture.getFormat(), texture.texelWidth(), texture.texelHeight(),
                                       texture.depthReadMode());
            backend.BindTexture(0, id, texture.getDimension());

            int target = backend.GetTextureTarget(texture.getDimension());
            byte[] data = texture.getPixelData();
            byte[][] faces = texture.getCubeFaceData();
            global::System.Collections.Generic.List<global::System.Collections.Generic.List<byte[]>> mipMaps = texture.getMipMapData();

            if (mipMaps != null)
            {
                TextureFormat sourceFormat = texture.getPixelFormat();
                if (sourceFormat == TextureFormat.RGB_DXT1 || sourceFormat == TextureFormat.RGBA_DXT1 ||
                    sourceFormat == TextureFormat.RGBA_DXT3 || sourceFormat == TextureFormat.RGBA_DXT5)
                {
                    backend.CompressedTexImage2D(target, sourceFormat, texture.texelWidth(), texture.texelHeight(), mipMaps);
                }
                else
                {
                int levelWidth = texture.texelWidth();
                int levelHeight = texture.texelHeight();
                for (int level = 0; level < mipMaps.Count; ++level)
                {
                    List<byte[]> levelFaces = mipMaps[level];
                    if (levelFaces == null)
                    {
                        break;
                    }
                    int faceCount = (texture.getDimension() == Texture.Dimension.DIM_CUBE_MAP ||
                                     texture.getDimension() == Texture.Dimension.DIM_CUBE_MAP_NPOT) ? 6 : 1;
                    for (int f = 0; f < faceCount && f < levelFaces.Count; ++f)
                    {
                        backend.TexImage2D(target + f, level, texture.getPixelFormat(),
                                          levelWidth, levelHeight, 0, levelFaces[f]);
                    }
                    levelWidth = Math.Max(1, levelWidth / 2);
                    levelHeight = Math.Max(1, levelHeight / 2);
                }
                }
            }
            else if (faces != null)
            {
                for (int f = 0; f < faces.Length; ++f)
                {
                    backend.TexImage2D(target + f, 0, texture.getPixelFormat(),
                                      texture.texelWidth(), texture.texelHeight(), 0, faces[f]);
                }
            }
            else if (data != null)
            {
                backend.TexImage2D(target, 0, texture.getPixelFormat(),
                                  texture.texelWidth(), texture.texelHeight(), 0, data);
            }

            backend.TexParameter(target, texture.getWrapMode(), texture.getWrapMode(),
                                 texture.getInterpolateMode(), texture.getInterpolateMode(),
                                 texture.maxAnisotropy());
            backend.SetTextureDepthCompare(target, texture.depthReadMode());

            if (texture.getInterpolateMode() == Texture.InterpolateMode.TRILINEAR_MIPMAP ||
                texture.getInterpolateMode() == Texture.InterpolateMode.BILINEAR_MIPMAP ||
                texture.getInterpolateMode() == Texture.InterpolateMode.NEAREST_MIPMAP)
            {
                backend.GenerateMipmap(target);
            }

            texture.setBackendTextureID(backend, id);
            return id;
        }

        public bool supportsShadows()
        {
            return backend != null && backend.Caps.SupportsShadows;
        }

        public uint numTextures()
        {
            return (uint)_numTextures;
        }

        public uint numTextureUnits()
        {
            return (uint)_numTextureUnits;
        }

        public uint numTextureCoords()
        {
            return (uint)_numTextureCoords;
        }

        public void setVertexAndPixelShader(VertexAndPixelShader s)
        {
            majStateChange();

            if (!ReferenceEquals(state.vertexAndPixelShader, s))
            {
                state.vertexAndPixelShader = s;

                if (s != null)
                {
                    Debug.alwaysAssertM(s.ok(), s.messages());
                }

                // The actual shader will change in beforePrimitive or when the arg list is bound.
            }
        }

        public void setVertexAndPixelShader(VertexAndPixelShader s, VertexAndPixelShader.ArgList args)
        {
            setVertexAndPixelShader(s);

            if (s != null)
            {
                s.bindArgList(this, args);
            }
        }

        public void setObjectShader(ObjectShader s)
        {
            if (!ReferenceEquals(state.objectShader, s))
            {
                state.objectShader = s;
            }
        }

        public void setShader(Shader s)
        {
            majStateChange();

            if (!ReferenceEquals(state.shader, s))
            {
                Debug.debugAssertM(!inShader, "Cannot set the Shader from within a Shader!");
                state.shader = s;
            }
        }

        public void setVertexProgram(VertexProgram vp)
        {
            state.vertexProgram = vp;
        }

        public void setVertexProgram(VertexProgram vp, GPUProgram.ArgList args)
        {
            setVertexProgram(vp);
        }

        public void setPixelProgram(PixelProgram pp)
        {
            state.pixelProgram = pp;
        }

        public void setPixelProgram(PixelProgram pp, GPUProgram.ArgList args)
        {
            setPixelProgram(pp);
        }

        public double getDepthBufferValue(int x, int y)
        {
            if (backend == null)
            {
                throw new InvalidOperationException("No graphics backend is attached.");
            }

            float[] depth = new float[1];
            backend.ReadDepth(x, getHeight() - 1 - y, 1, 1, depth);
            return depth[0];
        }

        public string getCardDescription()
        {
            return cardDescription;
        }

        public void enableTwoSidedLighting()
        {
            state.lights.twoSidedLighting = true;
            state.lights.changed = true;
        }

        public void disableTwoSidedLighting()
        {
            state.lights.twoSidedLighting = false;
            state.lights.changed = true;
        }

        public void setLight(int num, GLight light)
        {
            if (num >= 0 && num < MAX_LIGHTS)
            {
                state.lights.light[num] = new GLight(light);
                state.lights.lightEnabled[num] = true;
                state.lights.changed = true;
                if (backend != null)
                {
                    backend.SetLight(num, state.lights.light[num], true);
                }
            }
        }

        public void setLight(int num, object nullLight)
        {
            if (num >= 0 && num < MAX_LIGHTS)
            {
                state.lights.lightEnabled[num] = false;
                state.lights.changed = true;
                if (backend != null)
                {
                    backend.SetLight(num, new GLight(), false);
                }
            }
        }

        public void setSpecularCoefficient(float s)
        {
            minStateChange();
            Color3 c = new Color3(s, s, s);
            if (state.specular != c)
            {
                state.specular = c;
                if (backend != null)
                {
                    backend.SetSpecular(state.specular);
                }
                minGLStateChange();
            }
        }

        public void setSpecularCoefficient(Color3 c)
        {
            minStateChange();
            if (state.specular != c)
            {
                state.specular = c;
                if (backend != null)
                {
                    backend.SetSpecular(state.specular);
                }
                minGLStateChange();
            }
        }

        public void setShininess(float s)
        {
            minStateChange();
            if (state.shininess != s)
            {
                state.shininess = s;
                if (backend != null)
                {
                    backend.SetShininess(s);
                }
                minGLStateChange();
            }
        }

        public void setAmbientLightColor(Color3 color)
        {
            Debug.debugAssert(!inPrimitive);

            minStateChange();
            Color4 ambient = new Color4(color, 1.0f);
            if (state.lights.ambient != ambient)
            {
                state.lights.ambient = ambient;
                state.lights.changed = true;
                if (backend != null)
                {
                    backend.SetAmbientLight(state.lights.ambient);
                }
                minGLStateChange();
            }
        }

        public void setAmbientLightColor(Color4 color)
        {
            Debug.debugAssert(!inPrimitive);

            minStateChange();
            if (state.lights.ambient != color)
            {
                state.lights.ambient = color;
                state.lights.changed = true;
                if (backend != null)
                {
                    backend.SetAmbientLight(color);
                }
                minGLStateChange();
            }
        }

        public void enableLighting()
        {
            if (!state.lights.lighting)
            {
                state.lights.lighting = true;
                state.lights.changed = true;
                backend.SetLightingEnabled(true);
            }
        }

        public void disableLighting()
        {
            if (state.lights.lighting)
            {
                state.lights.lighting = false;
                state.lights.changed = true;
                backend.SetLightingEnabled(false);
            }
        }

        public Vector4 project(Vector4 v)
        {
            Matrix4 mvp = getModelViewProjectionMatrix();
            return mvp * v;
        }

        public Vector4 project(Vector3 v)
        {
            return project(new Vector4(v.x, v.y, v.z, 1.0f));
        }

        /**
          Transforms a point from world space into screen space using the same
          convention as the original glToScreen function.  The x and y
          coordinates are in pixels, z is the normalized device depth mapped
          through the depth range, and w is the reciprocal of the homogeneous
          w coordinate.
         */
        public Vector4 projectToScreen(Vector4 v)
        {
            Vector4 clip = project(v);
            Rect2D viewport = state.viewport;
            double rhw = 1.0 / clip.w;
            return new Vector4(
                (float)((1 + clip.x * rhw) * viewport.width() / 2 + viewport.x0()),
                (float)((1 - clip.y * rhw) * viewport.height() / 2 + viewport.y0()),
                (float)((clip.z * rhw) * (state.highDepthRange - state.lowDepthRange) + state.lowDepthRange),
                (float)rhw);
        }

        /**
          Transforms a point from world space into screen space using the same
          convention as the original glToScreen function.  The x and y
          coordinates are in pixels, z is the normalized device depth mapped
          through the depth range, and w is the reciprocal of the homogeneous
          w coordinate.
         */
        public Vector4 projectToScreen(Vector3 v)
        {
            return projectToScreen(new Vector4(v.x, v.y, v.z, 1.0f));
        }

        public Milestone createMilestone(string name)
        {
            if (backend == null)
            {
                throw new InvalidOperationException("No graphics backend is attached.");
            }
            Milestone milestone = new Milestone(name);
            milestone.setBackendMilestoneID(backend.CreateMilestone());
            return milestone;
        }

        public void setMilestone(Milestone m)
        {
            if (m == null)
            {
                throw new ArgumentNullException("m");
            }
            if (backend == null)
            {
                throw new InvalidOperationException("No graphics backend is attached.");
            }
            backend.SetMilestone(m.getBackendMilestoneID());
            m.markSet();
        }

        public void waitForMilestone(Milestone m)
        {
            if (m == null)
            {
                throw new ArgumentNullException("m");
            }
            if (!m.isSet())
            {
                throw new InvalidOperationException("The milestone has not been set.");
            }
            if (backend == null)
            {
                throw new InvalidOperationException("No graphics backend is attached.");
            }
            backend.WaitForMilestone(m.getBackendMilestoneID());
            m.markWaited();
        }

        public void configureShadowMap(uint textureUnit, Matrix4 lightMVP, Texture shadowMap)
        {
            if (backend == null)
            {
                throw new InvalidOperationException("No graphics backend is attached.");
            }
            if (shadowMap == null)
            {
                throw new ArgumentNullException("shadowMap");
            }
            if (textureUnit >= state.textureUnit.Length)
            {
                throw new ArgumentOutOfRangeException("textureUnit");
            }
            if (!shadowMap.getFormat().getIsDepth())
            {
                throw new ArgumentException("A shadow map must use a depth texture.", "shadowMap");
            }
            if (shadowMap.depthReadMode() == Texture.DepthReadMode.DEPTH_NORMAL)
            {
                throw new ArgumentException("A shadow map must use a depth comparison mode.", "shadowMap");
            }
            setTexture(textureUnit, shadowMap);
            state.textureUnit[textureUnit].shadowMapEnabled = true;
            state.textureUnit[textureUnit].shadowLightMVP = lightMVP;
            state.touchedTextureUnit((int)textureUnit);
            backend.ConfigureShadowMap((int)textureUnit, lightMVP,
                                       state.matrices.cameraToWorldMatrixInverse.toMatrix4(),
                                       shadowMap.getBackendTextureID(backend),
                                       shadowMap.depthReadMode());
        }

        public void configureReflectionMap(uint textureUnit, Texture reflectionTexture)
        {
            if (reflectionTexture == null)
            {
                throw new ArgumentNullException("reflectionTexture");
            }

            Debug.debugAssert(reflectionTexture.getDimension() == Texture.Dimension.DIM_CUBE_MAP);

            // Texture coordinates will be generated in object space.
            // Set the texture matrix to transform them into camera space.
            CoordinateFrame cframe = getCameraToWorldMatrix();

            // The environment map assumes we are always in the center,
            // so zero the translation.
            cframe.translation = Vector3.zero();

            // The environment map is in world space.  The reflection vector
            // will automatically be multiplied by the object->camera space
            // transformation by hardware (just like any other vector) so we
            // take it back from camera space to world space for the correct
            // vector.
            setTexture(textureUnit, reflectionTexture);

            setTextureMatrix(textureUnit, cframe);

            minStateChange();
            minGLStateChange();

            throw new NotSupportedException(
                "Reflection maps require reflection texture coordinate generation, which requires the " +
                "backend operation IGraphicsBackend.ConfigureReflectionMap(int textureUnit, int textureID, " +
                "Matrix4 textureMatrix), which is not implemented by the OpenGL or Direct3D 11 backends. " +
                "The texture and texture matrix have been bound; the reflection vector must be computed by a shader.");
        }

        public bool init(GWindow.Settings settings = null, Log log = null)
        {
            debugLog = log;

            if (settings != null && _window != null)
            {
                state.viewport = new Rect2D(0, 0, settings.width, settings.height);
                state.clip2D = state.viewport;
                if (backend != null)
                {
                    backend.Resize(settings.width, settings.height);
                    backend.SetViewport(state.viewport);
                }
            }

            _initialized = true;
            return true;
        }

        public bool init(GWindow window, Log log = null)
        {
            this._window = window;
            debugLog = log;
            if (window != null)
            {
                window.setRenderDevice(this);
                state.viewport = new Rect2D(0, 0, window.Width, window.Height);
                state.clip2D = state.viewport;
            }
            if (backend != null && window != null)
            {
                backend.Resize(window.Width, window.Height);
                backend.SetViewport(state.viewport);
                backend.SetProjectionMatrix(state.matrices.projectionMatrix);
                applyModelViewMatrix();
            }
            _initialized = true;
            return true;
        }

        public bool initialized()
        {
            return _initialized;
        }

        public Color4 color()
        {
            return state.color;
        }

        public ShadeMode shadeMode()
        {
            return state.shadeMode;
        }

        public void cleanup()
        {
            if (!cleanedup)
            {
                cleanedup = true;
                stateStack.Clear();
                statePool.Clear();

                if (deleteWindow && _window != null)
                {
                    _window.Dispose();
                    _window = null;
                }

                if (backend != null)
                {
                    backend.Dispose();
                    backend = null;
                }
            }
        }

        public void setCaption(string caption)
        {
            GWindow window = getWindow();
            if (window == null)
            {
                throw new InvalidOperationException(
                    "The caption can only be set when the render device was created from a GWindow.");
            }
            window.setCaption(caption);
        }

        /**
          Sets the brightness and gamma correction of the display.
         */
        public void setGamma(double brightness, double gamma)
        {
            GWindow window = getWindow();
            if (window == null)
            {
                throw new InvalidOperationException(
                    "Gamma correction can only be set when the render device was created from a GWindow.");
            }
            window.gamma((float)gamma);
        }

        /**
          Applies the current window size to the graphics backend.
         */
        public void setVideoMode()
        {
            if (_window == null)
            {
                throw new InvalidOperationException(
                    "The video mode can only be set when the render device was created from a GWindow.");
            }
            if (backend == null)
            {
                return;
            }

            backend.Resize(_window.Width, _window.Height);
            state.viewport = new Rect2D(0, 0, _window.Width, _window.Height);
            state.clip2D = state.viewport;
            backend.SetViewport(state.viewport);
        }

        public string screenshot(string filepath)
        {
            GImage screen = new GImage();
            screenshotPic(screen);

            string basename = global::System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            string filename;
            int i = 0;
            do
            {
                filename = filepath + basename + "_" + i.ToString("D3") + ".jpg";
                ++i;
            }
            while (global::System.IO.File.Exists(filename));

            screen.save(filename);

            return filename;
        }

        public void notifyResize(int w, int h)
        {
            if (backend == null)
            {
                return;
            }
            backend.Resize(w, h);
            state.viewport = new Rect2D(0, 0, w, h);
            state.clip2D = state.viewport;
            backend.SetViewport(state.viewport);
        }

        public void screenshotPic(GImage dest, bool useBackBuffer = false, bool getAlpha = false)
        {
            if (backend == null)
            {
                throw new InvalidOperationException("No graphics backend is attached.");
            }
            if (dest == null)
            {
                throw new ArgumentNullException("dest");
            }

            int w = getWidth();
            int h = getHeight();
            int ch = getAlpha ? 4 : 3;

            if ((dest.channels != ch) || (dest.width != w) || (dest.height != h))
            {
                dest.resize(w, h, ch);
            }

            byte[] rgba = new byte[w * h * 4];
            backend.ReadPixels(0, 0, w, h, rgba);

            double saturation = getLightSaturation();
            byte[] lookup = null;
            if (saturation != 1.0)
            {
                lookup = new byte[256];
                for (int i = 0; i < 256; ++i)
                {
                    lookup[i] = (byte)Math.Min(255, (int)Math.Round(i * saturation));
                }
            }

            // Flip right side up
            for (int y = 0; y < h; ++y)
            {
                int srcY = h - 1 - y;
                for (int x = 0; x < w; ++x)
                {
                    int src = (srcY * w + x) * 4;
                    byte r = rgba[src];
                    byte g = rgba[src + 1];
                    byte b = rgba[src + 2];

                    if (lookup != null)
                    {
                        r = lookup[r];
                        g = lookup[g];
                        b = lookup[b];
                    }

                    if (ch == 4)
                    {
                        dest.setPixel4(x, y, new Color4uint8(r, g, b, rgba[src + 3]));
                    }
                    else
                    {
                        dest.setPixel3(x, y, new Color3uint8(r, g, b));
                    }
                }
            }
        }

        public int getWidth()
        {
            return (int)state.viewport.width();
        }

        public int width()
        {
            return (int)state.viewport.width();
        }

        public int getHeight()
        {
            return (int)state.viewport.height();
        }

        public int height()
        {
            return (int)state.viewport.height();
        }

        public int getStencilBitDepth()
        {
            return stencilBits;
        }

        public int getZBufferBitDepth()
        {
            return depthBits;
        }

        public double getFrameRate()
        {
            return emwaFrameRate;
        }

        public double getTriangleRate()
        {
            return emwaTriangleRate;
        }

        public double getTrianglesPerFrame()
        {
            return emwaTriangleCount;
        }

        public int getTriangleCount()
        {
            return triangleCount;
        }

        public void beginFrame()
        {
            if (swapGLBuffersPending)
            {
                // The swap requested by the previous endFrame() is performed here so
                // that the application can render into the back buffer first.
                backend.EndFrame();
                swapGLBuffersPending = false;
            }

            mDebugNumMajorOpenGLStateChanges = 0;
            mDebugNumMinorOpenGLStateChanges = 0;
            mDebugNumMajorStateChanges = 0;
            mDebugNumMinorStateChanges = 0;
            mDebugPushStateCalls = 0;
            beginEndFrame++;
            triangleCount = 0;
            Debug.debugAssertM(beginEndFrame == 1, "Mismatched calls to beginFrame/endFrame");

            pushState();

            backend.BeginFrame();
        }

        public void endFrame()
        {
            beginEndFrame--;
            Debug.debugAssertM(beginEndFrame == 0, "Mismatched calls to beginFrame/endFrame");

            popState();

            // Schedule a swap buffer iff we are handling them automatically.
            swapGLBuffersPending = _swapBuffersAutomatically;

            Debug.debugAssertM(stateStack.Count == 0, "Missing RenderDevice::popState or pop2D.");

            double now = System.getTick();
            double dt = now - lastTime;
            if (dt == 0)
            {
                dt = 0.0001;
            }

            double interpolation = Math.Max(0.07, Math.Min(1.0, dt));
            double frameRate = 1.0 / dt;
            emwaFrameRate = emwaFrameRate + interpolation * (frameRate - emwaFrameRate);
            emwaTriangleRate = emwaTriangleRate + interpolation * (triangleCount / dt - emwaTriangleRate);
            emwaTriangleCount = emwaTriangleCount + interpolation * (triangleCount - emwaTriangleCount);

            if (double.IsNaN(emwaFrameRate) || double.IsInfinity(emwaFrameRate))
            {
                emwaFrameRate = 1000000;
            }
            else if (emwaFrameRate < 0)
            {
                emwaFrameRate = 0;
            }

            if (double.IsNaN(emwaTriangleRate) || double.IsInfinity(emwaTriangleRate))
            {
                emwaTriangleRate = 1e20;
            }
            else if (emwaTriangleRate < 0)
            {
                emwaTriangleRate = 0;
            }

            if (double.IsNaN(emwaTriangleCount) || double.IsInfinity(emwaTriangleCount))
            {
                emwaTriangleCount = 1e20;
            }
            else if (emwaTriangleCount < 0)
            {
                emwaTriangleCount = 0;
            }

            lastTime = now;

            if (_swapBuffersAutomatically)
            {
                backend.EndFrame();
                swapGLBuffersPending = false;
            }
        }

        public bool swapBuffersAutomatically()
        {
            return _swapBuffersAutomatically;
        }

        public void setSwapBuffersAutomatically(bool b)
        {
            if (b == _swapBuffersAutomatically)
            {
                // Setting to current state; nothing to do.
                return;
            }

            if (swapGLBuffersPending)
            {
                // Process the pending swap buffers call
                backend.EndFrame();
                swapGLBuffersPending = false;
            }

            _swapBuffersAutomatically = b;
        }

        public void push2D()
        {
            push2D(state.viewport);
        }

        public void push2D(Framebuffer fb)
        {
            pushState();
            setFramebuffer(fb);
            setViewport(new Rect2D(0, 0, fb.getWidth(), fb.getHeight()));
            push2D(new Rect2D(0, 0, fb.getWidth(), fb.getHeight()));
        }

        public void push2D(Framebuffer fb, Rect2D viewport)
        {
            pushState();

            setFramebuffer(fb);
            setDepthTest(DepthTest.DEPTH_ALWAYS_PASS);
            disableLighting();
            setCullFace(CullFace.CULL_NONE);
            disableDepthWrite();
            setViewport(viewport);
            setObjectToWorldMatrix(CoordinateFrame.identity());

            setCameraToWorldMatrix(CoordinateFrame.identity());

            setProjectionMatrix(Matrix4.orthogonalProjection(
                viewport.x0(), viewport.x0() + viewport.width(),
                viewport.y0() + viewport.height(), viewport.y0(),
                -1.0f, 1.0f));
        }

        public void push2D(Rect2D viewport)
        {
            pushState();
            setDepthTest(DepthTest.DEPTH_ALWAYS_PASS);
            disableLighting();
            setCullFace(CullFace.CULL_NONE);
            setDepthWrite(false);
            setViewport(viewport);
            setProjectionMatrix(Matrix4.orthogonalProjection(
                viewport.x, viewport.x + viewport.width(),
                viewport.y + viewport.height(), viewport.y,
                -1.0f, 1.0f));
            setCameraToWorldMatrix(CoordinateFrame.identity());
            setObjectToWorldMatrix(CoordinateFrame.identity());
        }

        public void pop2D()
        {
            popState();
        }

        public void setFramebuffer(Framebuffer fbo)
        {
            majStateChange();
            if (ReferenceEquals(fbo, state.framebuffer))
            {
                return;
            }

            majGLStateChange();

            // Set Framebuffer
            state.drawBuffer = (fbo == null) ? Buffer.BUFFER_BACK : Buffer.BUFFER_COLOR0;
            state.framebuffer = fbo;

            if (backend == null)
            {
                return;
            }
            if (fbo == null)
            {
                backend.BindFramebuffer(0);
                return;
            }
            if (fbo.getFramebufferID() == 0)
            {
                fbo.setFramebufferID(backend.CreateFramebuffer());
                foreach (Framebuffer.TextureAttachment attachment in fbo.getTextureAttachments())
                {
                    int textureID = attachment.texture.getBackendTextureID(backend);
                    if (textureID == 0)
                    {
                        textureID = allocateTexture(attachment.texture);
                    }
                    backend.AttachFramebufferTexture(fbo.getFramebufferID(), attachment.attachmentPoint, textureID,
                                                    attachment.mipLevel, attachment.cubeFace);
                }
                foreach (Framebuffer.RenderbufferAttachment attachment in fbo.getRenderbufferAttachments())
                {
                    int renderbufferID = attachment.renderbuffer.getRenderbufferID();
                    if (renderbufferID == 0)
                    {
                        renderbufferID = backend.CreateRenderbuffer();
                        backend.RenderbufferStorage(renderbufferID, attachment.renderbuffer.getFormat(),
                                                   attachment.renderbuffer.getWidth(), attachment.renderbuffer.getHeight());
                        attachment.renderbuffer.setRenderbufferID(renderbufferID);
                    }
                    backend.AttachFramebufferRenderbuffer(fbo.getFramebufferID(), attachment.attachmentPoint, renderbufferID);
                }
            }
            backend.BindFramebuffer(fbo.getFramebufferID());
        }

        public bool currentFramebufferComplete()
        {
            string why;
            return currentFramebufferComplete(out why);
        }

        /**
          Returns true if the currently bound framebuffer is complete.  When
          it is not, the reason is written to @a why.
         */
        public bool currentFramebufferComplete(out string why)
        {
            if (state.framebuffer == null)
            {
                why = "";
                return true;
            }
            if (!state.framebuffer.isComplete())
            {
                why = "The framebuffer is missing one or more required attachments.";
                return false;
            }
            if (backend != null && !backend.CheckFramebufferStatus())
            {
                why = "The graphics backend reports that the bound framebuffer is incomplete.";
                return false;
            }
            why = "";
            return true;
        }

        public bool supportsOpenGLExtension(string extension)
        {
            return backend != null && backend.SupportsExtension(extension);
        }

        public bool supportsTextureFormat(TextureFormat fmt)
        {
            Debug.debugAssertM(!inPrimitive,
                "Cannot call supportsTextureFormat between beginPrimitive and endPrimitive.");

            bool supported;
            if (!supportedTextureFormatCache.TryGetValue(fmt, out supported))
            {
                supported = (backend != null) && backend.SupportsTextureFormat(fmt);
                supportedTextureFormatCache[fmt] = supported;
            }
            return supported;
        }

        public bool supportsTwoSidedStencil()
        {
            return backend != null && backend.SupportsTwoSidedStencil();
        }

        public bool supportsTextureRectangle()
        {
            return backend != null && backend.SupportsTextureRectangle();
        }

        public bool supportsVertexProgram()
        {
            return backend != null && backend.SupportsVertexProgram();
        }

        public bool supportsVertexProgramNV2()
        {
            return backend != null && backend.SupportsVertexProgramNV2();
        }

        public bool supportsPixelProgram()
        {
            return backend != null && backend.SupportsPixelProgram();
        }

        public bool supportsVertexBufferObject()
        {
            return backend != null && backend.SupportsVertexBufferObject();
        }

        public double getLightSaturation()
        {
            return lightSaturation;
        }

        // Internal method to set backend
        public void setBackend(IGraphicsBackend b)
        {
            backend = b;
            supportedTextureFormatCache.Clear();
            if (backend != null)
            {
                GLCaps.setBackend(backend);

                string renderer = backend.GetRenderer();
                string driverVersion = backend.GetDriverVersion();
                cardDescription = (renderer ?? "") + " " + (driverVersion ?? "");
                cardDescription = cardDescription.Trim();

                BackendCaps caps = backend.Caps;
                int numTextureUnits = (caps.MaxTextureUnits > 0) ? caps.MaxTextureUnits : GLCaps.G3D_MAX_TEXTURE_UNITS;
                int numTextures = (caps.MaxTextures > 0) ? caps.MaxTextures : numTextureUnits;
                int numTextureCoords = (caps.MaxTextureCoords > 0) ? caps.MaxTextureCoords : numTextureUnits;
                setCapabilities(numTextureUnits, numTextures, numTextureCoords);

                stencilBits = caps.SupportsStencil ? 8 : 0;
                depthBits = 24;
                lightSaturation = 1.0;
            }
            if (backend != null && _window != null)
            {
                backend.Resize(_window.Width, _window.Height);
                state.viewport = new Rect2D(0, 0, _window.Width, _window.Height);
                state.clip2D = state.viewport;
                backend.SetProjectionMatrix(state.matrices.projectionMatrix);
                applyModelViewMatrix();
            }
        }

        public void setWindow(IGraphicsWindow w)
        {
            _window = w;
        }

        internal void setCapabilities(int numTextureUnits, int numTextures, int numTextureCoords)
        {
            _numTextureUnits = numTextureUnits;
            _numTextures = numTextures;
            _numTextureCoords = numTextureCoords;
            // Resize texture unit arrays
            state.textureUnit = new RenderState.TextureUnit[numTextureUnits];
            for (int i = 0; i < numTextureUnits; ++i)
            {
                state.textureUnit[i] = new RenderState.TextureUnit();
            }
        }

        /**
          Enables or disables automatic presentation of the back buffer at the
          end of each frame.  This is equivalent to setSwapBuffersAutomatically.
         */
        public void setSwapBuffers(bool enable)
        {
            setSwapBuffersAutomatically(enable);
        }

        /**
          Presents the back buffer to the window immediately.
         */
        public void swapBuffers()
        {
            if (_window == null)
            {
                throw new InvalidOperationException(
                    "The render device was not created from a window, so there are no buffers to swap.");
            }
            _window.Present();
        }

        /**
          Captures the contents of the framebuffer and writes them to a file.
          Returns the name of the file that was written.
         */
        public string capture(string filename)
        {
            return screenshot(filename);
        }

        /**
          Returns the number of pixels in the currently bound render target.
         */
        public int numPixelsInMemory()
        {
            return getWidth() * getHeight();
        }

        /**
          Blocks until the graphics pipeline has completed all of the work
          that has been submitted so far.
         */
        public void checkGPUIdle()
        {
            if (backend == null)
            {
                throw new InvalidOperationException("No graphics backend is attached.");
            }
            Milestone milestone = createMilestone("checkGPUIdle");
            setMilestone(milestone);
            waitForMilestone(milestone);
        }
    }
}