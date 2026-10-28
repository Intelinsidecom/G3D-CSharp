/**
  @file UserInput.h
  
  @maintainer Morgan McGuire, morgan@graphics3d.com
  @created 2003-10-29
  @edited  2005-08-06
*/

using System;
using G3D;

namespace G3D
{
    /**
     User input class that consolidates joystick, keyboard, and mouse input.

     Four axes are supported directly: joystick/keyboard x and y and 
     mouse x and y.  Mouse buttons, joystick buttons, and keyboard keys
     can all be used as generic buttons.

     Call beginEvents() immediately before your event handling routine and hand
     events to processEvent() as they become available.  Call endEvents() immediately
     after the loop.

     Note that the port uses G3D::GEvent structures instead of the SDL <B>event</B>
     structures (defined in SDL.h), but not the SDL <B>library</B>.
    */
    public class UserInput
    {
        /**
         The function of a key, used to control getX() and getY().
        */
        public enum UIFunction {UP, DOWN, LEFT, RIGHT, NONE}

        /**
         Some special key codes for use with UserInput.
        */
        // In an enum so they can be used with switch.
        public enum CustomKeyCode
        {
            // The order of the mouse buttons is intentionally chosen to match SDL's button codes
            // and cannot be changed.
            SDL_LEFT_MOUSE_KEY        = 324,
            SDL_MIDDLE_MOUSE_KEY,
            SDL_RIGHT_MOUSE_KEY,
            SDL_MOUSE_WHEEL_UP_KEY,
            SDL_MOUSE_WHEEL_DOWN_KEY,

            SDL_CUSTOM_LAST
        }

        /**
         The SDL 1.2 key codes used by the original G3D 6.09 implementation.
        */
        public enum KeyCode : ushort
        {
            UNKNOWN = 0,

            BACKSPACE = 8,
            TAB = 9,
            CLEAR = 12,
            RETURN = 13,
            PAUSE = 19,
            ESCAPE = 27,
            SPACE = 32,

            EXCLAIM = 33,
            QUOTEDBL = 34,
            HASH = 35,
            DOLLAR = 36,
            AMPERSAND = 38,
            QUOTE = 39,
            LPAREN = 40,
            RPAREN = 41,
            ASTERISK = 42,
            PLUS = 43,
            COMMA = 44,
            MINUS = 45,
            PERIOD = 46,
            SLASH = 47,

            ZERO = 48,
            ONE = 49,
            TWO = 50,
            THREE = 51,
            FOUR = 52,
            FIVE = 53,
            SIX = 54,
            SEVEN = 55,
            EIGHT = 56,
            NINE = 57,

            COLON = 58,
            SEMICOLON = 59,
            LESS = 60,
            EQUALS = 61,
            GREATER = 62,
            QUESTION = 63,
            AT = 64,

            LEFTBRACKET = 91,
            BACKSLASH = 92,
            RIGHTBRACKET = 93,
            CARET = 94,
            UNDERSCORE = 95,
            BACKQUOTE = 96,

            A = 97,
            B = 98,
            C = 99,
            D = 100,
            E = 101,
            F = 102,
            G = 103,
            H = 104,
            I = 105,
            J = 106,
            K = 107,
            L = 108,
            M = 109,
            N = 110,
            O = 111,
            P = 112,
            Q = 113,
            R = 114,
            S = 115,
            T = 116,
            U = 117,
            V = 118,
            W = 119,
            X = 120,
            Y = 121,
            Z = 122,

            DELETE = 127,

            KP0 = 256,
            KP1 = 257,
            KP2 = 258,
            KP3 = 259,
            KP4 = 260,
            KP5 = 261,
            KP6 = 262,
            KP7 = 263,
            KP8 = 264,
            KP9 = 265,

            KP_PERIOD = 266,
            KP_DIVIDE = 267,
            KP_MULTIPLY = 268,
            KP_MINUS = 269,
            KP_PLUS = 270,
            KP_ENTER = 271,
            KP_EQUALS = 272,

            UP = 273,
            DOWN = 274,
            RIGHT = 275,
            LEFT = 276,

            INSERT = 277,
            HOME = 278,
            END = 279,
            PAGEUP = 280,
            PAGEDOWN = 281,

            F1 = 282,
            F2 = 283,
            F3 = 284,
            F4 = 285,
            F5 = 286,
            F6 = 287,
            F7 = 288,
            F8 = 289,
            F9 = 290,
            F10 = 291,
            F11 = 292,
            F12 = 293,
            F13 = 294,
            F14 = 295,
            F15 = 296,

            NUMLOCK = 300,
            CAPSLOCK = 301,
            SCROLLOCK = 302,
            RSHIFT = 303,
            LSHIFT = 304,
            RCTRL = 305,
            LCTRL = 306,
            RALT = 307,
            LALT = 308,
            RMETA = 309,
            LMETA = 310,
            LSUPER = 311,
            RSUPER = 312,
            MODE = 313,

            HELP = 315,
            PRINT = 316,
            SYSREQ = 317,
            BREAK = 318
        }

        private const int KeyStateSize = (int)CustomKeyCode.SDL_CUSTOM_LAST;

        /**
         keyState[x] is true if key[x] is depressed.
        */
        private bool[] keyState = new bool[KeyStateSize];

        /**
          All keys that were just pressed down since the last call to
          poll().
        */
        // Since relatively few keys are pressed every frame, keeping an array of
        // key codes pressed is much more compact than clearing a large array of bools.
        private Array<KeyCode> justPressed = new Array<KeyCode>();

        /**
          All keys that were just released since the last call to
          poll().
        */
        private Array<KeyCode> justReleased = new Array<KeyCode>();

        /**
         Function of key[x]
        */
        private UIFunction[] keyFunction = new UIFunction[KeyStateSize];

        /** Whether each direction key is up or down.*/
        private bool left;
        private bool right;
        private bool up;
        private bool down;

        /**
         Joystick x, y
        */
        private double jx;
        private double jy;

        public bool useJoystick;

        /**
         This position is returned by mouseXY. 
         Also the last position of the mouse before
         setPureDeltaMouse = true.
        */
        private Vector2 guiMouse = new Vector2(0, 0);

        /** Center of the window.  Recomputed in endEvents. */
        private Vector2 windowCenter = new Vector2(0, 0);

        /** True if appHasFocus was true on the previous call to endEvents.
            Updated during endEvents. */
        private bool appHadFocus;

        /**
         In pixels
        */
        private Vector2 mouse = new Vector2(0, 0);

        private Vector2 previousMouse = new Vector2(0, 0);

        private Vector2 deltaMouse = new Vector2(0, 0);

        private bool pureDeltaMouseMode;

        private bool inEventProcessing;

        private byte mouseButtons;

        private byte mouseClickedButtons;

        private GWindow _window;

        /**
         @param keyMapping Mapping of various key codes to UI functions.
         If no mapping is provided, the arrow keys and WASD are mapped
         as the keys controlling getX() and getY().

         Example:
          <PRE>
         Table<int, UIFunction> map;
         map.set(SDLK_RIGHT, UserInput::RIGHT);
         map.set(SDLK_LEFT,  UserInput::LEFT);
         UserInput ui(&map);
          </PRE>
        */
        public UserInput(Table<KeyCode, UIFunction> keyMapping)
        {
            init(null, keyMapping);
        }

        public UserInput(GWindow window, Table<KeyCode, UIFunction> keyMapping)
        {
            init(window, keyMapping);
        }

        public UserInput(GWindow window)
        {
            init(window, null);
        }

        public UserInput()
        {
            init(null, null);
        }

        /** Called from the constructors */
        private void init(GWindow window, Table<KeyCode, UIFunction> keyMapping)
        {
            pureDeltaMouseMode = false;
            deltaMouse = new Vector2(0, 0);

            this._window = window;
            if (window != null)
            {
                windowCenter = new Vector2((float)window.width() / 2.0f, (float)window.height() / 2.0f);
            }

            up = left = down = right = false;
            jx = jy = 0.0;

            inEventProcessing = false;

            setKeyMapping(keyMapping);

            useJoystick = window != null && window.numJoysticks() > 0;
            if (window != null)
            {
                window.getRelativeMouseState(out mouse, out mouseButtons);
                guiMouse = mouse;
            }

            appHadFocus = appHasFocus();
        }

        /**
         Return the window used internally by the UserInput
        */
        public GWindow window()
        {
            return _window;
        }

        public void setWindow(GWindow window)
        {
            this._window = window;
            if (window != null)
            {
                windowCenter = new Vector2((float)window.width() / 2.0f, (float)window.height() / 2.0f);
                window.getRelativeMouseState(out mouse, out mouseButtons);
                guiMouse = mouse;
                useJoystick = window.numJoysticks() > 0;
                appHadFocus = appHasFocus();
            }
        }

        private static Table<KeyCode, UIFunction> defaultKeyMapping()
        {
            Table<KeyCode, UIFunction> map = new Table<KeyCode, UIFunction>();
            map.set(KeyCode.RIGHT, UIFunction.RIGHT);
            map.set(KeyCode.LEFT, UIFunction.LEFT);
            map.set(KeyCode.UP, UIFunction.UP);
            map.set(KeyCode.DOWN, UIFunction.DOWN);
            map.set(KeyCode.D, UIFunction.RIGHT);
            map.set(KeyCode.A, UIFunction.LEFT);
            map.set(KeyCode.W, UIFunction.UP);
            map.set(KeyCode.S, UIFunction.DOWN);
            return map;
        }

        /**
         Rebuilds the key mapping and clears the held key state.  Unlike the
         original, a null mapping selects the default arrow key and WASD
         mapping instead of dereferencing null.
        */
        public void setKeyMapping()
        {
            setKeyMapping(null);
        }

        public void setKeyMapping(Table<KeyCode, UIFunction> keyMapping)
        {
            Table<KeyCode, UIFunction> map = keyMapping ?? defaultKeyMapping();
            for (int i = keyState.Length - 1; i >= 0; --i)
            {
                keyState[i] = false;
                if (map.containsKey((KeyCode)i))
                {
                    keyFunction[i] = map.get((KeyCode)i);
                }
                else
                {
                    keyFunction[i] = UIFunction.NONE;
                }
            }
            left = false;
            right = false;
            up = false;
            down = false;
        }

        /**
         Expects mouse button events to be translated into key codes.
        */
        public void processKey(KeyCode code, bool down)
        {
            int index = (int)code;
            if (index < 0 || index >= keyFunction.Length)
            {
                return;
            }

            bool state = down;

            switch (keyFunction[index])
            {
                case UIFunction.RIGHT:
                    right = state;
                    break;

                case UIFunction.LEFT:
                    left = state;
                    break;

                case UIFunction.UP:
                    up = state;
                    break;

                case UIFunction.DOWN:
                    down = state;
                    break;

                case UIFunction.NONE:
                    break;
            }

            keyState[index] = state;

            if (state)
            {
                justPressed.append(code);
            }
            else
            {
                justReleased.append(code);
            }
        }

        /**
         Call from inside the event loop for every event inside
         processEvents()
        */
        public void processEvent(GEvent @event)
        {
            if (!inEventProcessing)
            {
                throw new InvalidOperationException("beginEvents() must be called before processEvent().");
            }

            switch (@event.type)
            {
                case GEventType.KEY_UP:
                    processKey((KeyCode)@event.key, false);
                    break;

                case GEventType.KEY_DOWN:
                    processKey((KeyCode)@event.key, true);
                    break;

                case GEventType.MOUSE_DOWN:
                    processKey((KeyCode)((int)CustomKeyCode.SDL_LEFT_MOUSE_KEY + @event.mouseButton - 1), true);
                    if (@event.mouseButton >= 1 && @event.mouseButton <= 8)
                    {
                        mouseButtons |= (byte)(1 << (@event.mouseButton - 1));
                        mouseClickedButtons |= (byte)(1 << (@event.mouseButton - 1));
                    }
                    break;

                case GEventType.MOUSE_UP:
                    processKey((KeyCode)((int)CustomKeyCode.SDL_LEFT_MOUSE_KEY + @event.mouseButton - 1), false);
                    if (@event.mouseButton >= 1 && @event.mouseButton <= 8)
                    {
                        mouseButtons &= (byte)~(1 << (@event.mouseButton - 1));
                    }
                    break;

                case GEventType.MOUSE_WHEEL:
                    if (@event.wheelDelta > 0)
                    {
                        processKey((KeyCode)CustomKeyCode.SDL_MOUSE_WHEEL_UP_KEY, true);
                        processKey((KeyCode)CustomKeyCode.SDL_MOUSE_WHEEL_UP_KEY, false);
                    }
                    else if (@event.wheelDelta < 0)
                    {
                        processKey((KeyCode)CustomKeyCode.SDL_MOUSE_WHEEL_DOWN_KEY, true);
                        processKey((KeyCode)CustomKeyCode.SDL_MOUSE_WHEEL_DOWN_KEY, false);
                    }
                    break;

                case GEventType.MOUSE_MOVE:
                    if (_window == null)
                    {
                        mouse = new Vector2(mouse.x + @event.mouseDeltaX, mouse.y + @event.mouseDeltaY);
                        deltaMouse = deltaMouse + new Vector2(@event.mouseDeltaX, @event.mouseDeltaY);
                    }
                    break;
            }
        }

        /**
         Call before your event polling loop.
        */
        public void beginEvents()
        {
            if (inEventProcessing)
            {
                throw new InvalidOperationException("The event processing cycle has already begun.");
            }
            inEventProcessing = true;
            justPressed.resize(0, false);
            justReleased.resize(0, false);
            mouseClickedButtons = 0;
            deltaMouse = new Vector2(0, 0);
        }

        /**
         Call after your event polling loop.
        */
        public void endEvents()
        {
            if (!inEventProcessing)
            {
                throw new InvalidOperationException("beginEvents() must be called before endEvents().");
            }

            inEventProcessing = false;

            if (useJoystick && _window != null)
            {
                Array<float> axis = new Array<float>();
                Array<bool> button = new Array<bool>();
                _window.getJoystickState(0, axis, button);

                if (axis.size() >= 2)
                {
                    jx = axis[0];
                    jy = -axis[1];
                }
            }

            if (_window != null)
            {
                windowCenter = new Vector2((float)_window.width(), (float)_window.height()) / 2.0f;
            }

            Vector2 oldMouse = mouse;
            if (_window != null)
            {
                _window.getRelativeMouseState(out mouse, out mouseButtons);
            }

            if ((mouse.x < 0) || (mouse.x > 10000))
            {
                // Sometimes we get bad values on the first frame;
                // ignore them.
                mouse = oldMouse;
            }

            if (_window != null)
            {
                deltaMouse = mouse - previousMouse;
            }
            previousMouse = mouse;

            bool focus = appHasFocus();

            if (pureDeltaMouseMode)
            {
                // Reset the mouse periodically.  We can't do this every
                // frame or the mouse will not be able to move substantially
                // at high frame rates.
                if ((mouse.x < windowCenter.x * 0.5f) || (mouse.x > windowCenter.x * 1.5f) ||
                    (mouse.y < windowCenter.y * 0.5f) || (mouse.y > windowCenter.y * 1.5f))
                {
                    mouse = windowCenter;
                    if (focus)
                    {
                        setMouseXY(mouse.x, mouse.y);
                    }
                }

                // Handle focus-in and focus-out gracefully
                if (focus && !appHadFocus)
                {
                    // We just gained focus.
                    grabMouse();
                }
                else if (!focus && appHadFocus)
                {
                    // Just lost focus
                    releaseMouse();
                }
            }
            else
            {
                guiMouse = mouse;
            }

            appHadFocus = focus;
        }

        /**
         Returns true iff the given key is currently held down.
         The SDL key codes are used, plus
            SDL_LEFT_MOUSE_KEY,
            SDL_MIDDLE_MOUSE_KEY,
            SDL_RIGHT_MOUSE_KEY,
            SDL_MOUSE_WHEEL_UP_KEY,
            SDL_MOUSE_WHEEL_DOWN_KEY
        */
        public bool keyDown(KeyCode code)
        {
            int index = (int)code;
            if (index < 0 || index >= keyState.Length)
            {
                return false;
            }
            return keyState[index];
        }

        /**
         Returns true if this key went down at least once since the last call to
         poll().
        */
        public bool keyPressed(KeyCode code)
        {
            for (int i = justPressed.size() - 1; i >= 0; --i)
            {
                if (code == justPressed[i])
                {
                    return true;
                }
            }

            return false;
        }

        /**
         Returns true if this key came up since the last call to
         poll().
        */
        public bool keyReleased(KeyCode code)
        {
            for (int i = justReleased.size() - 1; i >= 0; --i)
            {
                if (code == justReleased[i])
                {
                    return true;
                }
            }

            return false;
        }

        /**
         True if any key has been pressed since the last call to poll().
        */
        public bool anyKeyPressed()
        {
            return justPressed.size() > 0;
        }

        /** An array of all keys pressed since the last poll() call. */
        public void pressedKeys(Array<KeyCode> code)
        {
            code.resize(justPressed.size());
            for (int i = 0; i < justPressed.size(); ++i)
            {
                code[i] = justPressed[i];
            }
        }

        /** An array of all keys released since the last poll() call. */
        public void releasedKeys(Array<KeyCode> code)
        {
            code.resize(justReleased.size());
            for (int i = 0; i < justReleased.size(); ++i)
            {
                code[i] = justReleased[i];
            }
        }

        /**
         Returns a number between -1 and 1 indicating the horizontal
         input from the user.  Keyboard overrides joystick.
         @deprecated
        */
        public float getX()
        {
            if (left && !right)
            {
                return -1.0f;
            }
            else if (right && !left)
            {
                return 1.0f;
            }

            if (useJoystick && (Math.Abs(jx) > 0.1))
            {
                return (float)jx;
            }

            return 0.0f;
        }

        /**
         Returns a number between -1 and 1 indicating the vertical
         input from the user.  Up is positive, down is negative. 
         Keyboard overrides joystick.
         @deprecated
        */
        public float getY()
        {
            if (down && !up)
            {
                return -1.0f;
            }
            else if (up && !down)
            {
                return 1.0f;
            }

            if (useJoystick && (Math.Abs(jy) > 0.1))
            {
                return (float)jy;
            }

            return 0.0f;
        }

        public Vector2 getXY()
        {
            return new Vector2(getX(), getY());
        }

        public Vector2 mouseXY()
        {
            return guiMouse;
        }

        /** @deprecated */
        public Vector2 getMouseXY()
        {
            return guiMouse;
        }

        /** @deprecated */
        public float getMouseX()
        {
            return guiMouse.x;
        }

        /** @deprecated */
        public float getMouseY()
        {
            return guiMouse.y;
        }

        public Vector2 mouseDXY()
        {
            return deltaMouse;
        }

        public Vector2 pureMouseDXY()
        {
            return deltaMouse;
        }

        public bool mouseDown(int button)
        {
            if (button < 0 || button >= 8)
            {
                return false;
            }
            return (mouseButtons & (1 << button)) != 0;
        }

        public bool mouseClicked(int button)
        {
            if (button < 0 || button >= 8)
            {
                return false;
            }
            return (mouseClickedButtons & (1 << button)) != 0;
        }

        public bool shiftDown()
        {
            return keyDown(KeyCode.LSHIFT) || keyDown(KeyCode.RSHIFT);
        }

        public bool controlDown()
        {
            return keyDown(KeyCode.LCTRL) || keyDown(KeyCode.RCTRL);
        }

        public bool altDown()
        {
            return keyDown(KeyCode.LALT) || keyDown(KeyCode.RALT);
        }

        public float mouseDX()
        {
            return deltaMouse.x;
        }

        public float mouseDY()
        {
            return deltaMouse.y;
        }

        /**
         Sets the mouse position.  That new position will
         be returned by getMouseXY until the next endEvents()
         statement.
        */
        public void setMouseXY(float x, float y)
        {
            mouse = new Vector2(x, y);
            if (_window != null)
            {
                _window.setRelativeMousePosition(mouse);
            }
        }

        public void setMouseXY(Vector2 v)
        {
            setMouseXY(v.x, v.y);
        }

        /**
         When set to true, the mouse cursor is invisible and
         the mouse is restricted to the current window.  Regardless
         of how far the user moves the mouse in one direction,
         continous mouseDXY values are returned.  
         
         The mouseXY values are not particularly useful in this mode.

         This setting is commonly used by first-person games.

         When the app loses focus the mouse is automatically freed
         and recaptured when focus returns.  debugBreak also frees
         the mouse.
        */
        public void setPureDeltaMouse(bool m)
        {
            if (pureDeltaMouseMode != m)
            {
                pureDeltaMouseMode = m;

                if (pureDeltaMouseMode)
                {
                    grabMouse();
                }
                else
                {
                    releaseMouse();
                }
            }
        }

        public bool pureDeltaMouse()
        {
            return pureDeltaMouseMode;
        }

        /** Called from setPureDeltaMouse */
        private void grabMouse()
        {
            byte dummy;
            // Save the old mouse position for when we deactivate
            if (_window != null)
            {
                _window.getRelativeMouseState(out guiMouse, out dummy);
            }
            else
            {
                guiMouse = mouse;
            }

            mouse = windowCenter;
            if (_window != null)
            {
                _window.setRelativeMousePosition(mouse);
            }
            deltaMouse = new Vector2(0, 0);

            if (_window != null)
            {
                _window.incMouseHideCount();
            }

#if !DEBUG
            // In debug mode, don't grab the cursor because
            // it is annoying when you hit a breakpoint and
            // can't move the mouse.
            if (_window != null)
            {
                _window.incInputCaptureCount();
            }
#endif
        }

        /** Called from setPureDeltaMouse */
        private void releaseMouse()
        {
#if !DEBUG
            // In debug mode, don't grab the cursor because
            // it is annoying when you hit a breakpoint and
            // cannot move the mouse.
            if (_window != null)
            {
                _window.decInputCaptureCount();
            }
#endif

            // Restore the old mouse position
            setMouseXY(guiMouse.x, guiMouse.y);

            if (_window != null)
            {
                _window.decMouseHideCount();
            }
        }

        /**
         True if this app is in the "foreground" 
            (shortcut for window()->hasFocus()).
            @deprecated
        */
        public bool appHasFocus()
        {
            return _window == null ? true : _window.hasFocus();
        }

        public int getNumJoysticks()
        {
            return _window == null ? 0 : _window.numJoysticks();
        }

        public int numJoysticks()
        {
            return getNumJoysticks();
        }

        public string joystickName(int sticknum)
        {
            if (_window == null)
            {
                throw new NotSupportedException("No window is available for joystick sampling.");
            }
            return _window.joystickName((uint)sticknum);
        }

        public Vector2 joystickPosition(int sticknum)
        {
            if (_window == null)
            {
                throw new NotSupportedException("No window is available for joystick sampling.");
            }
            if (sticknum != 0)
            {
                throw new ArgumentOutOfRangeException("sticknum");
            }
            throw new NotSupportedException("The backend window contract does not expose joystick devices.");
        }

        public Array<bool> joystickButtons(int sticknum)
        {
            if (_window == null)
            {
                throw new NotSupportedException("No window is available for joystick sampling.");
            }
            if (sticknum != 0)
            {
                throw new ArgumentOutOfRangeException("sticknum");
            }
            return new Array<bool>();
        }

        public float joystickAxis(int sticknum, int axis)
        {
            if (_window == null)
            {
                throw new NotSupportedException("No window is available for joystick sampling.");
            }
            if (sticknum != 0)
            {
                throw new ArgumentOutOfRangeException("sticknum");
            }
            if (axis < 0 || axis > 1)
            {
                throw new ArgumentOutOfRangeException("axis");
            }
            throw new NotSupportedException("The backend window contract does not expose joystick devices.");
        }

        /**
         Turns a UserInput key code into a human readable string
         describing the key.
        */
        public static string keyCodeToString(KeyCode i)
        {
            int code = (int)i;

            if (code == (int)CustomKeyCode.SDL_LEFT_MOUSE_KEY)
            {
                return "L Mouse";
            }
            else if (code == (int)CustomKeyCode.SDL_MIDDLE_MOUSE_KEY)
            {
                return "Mid Mouse";
            }
            else if (code == (int)CustomKeyCode.SDL_RIGHT_MOUSE_KEY)
            {
                return "R Mouse";
            }
            else if (code == (int)CustomKeyCode.SDL_MOUSE_WHEEL_UP_KEY)
            {
                return "MWheel Up";
            }
            else if (code == (int)CustomKeyCode.SDL_MOUSE_WHEEL_DOWN_KEY)
            {
                return "MWheel Dn";
            }

            switch (i)
            {
                case KeyCode.BACKSPACE: return "Bksp";
                case KeyCode.TAB: return "Tab";
                case KeyCode.CLEAR: return "Clear";
                case KeyCode.RETURN: return "Enter";
                case KeyCode.PAUSE: return "Pause";
                case KeyCode.ESCAPE: return "Esc";
                case KeyCode.SPACE: return "Spc";
                case KeyCode.DELETE: return "Del";
                case KeyCode.KP_PERIOD: return "Keypad .";
                case KeyCode.KP_DIVIDE: return "Keypad \\";
                case KeyCode.KP_MULTIPLY: return "Keypad *";
                case KeyCode.KP_MINUS: return "Keypad -";
                case KeyCode.KP_PLUS: return "Keypad +";
                case KeyCode.KP_ENTER: return "Keypad Enter";
                case KeyCode.KP_EQUALS: return "Keypad =";
                case KeyCode.UP: return "Up";
                case KeyCode.DOWN: return "Down";
                case KeyCode.RIGHT: return "Right";
                case KeyCode.LEFT: return "Left";
                case KeyCode.INSERT: return "Ins";
                case KeyCode.HOME: return "Home";
                case KeyCode.END: return "End";
                case KeyCode.PAGEUP: return "Pg Up";
                case KeyCode.PAGEDOWN: return "Pg Dn";
                case KeyCode.NUMLOCK: return "Num Lock";
                case KeyCode.CAPSLOCK: return "Caps Lock";
                case KeyCode.SCROLLOCK: return "Scroll Lock";
                case KeyCode.RSHIFT: return "R Shft";
                case KeyCode.LSHIFT: return "L Shft";
                case KeyCode.RCTRL: return "R Ctrl";
                case KeyCode.LCTRL: return "L Ctrl";
                case KeyCode.RALT: return "R Alt";
                case KeyCode.LALT: return "L Alt";
                case KeyCode.RMETA: return "R Meta";
                case KeyCode.LMETA: return "L Meta";
                case KeyCode.LSUPER: return "L Win";
                case KeyCode.RSUPER: return "R Win";
                case KeyCode.MODE: return "Alt Gr";
                case KeyCode.HELP: return "Help";
                case KeyCode.PRINT: return "Print";
                case KeyCode.SYSREQ: return "Sys Req";
                case KeyCode.BREAK: return "Break";
            }

            if (code >= (int)KeyCode.ZERO && code <= (int)KeyCode.NINE)
            {
                return "" + (char)code;
            }

            if (code >= (int)KeyCode.A && code <= (int)KeyCode.Z)
            {
                return "" + char.ToUpperInvariant((char)code);
            }

            if (code >= (int)KeyCode.EXCLAIM && code <= (int)KeyCode.SLASH)
            {
                return "" + (char)code;
            }

            if (code >= (int)KeyCode.COLON && code <= (int)KeyCode.AT)
            {
                return "" + (char)code;
            }

            if (code >= (int)KeyCode.LEFTBRACKET && code <= (int)KeyCode.BACKQUOTE)
            {
                return "" + (char)code;
            }

            if (code >= (int)KeyCode.KP0 && code <= (int)KeyCode.KP9)
            {
                return "Keypad " + (char)('0' + (code - (int)KeyCode.KP0));
            }

            if (code >= (int)KeyCode.F1 && code <= (int)KeyCode.F15)
            {
                return "F" + (char)('1' + (code - (int)KeyCode.F1));
            }

            return "";
        }

        /**
         Inverse of keyCodeToString
        */
        public static KeyCode stringToKeyCode(string s)
        {
            string t = s.Trim().ToLowerInvariant();

            for (int i = 0; i < (int)CustomKeyCode.SDL_CUSTOM_LAST; ++i)
            {
                string name = keyCodeToString((KeyCode)i);
                if ((name.Length == t.Length) &&
                    (name.ToLowerInvariant() == t))
                {
                    return (KeyCode)i;
                }
            }

            return KeyCode.UNKNOWN;
        }
    }
}
