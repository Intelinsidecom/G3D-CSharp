/**
  @file GLSL_Demo/Program.cs

  Port of main() in GLSL_Demo/main.cpp.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace GLSL_Demo
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
            settings.window.fsaaSamples = 4;
            settings.window.width = 800;
            settings.window.height = 600;
            settings.useNetwork = false;

            App app = new App(settings, backend);
            app.run();
        }
    }
}
