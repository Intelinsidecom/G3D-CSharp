/**
  @file GWindow.h

  @maintainer Morgan McGuire, morgan@graphics3d.com
  @created 2005-02-10
  @edited  2006-03-04
 */

using System;
using System.Collections.Generic;
using G3D;

namespace G3D
{
    public sealed class GWindow : IGraphicsWindow
    {
        public class Settings
        {
            /** Width of the client area in pixels. */
            public int width = 800;

            public int height = 600;

            /** Initial x position of the client area in screen space(can be overridden by fullScreen and center) */
            public int x = 0;

            /** Initial y position of the client area in screen space(can be overridden by fullScreen and center) */
            public int y = 0;

            /** If true, overrides the x and y settings */
            public bool center = true;

            public bool centered = true;

            /** The number of bits in <B>each</B> color channel of the frame buffer.
               5, <B>8</B>.*/
            public int rgbBits = 8;

            /* The number of bits in the alpha channel of the frame buffer. <B>0</B>, 1, 8
             Used in Win32Window to help determine resolution color depth (Added to rgbBits). */
            public int alphaBits = 0;

            /** <B>0 (autodetect the best for this card)</B>, 16, <B>24</B>, 32 */
            public int depthBits = 24;

            /** <B>8</B> */
            public int stencilBits = 8;

            /** Number of samples per pixel for anti-aliasing purposes.  <B>1</B> (none), 4, 8 */
            public int fsaaSamples = 1;

            /** Will you accept a software rendering pipeline?  Used to determine if a
             hardware only pixel format is required. */
            public bool hardware = true;

            /** Applications that fill the screen may also want to set framed = false. */
            public bool fullScreen = false;

            /** Should buffer flips be un-hitched from refresh rate?  <B>true</B>, false.  True
                generally gives higher frame rates.*/
            public bool asychronous = true;

            /** Allocate a stereo display context. true, <B>false</B> */
            public bool stereo = false;

            /** Specify the value at which lighting saturates
                before it is applied to surfaces.  1.0 is the default OpenGL value,
                higher numbers increase the quality of bright lighting at the expense of
                color depth.Default is 1.0.  Set
                to 2.0 to make a Color3::white() light 50% of the maximum brightness. */
            public double lightSaturation = 1.0;

            /** In cycles/sec */
            public int refreshRate = 85;

            /**
             If true, you should set up your event loop as described in the
             docs for RenderDevice::resize.
             */
            public bool resizable = false;

            /**
             When true, a window frame and title bar are present.
             */
            public bool framed = true;

            /**
             When true, the window begins visible.  <B>true</B>, false.
             */
            public bool visible = true;

            /**
             Default icon that the GWindow implementation tries to set initially.
            */
            public string defaultIconFilename = "nodefault";
        }

        private sealed class LoopBody
        {
            public Action<object> func;
            public GApplet applet;
            public object arg;
            public bool isGApplet;

            public LoopBody()
            {
                func = null;
                applet = null;
                arg = null;
                isGApplet = false;
            }

            public LoopBody(GApplet a)
            {
                func = null;
                applet = a;
                arg = null;
                isGApplet = true;
            }

            public LoopBody(Action<object> f, object a)
            {
                func = f;
                applet = null;
                arg = a;
                isGApplet = false;
            }
        }

        private readonly IGraphicsWindow window;
        private readonly Array<LoopBody> loopBodyStack = new Array<LoopBody>();
        private readonly global::System.Collections.Generic.Queue<GEvent> eventQueue = new global::System.Collections.Generic.Queue<GEvent>();
        private readonly HashSet<UserInput.KeyCode> pressedKeys = new HashSet<UserInput.KeyCode>();
        private RenderDevice _renderDevice;
        private int _mouseHideCount;
        private int _inputCaptureCount;
        private int inhibitAppLoopBodyCount;
        private bool _mouseVisible = true;
        private bool inputCaptured;
        private bool closed;
        private Vector2 mousePosition = new Vector2(0, 0);
        private byte mouseButtons;
        private static GWindow currentWindow;

        public GWindow(IGraphicsWindow w)
        {
            if (w == null)
            {
                throw new ArgumentNullException("w");
            }
            window = w;
            window.InputEvent += postEvent;
        }

        /**
          Forwarded to the wrapped platform window so that subscribers see the
          same input events.  The events are additionally queued (through
          postEvent, subscribed above) so that pollEvent can drain them.
        */
        public event Action<GEvent> InputEvent
        {
            add { window.InputEvent += value; }
            remove { window.InputEvent -= value; }
        }

        public IntPtr NativeHandle
        {
            get { return window.NativeHandle; }
        }

        public int Width
        {
            get { return window.Width; }
        }

        public int Height
        {
            get { return window.Height; }
        }

        public void MakeCurrent()
        {
            makeCurrent();
        }

        public void Present()
        {
            window.Present();
        }

        public void Resize(int width, int height)
        {
            window.Resize(width, height);
        }

        public void Dispose()
        {
            if (!closed)
            {
                closed = true;
                if (currentWindow == this)
                {
                    currentWindow = null;
                }
                window.InputEvent -= postEvent;
                window.Dispose();
            }
        }

        /** Return the <I>actual</I> properties of this window (as opposed to
            the desired settings from which it was initialized) */
        public Settings getSettings()
        {
            return window.getSettings();
        }

        public string getAPIName()
        {
            return window.getAPIName();
        }

        public string getAPIVersion()
        {
            return window.getAPIVersion();
        }

        public IGraphicsWindow Window
        {
            get { return window; }
        }

        public bool hasFocus()
        {
            return !closed && window.hasFocus();
        }

        /** Measured in pixels, this is the client area of the window. */
        public int width()
        {
            return window.Width;
        }

        public int height()
        {
            return window.Height;
        }

        /** Shape of the client area of the window in screen coordinates. */
        public Rect2D dimensions()
        {
            Settings actual = getSettings();
            return new Rect2D(actual.x, actual.y, actual.width, actual.height);
        }

        /** Fails silently if unable to change the dimensions. */
        public void setDimensions(Rect2D dims)
        {
            window.Resize((int)dims.width(), (int)dims.height());
        }

        /** Fails silently if unable to change the position. */
        public void setPosition(int x, int y)
        {
            throw new NotSupportedException("The backend window contract does not expose window positioning.");
        }

        public void setSize(int width, int height)
        {
            window.Resize(width, height);
        }

        public void fullScreen(bool enabled)
        {
            throw new NotSupportedException("The backend window contract does not expose full-screen mode.");
        }

        public void gamma(float gamma)
        {
            throw new NotSupportedException("The backend window contract does not expose gamma correction.");
        }

        /** gammaRamp.length() = 256 */
        public void setGammaRamp(Array<ushort> gammaRamp)
        {
            throw new NotSupportedException("The backend window contract does not expose gamma correction.");
        }

        protected void loadExtensions()
        {
            GLCaps.init();
        }

        public bool requiresMainLoop()
        {
            return false;
        }

        /** Set the icon (if supported).  Fails silently if not supported
            or the window has no frame. */
        public void setIcon(GImage image)
        {
            throw new NotSupportedException("The backend window contract does not expose window icons.");
        }

        public void setIcon(string imageFilename)
        {
            throw new NotSupportedException("The backend window contract does not expose window icons.");
        }

        /** Swap the OpenGL front and back buffers.  Called by RenderDevice::endFrame. */
        public void swapGLBuffers()
        {
            window.Present();
        }

        public void swapBuffers()
        {
            swapGLBuffers();
        }

        /** Notifies the window that it has been resized. */
        public void notifyResize(int w, int h)
        {
            window.Resize(w, h);
        }

        public void setRelativeMousePosition(double x, double y)
        {
            setRelativeMousePosition(new Vector2((float)x, (float)y));
        }

        /** Relative to the window origin */
        public void setRelativeMousePosition(Vector2 p)
        {
            mousePosition = p;
        }

        /** Checks to see if any events are waiting. */
        public bool pollEvent(out GEvent e)
        {
            if (eventQueue.Count == 0)
            {
                e = null;
                return false;
            }

            e = eventQueue.Dequeue();
            handleEvent(e);
            return true;
        }

        public Array<GEvent> pollEvents()
        {
            Array<GEvent> events = new Array<GEvent>();
            GEvent e;
            while (pollEvent(out e))
            {
                events.append(e);
            }
            return events;
        }

        public void postEvent(GEvent e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            eventQueue.Enqueue(e);
        }

        public bool handleEvent(GEvent e)
        {
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }

            switch (e.type)
            {
                case GEventType.KEY_DOWN:
                    pressedKeys.Add((UserInput.KeyCode)e.key);
                    break;

                case GEventType.KEY_UP:
                    pressedKeys.Remove((UserInput.KeyCode)e.key);
                    break;

                case GEventType.MOUSE_DOWN:
                    if (e.mouseButton >= 1 && e.mouseButton <= 8)
                    {
                        mouseButtons |= (byte)(1 << (e.mouseButton - 1));
                    }
                    break;

                case GEventType.MOUSE_UP:
                    if (e.mouseButton >= 1 && e.mouseButton <= 8)
                    {
                        mouseButtons &= (byte)~(1 << (e.mouseButton - 1));
                    }
                    break;

                case GEventType.MOUSE_MOVE:
                    if (e.mouseX != 0 || e.mouseY != 0)
                    {
                        mousePosition = new Vector2(e.mouseX, e.mouseY);
                    }
                    else
                    {
                        mousePosition = new Vector2(mousePosition.x + e.mouseDeltaX, mousePosition.y + e.mouseDeltaY);
                    }
                    break;

                case GEventType.RESIZE:
                    notifyResize(e.width, e.height);
                    break;

                case GEventType.QUIT:
                    closed = true;
                    break;
            }

            return true;
        }

        public int update()
        {
            int count = 0;
            GEvent e;
            while (pollEvent(out e))
            {
                count++;
            }
            pressedKeys.Clear();
            return count;
        }

        public void setCaption(string caption)
        {
            throw new NotSupportedException("The backend window contract does not expose captions.");
        }

        public string caption()
        {
            return "";
        }

        /** Returns 0 if there are no joysticks available */
        public int numJoysticks()
        {
            return 0;
        }

        /** The name by which the OS refers to this joystick (e.g. "Gravis Gamepad") */
        public string joystickName(uint sticknum)
        {
            throw new NotSupportedException("The backend window contract does not expose joystick devices.");
        }

        public Vector2 getRelativeMouseState(out byte buttons)
        {
            buttons = mouseButtons;
            return mousePosition;
        }

        public void getRelativeMouseState(out Vector2 position, out byte mouseButtons)
        {
            position = mousePosition;
            mouseButtons = this.mouseButtons;
        }

        public void getRelativeMouseState(out int x, out int y, out byte mouseButtons)
        {
            x = (int)mousePosition.x;
            y = (int)mousePosition.y;
            mouseButtons = this.mouseButtons;
        }

        public void getRelativeMouseState(out double x, out double y, out byte mouseButtons)
        {
            x = mousePosition.x;
            y = mousePosition.y;
            mouseButtons = this.mouseButtons;
        }

        /** Returns the state of the given joystick. */
        public void getJoystickState(uint stickNum, Array<float> axis, Array<bool> button)
        {
            throw new NotSupportedException("The backend window contract does not expose joystick devices.");
        }

        /** Capture the keyboard and mouse focus, locking the mouse to the client area of this window. */
        public void setInputCapture(bool c)
        {
            inputCaptured = c;
        }

        public void setInputCaptureCount(int c)
        {
            if ((_inputCaptureCount > 0) && (c <= 0))
            {
                // Release mouse
                setInputCapture(false);
            } else if ((_inputCaptureCount <= 0) && (c > 0))
            {
                // Capture mouse
                setInputCapture(true);
            }

            // Set this variable after the setInputCapture statements since
            // they corrupt its value.
            _inputCaptureCount = c;
        }

        /** If the count is 1 or greater, then the keyboard and mouse focus is captured. */
        public int inputCaptureCount()
        {
            return _inputCaptureCount;
        }

        public int getInputCaptureCount()
        {
            return inputCaptureCount();
        }

        public void incInputCaptureCount()
        {
            setInputCaptureCount(inputCaptureCount() + 1);
        }

        public void decInputCaptureCount()
        {
            setInputCaptureCount(inputCaptureCount() - 1);
        }

        /** Returns the current state of input capture. */
        public bool inputCapture()
        {
            return inputCaptured;
        }

        public void setMouseVisible(bool b)
        {
            _mouseVisible = b;
        }

        public bool mouseVisible()
        {
            return _mouseVisible;
        }

        public void setMouseHideCount(int c)
        {
            if ((_mouseHideCount > 0) && (c <= 0))
            {
                // Release mouse
                setMouseVisible(true);
            } else if ((_mouseHideCount <= 0) && (c > 0))
            {
                // Capture mouse
                setMouseVisible(false);
            }

            // Set this variable after the setMouseVisible statements since
            // they corrupt its value.
            _mouseHideCount = c;
        }

        /** If the count is 1 or greater, then the keyboard and mouse focus is captured. */
        public int mouseHideCount()
        {
            return _mouseHideCount;
        }

        public int getMouseHideCount()
        {
            return mouseHideCount();
        }

        public void incMouseHideCount()
        {
            setMouseHideCount(mouseHideCount() + 1);
        }

        public void decMouseHideCount()
        {
            setMouseHideCount(mouseHideCount() - 1);
        }

        public void hideCursor()
        {
            incMouseHideCount();
        }

        public void showCursor()
        {
            decMouseHideCount();
        }

        protected bool notDone()
        {
            return loopBodyStack.size() > 0;
        }

        /** Subclasses should call from their idle function. */
        protected void executeLoopBody()
        {
            if (notDone() && inhibitAppLoopBodyCount == 0)
            {
                LoopBody body = loopBodyStack.last();
                if (body.isGApplet)
                {
                    body.applet.oneFrame();
                } else
                {
                    body.func(body.arg);
                }
            }
        }

        /** Invokes GApplet::beginRun after the applet is on the stack. */
        public void pushLoopBody(Action<object> body, object arg)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            loopBodyStack.push(new LoopBody(body, arg));
        }

        public void pushLoopBody(Action body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            pushLoopBody(delegate(object ignored) { body(); }, null);
        }

        public void pushLoopBody(GApplet applet)
        {
            if (applet == null)
            {
                throw new ArgumentNullException("applet");
            }
            loopBodyStack.push(new LoopBody(applet));
            applet.beginRun();
        }

        public void pushApplet(GApplet applet)
        {
            pushLoopBody(applet);
        }

        public GApplet applet()
        {
            if (loopBodyStack.size() == 0)
            {
                return null;
            }
            return loopBodyStack.last().applet;
        }

        /** Pops a loop body off the stack.  If the loop body was a GApplet,
            invokes GApplet::endRun on it.*/
        public void popLoopBody()
        {
            if (loopBodyStack.size() > 0)
            {
                if (loopBodyStack.last().isGApplet)
                {
                    loopBodyStack.last().applet.endRun();
                    loopBodyStack.pop();
                }
            }
        }

        public void popApplet()
        {
            popLoopBody();
        }

        public void oneFrame()
        {
            executeLoopBody();
        }

        public void inhibitAppLoopBody()
        {
            inhibitAppLoopBodyCount++;
        }

        public void restoreAppLoopBody()
        {
            if (inhibitAppLoopBodyCount > 0)
            {
                inhibitAppLoopBodyCount--;
            }
        }

        public bool appLoopBodyInhibited()
        {
            return inhibitAppLoopBodyCount > 0;
        }

        /** Executes an event loop, invoking callback repeatedly. */
        public void runMainLoop()
        {
            while (notDone())
            {
                executeLoopBody();
            }
        }

        public RenderDevice renderDevice()
        {
            return _renderDevice;
        }

        internal void setRenderDevice(RenderDevice device)
        {
            _renderDevice = device;
        }

        public static GWindow current()
        {
            return currentWindow;
        }

        /** Makes the OpenGL context of this window current. */
        public void makeCurrent()
        {
            if (currentWindow != this)
            {
                reallyMakeCurrent();
                currentWindow = this;
            }
        }

        protected void reallyMakeCurrent()
        {
            window.MakeCurrent();
        }

        public bool keyPressed(UserInput.KeyCode code)
        {
            return pressedKeys.Contains(code);
        }

        public Vector2 mouseXY()
        {
            return mousePosition;
        }

        public bool exists()
        {
            return !closed;
        }

        public void close()
        {
            Dispose();
        }
    }

    /** @deprecated For backwards compatibility to 6.08 */
    public class GWindowSettings : GWindow.Settings
    {
    }

    /** @deprecated For backwards compatibility to 6.00 */
    public class RenderDeviceSettings : GWindowSettings
    {
    }
}
