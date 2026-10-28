/**
  @file GApplet.h
  
  @maintainer Morgan McGuire, morgan@graphics3d.com

  @created 2003-11-03
  @edited  2006-03-04
*/

using System;
using G3D;

namespace G3D
{
    /**
      A self-contained mode of operation for an application; a "screen".
      One large-scale state of an application.  For example, a game might have one GApplet subclass
      for the title screen, one for the main menu, one for the game itself, and one for the
      credits.

      For each frame, the GApplet has several tasks that can be implemented by overriding
      base class methods.  The use of cooperative, round-robbin scheduling avoids the need
      for threads in most applications.  These tasks are:

      <ul>
       <li> doGraphics(G3D::RenderDevice*)
       <li> doUserInput(G3D::UserInput*)
       <li> doLogic()
       <li> doNetwork()
       <li> doSimulation(G3D::RealTime, G3D::SimTime)
       <li> doWait(G3D::RealTime cumulativeTime, G3D::RealTime desiredCumulativeTime)
      </ul>

      To invoke a GApplet and let it control the main loop, call
      run().  To control the main loop explicitly, invoke beginRun on
      initialization, call oneFrame() from the main loop, and call endRun on cleanup.
    */ 
    public class GApplet
    {
        private GApp app;

        /** Used by doSimulation for elapsed time. */
        private RealTime now, lastTime;

        /** Used by doWait for elapsed time. */
        private RealTime lastWaitTime;

        private float m_desiredFrameRate;
        private double m_simTimeRate;
        private RealTime m_realTime;
        private SimTime m_simTime;
        private SimTime m_idealSimTime;

        protected ReferenceCountedPointer<GModuleManager> m_moduleManager = GModuleManager.create();

        /** @param _app This is usually your own subclass of GApp.*/
        public GApplet(GApp _app)
        {
            app = _app;
            m_desiredFrameRate = (float)g3dmath.inf();
            m_simTimeRate = 1.0;
            m_realTime = 0;
            m_simTime = 0;
            m_idealSimTime = 0;
            lastWaitTime = System.time();

            Debug.debugAssert(app != null);
        }

        /**
         An empty virtual destructor for virtual methods.
        */
        public virtual void Dispose()
        {
        }

        public virtual string name()
        {
            return GetType().Name;
        }

        /** Amount of time that passes in simTime for every second of realTime.
            e.g., 1.0 == real-time, 2.0 == fast, 0.5 == slow, 0.0 = stop time.
            Default is 1.0.
            */
        public double simTimeRate()
        {
            return m_simTimeRate;
        }

        public virtual void setSimTimeRate(double s)
        {
            m_simTimeRate = s;
        }

        /** Accumulated wall-clock time since init was called on this applet. 
            Since this time is accumulated, it may drift from the true
            wall-clock obtained by System::time().*/
        public RealTime realTime()
        {
            return m_realTime;
        }

        public virtual void setRealTime(RealTime r)
        {
            m_realTime = r;
        }

        /** In-simulation time since init was called on this applet.  
            Takes into account simTimeSpeed.  Automatically incremented
            after doSimulation.
          */
        public SimTime simTime()
        {
            return m_simTime;
        }

        public virtual void setIdealSimTime(SimTime s)
        {
            m_idealSimTime = s;
        }

        /**
           Simulation time that is always advanced by precisely the
           desiredFrameDuration * simTimeRate, regardless of the 
           actual frame duration.
         */
        public SimTime idealSimTime()
        {
            return m_idealSimTime;
        }

        public virtual void setSimTime(SimTime s)
        {
            m_simTime = s;
        }

        /** Change to invoke frame limiting via doWait.
            Defaults to inf() */
        public virtual void setDesiredFrameRate(float fps)
        {
            Debug.debugAssert(fps > 0);
            m_desiredFrameRate = fps;
        }

        public float desiredFrameRate()
        {
            return m_desiredFrameRate;
        }

        public RealTime desiredFrameDuration()
        {
            return 1.0 / m_desiredFrameRate;
        }

        /**
         Installs a module.  Actual insertion may be delayed until the next frame.

         Modules can be installed either on a GApp or a GApplet.  Those installed
         on the GApp will be run by the GApplet every frame in addition to its own.
         
         By default events have NORMAL_PRIORITY. HIGH_PRIORITY is only intended
         for debugging modules that need to temporarily superceed other components.

         All modules within the same priority class receive events
         with undetermined priority.  All high priority modules
         receive events before.
         
         Event priority is important because a module can consume
         events, preventing other modules from observing them.
         For example, a debugging console might run at high priority 
         so that it can consume arrow keys before they reach a normal priority
         first-person controller.

         @beta
        */
        public virtual void addModule(ReferenceCountedPointer<GModule> module, GModuleManager.EventPriority p = GModuleManager.EventPriority.NORMAL_PRIORITY)
        {
            m_moduleManager.pointer().add(module, p);
        }

        /**
         The actual removal of the module may be delayed until the next frame.
         @beta
        */
        public virtual void removeModule(ReferenceCountedPointer<GModule> module)
        {
            m_moduleManager.pointer().remove(module);
        }

        /**
          Run until app->endProgram or endApplet is set to true. 
          Calls beginRun(), then oneFrame in a loop, then endRun().
          
          Consider instead calling app->window()->pushLoopBody(applet), which
          allows you to push and pop additional applets.
            
          For use with GWindows for which requiresMainLoop is false.
          On GWindows where requiresMainLoop is true, this will schedule
          the applet to run and return immediately.  (This odd behavior 
          is required for backwards compatibility; in version 7.00 it will
          change).
        */
        public virtual void run()
        {
            if (app.window() != null && app.window().requiresMainLoop())
            {
                app.window().pushLoopBody(this);
            } else
            {
                beginRun();

                // Main loop
                do
                {
                    oneFrame();
                } while (!app.endProgram && !endApplet);

                endRun();
            }
        }

        /** 
          Usually you'll call GApplet::run instead.  This is used by GApp.

          Prepare for running.  The default implementation sets endApplet
          to false, calls GApplet::onInit(), copies the debug camera
          position to the debug camera controller,
        */
        public virtual void beginRun()
        {
            begin();

            // Move the controller to the camera's location
            app.debugController.setCoordinateFrame
                (app.debugCamera.getCoordinateFrame());

            now = System.getTick() - 0.001;
        }

        public virtual void begin()
        {
            endApplet = false;

            onInit();
        }

        /** 
          Usually you'll call GApplet::run instead.  This is used by GApp.

          A single frame of rendering, simulation, AI, events, networking,
          etc.  Invokes the onXXX methods.  For use with GWindows that
          require a main loop.  This if your GWindow does not require
          control of the main loop, GApp will call this for you.
        */
        public virtual void oneFrame()
        {
            if (app.processEvents != null)
            {
                app.processEvents();
            }

            lastTime = now;
            now = System.getTick();
            double timeStep = (double)now - (double)lastTime;

            // User input
            app.m_userInputWatch.tick();
            doUserInput(); // TODO: remove
            onUserInput(app.userInput);
            app.onUserInput(app.userInput);
            m_moduleManager.pointer().onUserInput(app.userInput);
            app.m_userInputWatch.tock();

            // Network
            app.m_networkWatch.tick();
            onNetwork();
            app.onNetwork();
            m_moduleManager.pointer().onNetwork();
            app.m_networkWatch.tock();

            // Simulation
            app.m_simulationWatch.tick();
            if (!app.paused())
            {
                app.debugController.doSimulation(g3dmath.clamp(timeStep, 0.0, 0.1));
                app.debugCamera.setCoordinateFrame
                    (app.debugController.getCoordinateFrame());

                double rate = simTimeRate();
                RealTime rdt = timeStep;
                SimTime  sdt = timeStep * rate;
                SimTime  idt = (double)desiredFrameDuration() * rate;

                onSimulation(rdt, sdt, idt);
                app.onSimulation(rdt, sdt, idt);
                m_moduleManager.pointer().onSimulation(rdt, sdt, idt);

                setRealTime((double)realTime() + (double)rdt);
                setSimTime((double)simTime() + (double)sdt);
                setIdealSimTime((double)idealSimTime() + (double)idt);
            }
            app.m_simulationWatch.tock();

            // Logic
            app.m_logicWatch.tick();
            if (!app.paused())
            {
                onLogic();
                app.onLogic();
                m_moduleManager.pointer().onLogic();
            }
            app.m_logicWatch.tock();

            // Wait 
            // Note: we might end up spending all of our time inside of
            // RenderDevice::beginFrame.  Waiting here isn't double waiting,
            // though, because while we're sleeping the CPU the GPU is working
            // to catch up.

            app.m_waitWatch.tick();
            {
                RealTime waitNow = System.time();
                // Compute accumulated time
                onWait((double)waitNow - (double)lastWaitTime, desiredFrameDuration());
                lastWaitTime = System.time();
            }
            app.m_waitWatch.tock();

            // Graphics
            app.m_graphicsWatch.tick();
            app.renderDevice.beginFrame();
                app.renderDevice.pushState();
                    onGraphics(app.renderDevice);
                app.renderDevice.popState();
                app.renderDebugInfo();
            app.renderDevice.endFrame();
            app.debugText.clear();
            app.m_graphicsWatch.tock();

            if ((endApplet || app.endProgram) && app.window() != null && app.window().requiresMainLoop()) {
                app.window().popLoopBody();
            }
        }

        /**
          Usually you'll call GApplet::run instead.  This is used by GApp.

          Invokes cleanup().
         */
        public virtual void endRun()
        {
            end();

            Log.common().section("Files Used");
            Log.common().println("");

            if (app.window() != null && app.window().requiresMainLoop() && app.endProgram) {
                app.exit(0);
            }
        }

        public virtual void end()
        {
            onCleanup();
        }

        protected virtual void getPosedModel(
            Array<ReferenceCountedPointer<PosedModel>> posedArray, 
            Array<ReferenceCountedPointer<PosedModel2D>> posed2DArray)
        {
            // Default implementation poses the managed modules from the GApplet and GApp.
            m_moduleManager.pointer().getPosedModel(posedArray, posed2DArray);
            app.m_moduleManager.pointer().getPosedModel(posedArray, posed2DArray);
        }

        /** Set to false to break out of the run() loop. */
        public bool endApplet = false;

        /** @deprecated */
        public virtual void doSimulation(RealTime rdt)
        {
        }

        /**
         Override this with your simulation code.
         Called from GApp::run.
            
         Default implementation does nothing.

         simTime(), idealSimTime() and realTime() are incremented after
         doSimulation is called, so at the beginning of call the current
         time is the end of the previous frame.

         @param rdt Elapsed real-world time since the last call to doSimulation.

         @param sdt Elapsed sim-world time since the last call to
         doSimulation, computed by multiplying the wall-clock time by the
         simulation time rate.

         @param idt Elapsed ideal sim-world time.  Use this for perfectly
         reproducible timing results.  Ideal time always advances by the
         desiredFrameDuration * simTimeRate, no matter how much wall-clock
         time has elapsed.
        */
        public virtual void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            doSimulation((double)sdt);
        }

        /**
         Override and implement.  The debugCamera's projection and object to world
         matrices are set by default; you can set other cameras as desired. 
         RenderDevice::beginFrame and endFrame are called for you.
         
         @deprecated
        */
        public virtual void doGraphics()
        {
            Array<ReferenceCountedPointer<PosedModel>>        posedArray = new Array<ReferenceCountedPointer<PosedModel>>();
            Array<ReferenceCountedPointer<PosedModel2D>>      posed2DArray = new Array<ReferenceCountedPointer<PosedModel2D>>();
            Array<ReferenceCountedPointer<PosedModel>>        opaque = new Array<ReferenceCountedPointer<PosedModel>>();
            Array<ReferenceCountedPointer<PosedModel>>        transparent = new Array<ReferenceCountedPointer<PosedModel>>();

            // By default, render the installed modules
            getPosedModel(posedArray, posed2DArray);

            // 3D
            if (posedArray.size() > 0) {
                Vector3 lookVector = app.renderDevice.getCameraToWorldMatrix().getLookVector();
                PosedModel.sort(posedArray, lookVector, opaque, transparent);

                for (int i = 0; i < opaque.size(); ++i) {
                    opaque[i].pointer().render(app.renderDevice);
                }

                for (int i = 0; i < transparent.size(); ++i) {
                    transparent[i].pointer().render(app.renderDevice);
                }
            }

            // 2D
            if (posed2DArray.size() > 0) {
                app.renderDevice.push2D();
                    PosedModel2D.sort(posed2DArray);
                    for (int i = 0; i < posed2DArray.size(); ++i) {
                        posed2DArray[i].pointer().render(app.renderDevice);
                    }
                app.renderDevice.pop2D();
            }
        }

        /**
         Rendering callback.

         Override and implement.  The debugCamera's projection and object to world
         matrices are set by default; you can set other cameras as desired. 
         RenderDevice::beginFrame and endFrame are called for you.
         
         Use getPosedModel() to obtain the installed GModules to be rendered.

         See <A HREF="../demos/main.cpp">demos/main.cpp</A> for an example of
         overriding lights.
        */
        public virtual void onGraphics(RenderDevice rd)
        {
            doGraphics();
        }

        /**
            @deprecated
        */
        public virtual void doNetwork()
        {
        }

        /**
         For a networked app, override this to implement your
         network message polling.
        */
        public virtual void onNetwork()
        {
            doNetwork();
        }

        /**
           Task to be used for frame rate limiting.  

           Overriding onWait is not recommended.

           Frame rate limiting is useful
           to avoid overloading a maching that is running background tasks and
           for situations where fixed time steps are needed for simulation and there
           is no reason to render faster.

           Default implementation System::sleep()s until cumulativeTime + time
           in wait is greater than or equal to @a frameDuration = 1 / desiredFrameRate.
         */
        public virtual void onWait(RealTime t, RealTime desiredT)
        {
            System.sleep(Math.Max(0.0, (double)desiredT - (double)t));
        }

        /**
          Update any state you need to here.  This is a good place for
          AI code, for example.  Called after network and user input,
          before simulation.
          @deprecated
        */
        public virtual void doLogic()
        {
        }

        /**
         Update any state you need to here.  This is a good place for
         AI code, for example.  Called after network and user input,
         before simulation.
        */
        public virtual void onLogic()
        {
            doLogic();
        }

        /**
           @deprecated
        */
        public virtual void init()
        {
            m_simTime     = 0;
            m_realTime    = 0;
            m_simTimeRate = 1.0;
            lastWaitTime  = System.time();
        }

        /**
         Invoked every time run is called.  Default implementation
         resets timers and simTimeRate.

         Sublcasses should invoke GApplet::init to reset the timers.
        */
        public virtual void onInit()
        {
            init();
        }

        /**
         Invoked at the end of every run call.  Default implementation
         does nothing.
         @deprecated
        */
        public virtual void cleanup()
        {
        }

        /**
         Invoked at the end of every run call.  Default implementation
         does nothing.
        */
        public virtual void onCleanup()
        {
            cleanup();
        }
        
        /** @deprecated Use onEvent */
        public virtual void processEvent(GEvent @event)
        {
        }

        /**
         It is recommended to override doUserInput instead of this method.

         Override if you need to explicitly handle events in the order
         they appear.
         
         Note that the userInput contains a record of all
         keys pressed/held, mouse, and joystick state, so 
         you do not have to override this method to handle
         basic input events.

         Return true if the event has been consumed (i.e., no-one else including GApp should process it further).

         The default implementation does nothing.
        */
        public virtual bool onEvent(GEvent @event)
        {
            processEvent(@event); // TODO: Remove when deprecated

            return GModuleManager.onEvent(@event, app.m_moduleManager, m_moduleManager);
        }

        /**
         Updates the userInput.  Called from run.  Rarely needs to be
         called by user programs.

         Never overriden by a subclass.
         Instead, override GApp::processEvent to handle your own events.

         @deprecated In 7.0, will be private and doUserInput will be a function
          you can override to handle events.
        */
        public void doUserInput()
        {
            if (app.window() == null)
            {
                throw new InvalidOperationException("The GApplet requires a GWindow to process user input.");
            }

            app.userInput.beginEvents();

            // Event handling
            GEvent @event = new GEvent();
            while (app.window().pollEvent(out @event))
            {
                if (onEvent(@event))
                {
                    continue;
                }

                switch(@event.type)
                {
                case GEventType.QUIT:
                    app.endProgram = true;
                    endApplet = true;
                    break;

                case GEventType.RESIZE:
                    if (app.autoResize)
                    {
                        app.renderDevice.notifyResize
                            (@event.width, @event.height);
                        Rect2D full = 
                            Rect2D.xywh(0, 0, 
                                         app.renderDevice.getWidth(), 
                                         app.renderDevice.getHeight());
                        app.renderDevice.setViewport(full);
                    }
                    break;

                case GEventType.KEY_DOWN:
                    switch ((UserInput.KeyCode)@event.key)
                    {
                    case UserInput.KeyCode.ESCAPE:
                        if (app.debugMode() && app.debugQuitOnEscape)
                        {
                            app.endProgram = true;
                        }
                        break;

                    case UserInput.KeyCode.TAB:
                        // Make sure it wasn't ALT-TAB that was pressed !
                        if (app.debugMode() && app.debugTabSwitchCamera && 
                            ! (app.userInput.keyDown(UserInput.KeyCode.RALT) || 
                               app.userInput.keyDown(UserInput.KeyCode.LALT))) {

                            app.debugController.setActive
                                (! app.debugController.active());
                        }
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

                app.userInput.processEvent(@event);
            }

            app.userInput.endEvents();
        }

        /**
         Routine for processing user input from the previous frame.
        */
        public virtual void onUserInput(UserInput userInput)
        {
        }
    }
}
