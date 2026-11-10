/**
  @file GLSL_Demo/App.cs

  Port of the App class body in GLSL_Demo/main.cpp.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace GLSL_Demo
{
    public class App : GApp
    {
        private static System.Windows.Forms.Control s_control;
        private static System.Windows.Forms.Form s_form;

        protected override void main()
        {
            setDebugMode(true);
            debugController.setActive(false);

            // Load objects here
            debugLog.println("Loading textures\n");

            sky = Sky.create(renderDevice, dataDir + "sky/");

            // Find the path to our runtime files
            string path = "GLSL_Demo/";
            if (!System.IO.File.Exists(path + "bump.frag"))
            {
                path = "";
            }

            bumpShader = Shader.fromFiles(path + "bump.vert", path + "bump.frag");

            textureMap = new ReferenceCountedPointer<Texture>(Texture.fromFile(path + "rockwall.tga"));
            normalBumpMap = loadBumpAsNormalMap(path + "rockwall-bump.tga");
            debugLog.println("Launching Viewer\n");

            new Viewer(this).run();
        }

        public ReferenceCountedPointer<Sky> sky;
        public ReferenceCountedPointer<Texture> textureMap;
        public ReferenceCountedPointer<Texture> normalBumpMap;
        public Shader bumpShader;

        public App(GAppSettings settings, string backend) : base(settings, CreateWindow(backend))
        {
            // Register the desktop image codecs used to load the sky textures
            G3D.Image.SystemDrawing.SystemDrawingImageDecoder.register();

            // Set backend on render device
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
            dataDir = System.IO.Path.Combine(exeDir, "data") + System.IO.Path.DirectorySeparatorChar;
        }

        private static ReferenceCountedPointer<Texture> loadBumpAsNormalMap(string filename)
        {
            GImage normal = new GImage();
            GImage.computeNormalMap(new GImage(filename), normal, true, true);
            return new ReferenceCountedPointer<Texture>(
                Texture.fromGImage(filename, normal, null, Texture.WrapMode.TILE,
                    Texture.InterpolateMode.TRILINEAR_MIPMAP, Texture.Dimension.DIM_2D,
                    Texture.DepthReadMode.DEPTH_NORMAL, 2.0f));
        }

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
            form.Text = "GLSL Demo";
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
