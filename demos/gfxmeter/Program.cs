/**
  @file gfxmeter/Program.cs

  Port of main() in gfxmeter/main.cpp.
*/

using System;
using System.Windows.Forms;
using G3D;

namespace gfxmeter
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

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
            settings.window.fsaaSamples = 4;
            settings.dataDir = "./";
            settings.window.defaultIconFilename = "g3d.ico";

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeDir = System.IO.Path.GetDirectoryName(exePath);
            // The original checks for g3d.ico beside its data files and chdirs
            // into install/ when it is missing; the port runs from the demo
            // output directory, where the data-files folder holds it.
            string icon = System.IO.Path.Combine(exeDir, "data-files", settings.window.defaultIconFilename);

            if (!System.IO.File.Exists(icon))
            {
                // We are probably running in the debugger and launched from the wrong directory
                System.IO.Directory.SetCurrentDirectory(exeDir);
            }

            if (!System.IO.File.Exists(icon))
            {
                G3D.Error.error("Error",
                    "gfxmeter was run from the wrong directory and can't find its data files.",
                    true);
                return;
            }

            App app = new App(settings, backend);
            app.run();
        }
    }
}
