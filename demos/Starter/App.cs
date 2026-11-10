/**
  @file Starter/App.cs

  Port of demos/main.cpp.  This is the canonical GApp/GApplet starter program;
  it exists as a C# demo so the bootstrap pattern can be verified in isolation.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace Starter
{
    public class App : GApp
    {
        private static System.Windows.Forms.Control s_control;
        private static System.Windows.Forms.Form s_form;

        protected override void main()
        {
            setDebugMode(true);
            debugController.setActive(true);

            // Load objects here
            sky = Sky.create(null, dataDir + "sky/");

            applet.run();
        }

        public ReferenceCountedPointer<Sky> sky;

        public Demo applet;

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

            applet = new Demo(this);
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
            form.Text = "G3D Starter Demo";
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
