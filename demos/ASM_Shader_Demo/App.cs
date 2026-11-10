/**
  @file ASM_Shader_Demo/App.cs

  Port of the App class body in ASM_Shader_Demo/main.cpp.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace ASM_Shader_Demo
{
    public class App : GApp
    {
        private static System.Windows.Forms.Control s_control;
        private static System.Windows.Forms.Form s_form;

        protected override void main()
        {
            if (!renderDevice.supportsVertexProgram())
            {
                G3D.Error.error("Critical Error",
                    "This demo requires a graphics card and driver with OpenGL Vertex programs.", true);
                endProgram = true;
                return;
            }

            // The original set the window caption; the backend-neutral window
            // contract does not expose one.
            setDebugMode(true);
            debugController.setActive(true);

            applet = new Demo(this);

            applet.run();
        }

        public Demo applet;

        public App(GAppSettings settings, string backend) : base(settings, CreateWindow(backend))
        {
            Model.globalApp = this;

            // Register the desktop image codecs used to load the sky
            G3D.Image.SystemDrawing.SystemDrawingImageDecoder.register();

            G3D.OpenGL.OpenGLWindow openGlWindow = this.window().Window as G3D.OpenGL.OpenGLWindow;
            G3D.Direct3D11.Direct3D11Window direct3D11Window = this.window().Window as G3D.Direct3D11.Direct3D11Window;
            IGraphicsWindow window = openGlWindow ?? (IGraphicsWindow)direct3D11Window;
            if (window == null)
            {
                throw new InvalidOperationException("GApp did not store the created window.");
            }

            if (string.Equals(backend, "d3d11", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(
                    "ASM_Shader_Demo loads an ARB vertex program (!!ARBvp1.0), which Direct3D 11 does not support. Run it with --backend=opengl.");
            }

            G3D.OpenGL.OpenGLBackend openGlBackend = new G3D.OpenGL.OpenGLBackend();
            openGlBackend.Initialize(window);
            renderDevice.setBackend(openGlBackend);

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

        private static GWindow CreateWindow(string backend)
        {
            IGraphicsWindow window = new G3D.OpenGL.OpenGLWindow();

            s_control = window is G3D.OpenGL.OpenGLWindow ? ((G3D.OpenGL.OpenGLWindow)window).Control : ((G3D.Direct3D11.Direct3D11Window)window).Control;

            // Create and show a Form to host the graphics control
            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            s_form = form;
            form.Text = "G3D Vertex Program Demo";
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
