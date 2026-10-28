/**
  @file ManualCameraController.h
  
  @maintainer Morgan McGuire, morgan@graphics3d.com
  @created 2003-10-29
  @edited  2005-08-06
*/

using System;
using G3D;

namespace G3D
{
    /**
     Uses a First Person (Quake- or World of Warcraft style) mapping to translate keyboard and mouse input
     into a flying camera position.  The result is an Euler-angle 
     camera controller suitable for games and fly-throughs.  

      To use without G3D::GApp:
      <OL>
        <LI> Create a G3D::RenderDevice
        <LI> Create a UserInput object (set the keyboard controls when creating it)
        <LI> Create a ManualCameraController
        <LI> Call ManualCameraController::setActive(true)
        <LI> Invoke ManualCameraController::doSimulation every time simulation is invoked (e.g. once per rendering iteration)
        <LI> Use ManualCameraController::getCoordinateFrame() to set the camera's position
      </OL>

     */
    public class FirstPersonManipulator : Manipulator
    {
        /**
         MOUSE_DIRECT              = Shooter/Quake style (default), mouse cursor is hidden and mouse controls yaw/pitch
         MOUSE_DIRECT_RIGHT_BUTTON = RPG/World of Warcraft style, on right mouse button cursor is hidden and mouse controls yaw/pitch
         MOUSE_SCROLL_AT_EDGE      = Leaves mouse cursor visible and rotates while mouse is near the window edge
         MOUSE_PUSH_AT_EDGE        = Leaves mouse cursor visible and rotates when the mouse is actively pushing against the window edge
         */
        public enum MouseMode {
            MOUSE_DIRECT,
            MOUSE_DIRECT_RIGHT_BUTTON,
            MOUSE_SCROLL_AT_EDGE,
            MOUSE_PUSH_AT_EDGE
        }

        /** m/s */
        private double maxMoveRate;

        /** rad/s */
        private double maxTurnRate;

        private double yaw;
        private double pitch;
        private Vector3 translation;

        private bool _active;

        private UserInput userInput;

        private MouseMode m_mouseMode;

        // G3D_DEPRECATED removed from in front of the constructor below -
        // doesn't compile on linux

        /** @deprecated Use create. */
        public FirstPersonManipulator()
        {
            m_mouseMode = MouseMode.MOUSE_DIRECT;
            _active = false;
        }

        // G3D_DEPRECATED removed from in front of the constructor below -
        // doesn't compile on linux

        /** Creates and initializes.
            @deprecated Use create.
          */
        public FirstPersonManipulator(RenderDevice device, UserInput input)
        {
            m_mouseMode = MouseMode.MOUSE_DIRECT;
            _active = false;
            init(device, input);
        }

        public static ReferenceCountedPointer<FirstPersonManipulator> create()
        {
            return new ReferenceCountedPointer<FirstPersonManipulator>(new FirstPersonManipulator());
        }

        /** You need to call setActive(true) before the controller will work. 
            @deprecated Not needed anymore
          */
        public void init(RenderDevice device, UserInput input)
        {
            userInput = input;
            reset();
        }

        /** When active, the FirstPersonManipulator takes over the mouse.  It turns
            off the mouse cursor and switches to first person controller style.
            Use this to toggle between your menu system and first person camera control.

            When deactivated, the mouse cursor is restored and the mouse is located
            where it was when the camera controller was activated.

            In release mode, the cursor movement is restricted to the window
            while the controller is active.  This does not occur in debug mode because
            you might hit a breakpoint while the controller is active and it
            would be annoying to not be able to move the mouse.*/
        public void setActive(bool a)
        {
            if (_active == a)
            {
                return;
            }
            _active = a;

            if (userInput == null)
            {
                throw new InvalidOperationException("The FirstPersonManipulator has no UserInput; call init() first.");
            }

            switch (m_mouseMode) {
            case MouseMode.MOUSE_DIRECT:
                userInput.setPureDeltaMouse(_active);
                break;

            case MouseMode.MOUSE_DIRECT_RIGHT_BUTTON:
                // Only turn on when activeand the right mouse button is down
                userInput.setPureDeltaMouse(_active && userInput.keyDown((UserInput.KeyCode)(int)UserInput.CustomKeyCode.SDL_RIGHT_MOUSE_KEY));
                break;

            case MouseMode.MOUSE_SCROLL_AT_EDGE:
            case MouseMode.MOUSE_PUSH_AT_EDGE:        
                userInput.setPureDeltaMouse(false);
                if (_active) {
                    userInput.window().incInputCaptureCount();
                } else {
                    userInput.window().decInputCaptureCount();
                }
                break;

            default:
                Debug.debugAssert(false);
                break;
            }
        }

        public MouseMode mouseMode()
        {
            return m_mouseMode;
        }

        public void setMouseMode(MouseMode m)
        {
            if (m != m_mouseMode)
            {
                // Toggle activity to let the cursor and 
                // state variables reset.
                bool wasActive = active();
                if (wasActive)
                {
                    setActive(false);
                }
                m_mouseMode = m;
                if (wasActive)
                {
                    setActive(true);
                }
            }
        }

        public bool active()
        {
            return _active;
        }

        /** Initial value is 10 */
        public void setMoveRate(double metersPerSecond)
        {
            maxMoveRate = metersPerSecond;
        }

        /** Initial value is PI / 2 */
        public void setTurnRate(double radiansPerSecond)
        {
            maxTurnRate = radiansPerSecond;
        }

        /** Invoke immediately before entering the main game loop. */
        public void reset()
        {
            _active      = false;
            yaw         = -g3dmath.G3D_PI / 2;
            pitch       = 0;
            translation = Vector3.zero();
            setMoveRate(10);
            setTurnRate(g3dmath.G3D_PI * 5);
        }

        /**
         Increments the ManualCameraController's orientation and position.
         Invoke once per simulation step.
         @deprecated
        */
        public void doSimulation(double elapsedTime)
        {
            onSimulation(elapsedTime, elapsedTime, elapsedTime);
        }

        public void setPosition(Vector3 t)
        {
            translation = t;
        }

        public void lookAt(Vector3 position)
        {
            Vector3 look = (position - translation);

            yaw   = Math.Atan2(look.x, -look.z);
            pitch = -Math.Atan2(look.y, g3dmath.distance(look.x, look.z));
        }

        public double getYaw()
        {
            return yaw;
        }

        public double getPitch()
        {
            return pitch;
        }

        public Vector3 getPosition()
        {
            return translation;
        }

        public Vector3 getLookVector()
        {
            return frame().getLookVector();
        }

        /** Right vector */
        public Vector3 getStrafeVector()
        {
            return frame().getRightVector();
        }

        /** @deprecated Use frame */
        public CoordinateFrame getCoordinateFrame()
        {
            return frame();
        }

        /** @deprecated Use getFrame.
          */
        public void getCoordinateFrame(out CoordinateFrame c)
        {
            getFrame(out c);
        }

        /** @deprecated Use setFrame */
        public void setCoordinateFrame(CoordinateFrame c)
        {
            setFrame(c);
        }

        /**
          Sets to the closest legal controller orientation to the coordinate frame.
         */
        public void setFrame(CoordinateFrame c)
        {
            Vector3 look = c.getLookVector();

            setPosition(c.translation);

            // this is work towards a patch for bug #1022341
            lookAt(c.translation + look);
        }

        // Inherited from Manipulator
        public override void getFrame(out CoordinateFrame c)
        {
            c = new CoordinateFrame();
            c.translation = translation;
            c.rotation = Matrix3.fromEulerAnglesZYX(0, (float)-yaw, (float)-pitch);
        }

        // Inherited from Manipulator
        public override CoordinateFrame frame()
        {
            CoordinateFrame c;
            getFrame(out c);
            return c;
        }

        // Inherited from GModule
        public override void getPosedModel(
            Array<ReferenceCountedPointer<PosedModel>> p3d,
            Array<ReferenceCountedPointer<PosedModel2D>> p2d)
        {
        }

        // Inherited from GModule
        public override void onNetwork()
        {
        }

        // Inherited from GModule
        public override void onLogic()
        {
        }

        // Inherited from GModule
        public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            if (!_active)
            {
                return;
            }

            if (userInput == null)
            {
                return;
            }

            double elapsedTime = sdt;

            {
                // Translation direction
                Vector2 direction = new Vector2(userInput.getX(), userInput.getY());
                direction.unitize(1e-06f);

                // Translate forward
                translation += (getLookVector() * direction.y +
                    getStrafeVector() * direction.x) * elapsedTime * maxMoveRate;
            }

            // Desired change in yaw and pitch
            Vector2 delta = new Vector2(0, 0);
            float maxTurn = (float)(maxTurnRate * elapsedTime);

            switch (m_mouseMode) {
            case MouseMode.MOUSE_DIRECT_RIGHT_BUTTON:
                {
                    bool mouseDown = userInput.keyDown((UserInput.KeyCode)(int)UserInput.CustomKeyCode.SDL_RIGHT_MOUSE_KEY);
                    userInput.setPureDeltaMouse(mouseDown);
                    if (!mouseDown)
                    {
                        // Skip bottom case
                        break;
                    }
                }
                // Intentionally fall through to MOUSE_DIRECT
                goto case MouseMode.MOUSE_DIRECT;

            case MouseMode.MOUSE_DIRECT:
                delta = userInput.mouseDXY() / 100.0f;
                break;


            case MouseMode.MOUSE_SCROLL_AT_EDGE:
                {
                    if (userInput.window() == null)
                    {
                        throw new InvalidOperationException("MOUSE_SCROLL_AT_EDGE requires a UserInput with a window.");
                    }

                    Rect2D viewport = Rect2D.xywh(0, 0, userInput.window().Width, userInput.window().Height);
                    Vector2 mouse = userInput.mouseXY();

                    Vector2 hotExtent = new Vector2(Math.Max(50.0f, viewport.width() / 8),
                                                    Math.Max(50.0f, viewport.height() / 6));

                    // The hot region is outside this rect
                    Rect2D hotRegion = Rect2D.xyxy(
                        viewport.x0() + hotExtent.x, viewport.y0() + hotExtent.y,
                        viewport.x1() - hotExtent.y, viewport.y1() - hotExtent.y);

                    // See if the mouse is near an edge
                    if (mouse.x <= hotRegion.x0()) {
                        delta.x = -(float)g3dmath.square(1.0f - (mouse.x - viewport.x0()) / hotExtent.x);
                        // - Yaw
                    } else if (mouse.x >= hotRegion.x1()) {
                        delta.x = (float)g3dmath.square(1.0f - (viewport.x1() - mouse.x) / hotExtent.x);
                        // + Yaw
                    }

                    if (mouse.y <= hotRegion.y0()) {
                        delta.y = -(float)g3dmath.square(1.0f - (mouse.y - viewport.y0()) / hotExtent.y) * 0.6f;
                        // - pitch
                    } else if (mouse.y >= hotRegion.y1()) {
                        delta.y = (float)g3dmath.square(1.0f - (viewport.y1() - mouse.y) / hotExtent.y) * 0.6f;
                        // + pitch
                    }

                    delta *= maxTurn / 5;
                }
                break;

//    case MOUSE_PUSH_AT_EDGE: 
            default:
                Debug.debugAssert(false);
                break;
            }


            // Turn rate limiter
            if (Math.Abs(delta.x) > maxTurn) {
                delta.x = maxTurn * (float)Math.Sign(delta.x);
            }

            if (Math.Abs(delta.y) > maxTurn) {
                delta.y = maxTurn * (float)Math.Sign(delta.y);
            }

            yaw   += delta.x;
            pitch += delta.y;

            // As a patch for a setCoordinateFrame bug, we prevent 
            // the camera from looking exactly along the y-axis.
            pitch = g3dmath.clamp(pitch, -g3dmath.G3D_PI / 2 + 0.001, g3dmath.G3D_PI / 2 - 0.001);
        }

        // Inherited from GModule
        public override void onUserInput(UserInput ui)
        {
            userInput = ui;
        }

        // Inherited from GModule
        public override bool onEvent(GEvent @event)
        {
            return false;
        }
    }

    /** Use FirstPersonManipulator instead 
        @deprecated */
    public class ManualCameraController : FirstPersonManipulator
    {
        public ManualCameraController()
        {
        }

        public ManualCameraController(RenderDevice device, UserInput input)
            : base(device, input)
        {
        }
    }

    /** Use FirstPersonManipulator instead 
        @deprecated */
    public class FPCameraController : FirstPersonManipulator
    {
        public FPCameraController()
        {
        }

        public FPCameraController(RenderDevice device, UserInput input)
            : base(device, input)
        {
        }
    }
}
