/**
  @file gfxmeter/App.cs

  Port of gfxmeter/App.h and the App members of gfxmeter/main.cpp.
*/

using System;
using System.Windows.Forms;
using G3D;

namespace gfxmeter
{
    public class App : GApp
    {
        private static System.Windows.Forms.Control s_control;
        private static System.Windows.Forms.Form s_form;

        public ReferenceCountedPointer<Sky> sky;

        public Texture cardLogo;
        public Texture chipLogo;
        public string chipSpeed;
        public Texture osLogo;

        public ReferenceCountedPointer<GFont> titleFont;
        public ReferenceCountedPointer<GFont> reportFont;

        public string combineShader;
        public string asmShader;
        public string glslShader;

        public int featureRating;
        public float performanceRating;
        public int bugCount;

        public struct VertexPerformanceData
        {
            public int numTris;
            public float[] beginEndFPS;
            public float[] drawElementsRAMFPS;
            public float[] drawElementsVBOFPS;
            public float[] drawElementsVBO16FPS;
            public float[] drawElementsVBOIFPS;
            /* Turn shading off and just slam vertices through */
            public float[] drawElementsVBOPeakFPS;
            public float drawArraysVBOPeakFPS;
        }

        public VertexPerformanceData vertexPerformance;

        public Report applet;

        /** The backend name this instance was created for. */
        public string backend;

        public App(GAppSettings settings, string backendArg)
            : base(settings, CreateWindow(backendArg))
        {
            backend = backendArg;

            // Register the desktop image codecs used to load the logos and skins
            G3D.Image.SystemDrawing.SystemDrawingImageDecoder.register();

            G3D.OpenGL.OpenGLWindow openGlWindow = this.window().Window as G3D.OpenGL.OpenGLWindow;
            G3D.Direct3D11.Direct3D11Window direct3D11Window = this.window().Window as G3D.Direct3D11.Direct3D11Window;
            IGraphicsWindow window = openGlWindow ?? (IGraphicsWindow)direct3D11Window;
            if (window == null)
            {
                throw new InvalidOperationException("GApp did not store the created window.");
            }

            G3D.OpenGL.OpenGLBackend openGlBackend = null;
            G3D.Direct3D11.Direct3D11Backend direct3D11Backend = null;

            if (string.Equals(backend, "d3d11", StringComparison.OrdinalIgnoreCase))
            {
                direct3D11Backend = new G3D.Direct3D11.Direct3D11Backend();
                direct3D11Backend.Initialize(window);
                renderDevice.setBackend(direct3D11Backend);
            }
            else
            {
                openGlBackend = new G3D.OpenGL.OpenGLBackend();
                openGlBackend.Initialize(window);
                renderDevice.setBackend(openGlBackend);
            }

            s_control.Resize += delegate
            {
                renderDevice.notifyResize(s_control.ClientSize.Width, s_control.ClientSize.Height);
            };

            // Closing the host window ends the frame loop, the way the original
            // harness ends the program when SDL reports SDL_QUIT.
            s_form.FormClosed += delegate
            {
                endProgram = true;
            };

            // Set process events callback
            processEvents = () => System.Windows.Forms.Application.DoEvents();

            // Use data directory relative to executable
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = System.IO.Path.GetDirectoryName(exePath);
            dataDir = System.IO.Path.Combine(exeDir, "data-files") + System.IO.Path.DirectorySeparatorChar;

            // The original sets the caption on the GWindow; GApp exposes the
            // window through accessors that are not available before construction
            // completes, so the form title is set by the bootstrap instead.
            s_form.Text = Format.format("gfx-meter %03.1f", gfxMeterVersion);

            reportFont = GFont.fromFile(renderDevice, dataDir + "arial.fnt");

            applet = new Report(this);
        }

        protected override void main()
        {
            setDebugMode(false);
            debugController.setActive(false);

            // Choose a card logo
            {
                string filename = "";
                if (GLCaps.vendor().StartsWith("NVIDIA"))
                {
                    filename = "nvidia.jpg";
                }
                else if (GLCaps.vendor().StartsWith("ATI"))
                {
                    filename = "ati.jpg";
                }

                if (filename != "")
                {
                    cardLogo = Texture.fromFile(dataDir + filename, TextureFormat.AUTO, Texture.WrapMode.CLAMP, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
                }
            }

            // Choose chip logo
            {
                string filename = "";
                if (G3D.System.cpuVendor().ToLowerInvariant().EndsWith("intel"))
                {
                    filename = "intel.jpg";
                }
                else if (G3D.System.cpuVendor().ToLowerInvariant().EndsWith("amd"))
                {
                    filename = "amd.jpg";
                }

                if (filename != "")
                {
                    chipLogo = Texture.fromFile(dataDir + filename, TextureFormat.AUTO, Texture.WrapMode.CLAMP, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
                }

                double speed = CPUSpeedMHz * 1e6;
                if (speed > 1e9)
                {
                    chipSpeed = Format.format("%.1f GHz", speed / 1e9);
                }
                else if (speed > 10e6)
                {
                    chipSpeed = Format.format("%.1f MHz", speed / 1e6);
                }
                // Probably a bad result if speed is less than 1 MHz
            }

            // Choose os logo
            {
                string filename = "";
                string os = G3D.System.operatingSystem();

                if (os.StartsWith("Windows 5.0"))
                {
                    filename = "win2k.jpg";
                }
                else if (os.StartsWith("Windows 5.1"))
                {
                    filename = "winxp.jpg";
                }

                if (filename != "")
                {
                    osLogo = Texture.fromFile(dataDir + filename, TextureFormat.AUTO, Texture.WrapMode.CLAMP, Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D, 1.0);
                }
            }

            titleFont = GFont.fromFile(renderDevice, dataDir + "carbon.fnt");

            shaderVersions(out combineShader, out asmShader, out glslShader);
            computeFeatureRating();

            Log.common().printf("Shaders:\n");
            Log.common().printf("   Combiners: %s\n", combineShader);
            Log.common().printf("   Assembly:  %s\n", asmShader);
            Log.common().printf("   GLSL:      %s\n", glslShader);
            Log.common().printf("\n\n");

            countBugs();

            // Load objects here
            sky = null;//Sky.create(null, dataDir + "sky/");

            applet.run();
        }

        /**
          The original reads the clock frequency with inline assembly (RDTSC).
          Safe C# cannot execute inline assembly, so the frequency is read from
          the registry, which is the same value Windows reports for the nominal
          speed and the port's RegistryUtil can read.
        */
        private double CPUSpeedMHz
        {
            get
            {
                int value = 0;
                if (RegistryUtil.readInt32(
                        "HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0",
                        ref value))
                {
                    return value;
                }
                return 0;
            }
        }

        public void countBugs()
        {
            bugCount = 0;

            Log.common().printf("\n\nBugs:\n");
            if (GLCaps.hasBug_glMultiTexCoord3fvARB())
            {
                ++bugCount;
                Log.common().printf("   Detected glMultiTexCoord3fvARB bug\n\n");
            }


            if (GLCaps.hasBug_normalMapTexGen())
            {
                ++bugCount;
                Log.common().printf("   Detected normalMapTexGen bug\n\n");
            }

            if (GLCaps.hasBug_slowVBO())
            {
                ++bugCount;
                Log.common().printf("   Detected slowVBO bug\n\n");
            }

            if (GLCaps.hasBug_redBlueMipmapSwap())
            {
                ++bugCount;
                Log.common().printf("   Detected redBlueMipmapSwap bug\n\n");
            }

            if (GLCaps.renderer().StartsWith("RADEON") &&
                GLCaps.supports("GL_ARB_shadow") &&
                GLCaps.supports("GL_ARB_shading_language_100"))
            {
                // Slow shadow map and GLSL texture binding bugs.
                // TODO: add actual tests
                Log.common().printf("   Detected slowShadowMap bug\n\n");
                Log.common().printf("   Detected GLSL Texture Binding bug\n\n");
                bugCount += 2;
            }

            if (GLCaps.hasBug_mipmapGeneration())
            {
                ++bugCount;
                Log.common().printf("   Detected mipmapGeneration bug\n\n");
            }

        }


        public void computeFeatureRating()
        {

            if ((glslShader != "None") &&
                GLCaps.supports("GL_ARB_shadow") &&
                GLCaps.supports("GL_EXT_framebuffer_object") &&
                GLCaps.supports("GL_EXT_stencil_wrap"))
            {

                // NV47 w/ good drivers
                featureRating = 60;

            }
            else if ((glslShader != "None") &&
                GLCaps.supports("GL_ARB_shadow") &&
                GLCaps.supports("GL_EXT_stencil_wrap"))
            {

                // R800 w/ good drivers
                featureRating = 50;

            }
            else if ((asmShader != "None") &&
                GLCaps.supports("GL_ARB_shadow") &&
                GLCaps.supports("GL_EXT_stencil_wrap"))
            {

                // NV30
                featureRating = 40;

            }
            else if ((asmShader != "None") &&
                GLCaps.supports("GL_EXT_stencil_wrap"))
            {

                featureRating = 35;

            }
            else if (asmShader != "None")
            {

                // PS 2.0 card
                featureRating = 30;

            }
            else if ((combineShader != "None") &&
                GLCaps.supports_GL_ARB_multitexture() &&
                GLCaps.supports_GL_EXT_texture_cube_map())
            {

                // PS 1.4 card
                featureRating = 20;

            }
            else if (
                GLCaps.supports_GL_ARB_multitexture() &&
                GLCaps.supports_GL_EXT_texture_cube_map())
            {

                featureRating = 15;

            }
            else if (GLCaps.supports_GL_EXT_texture_cube_map())
            {

                featureRating = 14;

            }
            else
            {

                featureRating = 10;
            }
        }

        private static void shaderVersions(out string regStr, out string asmStr, out string glslStr)
        {
            Array<string> reg = new Array<string>();
            Array<string> xasm = new Array<string>();

            glslStr = "None";

            if (GLCaps.supports("GL_NV_register_combiners"))
            {
                if (GLCaps.supports("GL_NV_register_combiners2"))
                {
                    reg.append("NV rc2");
                }
                else
                {
                    reg.append("NV rc");
                }
            }

            if (GLCaps.supports("GL_NV_texture_shader"))
            {
                if (GLCaps.supports("GL_NV_texture_shader2"))
                {
                    if (GLCaps.supports("GL_NV_texture_shader3"))
                    {
                        reg.append("NV ts3");
                    }
                    else
                    {
                        reg.append("NV ts2");
                    }
                }
                else
                {
                    reg.append("NV ts");
                }
            }


            if (GLCaps.supports("GL_ATI_fragment_shader"))
            {
                reg.append("ATI fs");
            }


            if (GLCaps.supports("GL_EXT_vertex_shader"))
            {
                reg.append("EXT vs");
            }

            if (GLCaps.supports("GL_ARB_fragment_program"))
            {
                // arbfp1;
                xasm.append("ARB fp1");
            }

            if (GLCaps.supports("GL_ARB_vertex_program"))
            {
                // arbvp1
                xasm.append("ARB vp1");
            }

            if (GLCaps.supports("GL_NV_fragment_program"))
            {
                if (GLCaps.supports("GL_NV_fragment_program2"))
                {
                    xasm.append("NV fp40");
                }
                else
                {
                    xasm.append("NV fp30");
                }
            }

            if (GLCaps.supports("GL_NV_vertex_program"))
            {
                if (GLCaps.supports("GL_NV_vertex_program2"))
                {
                    if (GLCaps.supports("GL_NV_vertex_program3"))
                    {
                        xasm.append("NV vp3");
                    }
                    else
                    {
                        xasm.append("NV vp2");
                    }
                }
                else
                {
                    xasm.append("NV vp");
                }
            }


            if (GLCaps.supports_GL_ARB_shader_objects() &&
                GLCaps.supports_GL_ARB_shading_language_100() &&
                GLCaps.supports_GL_ARB_fragment_shader() &&
                GLCaps.supports_GL_ARB_vertex_shader())
            {
                glslStr = "1.00";
            }

            if (reg.length() == 0)
            {
                regStr = "None";
            }
            else
            {
                regStr = G3D.StringUtils.stringJoin(reg, ", ");
            }


            if (xasm.length() == 0)
            {
                asmStr = "None";
            }
            else
            {
                asmStr = G3D.StringUtils.stringJoin(xasm, ", ");
            }

        }

        /** Also loads reportFont and gfxMeterTexture
          The original shows a splash screen that measures vertex performance and
          is compiled out of its shipped build by FAST, because the measurement
          path is 936 lines of raw OpenGL that bypass the renderer's abstraction.
          The port leaves it out for the same reason and reports the omission here
          rather than leaving a silent gap.
        */
        public void showSplashScreen()
        {
            throw new NotSupportedException(
                "gfxmeter's splash screen is not ported.  It exists only to run "
                + "measureVertexPerformance, which benchmarks raw OpenGL paths "
                + "(immediate mode, VBO, uint16 indices, interleaved arrays) "
                + "that the port's backend-neutral renderer deliberately does "
                + "not expose.  The original's own shipped build compiles the "
                + "same code out under FAST.");
        }

        private const float gfxMeterVersion = 0.9f;

        private static GWindow CreateWindow(string backend)
        {
            IGraphicsWindow window;
            if (string.Equals(backend, "d3d11", StringComparison.OrdinalIgnoreCase))
            {
                window = new G3D.Direct3D11.Direct3D11Window();
            }
            else
            {
                window = new G3D.OpenGL.OpenGLWindow();
            }

            s_control = window is G3D.OpenGL.OpenGLWindow ? ((G3D.OpenGL.OpenGLWindow)window).Control : ((G3D.Direct3D11.Direct3D11Window)window).Control;

            // Create and show a Form to host the graphics control
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            s_form = form;
            form.Text = "gfx-meter";
            form.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            form.ClientSize = new System.Drawing.Size(800, 600);
            s_control.Dock = System.Windows.Forms.DockStyle.Fill;
            form.Controls.Add(s_control);
            form.Show();
            s_control.CreateControl();
            System.Windows.Forms.Application.DoEvents();
            System.Windows.Forms.Application.DoEvents();

            GWindow gw = new GWindow(window);
            return gw;
        }
    }
}
