/**
  @file Starter/Program.cs

  Port of main() in demos/main.cpp.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace Starter
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

            App app = new App(settings, backend);
            app.run();
        }
    }
}
