/**
  @file Network_Demo/App.cs

  Port of Network_Demo/App.h and Network_Demo/main.cpp's App::main.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace Network_Demo
{
    /** Used for debugging */
    public static class GlobalApp
    {
        public static App app;
    }

    public class App : GApp
    {
        private static System.Windows.Forms.Control s_control;
        private static System.Windows.Forms.Form s_form;

        protected override void main()
        {
            setDebugMode(true);
            debugController.setActive(false);
            debugShowRenderingStats = false;
            debugQuitOnEscape = false;

            // Load objects here
            sky = Sky.create(renderDevice, dataDir + "sky/");
            font = GFont.fromFile(renderDevice, dataDir + "font/dominant.fnt");

            Browse browse = new Browse(this);
            Client client = new Client(this);

            while (!endProgram)
            {
                bool error = false;

                // First browse for a server (or start our own).
                browse.run();

                if (endProgram)
                {
                    return;
                }

                if (browse.host)
                {
                    // Start a server
                    hostingServer = new Server(this);

                    if (hostingServer.ok())
                    {
                        // Connect to that server
                        client.selectedServer = hostingServer.advertisement;

                        Array<NetAddress> myAddresses = new Array<NetAddress>();
                        networkDevice.localHostAddresses(myAddresses);
                        client.selectedServer.address = new NetAddress(myAddresses[0].ip(), (ushort)GamePort.GAME_PORT);
                    }
                    else
                    {
                        showError(
                            "Unable to create a server.",
                            "(Maybe there is already a server running on this machine.)",
                            renderDevice, font);
                        error = true;
                    }
                }
                else
                {
                    // Overwrite the port
                    client.selectedServer = browse.selectedServer;
                    client.selectedServer.address = new NetAddress(client.selectedServer.address.ip(), (ushort)GamePort.GAME_PORT);
                }

                if (!error)
                {
                    // Now enter the world as a client (our own server runs in the background)
                    client.run();
                }

                debugController.setActive(false);

                // If we were hosting, delete the server
                if (hostingServer != null)
                {
                    hostingServer.cleanup();
                    hostingServer = null;
                }
            }
        }

        public ReferenceCountedPointer<Sky> sky;

        public ReferenceCountedPointer<GFont> font;

        /** NULL if not hosting.  Client is responsible
            for calling doSimulation and doNetwork
            on the hostingServer when it is non-null.
         */
        public Server hostingServer;

        public DiscoverySettings discoverySettings;

        private static void showError(string s1, string s2, RenderDevice rd, ReferenceCountedPointer<GFont> font)
        {
            double t0 = global::G3D.System.time();

            double w = rd.width();
            while (global::G3D.System.time() < t0 + 4)
            {
                rd.beginFrame();
                rd.clear();
                rd.push2D();
                font.pointer().draw2D(rd, s1, new Vector2((float)(w / 2), 100), 30,
                    new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_CENTER);
                font.pointer().draw2D(rd, s2, new Vector2((float)(w / 2), 200), 20,
                    new Color4(Color3.cyan(), 1), new Color4(Color3.black(), 1), GFont.XAlign.XALIGN_CENTER);
                rd.pop2D();
                rd.endFrame();
            }
        }

        public App(GAppSettings settings, string backend) : base(settings, CreateWindow(backend))
        {
            GlobalApp.app = this;

            // Register the desktop image codecs used to load the sky textures
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

            processEvents = () => System.Windows.Forms.Application.DoEvents();

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = System.IO.Path.GetDirectoryName(exePath);
            dataDir = System.IO.Path.Combine(exeDir, "data") + System.IO.Path.DirectorySeparatorChar;

            debugFontName = "dominant.fnt";

            hostingServer = null;
            discoverySettings = new DiscoverySettings("Demo Discovery Protocol", 1);
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
            form.Text = "G3D Network Demo";
            form.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            form.ClientSize = new System.Drawing.Size(640, 480);
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
