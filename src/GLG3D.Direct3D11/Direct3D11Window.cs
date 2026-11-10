using System;
using System.Windows.Forms;

namespace G3D.Direct3D11
{
    public sealed class Direct3D11Window : IGraphicsWindow
    {
        private readonly D3D11Control control;

        public event Action<GEvent> InputEvent;

        public Direct3D11Window()
        {
            control = new D3D11Control();
            control.KeyDown += onKeyDown;
            control.KeyUp += onKeyUp;
            control.MouseDown += onMouseDown;
            control.MouseUp += onMouseUp;
            control.MouseMove += onMouseMove;
            control.MouseWheel += onMouseWheel;
        }

        private void raiseInputEvent(GEvent e)
        {
            Action<GEvent> handler = InputEvent;
            if (handler != null)
            {
                handler(e);
            }
        }

        /**
          WinForms reports virtual-key codes; UserInput expects the SDL 1.2
          codes the original used, which match ASCII for the printable keys.
        */
        private static int translateKey(Keys key, bool shiftDown)
        {
            Keys code = key & Keys.KeyCode;
            if (code >= Keys.A && code <= Keys.Z)
            {
                int letter = (int)code;
                return shiftDown ? letter : letter + 32;
            }
            return (int)code;
        }

        private static int translateMouseButton(MouseButtons button)
        {
            switch (button)
            {
                case MouseButtons.Left: return 1;
                case MouseButtons.Middle: return 2;
                case MouseButtons.Right: return 3;
                case MouseButtons.XButton1: return 4;
                case MouseButtons.XButton2: return 5;
                default: return 0;
            }
        }

        private void onKeyDown(object sender, KeyEventArgs e)
        {
            GEvent evt = new GEvent(GEventType.KEY_DOWN);
            evt.key = translateKey(e.KeyCode, e.Shift);
            raiseInputEvent(evt);
        }

        private void onKeyUp(object sender, KeyEventArgs e)
        {
            GEvent evt = new GEvent(GEventType.KEY_UP);
            evt.key = translateKey(e.KeyCode, e.Shift);
            raiseInputEvent(evt);
        }

        private void onMouseDown(object sender, MouseEventArgs e)
        {
            GEvent evt = new GEvent(GEventType.MOUSE_DOWN);
            evt.mouseButton = translateMouseButton(e.Button);
            evt.mouseX = e.X;
            evt.mouseY = e.Y;
            raiseInputEvent(evt);
        }

        private void onMouseUp(object sender, MouseEventArgs e)
        {
            GEvent evt = new GEvent(GEventType.MOUSE_UP);
            evt.mouseButton = translateMouseButton(e.Button);
            evt.mouseX = e.X;
            evt.mouseY = e.Y;
            raiseInputEvent(evt);
        }

        private void onMouseMove(object sender, MouseEventArgs e)
        {
            GEvent evt = new GEvent(GEventType.MOUSE_MOVE);
            evt.mouseX = e.X;
            evt.mouseY = e.Y;
            raiseInputEvent(evt);
        }

        private void onMouseWheel(object sender, MouseEventArgs e)
        {
            GEvent evt = new GEvent(GEventType.MOUSE_WHEEL);
            evt.wheelDelta = e.Delta;
            raiseInputEvent(evt);
        }

        public Control Control
        {
            get
            {
                return control;
            }
        }

        public IntPtr NativeHandle
        {
            get
            {
                return control.Handle;
            }
        }

        public int Width
        {
            get
            {
                return control.ClientSize.Width;
            }
        }

        public int Height
        {
            get
            {
                return control.ClientSize.Height;
            }
        }

        public void MakeCurrent()
        {
        }

        public void Present()
        {
        }

        public void Resize(int width, int height)
        {
        }

        public void Dispose()
        {
            control.Dispose();
        }

        // For describeSystem
        public GWindow.Settings getSettings()
        {
            GWindow.Settings settings = new GWindow.Settings();
            settings.width = Width;
            settings.height = Height;
            settings.rgbBits = 8;
            settings.alphaBits = 0;
            settings.depthBits = 24;
            settings.stencilBits = 8;
            settings.fsaaSamples = 0;
            settings.stereo = false;
            settings.fullScreen = false;
            settings.centered = true;
            settings.framed = true;
            settings.visible = true;
            settings.resizable = false;
            settings.x = 220;
            settings.y = 220;
            settings.refreshRate = 60;
            settings.asychronous = true;
            return settings;
        }

        public string getAPIName()
        {
            return "Win32";
        }

        public string getAPIVersion()
        {
            return "1.1";
        }

        public bool hasFocus()
        {
            return true;
        }
    }
}
