/**
  @file demos/main-no-GApp.cpp

  This is a prototype main.cpp to use for your programs.  It is a good
  infrastructure for building an interactive demo.  See also G3D::GApp
  and G3D::GApplet for a more object-oriented approach (that automates
  most of the boilerplate).

  @maintainer Morgan McGuire, matrix@graphics3d.com

  @created 2002-02-27
  @edited  2006-05-10
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using G3D;

namespace No_App
{
    public static class Program
    {
        private static string DATA_DIR;

        private static Log debugLog = null;
        private static RenderDevice renderDevice = null;
        private static ReferenceCountedPointer<GFont> font = null;
        private static UserInput userInput = null;
        private static ManualCameraController controller = null;
        private static GCamera camera = new GCamera();
        private static bool endProgram = false;

        private static System.Windows.Forms.Control s_control;
        private static System.Windows.Forms.Form s_form;

        // No error checking
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Initialize
            DATA_DIR = G3D.System.demoFindData(true);

            debugLog = new Log("log.txt");

            renderDevice = new RenderDevice();

            GWindow.Settings settings = new GWindow.Settings();
            settings.fsaaSamples = 1;
            settings.resizable = true;

            GWindow gw = CreateWindow(settings);
            renderDevice.init(gw, debugLog);

            userInput = new UserInput();

            font = GFont.fromFile(renderDevice, DATA_DIR + "font/dominant.fnt");

            controller = new ManualCameraController(renderDevice, userInput);

            controller.setMoveRate(10);
            controller.setPosition(new Vector3(0.0f, 0.0f, 4.0f));
            controller.lookAt(new Vector3(-2.0f, 3.0f, -5.0f));

            controller.setActive(true);

            renderDevice.resetState();
            renderDevice.setColorClearValue(new Color4(0.1f, 0.5f, 1.0f));

            double now = G3D.System.getTick() - 0.001f, lastTime;

            // Main loop
            do
            {
                lastTime = now;
                now = G3D.System.getTick();
                GameTime timeStep = now - lastTime;

                doUserInput();

                doSimulation(timeStep);

                doGraphics();

                Application.DoEvents();
            }
            while (!endProgram);

            // Cleanup
            controller = null;
            userInput = null;
            renderDevice.cleanup();
            renderDevice = null;
            debugLog.Dispose();
            debugLog = null;
        }

        /**
          Creates the platform window the original let RenderDevice::init
          create from RenderDeviceSettings, and binds the render device to it
          with the same settings.  The bootstrap is the same one the other
          demo projects use.
        */
        private static GWindow CreateWindow(GWindow.Settings settings)
        {
            string backend = "opengl";
            foreach (string arg in Environment.GetCommandLineArgs())
            {
                if (arg != null && arg.StartsWith("--backend=", StringComparison.OrdinalIgnoreCase))
                {
                    backend = arg.Substring("--backend=".Length);
                }
            }

            System.Windows.Forms.Form form = new System.Windows.Forms.Form();
            s_form = form;
            form.Text = "G3D Demo";
            form.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            form.ClientSize = new Size(settings.width, settings.height);

            IGraphicsWindow window;
            if (string.Equals(backend, "d3d11", StringComparison.OrdinalIgnoreCase))
            {
                window = new G3D.Direct3D11.Direct3D11Window();
            }
            else
            {
                window = new G3D.OpenGL.OpenGLWindow();
            }

            s_control = window is G3D.OpenGL.OpenGLWindow
                ? ((G3D.OpenGL.OpenGLWindow)window).Control
                : ((G3D.Direct3D11.Direct3D11Window)window).Control;
            s_control.Dock = System.Windows.Forms.DockStyle.Fill;
            form.Controls.Add(s_control);
            form.Show();
            s_control.CreateControl();
            Application.DoEvents();
            Application.DoEvents();

            IGraphicsBackend graphicsBackend;
            if (string.Equals(backend, "d3d11", StringComparison.OrdinalIgnoreCase))
            {
                graphicsBackend = new G3D.Direct3D11.Direct3D11Backend();
            }
            else
            {
                graphicsBackend = new G3D.OpenGL.OpenGLBackend();
            }
            graphicsBackend.Initialize(window);
            renderDevice.setBackend(graphicsBackend);

            form.Resize += delegate
            {
                renderDevice.notifyResize(s_control.ClientSize.Width, s_control.ClientSize.Height);
            };

            // Closing the host window ends the frame loop, the way the original
            // harness ends the program when SDL reports SDL_QUIT.
            form.FormClosed += delegate
            {
                endProgram = true;
            };

            GWindow gw = new GWindow(window);

            // The original window reports SDL_VIDEORESIZE for every host resize
            form.Resize += delegate
            {
                GEvent resizeEvent = new GEvent(GEventType.RESIZE);
                resizeEvent.width = s_control.ClientSize.Width;
                resizeEvent.height = s_control.ClientSize.Height;
                gw.postEvent(resizeEvent);
            };

            return gw;
        }

        //////////////////////////////////////////////////////////////////////////////


        private static void doSimulation(GameTime timeStep)
        {
            // Simulation
            controller.doSimulation(g3dmath.clamp(timeStep, 0.0, 0.1));
            camera.setCoordinateFrame(controller.getCoordinateFrame());
        }



        private static void doGraphics()
        {
            LightingParameters lighting = new LightingParameters(G3D.G3DGameUnits.toSeconds(11, 0, 0, G3D.AMPM.AM));

            renderDevice.beginFrame();
                // Cyan background
                renderDevice.setColorClearValue(new Color4(0.1f, 0.5f, 1.0f));

                renderDevice.clear(true, true, true);
                renderDevice.pushState();

                    renderDevice.setProjectionAndCameraMatrix(camera);

                    renderDevice.enableLighting();
                    renderDevice.setLight(0, GLight.directional(lighting.lightDirection,
                        lighting.lightColor));
                    renderDevice.setAmbientLightColor(lighting.ambient);

                    Draw.axes(renderDevice);

                renderDevice.popState();

            renderDevice.endFrame();
        }


        private static void doUserInput()
        {
            userInput.beginEvents();

            // Event handling
            GEvent inputEvent;
            while (renderDevice.window().pollEvent(out inputEvent))
            {
                switch (inputEvent.type)
                {
                case GEventType.QUIT:
                    endProgram = true;
                    break;

                case GEventType.RESIZE:
                    {
                        renderDevice.notifyResize(inputEvent.width, inputEvent.height);
                        Rect2D full = Rect2D.xywh(0.0f, 0.0f, renderDevice.width(), renderDevice.height());
                        renderDevice.setViewport(full);
                    }
                    break;


                case GEventType.KEY_DOWN:
                    switch (inputEvent.key)
                    {
                    case 27: // SDLK_ESCAPE
                        endProgram = true;
                        break;

                    case 9: // SDLK_TAB
                        controller.setActive(!controller.active());
                        break;

                    // Add other key handlers here
                    default:
                        break;
                    }
                    break;

                // Add other event handlers here

                default:
                    break;
                }

                userInput.processEvent(inputEvent);
            }

            userInput.endEvents();
        }
    }
}
