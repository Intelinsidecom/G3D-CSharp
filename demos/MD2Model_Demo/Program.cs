/**
  @file MD2Model_Demo/Program.cs

  Port of main() in MD2Model_Demo/main.cpp.
 */

using System;
using System.Windows.Forms;
using G3D;

namespace MD2Model_Demo
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

            App app = new App(settings, backend);
            app.run();
        }
    }
}
