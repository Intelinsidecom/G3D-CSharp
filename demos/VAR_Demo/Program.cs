/**
  @file VAR_Demo/Program.cs

  Port of main() in VAR_Demo/main.cpp.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace VAR_Demo
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            string backend = "opengl";
            foreach (string arg in args)
            {
                if (arg != null && arg.StartsWith("--backend=", StringComparison.OrdinalIgnoreCase))
                {
                    backend = arg.Substring("--backend=".Length);
                }
            }

            GAppSettings settings = new GAppSettings();
            settings.useNetwork = false;
            settings.debugFontName = "dominant.fnt";
            settings.window.depthBits = 16;
            settings.window.stencilBits = 0;
            settings.window.alphaBits = 0;
            settings.window.fsaaSamples = 1;
            settings.window.rgbBits = 0;

            App app = new App(settings, backend);
            app.run();
        }
    }
}
