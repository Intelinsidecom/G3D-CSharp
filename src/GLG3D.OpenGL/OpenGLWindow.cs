using System;
using OpenTK;
using OpenTK.Graphics;
using global::System.Windows.Forms;

namespace G3D.OpenGL
{
    public sealed class OpenGLWindow : IGraphicsWindow
    {
        private readonly GLControl control;

        public OpenGLWindow()
        {
            control = new GLControl(new GraphicsMode(32, 24, 8, 0));
            control.Dock = DockStyle.Fill;
            control.BackColor = global::System.Drawing.Color.Black;
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
            control.MakeCurrent();
        }

        public void Present()
        {
            control.SwapBuffers();
        }

        public void Resize(int width, int height)
        {
            control.MakeCurrent();
        }

        public void Dispose()
        {
            control.Dispose();
        }

        // For describeSystem
        public GWindow.Settings getSettings()
        {
            var settings = new GWindow.Settings();
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
            settings.refreshRate = 85;
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
