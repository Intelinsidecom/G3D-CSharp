/**
  @file GApp.h
  
  @maintainer Morgan McGuire, morgan@graphics3d.com

  @created 2003-11-03
  @edited  2006-03-04
*/

using System;
using System.IO;
using G3D;

namespace G3D
{
    // Forward declarations not needed in C#
    // class RenderDevice;
    // class UserInput;
    // class CFont;

    /**
      See @link guideapp @endlink for a discussion of GApp and GApplet.
    */
    public abstract class GApp : IDisposable
    {
        public class Settings
        {
            public GWindow.Settings    window;

            /**
             If "<AUTO>", G3D will search for the standard
             data files.  It is recommended that you override this
             default and set dataDir to a directory relative
             to your executable (e.g. "./data/")
             so your programs can be distributed to users who
             do not have full the G3D data directory.
            */
            public string             dataDir;

            /**
             Can be relative to the G3D data directory (e.g. "font/dominant.fnt")
             or relative to the current directory.
             Default is "console-small.fnt"
            */
            public string             debugFontName;

            public string             logFilename;

            /** 
              When true, GAapp ensures that g3d-license.txt exists in the current
              directory.  That file is written from the return value of G3D::license() */
            public bool               writeLicenseFile;

            /** When true, the networkDevice is initialized.  Defaults to true. */
            public bool               useNetwork;

            public Settings()
            {
                window = new GWindow.Settings();
                dataDir = "<AUTO>";
                debugFontName = "console-small.fnt";
                logFilename = "log.txt";
                writeLicenseFile = true;
                useNetwork = true;
            }
        }

        private bool                    _debugMode;

        /**
         Tracks whether the debug controller (activated with TAB) was
         active when last we were in debug mode.
        */
        private bool                    _debugControllerWasActive;

        private bool                    _fontWarningLogged;

        private bool                    _paused;

        /** Called from init. */
        private void loadFont(string fontName)
        {
            loadFont(fontName, true);
        }

        private void loadFont(string fontName, bool warn)
        {
            string filename = fontName;
            if (!File.Exists(filename))
            {

                if (File.Exists(dataDir + filename))
                {
                    filename = dataDir + filename;
                }
                else if (File.Exists(dataDir + "font/" + filename))
                {
                    filename = dataDir + "font/" + filename;
                }
            }

            if (File.Exists(filename))
            {
                debugFont = GFont.fromFile(renderDevice, filename);
            }
            else
            {
                if (warn && !_fontWarningLogged)
                {
                    _fontWarningLogged = true;
                    debugLog.printf(
                        "Warning: G3D::GApp could not load font \"%s\".\n"
                        + "This may be because the G3D::GApp::Settings::dataDir was not\n"
                        + "properly set in main().\n",
                        filename);
                }

                debugFont = null;
            }
        }

        private GWindow                _window;
        private bool                    _hasUserCreatedWindow;

        private Stopwatch               m_timer = new Stopwatch();

        private double                  m_lastTick = 0;

        protected internal Stopwatch           m_graphicsWatch = new Stopwatch();
        protected internal Stopwatch           m_logicWatch = new Stopwatch();
        protected internal Stopwatch           m_networkWatch = new Stopwatch();
        protected internal Stopwatch           m_userInputWatch = new Stopwatch();
        protected internal Stopwatch           m_simulationWatch = new Stopwatch();
        protected internal Stopwatch           m_waitWatch = new Stopwatch();

        protected internal ReferenceCountedPointer<GModuleManager>   m_moduleManager = GModuleManager.create();


        /**
         Initialized to GApp::Settings::dataDir, or if that is "<AUTO>", 
            to System::demoFindData(). To make your program
            distributable, override the default 
            and copy all data files you need to a local directory.
            Recommended setting is "data/" or "./", depending on where
            you put your data relative to the executable.

            Your data directory must contain the default debugging font, 
            "console-small.fnt", unless you change it.
        */
        public string             dataDir;
        public Log                debugLog;
        public RenderDevice       renderDevice;
        public NetworkDevice      networkDevice;

        /**
         NULL if not loaded
        */
        public ReferenceCountedPointer<GFont>                debugFont;
        public UserInput          userInput;
        public bool                endProgram;

        /** The name of the font file used for the debug text. */
        public string             debugFontName = "console-small.fnt";

        /**
         A global texture manager.
        */
        public TextureManager     textureManager = new TextureManager();

        /**
         A default camera that is driven by the debugController.
        */
        public GCamera            debugCamera = new GCamera();

        /**
         When in debugMode, this allows first person (Quake game-style) control
         using the arrow keys or W,A,S,D and the mouse.
         @deprecated Will become a FirstPersonManipulatorRef in 7.00
        */
        public ManualCameraController  debugController = new ManualCameraController();

        /**
         Strings that have been printed with debugPrint.
        */
        public Array<string>      debugText = new Array<string>();

        public Stopwatch graphicsWatch()
        {
            return m_graphicsWatch;
        }

        public Stopwatch waitWatch()
        {
            return m_waitWatch;
        }

        public Stopwatch logicWatch()
        {
            return m_logicWatch;
        }

        public Stopwatch networkWatch()
        {
            return m_networkWatch;
        }

        public Stopwatch userInputWatch()
        {
            return m_userInputWatch;
        }

        public Stopwatch simulationWatch()
        {
            return m_simulationWatch;
        }

        public Stopwatch timer()
        {
            return m_timer;
        }

        public ReferenceCountedPointer<GModuleManager> moduleManager()
        {
            return m_moduleManager;
        }

        public GWindow window()
        {
            return _window;
        }

        /** Returns the state of debugMode.
            All debugX options are only in effect
            when debugMode is also true.  Default is false*/
        public bool debugMode()
        {
            return _debugMode;
        }

        /**
          Changes the state of debugMode. 
          You must <B>separately</B> activate the debugController 
          if you want events to go to it (by default, the TAB key
          activates it).
         */
        public virtual void setDebugMode(bool b)
        {
            if (!b)
            {
                _debugControllerWasActive = debugMode();
            } else
            {
                debugController.setActive(_debugControllerWasActive);
            }
            _debugMode = b;
        }

        /**
         When true and debugMode is true, debugPrintf prints to the screen.
         (default is true)
        */
        public bool                    debugShowText = true;

        /**
         When true and debugMode is true, an SDL_ESCAPE keydown event
         quits the program.
         (default is true)
        */
        public bool                    debugQuitOnEscape = true;

        /**
         When true and debugMode is true, SDL_TAB keydown deactivates
         the camera and restores the mouse cursor.
         (default is true)
        */
        public bool                    debugTabSwitchCamera = true;

        /**
         When debugMode is true and debugShowRenderingStats is true,
         renderDebugInfo prints the frame rate and other data to the screen.
        */
        public bool                    debugShowRenderingStats = true;

        /**
         When true and the window is resizable, automatically
         responds to SDL_RESIZE events by notifying the
         RenderDevice that the window has been resized and
         resetting the viewport to full screen.
         (default is true)
        */
        public bool                    autoResize = true;

        /**
          When true, there is an assertion failure if an exception is 
          thrown during GApp::main().

          Default is true.
          */
        public bool                    catchCommonExceptions = true;

        /**
         If app->debugShowText is true, prints to an on-screen buffer that
         is cleared every frame.
        */
        public virtual void debugPrintf(string fmt, params object[] args)
        {
            if (debugMode() && debugShowText)
            {
                string s = Format.format(fmt, args);
                debugText.append(s);
            }    
        }

        /**
         Called from GApplet::run immediately after doGraphics to render
         the debugging text.  Does nothing if debugMode is false.  It
         is not usually necessary to override this method.
        */
        public virtual void renderDebugInfo()
        {
            if (!debugMode())
            {
                return;
            }

            if (renderDevice == null)
            {
                return;
            }

            if (debugFont == null)
            {
                loadFont(debugFontName, false);
            }

            if (debugFont == null)
            {
                return;
            }

            RenderDevice rd = renderDevice;
            Color4 color = new Color4(1, 1, 1, 1);
            double size = 10;

            double x = 5;
            Vector2 pos = new Vector2((float)x, 5);

            uint majGL = rd.debugNumMajorOpenGLStateChanges();
            uint majAll = rd.debugNumMajorStateChanges();
            uint minGL = rd.debugNumMinorOpenGLStateChanges();
            uint minAll = rd.debugNumMinorStateChanges();
            uint pushCalls = rd.debugNumPushStateCalls();

            rd.push2D();

            if (debugShowRenderingStats)
            {
                rd.setBlendFunc(RenderDevice.BlendFunc.BLEND_SRC_ALPHA, RenderDevice.BlendFunc.BLEND_ONE_MINUS_SRC_ALPHA);
                Draw.fastRect2D(Rect2D.xywh(2, 2, 796, (float)(size * 5)), rd, new Color4(0, 0, 0, 0.3f));

                Color4 statColor = new Color4(Color3.yellow(), 1.0f);

                debugFont.pointer().draw2D(rd, rd.getCardDescription() + "   " + System.version(), pos, size, color);
                pos.y += (float)(size * 1.5);

                string s = Format.format(
                    "% 4dfps % 4.1gM tris % 4.1gM tris/s   GL Calls: %d/%d Maj; %d/%d Min; %d push",
                    (int)g3dmath.iRound(m_graphicsWatch.smoothFPS()),
                    g3dmath.iRound(rd.getTrianglesPerFrame() / 1e5) * .1f,
                    g3dmath.iRound(rd.getTrianglesPerFrame() / 1e5) * .1f,
                    majGL, majAll, minGL, minAll, pushCalls);
                debugFont.pointer().draw2D(rd, s, pos, size, statColor);

                pos.x = (float)x;
                pos.y += (float)(size * 1.5);

                string timing = Format.format(
                    "Time: %3.0f%% Gfx, %3.0f%% Sim, %3.0f%% Lgc, %3.0f%% Net, %3.0f%% UI, %3.0f%% wait",
                    getNormalizedTime(m_graphicsWatch),
                    getNormalizedTime(m_simulationWatch),
                    getNormalizedTime(m_logicWatch),
                    getNormalizedTime(m_networkWatch),
                    getNormalizedTime(m_userInputWatch),
                    getNormalizedTime(m_waitWatch));
                debugFont.pointer().draw2D(rd, timing, pos, size, statColor);

                pos.x = (float)x;
                pos.y += (float)(size * 3);
            }

            for (int i = 0; i < debugText.size(); ++i)
            {
                debugFont.pointer().draw2D(rd, debugText[i], pos, size, color, new Color4(0, 0, 0, 1));
                pos.y += (float)(size * 1.5);
            }

            rd.pop2D();
        }

        private double getNormalizedTime(Stopwatch watch)
        {
            float g = (float)m_graphicsWatch.smoothElapsedTime();
            float n = (float)m_networkWatch.smoothElapsedTime();
            float s = (float)m_simulationWatch.smoothElapsedTime();
            float L = (float)m_logicWatch.smoothElapsedTime();
            float u = (float)m_userInputWatch.smoothElapsedTime();
            float w = (float)m_waitWatch.smoothElapsedTime();

            float total = g + n + s + L + u + w;
            if (total <= 0)
            {
                return 0;
            }

            return (float)watch.smoothElapsedTime() * (100.0f / total);
        }

        /**
          @param window If null, a SDLWindow will be created for you. This
              argument is useful for substituting a different window
              system (e.g. GlutWindow)
        */
        public GApp(Settings options = null, GWindow window = null)
        {
            if (options == null)
            {
                options = new Settings();
            }

            debugLog          = null;
            debugFont         = null;
            endProgram        = false;
            _debugControllerWasActive = false;
            m_moduleManager = GModuleManager.create();

            if (options.dataDir == "<AUTO>")
            {
                dataDir = System.demoFindData(false);
            } else
            {
                dataDir = options.dataDir;
            }

            debugFontName = options.debugFontName;

            if (options.writeLicenseFile && !FileUtils.fileExists("g3d-license.txt"))
            {
                writeLicense();
            }

            debugLog    = new Log(options.logFilename);

            if (window != null)
            {
                _hasUserCreatedWindow = true;
            } else
            {
                _hasUserCreatedWindow = false;
                window = createWindow(options);
            }

            renderDevice = createRenderer(options, window);

            _window = renderDevice.window();
            if (_window == null)
            {
                throw new InvalidOperationException("The RenderDevice did not produce a window.");
            }

            _window.makeCurrent();

            if (options.useNetwork)
            {
                networkDevice = new NetworkDevice();
                networkDevice.init(debugLog);
            } else
            {
                networkDevice = null;
            }

            {
                TextOutput t = new TextOutput(new TextOutput.Options());

                t.writeSymbols("System", "{", "", "", "", "");
                t.pushIndent();
                t.writeNewline();
                System.describeSystem(t);
                if (renderDevice != null)
                {
                    renderDevice.describeSystem(t);
                }

                if (networkDevice != null)
                {
                    networkDevice.describeSystem(t);
                }
                t.writeNewline();
                t.writeSymbol("}");
                t.writeNewline();

                string s = "";
                t.commitString(ref s);
                debugLog.printf("%s\n", s);
            }

            debugCamera  = new GCamera();

            createGUI();

            userInput = new UserInput(_window, null);

            createDebugController();

            m_lastTick = System.getTick();

            autoResize                  = true;
            _debugMode                  = false;
            debugShowText               = true;
            debugQuitOnEscape           = true;
            debugTabSwitchCamera        = true;
            debugShowRenderingStats     = true;
            catchCommonExceptions       = true;
        }

        /** Attempt to write license file */
        private static void writeLicense()
        {
            StreamWriter f = null;
            try
            {
                f = new StreamWriter("g3d-license.txt", false);
                f.Write(System.license());
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            finally
            {
                if (f != null)
                {
                    f.Dispose();
                }
            }
        }

        public virtual RenderDevice createRenderer(Settings options, GWindow window)
        {
            RenderDevice device = new RenderDevice();
            device.init(window, debugLog);
            return device;
        }

        protected virtual GWindow createWindow(Settings options)
        {
            throw new NotSupportedException(
                "GLG3D cannot create a platform window.  Pass a GWindow to the GApp constructor or override GApp.createWindow.");
        }

        public virtual void createGUI()
        {
            loadFont(debugFontName);
        }

        public virtual void createDebugController()
        {
            debugController.init(renderDevice, userInput);
            debugController.setMoveRate(10);
            debugController.setPosition(new Vector3(0, 0, 4));
            debugController.lookAt(Vector3.zero());
            debugController.setActive(true);
            debugCamera.setPosition(debugController.getPosition());
            debugCamera.lookAt(Vector3.zero());
        }

        public virtual void init()
        {
            createGUI();
        }

        public virtual void shutdown()
        {
            debugText.clear();
        }

        public virtual void beginRun()
        {
            init();
        }

        public virtual void endRun()
        {
            shutdown();
        }

        public virtual void oneFrame()
        {
            m_timer.tick();

            double now = System.getTick();
            double timeStep = now - m_lastTick;
            m_lastTick = now;

            m_userInputWatch.tick();
            onUserInput(userInput);
            m_userInputWatch.tock();

            m_networkWatch.tick();
            onNetwork();
            m_networkWatch.tock();

            m_simulationWatch.tick();
            if (!_paused)
            {
                double rate = 1.0;
                RealTime rdt = timeStep;
                SimTime sdt = timeStep * rate;
                SimTime idt = 0;
                onSimulation(rdt, sdt, idt);
            }
            m_simulationWatch.tock();

            m_logicWatch.tick();
            if (!_paused)
            {
                onLogic();
            }
            m_logicWatch.tock();

            m_graphicsWatch.tick();
            if (renderDevice != null)
            {
                renderDevice.beginFrame();
                renderDevice.pushState();
                displayOverlay();
                renderDevice.popState();
                renderDevice.endFrame();
                debugText.clear();
            }
            m_graphicsWatch.tock();

            m_timer.tock();
        }

        public virtual bool onEvent(GEvent @event)
        {
            return m_moduleManager.pointer().onEvent(@event);
        }

        public virtual void onLogic()
        {
            m_moduleManager.pointer().onLogic();
        }

        public virtual void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            m_moduleManager.pointer().onSimulation(rdt, sdt, idt);
        }

        public virtual void onNetwork()
        {
            m_moduleManager.pointer().onNetwork();
        }

        public virtual void onUserInput(UserInput input)
        {
            m_moduleManager.pointer().onUserInput(input);
        }

        public virtual void displayOverlay()
        {
            renderDebugInfo();
        }

        public virtual bool debugOverlay()
        {
            return debugMode() && debugShowText;
        }

        public bool paused()
        {
            return _paused;
        }

        public void togglePause()
        {
            _paused = !_paused;
        }

        public bool programEnded()
        {
            return endProgram;
        }

        public void exit(int code = 0)
        {
            endProgram = true;
            Environment.Exit(code);
        }

        public string takeScreenshot()
        {
            return saveScreenshot(null);
        }

        public string saveScreenshot(string filename)
        {
            if (renderDevice == null)
            {
                throw new InvalidOperationException("GApp does not have a RenderDevice.");
            }

            string path = renderDevice.screenshot(filename);
            if (string.IsNullOrEmpty(path))
            {
                throw new NotSupportedException(
                    "The active render device backend does not implement screenshot readback.");
            }
            return path;
        }

        /**
         Call this to run the app.  Subclasses should override main(), not run.
         */
        public void run()
        {
            beginRun();

            try
            {
                if (catchCommonExceptions)
                {
                    try
                    {
                        this.main();
                    }
                    catch (GImage.Error e)
                    {
                        Debug.alwaysAssertM(false, e.reason + "\n" + e.filename);
                    }
                    catch (TextInput.WrongTokenType t)
                    {
                        Debug.alwaysAssertM(false, t.message);
                    }
                    catch (TextInput.WrongSymbol t)
                    {
                        Debug.alwaysAssertM(false, t.message);
                    }
                    catch (VertexAndPixelShader.ArgumentError e)
                    {
                        Debug.alwaysAssertM(false, e.message);
                    }
                    catch (LightweightConduit.PacketSizeException e)
                    {
                        Debug.alwaysAssertM(false, e.Message);
                    }
                } else
                {
                    this.main();
                }
            }
            finally
            {
                endRun();
            }
        }

        /**
         Installs a module.  Actual insertion may be delayed until the next frame.
         See GApplet::addModule for further discussion.

         @beta
        */
        public virtual void addModule(ReferenceCountedPointer<GModule> module, GModuleManager.EventPriority p = GModuleManager.EventPriority.NORMAL_PRIORITY)
        {
            m_moduleManager.pointer().add(module, p);
        }

        /**
         The actual removal of the module may be delayed until the next frame.
         See GApplet::removeModule for further discussion.
         @beta
        */
        public virtual void removeModule(ReferenceCountedPointer<GModule> module)
        {
            m_moduleManager.pointer().remove(module);
        }

        public Action processEvents = null;

        public virtual void Dispose()
        {
            if (networkDevice != null)
            {
                networkDevice.cleanup();
                networkDevice = null;
            }

            debugFont = null;
            userInput = null;

            if (renderDevice != null)
            {
                renderDevice.cleanup();
                renderDevice = null;
            }

            if (_hasUserCreatedWindow)
            {
                if (_window != null)
                {
                    _window.Dispose();
                }
                _window = null;
            }

            VARArea.cleanupAllVARAreas();

            if (debugLog != null)
            {
                debugLog.Dispose();
                debugLog = null;
            }
        }

        /**
         Override this with your main application logic.
        */
        protected abstract void main();
    }
}
