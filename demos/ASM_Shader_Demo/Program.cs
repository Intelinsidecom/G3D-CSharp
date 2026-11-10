/**
  @file ASM_Shader_Demo/Program.cs

  Port of main() in ASM_Shader_Demo/main.cpp.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace ASM_Shader_Demo
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

            App app = new App(settings, backend);
            app.run();
        }
    }
}
