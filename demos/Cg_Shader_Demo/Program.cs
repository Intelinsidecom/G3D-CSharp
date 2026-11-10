/**
  @file Cg_Shader_Demo/Program.cs

  Port of main() in Cg_Shader_Demo/main.cpp.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace Cg_Shader_Demo
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
            settings.window.width = 800;
            settings.window.height = 600;

            App app = new App(settings, backend);
            app.run();
        }
    }
}
