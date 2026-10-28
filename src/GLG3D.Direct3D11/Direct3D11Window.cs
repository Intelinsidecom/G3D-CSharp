using System;
using System.Windows.Forms;

namespace G3D.Direct3D11
{
    public sealed class Direct3D11Window : IGraphicsWindow
    {
        private readonly D3D11Control control;

        public Direct3D11Window()
        {
            control = new D3D11Control();
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
